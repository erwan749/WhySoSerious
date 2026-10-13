using Server.Domain.Base;

namespace Server.Domain;
public class Candidate : BaseModel
{
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    public required int SalaryRequirement { get; init; }
    public ICollection<ConsultantSkill> Skills { get; } = [];

    public string FullName => $"{Firstname} {Lastname}";
}