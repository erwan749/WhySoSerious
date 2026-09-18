namespace Server.Domain;

/// <summary>
/// Modèle de consultant issu des cartes du jeu d'essai. Sert de blueprint à ConsultantFactory (US05)
/// pour créer un Consultant réel rattaché à une Company. Contrairement à Consultant, ne référence
/// aucune Company : c'est une donnée de référentiel, pas une instance de jeu.
/// </summary>
public class ConsultantSeed
{
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
    public required int SalaryRequirement { get; init; }
    public ICollection<ConsultantSkill> Skills { get; init; } = [];
}