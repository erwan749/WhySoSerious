using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class BestCoverageAwardStrategyTests
{
    private static readonly Skill Html = new() { Id = 1, Name = "HTML" };

    private readonly BestCoverageAwardStrategy _strategy = new();

    private static Player MakePlayer() =>
        new() { Id = Guid.NewGuid().ToString(), Nickname = "Ada", ConnectionId = "c" };

    private static Company MakeCompany(string name) =>
        new() { Name = name, PlayerOwner = MakePlayer(), InitialTreasury = 1_000_000 };

    private static Consultant MakeConsultant(Company company, Level level)
    {
        var consultant = new Consultant { Firstname = "Test", Lastname = company.Name, Company = company };
        var consultantSkill = new ConsultantSkill { Skill = Html };

        while (consultantSkill.Level < level) consultantSkill.LevelUp();

        consultant.Skills.Add(consultantSkill);
        company.Staffs.Add(consultant);

        return consultant;
    }

    private static Tender MakeTender()
    {
        var tender = new Tender { Name = "AO", Budget = 15_000, RoundsNumber = 2, RequiredConsultants = 1 };
        tender.RequiredSkills.Add(new RequiredSkill { Skill = Html, Level = Level.Basic });

        return tender;
    }

    private static Round MakeRound() =>
        new() { Game = new Game { Name = "G", Owner = MakePlayer() }, Order = 1 };

    private static TenderApplication MakeApplication(Tender tender, Company company, Consultant consultant) =>
        new() { Round = MakeRound(), Company = company, Tender = tender, AssignedConsultants = [consultant] };

    [Fact]
    public void SelectWinner_PicksTheBestCoverage()
    {
        var tender = MakeTender();

        var modest = MakeCompany("Modeste");
        var strong = MakeCompany("Costaud");

        var applications = new List<TenderApplication>
        {
            MakeApplication(tender, modest, MakeConsultant(modest, Level.Basic)),
            MakeApplication(tender, strong, MakeConsultant(strong, Level.Expert))
        };

        var winner = _strategy.SelectWinner(tender, applications);

        Assert.Same(applications[1], winner);
    }

    [Fact]
    public void SelectWinner_ReturnsNull_WhenNoApplicationIsAdmissible()
    {
        var tender = MakeTender();
        var company = MakeCompany("Acme");

        // Aucune compétence : la candidature ne couvre rien.
        var consultant = new Consultant { Firstname = "Sans", Lastname = "Competence", Company = company };
        company.Staffs.Add(consultant);

        var applications = new List<TenderApplication> { MakeApplication(tender, company, consultant) };

        Assert.Null(_strategy.SelectWinner(tender, applications));
    }

    [Fact]
    public void SelectWinner_BreaksTiesOnTheEarliestApplication()
    {
        var tender = MakeTender();

        var first = MakeCompany("Premiere");
        var second = MakeCompany("Seconde");

        // Même niveau des deux côtés : seul l'ordre d'arrivée départage.
        var applications = new List<TenderApplication>
        {
            MakeApplication(tender, first, MakeConsultant(first, Level.Advanced)),
            MakeApplication(tender, second, MakeConsultant(second, Level.Advanced))
        };

        var winner = _strategy.SelectWinner(tender, applications);

        Assert.Same(applications[0], winner);
    }
}