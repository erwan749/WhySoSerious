using Server.Infrastructure;

namespace SeriousGame.UnitTests;

public class AppMemoryTests
{
    private readonly AppMemory _memory = new();

    [Fact]
    public void TenderSeeds_ContainsEnoughNames()
    {
        // Assez de noms pour qu'une partie longue n'en réutilise pas trop vite.
        Assert.True(_memory.TenderSeeds.Count >= 20, $"Seulement {_memory.TenderSeeds.Count} noms d'appels d'offres.");
    }

    [Fact]
    public void TenderSeeds_HaveNoBlankName()
    {
        Assert.All(_memory.TenderSeeds, seed => Assert.False(string.IsNullOrWhiteSpace(seed.Name)));
    }

    [Fact]
    public void TenderSeeds_HaveNoDuplicate()
    {
        var distinctCount = _memory.TenderSeeds.Select(seed => seed.Name).Distinct().Count();

        Assert.Equal(_memory.TenderSeeds.Count, distinctCount);
    }

    [Fact]
    public void ConsultantsSeed_HaveNoDuplicateFullName()
    {
        var distinctCount = _memory.ConsultantsSeed
            .Select(seed => $"{seed.Firstname} {seed.Lastname}")
            .Distinct()
            .Count();

        Assert.Equal(_memory.ConsultantsSeed.Count, distinctCount);
    }

    [Fact]
    public void Skills_HaveUniqueIdsAndNames()
    {
        Assert.NotEmpty(_memory.Skills);
        Assert.Equal(_memory.Skills.Count, _memory.Skills.Select(skill => skill.Id).Distinct().Count());
        Assert.Equal(_memory.Skills.Count, _memory.Skills.Select(skill => skill.Name).Distinct().Count());
    }
}