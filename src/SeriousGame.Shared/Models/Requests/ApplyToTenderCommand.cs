namespace Shared.Models.Requests;

public record ApplyToTenderCommand
{
    public required string PlayerId { get; init; }
    public required string RoundId { get; init; }
    public required string TenderId { get; init; }
    public required ICollection<string> ConsultantIds { get; init; }
}