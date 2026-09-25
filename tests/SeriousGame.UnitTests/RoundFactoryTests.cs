using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;
using Server.Options;

namespace SeriousGame.UnitTests;

public class RoundFactoryTests
{
	private static readonly Skill Html = new() { Id = 1, Name = "HTML" };
	private static readonly Skill Sql = new() { Id = 17, Name = "SQL" };

	private static readonly IReadOnlyList<TenderSeed> Seeds = Enumerable.Range(1, 20)
		.Select(i => new TenderSeed { Name = $"Projet {i}" })
		.ToList();

	private static GameOptions MakeOptions(int tendersPerPlayer = 2) => new()
	{
		TenderNumberPerPlayer = tendersPerPlayer,
		SalaryReferencePerConsultant = 5_000,
		TenderMarginPercent = 150
	};

	/// <summary>
	/// Partie prête à jouer : un joueur et une entreprise par index, chaque consultant portant une
	/// compétence au niveau Intermediate (deux LevelUp depuis Zero).
	/// </summary>
	private static Game MakeGame(int playerCount = 2, int consultantsPerCompany = 3, int roundsNumber = 5)
	{
		var owner = MakePlayer(1);

		var game = new Game
		{
			Name = "Partie de test",
			Owner = owner,
			RoundsNumber = roundsNumber,
			IsInProgress = true
		};

		for (var i = 1; i <= playerCount; i++)
		{
			var player = i == 1 ? owner : MakePlayer(i);
			game.Players.Add(player);

			var company = new Company
			{
				Name = $"Entreprise {i}",
				PlayerOwner = player,
				InitialTreasury = 1_000_000
			};

			for (var j = 0; j < consultantsPerCompany; j++)
			{
				company.Staffs.Add(MakeConsultant(company, j % 2 == 0 ? Html : Sql));
			}

			game.Companies.Add(company);
		}

		return game;
	}

	private static Player MakePlayer(int index) =>
		new() { Id = $"p{index}", Nickname = $"Joueur {index}", ConnectionId = $"c{index}" };

	private static Consultant MakeConsultant(Company company, Skill skill, Level level = Level.Intermediate)
	{
		var consultant = new Consultant
		{
			Firstname = "Test",
			Lastname = $"Consultant {company.Staffs.Count + 1}",
			Company = company
		};

		var consultantSkill = new ConsultantSkill { Skill = skill };

		// Level est en private set : on ne l'atteint qu'en montant cran par cran.
		while (consultantSkill.Level < level)
		{
			consultantSkill.LevelUp();
		}

		consultant.Skills.Add(consultantSkill);

		return consultant;
	}

	[Fact]
	public void Create_DrawsOneTenderPerPlayerSlot()
	{
		var game = MakeGame(playerCount: 2);

		var round = RoundFactory.Create(game, Seeds, MakeOptions(tendersPerPlayer: 2), new Random(42));

		Assert.Equal(4, round.Tenders.Count);
		Assert.Equal(1, round.Order);
		Assert.Contains(round, game.Rounds);
	}

	[Fact]
	public void Create_NeverAsksForMoreConsultantsThanAvailable()
	{
		// Une seule entreprise, un seul consultant libre : aucun appel d'offres ne peut en exiger deux.
		var game = MakeGame(playerCount: 1, consultantsPerCompany: 1);

		var round = RoundFactory.Create(game, Seeds, MakeOptions(), new Random(42));

		Assert.NotEmpty(round.Tenders);
		Assert.All(round.Tenders, tender => Assert.Equal(1, tender.RequiredConsultants));
	}

	[Fact]
	public void Create_OnlyRequiresSkillsSomeoneActuallyHas()
	{
		var game = MakeGame();

		var round = RoundFactory.Create(game, Seeds, MakeOptions(), new Random(42));

		Assert.All(round.Tenders, tender =>
			Assert.All(tender.RequiredSkills, required =>
				Assert.Contains(game.Companies.SelectMany(c => c.Staffs).SelectMany(c => c.Skills),
					owned => owned.Skill == required.Skill && owned.Level >= required.Level)));
	}

	[Fact]
	public void Create_NeverExceedsTheRoundsLeftInTheGame()
	{
		// Partie de 2 tours : au tour 2, il ne reste qu'un tour à jouer.
		var game = MakeGame(roundsNumber: 2);
		var options = MakeOptions();
		var random = new Random(42);

		RoundFactory.Create(game, Seeds, options, random);
		var lastRound = RoundFactory.Create(game, Seeds, options, random);

		Assert.Equal(2, lastRound.Order);
		Assert.All(lastRound.Tenders, tender => Assert.Equal(1, tender.RoundsNumber));
	}

	[Fact]
	public void Create_PricesTendersOnManpowerAndDuration()
	{
		var game = MakeGame();
		var options = MakeOptions();

		var round = RoundFactory.Create(game, Seeds, options, new Random(42));

		Assert.All(round.Tenders, tender =>
		{
			var expected = tender.RequiredConsultants
						   * tender.RoundsNumber
						   * options.SalaryReferencePerConsultant
						   * options.TenderMarginPercent / 100;

			Assert.Equal(expected, tender.Budget);
		});
	}

	[Fact]
	public void Create_DoesNotReuseAProjectNameWithinAGame()
	{
		var game = MakeGame();
		var options = MakeOptions();
		var random = new Random(42);

		RoundFactory.Create(game, Seeds, options, random);
		RoundFactory.Create(game, Seeds, options, random);

		var names = game.Rounds.SelectMany(r => r.Tenders).Select(t => t.Name).ToList();

		Assert.Equal(names.Count, names.Distinct().Count());
	}

	[Fact]
	public void Create_ProducesNothingWhenEveryConsultantIsBusy()
	{
		var game = MakeGame(playerCount: 1, consultantsPerCompany: 1);
		var company = game.Companies.Single();
		var consultant = company.Staffs.Single();

		company.Contracts.Add(new Contract
		{
			Company = company,
			Tender = new Tender
			{
				Name = "Contrat en cours",
				Budget = 10_000,
				RoundsNumber = 2,
				RequiredConsultants = 1
			},
			AssignedConsultants = { consultant },
			RemainingRounds = 2
		});

		var round = RoundFactory.Create(game, Seeds, MakeOptions(), new Random(42));

		Assert.Empty(round.Tenders);
	}
}