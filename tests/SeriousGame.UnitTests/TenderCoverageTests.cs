using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class TenderCoverageTests
{
    private static readonly Skill Html = new() { Id = 1, Name = "HTML" };
    private static readonly Skill Sql = new() { Id = 17, Name = "SQL" };

    private static Player MakePlayer() =>
        new() { Id = Guid.NewGuid().ToString(), Nickname = "Ada", ConnectionId = "c" };

    private static Company MakeCompany() =>
        new() { Name = "Acme", PlayerOwner = MakePlayer(), InitialTreasury = 1_000_000 };

    private static Consultant MakeConsultant(Company company, Skill skill, Level level)
    {
        var consultant = new Consultant
        {
            Firstname = "Test",
            Lastname = $"Consultant {company.Staffs.Count + 1}",
            Company = company
        };

        var consultantSkill = new ConsultantSkill { Skill = skill };
        while (consultantSkill.Level < level) consultantSkill.LevelUp();

        consultant.Skills.Add(consultantSkill);
        company.Staffs.Add(consultant);

        return consultant;
    }

    private static Tender MakeTender(int requiredConsultants, params RequiredSkill[] requiredSkills)
    {
        var tender = new Tender
        {
            Name = "AO",
            Budget = 15_000,
            RoundsNumber = 2,
            RequiredConsultants = requiredConsultants
        };

        foreach (var requiredSkill in requiredSkills) tender.RequiredSkills.Add(requiredSkill);

        return tender;
    }

    private static RequiredSkill Require(Skill skill, Level level) => new() { Skill = skill, Level = level };

    private static Round MakeRound() =>
        new() { Game = new Game { Name = "G", Owner = MakePlayer() }, Order = 1 };

    private static TenderApplication MakeApplication(Company company, Tender tender, params Consultant[] consultants) =>
        new()
        {
            Round = MakeRound(),
            Company = company,
            Tender = tender,
            AssignedConsultants = [.. consultants]
        };

    [Fact]
    public void IsAdmissible_Fails_WhenNotEnoughConsultantsAreAssigned()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company, Html, Level.Advanced);
        var tender = MakeTender(2, Require(Html, Level.Basic));

        var application = MakeApplication(company, tender, consultant);

        Assert.False(TenderCoverage.IsAdmissible(tender, application));
    }

    [Fact]
    public void IsAdmissible_Fails_WhenARequiredSkillIsMissing()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company, Html, Level.Advanced);
        var tender = MakeTender(1, Require(Sql, Level.Basic));

        var application = MakeApplication(company, tender, consultant);

        Assert.False(TenderCoverage.IsAdmissible(tender, application));
    }

    [Fact]
    public void IsAdmissible_Fails_WhenTheLevelIsTooLow()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company, Html, Level.Basic);
        var tender = MakeTender(1, Require(Html, Level.Advanced));

        var application = MakeApplication(company, tender, consultant);

        Assert.False(TenderCoverage.IsAdmissible(tender, application));
    }

    [Fact]
    public void IsAdmissible_Succeeds_WhenEveryRequirementIsMet()
    {
        var company = MakeCompany();
        var frontend = MakeConsultant(company, Html, Level.Intermediate);
        var backend = MakeConsultant(company, Sql, Level.Basic);
        var tender = MakeTender(2, Require(Html, Level.Basic), Require(Sql, Level.Basic));

        var application = MakeApplication(company, tender, frontend, backend);

        Assert.True(TenderCoverage.IsAdmissible(tender, application));
    }

    [Fact]
    public void CoverageScore_IsZero_WhenTheTeamMatchesExactly()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company, Html, Level.Basic);
        var tender = MakeTender(1, Require(Html, Level.Basic));

        var application = MakeApplication(company, tender, consultant);

        Assert.Equal(0, TenderCoverage.CoverageScore(tender, application));
    }

    [Fact]
    public void CoverageScore_CountsEachNotchAboveTheRequirement()
    {
        var company = MakeCompany();
        // Basic -> Expert : trois crans au-dessus.
        var consultant = MakeConsultant(company, Html, Level.Expert);
        var tender = MakeTender(1, Require(Html, Level.Basic));

        var application = MakeApplication(company, tender, consultant);

        Assert.Equal(3, TenderCoverage.CoverageScore(tender, application));
    }

    [Fact]
    public void CoverageScore_KeepsOnlyTheBestLevelPerSkill()
    {
        var company = MakeCompany();
        var junior = MakeConsultant(company, Html, Level.Basic);
        var senior = MakeConsultant(company, Html, Level.Advanced);
        var tender = MakeTender(2, Require(Html, Level.Basic));

        var application = MakeApplication(company, tender, junior, senior);

        // Deux consultants sur la même compétence ne cumulent pas : seul le meilleur compte.
        Assert.Equal(2, TenderCoverage.CoverageScore(tender, application));
    }
}