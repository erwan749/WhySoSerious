namespace Shared.Models.Dtos;

public class RoundResultDto
{
    public required string RoundId { get; init; }
    public required int RoundOrder { get; init; }
    public ICollection<TenderOutcomeDto> TenderOutcomes { get; init; } = [];
    public ICollection<TrainingOutcomeDto> CompletedTrainings { get; init; } = [];
    public ICollection<CompanyDto> Companies { get; init; } = [];
}

public class TenderOutcomeDto
{
    public required string TenderId { get; init; }
    public required string TenderName { get; init; }
    public string? WinningCompanyId { get; init; }
}

public class TrainingOutcomeDto
{
    public required string ConsultantId { get; init; }
    public required string ConsultantFullName { get; init; }
    public required string SkillName { get; init; }
}