using Server.Application.Services;
using Server.Domain;
using Server.Domain.Enums;

namespace SeriousGame.UnitTests;

public class TrainingEnrollmentValidatorTests
{
    private static Player MakePlayer() => new() { Id = Guid.NewGuid().ToString(), Nickname = "Ada", ConnectionId = "c" };
    private static Company MakeCompany(int treasury = 5000) => new() { Name = "Acme", PlayerOwner = MakePlayer(), InitialTreasury = treasury };
    private static Consultant MakeConsultant(Company company) => new() { Firstname = "Bob", Lastname = "Test", Company = company };
    private static Training MakeTraining(int cost = 500) => new() { Name = "Formation", Skill = new Skill { Id = 1, Name = "C#" }, Cost = cost, RoundsNumber = 1 };
    private static Round MakeRound(params Training[] trainings)
    {
        var round = new Round { Game = new Game { Name = "G", Owner = MakePlayer() }, Order = 1 };
        foreach (var training in trainings) round.Trainings.Add(training);
        return round;
    }

    [Fact]
    public void Validate_Fails_WhenTrainingNotInRound()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var training = MakeTraining();
        var round = MakeRound(); // ne contient pas training

        var result = TrainingEnrollmentValidator.Validate(round, company, training, consultant);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenConsultantBelongsToAnotherCompany()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(MakeCompany());
        var training = MakeTraining();
        var round = MakeRound(training);

        var result = TrainingEnrollmentValidator.Validate(round, company, training, consultant);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenConsultantAlreadyHasPendingApplicationThisRound()
    {
        var company = MakeCompany();
        var consultant = MakeConsultant(company);
        var training = MakeTraining();
        var round = MakeRound(training);
        var tender = new Tender { Name = "AO", Budget = 1000, RoundsNumber = 1 };

        round.Applications.Add(new TenderApplication
        {
            Round = round,
            Company = company,
            Tender = tender,
            AssignedConsultants = [consultant],
            Status = ApplicationStatus.Pending
        });

        var result = TrainingEnrollmentValidator.Validate(round, company, training, consultant);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Fails_WhenTreasuryIsInsufficient()
    {
        var company = MakeCompany(treasury: 100);
        var consultant = MakeConsultant(company);
        var training = MakeTraining(cost: 500);
        var round = MakeRound(training);

        var result = TrainingEnrollmentValidator.Validate(round, company, training, consultant);

        Assert.False(result.Success);
    }

    [Fact]
    public void Validate_Succeeds_WhenConsultantIsFreeAndTreasuryIsSufficient()
    {
        var company = MakeCompany(treasury: 5000);
        var consultant = MakeConsultant(company);
        var training = MakeTraining(cost: 500);
        var round = MakeRound(training);

        var result = TrainingEnrollmentValidator.Validate(round, company, training, consultant);

        Assert.True(result.Success);
    }
}