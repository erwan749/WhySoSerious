namespace Shared.Models.Dtos;

public class RequiredSkillDto
{
    public required SkillDto Skill { get; init; }
    public required SkillLevel Level { get; init; }
}