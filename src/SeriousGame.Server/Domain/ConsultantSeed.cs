namespace Server.Domain;

/// <summary>
/// Nom de consultant issu des cartes du jeu. Le salaire et les compétences sont tirés au hasard à
/// la création (voir ConsultantFactory), pour que chaque partie soit différente.
/// </summary>
public class ConsultantSeed
{
    public required string Firstname { get; init; }
    public required string Lastname { get; init; }
}