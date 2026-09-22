using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class TenderApplicationValidatorTests
{
    private static Player MakePlayer() => new() { Id = Guid.NewGuid().ToString(), Nickname = "Ada", ConnectionId = "c" };
    private static Company MakeCompany() => new() { Name = "Acme", PlayerOwner = MakePlayer(), InitialTreasury = 1000 };
    private static Consultant MakeConsultant(Company company) => new() { Firstname = "Bob", Lastname = "Test", Company = company };
    private static Tender MakeTender() => new() { Name = "Refonte", Budget = 1000, RoundsNumber = 1 };
    private static Game MakeGame() => new() { Name = "G", Owner = MakePlayer() };

    private static Round MakeRound(params Tender[] tenders)
    {
        var round = new Round { Game = MakeGame(), Order = 1 };
        foreach (var tender in tenders) round.Tenders.Add(tender);
        return round;
    }

    [Fact]
    public void Validate_Fails_WhenTenderNotInRound()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var tender = MakeTender();
        var round = MakeRound(); // ne contient pas tender

        var result = TenderApplicationValidator.Validate(round, company, tender, [consultant]);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenNoConsultantsProvided()
    {
        var company = MakeCompany();
        var tender = MakeTender();
        var round = MakeRound(tender);

        var result = TenderApplicationValidator.Validate(round, company, tender, []);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenConsultantBelongsToAnotherCompany()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(MakeCompany()); // autre entreprise
        var tender = MakeTender();
        var round = MakeRound(tender);

        var result = TenderApplicationValidator.Validate(round, company, tender, [consultant]);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenConsultantIsOnActiveContract()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var tender = MakeTender();
        var round = MakeRound(tender);

        company.Contracts.Add(new Contract
        {
            Company = company,
            Tender = MakeTender(),
            AssignedConsultants = [consultant],
            Status = ContractStatus.Active
        });

        var result = TenderApplicationValidator.Validate(round, company, tender, [consultant]);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenConsultantIsInActiveTraining()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var tender = MakeTender();
        var round = MakeRound(tender);
        var training = new Training { Name = "Formation", Skill = new Skill { Id = 1, Name = "C#" }, Cost = 100, RoundsNumber = 1 };

        company.TrainingEnrollments.Add(new TrainingEnrollment
        {
            Company = company,
            Consultant = consultant,
            Training = training,
            Status = EnrollmentStatus.InProgress
        });

        var result = TenderApplicationValidator.Validate(round, company, tender, [consultant]);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenConsultantAlreadyHasPendingApplicationThisRound()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var tender = MakeTender();
        var otherTender = MakeTender();
        var round = MakeRound(tender, otherTender);

        round.Applications.Add(new TenderApplication
        {
            Round = round,
            Company = company,
            Tender = otherTender,
            AssignedConsultants = [consultant],
            Status = ApplicationStatus.Pending
        });

        var result = TenderApplicationValidator.Validate(round, company, tender, [consultant]);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Succeeds_WhenConsultantsAreFreeAndTenderIsInRound()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var tender = MakeTender();
        var round = MakeRound(tender);

        var result = TenderApplicationValidator.Validate(round, company, tender, [consultant]);

        Assert.True(result.Success);
    }
}