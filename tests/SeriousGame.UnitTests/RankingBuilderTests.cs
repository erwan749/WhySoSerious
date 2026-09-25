using Server.Application.Services;
using Server.Domain;

namespace SeriousGame.UnitTests;

public class RankingBuilderTests
{
    private static Player MakePlayer(string nickname) =>
        new() { Id = Guid.NewGuid().ToString(), Nickname = nickname, ConnectionId = "c" };

    private static Company MakeCompany(string name, int revenue)
    {
        var company = new Company { Name = name, PlayerOwner = MakePlayer(name), InitialTreasury = 0 };
        if (revenue > 0) company.RecordContractRevenue(revenue);
        return company;
    }

    [Fact]
    public void Build_ReturnsEmptyRanking_WhenNoCompanies()
    {
        var ranking = RankingBuilder.Build([]);

        Assert.Empty(ranking.Entries);
    }

    [Fact]
    public void Build_RanksSingleCompanyFirst()
    {
        var company = MakeCompany("Acme", 5000);

        var ranking = RankingBuilder.Build([company]);

        var entry = Assert.Single(ranking.Entries);
        Assert.Equal(1, entry.Rank);
        Assert.Equal(company.Id, entry.CompanyId);
        Assert.Equal(5000, entry.Revenue);
    }

    [Fact]
    public void Build_OrdersCompaniesByRevenueDescending()
    {
        var lowest = MakeCompany("Low", 1000);
        var highest = MakeCompany("High", 9000);
        var middle = MakeCompany("Mid", 5000);

        var ranking = RankingBuilder.Build([lowest, highest, middle]);

        Assert.Equal(["High", "Mid", "Low"], ranking.Entries.Select(e => e.CompanyName).ToList());
        Assert.Equal([1, 2, 3], ranking.Entries.Select(e => e.Rank).ToList());
    }

    [Fact]
    public void Build_GivesSameRank_ToTiedCompanies_AndSkipsNextRank()
    {
        var tiedA = MakeCompany("Alpha", 5000);
        var tiedB = MakeCompany("Beta", 5000);
        var third = MakeCompany("Gamma", 3000);

        var ranking = RankingBuilder.Build([third, tiedA, tiedB]);

        // Ex-aequo triés par nom (déterministe), rang 1 partagé, puis saut direct à 3.
        Assert.Equal(["Alpha", "Beta", "Gamma"], ranking.Entries.Select(e => e.CompanyName).ToList());
        Assert.Equal([1, 1, 3], ranking.Entries.Select(e => e.Rank).ToList());
    }

    [Fact]
    public void Build_HandlesAllCompaniesTied()
    {
        var a = MakeCompany("A", 1000);
        var b = MakeCompany("B", 1000);
        var c = MakeCompany("C", 1000);

        var ranking = RankingBuilder.Build([c, a, b]);

        Assert.All(ranking.Entries, e => Assert.Equal(1, e.Rank));
    }
}