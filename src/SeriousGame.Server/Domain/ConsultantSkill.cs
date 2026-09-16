using Server.Domain.Enums;

namespace Server.Domain;

public class ConsultantSkill
{
    public required Skill Skill { get; init; }
    public Level Level { get; private set; } = Level.Zero;

    /// <summary>Fait progresser le niveau d'un cran (typiquement à la fin d'une formation), plafonné à Expert.</summary>
    public void LevelUp()
    {
        if (Level < Level.Expert) Level++;
    }
}