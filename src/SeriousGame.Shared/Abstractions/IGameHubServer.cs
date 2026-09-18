using Shared.Models.Requests;

namespace Shared.Abstractions;

/// <summary>
/// actions que le client peut 
/// appeler pendant une partie
/// </summary>

public interface IGameHubServer
{

    Task JoinGameRoom(string gameId , string playerId);
    Task ApplyToTender(ApplyToTenderCommand applyToTenderCommand);
    Task EnrollInTraining(EnrollInTrainingCommand enrollInTrainingCommand);
    Task SubmitDecisions(SubmitDecisionsCommand submitDecisionsCommand);

}
