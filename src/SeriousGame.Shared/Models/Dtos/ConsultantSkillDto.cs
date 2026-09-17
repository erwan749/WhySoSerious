namespace Shared.Models.Dtos;

public class ConsultantSkillDto
{
    public required SkillDto Skill { get; init; }
    public required SkillLevel Level { get; init; }
}