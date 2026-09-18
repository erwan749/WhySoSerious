namespace Shared.Models.Dtos;

public class TrainingDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required SkillDto Skill { get; init; }
    public int Cost { get; init; }
    public required int RoundsNumber { get; init; }
}