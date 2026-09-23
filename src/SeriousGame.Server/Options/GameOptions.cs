namespace Server.Options;

/// <summary>
/// Paramètres de partie, injectés depuis la section "Game" de appsettings.json.
/// Les valeurs par défaut servent de repli si la section est absente.
/// </summary>
public class GameOptions
{
    public int MinimumPlayers { get; init; } = 3;
    public int MaximumPlayers { get; init; } = 8;
    public int RoundsNumber { get; init; } = 15;
    public int CompanyInitialTreasury { get; init; } = 1_000_000;
    public int SalaryReferencePerConsultant { get; init; } = 5_000;
    public int TenderMarginPercent { get; init; } = 150;
    public int TenderNumberPerPlayer { get; init; } = 2;
    public int TrainingBaseCost { get; init; } = 800;
    public int TrainingRoundsNumber { get; init; } = 1;
    public int TrainingNumberPerPlayer { get; init; } = 1;

}
