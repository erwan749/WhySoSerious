using Server.Domain.Base;

namespace Server.Domain;

public class Tender : BaseModel
{
    public required string Name { get; init; }
    public ICollection<RequiredSkill> RequiredSkills { get; } = [];
    public required int Budget {get; init;}
    public required int RoundsNumber {get; init;}
}