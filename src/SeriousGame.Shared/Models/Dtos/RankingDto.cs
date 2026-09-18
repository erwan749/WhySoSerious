namespace Shared.Models.Dtos;

public class RankingDto
{
    public ICollection<RankingEntryDto> Entries { get; init; } = [];
}

public class RankingEntryDto
{
    public required int Rank { get; init; }
    public required string CompanyId { get; init; }
    public required string CompanyName { get; init; }
    public required int Revenue { get; init; }
}