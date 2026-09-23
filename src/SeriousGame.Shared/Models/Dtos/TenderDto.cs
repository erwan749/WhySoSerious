namespace Shared.Models.Dtos;

public class TenderDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public ICollection<RequiredSkillDto> RequiredSkills { get; init; } = [];
    public required int Budget { get; init; }
    public required int RoundsNumber { get; init; }
    public required int RequiredConsultants { get; init; }
}