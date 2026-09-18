using Server.Domain.Enums;

namespace Server.Domain;

/// <summary>
/// Compétence exigée par un Tender, avec le niveau minimum requis. Symétrique de ConsultantSkill
/// (niveau possédé), mais du côté "exigé" : contrairement à ConsultantSkill, son Level ne progresse
/// jamais (pas de LevelUp) — c'est une exigence figée à la création du Tender.
/// </summary>
public class RequiredSkill
{
    public required Skill Skill { get; init; }
    public required Level Level { get; init; }
}