using Microsoft.Extensions.Logging.Abstractions;
using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class RoundResolverTests
{
    private static readonly Skill Html = new() { Id = 1, Name = "HTML" };

    private readonly RoundResolver _resolver =
        new(new BestCoverageAwardStrategy(), NullLogger<RoundResolver>.Instance);

    private static Player MakePlayer() =>
        new() { Id = Guid.NewGuid().ToString(), Nickname = "Ada", ConnectionId = "c" };

    private static Company MakeCompany(string name) =>
        new() { Name = name, PlayerOwner = MakePlayer(), InitialTreasury = 1_000_000 };

    private static Consultant MakeConsultant(Company company, Level? level)
    {
        var consultant = new Consultant { Firstname = "Test", Lastname = company.Name, Company = company };

        if (level is not null)
        {
            var consultantSkill = new ConsultantSkill { Skill = Html };
            while (consultantSkill.Level < level) consultantSkill.LevelUp();
            consultant.Skills.Add(consultantSkill);
        }

        company.Staffs.Add(consultant);

        return consultant;
    }

    private static Round MakeRound(Tender tender)
    {
        var round = new Round { Game = new Game { Name = "G", Owner = MakePlayer() }, Order = 1 };
        round.Tenders.Add(tender);

        return round;
    }

    private static Tender MakeTender(int roundsNumber = 2, int budget = 15_000)
    {
        var tender = new Tender
        {
            Name = "AO",
            Budget = budget,
            RoundsNumber = roundsNumber,
            RequiredConsultants = 1
        };

        tender.RequiredSkills.Add(new RequiredSkill { Skill = Html, Level = Level.Basic });

        return tender;
    }

    private static TenderApplication AddApplication(Round round, Tender tender, Company company, Consultant consultant)
    {
        var application = new TenderApplication
        {
            Round = round,
            Company = company,
            Tender = tender,
            AssignedConsultants = [consultant]
        };

        round.Applications.Add(application);

        return application;
    }

    [Fact]
    public void ResolveTenders_AwardsTheBestApplicationAndRejectsTheOthers()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);

        var modest = MakeCompany("Modeste");
        var strong = MakeCompany("Costaud");

        var loser = AddApplication(round, tender, modest, MakeConsultant(modest, Level.Basic));
        var winner = AddApplication(round, tender, strong, MakeConsultant(strong, Level.Expert));

        _resolver.ResolveTenders(round);

        Assert.Equal(ApplicationStatus.Won, winner.Status);
        Assert.Equal(ApplicationStatus.Lost, loser.Status);
    }

    [Fact]
    public void ResolveTenders_CreatesAnActiveContractForTheWinner()
    {
        var tender = MakeTender(roundsNumber: 3);
        var round = MakeRound(tender);

        var company = MakeCompany("Acme");
        var consultant = MakeConsultant(company, Level.Advanced);

        AddApplication(round, tender, company, consultant);

        _resolver.ResolveTenders(round);

        var contract = Assert.Single(company.Contracts);

        Assert.Equal(ContractStatus.Active, contract.Status);
        Assert.Equal(tender.RoundsNumber, contract.RemainingRounds);
        Assert.Contains(consultant, contract.AssignedConsultants);
    }

    [Fact]
    public void ResolveTenders_TiesUpTheAssignedConsultants()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);

        var company = MakeCompany("Acme");
        var consultant = MakeConsultant(company, Level.Advanced);

        AddApplication(round, tender, company, consultant);

        _resolver.ResolveTenders(round);

        // Le contrat actif rend le consultant indisponible pour le tour suivant.
        Assert.True(company.IsConsultantBusy(consultant));
        Assert.DoesNotContain(consultant, company.GetAvailableConsultants());
    }

    [Fact]
    public void ResolveTenders_RejectsEveryoneWhenNoApplicationIsAdmissible()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);

        var company = MakeCompany("Acme");
        var application = AddApplication(round, tender, company, MakeConsultant(company, level: null));

        _resolver.ResolveTenders(round);

        Assert.Equal(ApplicationStatus.Lost, application.Status);
        Assert.Empty(company.Contracts);
    }

    [Fact]
    public void ResolveTenders_DoesNothingWhenNobodyApplied()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);

        _resolver.ResolveTenders(round);

        Assert.Empty(round.Applications);
    }
    private static Contract AddContract(Company company, Consultant consultant, int remainingRounds, int budget = 15_000)
    {
        var tender = MakeTender(budget: budget);

        var contract = new Contract
        {
            Company = company,
            Tender = tender,
            AssignedConsultants = [consultant],
            RemainingRounds = remainingRounds
        };

        company.Contracts.Add(contract);

        return contract;
    }

    private static TrainingEnrollment AddEnrollment(
        Company company,
        Consultant consultant,
        int remainingRounds,
        int startedAtRound)
    {
        var enrollment = new TrainingEnrollment
        {
            Company = company,
            Consultant = consultant,
            Training = new Training
            {
                Name = "Perfectionnement HTML",
                Skill = Html,
                Cost = 800,
                RoundsNumber = 1
            },
            RemainingRounds = remainingRounds,
            StartedAtRound = startedAtRound
        };

        company.TrainingEnrollments.Add(enrollment);

        return enrollment;
    }

    [Fact]
    public void Advance_CountsDownAnOngoingContractWithoutPayingIt()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);
        var game = round.Game;

        var company = MakeCompany("Acme");
        game.Companies.Add(company);

        var contract = AddContract(company, MakeConsultant(company, Level.Advanced), remainingRounds: 2);

        _resolver.AdvanceContractsAndTrainings(game, round);

        Assert.Equal(1, contract.RemainingRounds);
        Assert.Equal(ContractStatus.Active, contract.Status);
        Assert.Equal(0, company.Revenue);
    }

    [Fact]
    public void Advance_PaysAndClosesAFinishedContract()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);
        var game = round.Game;

        var company = MakeCompany("Acme");
        game.Companies.Add(company);

        var consultant = MakeConsultant(company, Level.Advanced);
        var contract = AddContract(company, consultant, remainingRounds: 1, budget: 22_000);
        var treasuryBefore = company.Treasury;

        _resolver.AdvanceContractsAndTrainings(game, round);

        Assert.Equal(ContractStatus.Completed, contract.Status);
        Assert.Equal(22_000, company.Revenue);
        Assert.Equal(treasuryBefore + 22_000, company.Treasury);

        // Le contrat n'étant plus actif, ses consultants redeviennent mobilisables.
        Assert.False(company.IsConsultantBusy(consultant));
    }

    [Fact]
    public void Advance_LeavesAnEnrollmentTakenThisRoundUntouched()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);
        var game = round.Game;

        var company = MakeCompany("Acme");
        game.Companies.Add(company);

        // round.Order vaut 1 : l'inscription vient d'être prise, elle ne commence qu'au tour suivant.
        var enrollment = AddEnrollment(company, MakeConsultant(company, Level.Basic), remainingRounds: 1, startedAtRound: round.Order);

        _resolver.AdvanceContractsAndTrainings(game, round);

        Assert.Equal(1, enrollment.RemainingRounds);
        Assert.Equal(EnrollmentStatus.InProgress, enrollment.Status);
    }

    [Fact]
    public void Advance_LevelsUpTheSkillWhenATrainingEnds()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);
        var game = round.Game;

        var company = MakeCompany("Acme");
        game.Companies.Add(company);

        var consultant = MakeConsultant(company, Level.Basic);
        var enrollment = AddEnrollment(company, consultant, remainingRounds: 1, startedAtRound: round.Order - 1);

        _resolver.AdvanceContractsAndTrainings(game, round);

        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.Equal(Level.Intermediate, consultant.Skills.Single().Level);
        Assert.False(company.IsConsultantBusy(consultant));
    }

    [Fact]
    public void Advance_IgnoresAlreadyCompletedContracts()
    {
        var tender = MakeTender();
        var round = MakeRound(tender);
        var game = round.Game;

        var company = MakeCompany("Acme");
        game.Companies.Add(company);

        var contract = AddContract(company, MakeConsultant(company, Level.Advanced), remainingRounds: 0);
        contract.Status = ContractStatus.Completed;

        _resolver.AdvanceContractsAndTrainings(game, round);

        Assert.Equal(0, contract.RemainingRounds);
        Assert.Equal(0, company.Revenue);
    }
    [Fact]
    public void PaySalaries_WithdrawsThePayrollOfEveryCompany()
    {
        var game = new Game { Name = "G", Owner = MakePlayer() };

        var acme = MakeCompany("Acme");
        var globex = MakeCompany("Globex");
        game.Companies.Add(acme);
        game.Companies.Add(globex);

        MakeConsultant(acme, Level.Basic).SetSalaryRequirement(3_000);
        MakeConsultant(globex, Level.Basic).SetSalaryRequirement(5_000);

        _resolver.PayDaysForStaffs(game);

        // MakeCompany démarre à 1 000 000 : aucune des deux ne tombe.
        Assert.Equal(1_000_000 - 3_000, acme.Treasury);
        Assert.Equal(1_000_000 - 5_000, globex.Treasury);
        Assert.False(acme.IsBankrupt);
        Assert.False(globex.IsBankrupt);
    }
}