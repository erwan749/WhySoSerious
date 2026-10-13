namespace Shared.Models.Dtos;

public class CandidateDto
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required int SalaryRequirement { get; init; }
    public ICollection<ConsultantSkillDto> Skills { get; init; } = [];
}