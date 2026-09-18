namespace Shared.Models.Requests;

public record EnrollInTrainingCommand
{
    public required string PlayerId { get; init; }
    public required string RoundId { get; init; }
    public required string TrainingId { get; init; }
    public required string ConsultantId { get; init; }
}