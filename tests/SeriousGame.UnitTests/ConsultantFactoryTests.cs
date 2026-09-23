using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class ConsultantFactoryTests
{
    private static readonly IReadOnlyList<Skill> SkillCatalog =
    [
        new Skill { Id = 1, Name = "HTML" },
        new Skill { Id = 2, Name = "CSS" },
        new Skill { Id = 3, Name = "JavaScript" }
    ];

    private static Company MakeCompany(string name = "Test Co") => new()
    {
        Name = name,
        PlayerOwner = new Player { Id = "p1", Nickname = "Owner", ConnectionId = "c1" },
        InitialTreasury = 1000
    };

    private static IReadOnlyList<ConsultantSeed> MakeSeeds(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new ConsultantSeed { Firstname = $"First{i}", Lastname = $"Last{i}" })
            .ToList();

    [Fact]
    public void CreateFromSeed_StartingSkillsAreBasicOrIntermediate()
    {
        var company = MakeCompany();
        var random = new Random(1);

        // Le niveau est tiré au hasard : on répète pour couvrir les deux valeurs possibles.
        for (var i = 0; i < 100; i++)
        {
            var consultant = ConsultantFactory.CreateFromSeed(MakeSeeds(1)[0], SkillCatalog, company, random);

            Assert.All(consultant.Skills, skill =>
                Assert.True(skill.Level is Level.Basic or Level.Intermediate, $"Niveau inattendu : {skill.Level}"));
        }
    }

    [Fact]
    public void CreateFromSeed_DrawsBothStartingLevels()
    {
        var company = MakeCompany();
        var random = new Random(1);

        var levels = Enumerable.Range(0, 100)
            .SelectMany(_ => ConsultantFactory.CreateFromSeed(MakeSeeds(1)[0], SkillCatalog, company, random).Skills)
            .Select(skill => skill.Level)
            .Distinct()
            .ToList();

        // Garde-fou contre une borne exclusive oubliée dans random.Next : sans le +1,
        // seul Basic sortirait.
        Assert.Contains(Level.Basic, levels);
        Assert.Contains(Level.Intermediate, levels);
    }

    [Fact]
    public void CreateFromSeed_DrawsSkillsFromCatalogOnly()
    {
        var company = MakeCompany();
        var random = new Random(1);

        var consultant = ConsultantFactory.CreateFromSeed(MakeSeeds(1)[0], SkillCatalog, company, random);

        Assert.NotEmpty(consultant.Skills);
        Assert.All(consultant.Skills, skill => Assert.Contains(skill.Skill, SkillCatalog));
    }

    [Fact]
    public void AssignInitialStaff_GivesEveryCompanyTheSameStaffSize()
    {
        var companies = new List<Company> { MakeCompany("A"), MakeCompany("B") };
        var seeds = MakeSeeds(companies.Count * ConsultantFactory.ConsultantsPerCompany);

        ConsultantFactory.AssignInitialStaff(companies, seeds, SkillCatalog, new Random(1));

        Assert.All(companies, company =>
            Assert.Equal(ConsultantFactory.ConsultantsPerCompany, company.Staff.Count));
    }

    [Fact]
    public void AssignInitialStaff_NotEnoughSeeds_Throws()
    {
        var companies = new List<Company> { MakeCompany("A"), MakeCompany("B") };
        var seeds = MakeSeeds(1);

        Assert.Throws<InvalidOperationException>(() =>
            ConsultantFactory.AssignInitialStaff(companies, seeds, SkillCatalog, new Random(1)));
    }
}