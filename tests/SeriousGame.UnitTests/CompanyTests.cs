using Server.Domain;

namespace SeriousGame.UnitTests;

public class CompanyTests
{
    private static Company MakeCompany() => new()
    {
        Name = "Test Co",
        PlayerOwner = new Player { Id = "p1", Nickname = "Owner", ConnectionId = "c1" },
        InitialTreasury = 1000
    };

    [Fact]
    public void Deposit_IncreasesTreasury()
    {
        var company = MakeCompany();
        var before = company.Treasury;

        company.Deposit(500);

        Assert.Equal(before + 500, company.Treasury);
    }

    [Fact]
    public void Withdraw_DecreasesTreasury()
    {
        var company = MakeCompany();
        var before = company.Treasury;

        company.Withdraw(500);

        Assert.Equal(before - 500, company.Treasury);
    }

    [Fact]
    public void Deposit_NegativeAmount_Throws()
    {
        var company = MakeCompany();

        Assert.Throws<ArgumentOutOfRangeException>(() => company.Deposit(-1));
    }

    [Fact]
    public void Withdraw_NegativeAmount_Throws()
    {
        var company = MakeCompany();

        Assert.Throws<ArgumentOutOfRangeException>(() => company.Withdraw(-1));
    }
    private static Consultant AddConsultant(Company company, int salary)
    {
        var consultant = new Consultant
        {
            Firstname = "Test",
            Lastname = company.Name,
            Company = company
        };

        consultant.SetSalaryRequirement(salary);
        company.Staffs.Add(consultant);

        return consultant;
    }

    [Fact]
    public void RecordSalaryPayment_WithdrawsTheWholePayroll()
    {
        var company = MakeCompany();
        AddConsultant(company, 300);
        AddConsultant(company, 400);
        var before = company.Treasury;

        company.RecordSalaryPayment();

        Assert.Equal(700, company.GetPayroll());
        Assert.Equal(before - 700, company.Treasury);
    }

    [Fact]
    public void RecordSalaryPayment_LeavesTheCompanySolventWhenTreasuryCoversThePayroll()
    {
        // MakeCompany démarre à 1 000.
        var company = MakeCompany();
        AddConsultant(company, 400);

        company.RecordSalaryPayment();

        Assert.False(company.IsBankrupt);
        Assert.Equal(600, company.Treasury);
    }

    [Fact]
    public void RecordSalaryPayment_BankruptsTheCompanyWhenTreasuryRunsOut()
    {
        var company = MakeCompany();
        AddConsultant(company, 1_500);

        company.RecordSalaryPayment();

        Assert.True(company.IsBankrupt);
        // La trésorerie passe réellement dans le rouge, elle n'est pas bornée à zéro.
        Assert.Equal(-500, company.Treasury);
    }
}
