using Server.Domain;
using Shared.Models.Requests;

namespace Server.Application.Abstractions;
/// <summary>
/// Flux de la partie : création/join/leave d'une partie et gestion des déconnexions.
/// </summary>

public interface IGameFlowService
{

    Task JoinGameRoom(string gameId, string playerId, string connectionId);
    Task<CommandResult> ApplyToTender(ApplyToTenderCommand command);
    Task EnrollInTraining(EnrollInTrainingCommand command);
    Task SubmitDecisions(SubmitDecisionsCommand command);
    Task StartGame(Game game);

}
