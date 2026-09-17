namespace Shared.Models.Dtos;

public class RoundDto
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public required int TotalRounds { get; init; }
    public ICollection<TenderDto> Tenders { get; init; } = [];
    public ICollection<TrainingDto> Trainings { get; init; } = [];
}