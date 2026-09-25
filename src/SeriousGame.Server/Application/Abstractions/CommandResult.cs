namespace Server.Application.Abstractions;

/// <summary>
/// Résultat d'une commande de jeu (ApplyToTender, EnrollInTraining...) : succès, ou échec avec un
/// message destiné au client. Reste agnostique du transport — c'est la couche Hubs qui décide
/// comment le traduire (HubException pour SignalR).
/// </summary>
public readonly record struct CommandResult(bool Success, string? Error)
{
    public static CommandResult Ok() => new(true, null);
    public static CommandResult Fail(string error) => new(false, error);
}