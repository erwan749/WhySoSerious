namespace Shared.Models.Requests;

public record SubmitDecisionsCommand
{
    public required string PlayerId { get; init; }
    public required string RoundId { get; init; }
}