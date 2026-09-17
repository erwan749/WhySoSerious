using Shared.Models.Dtos;
using Shared.Models.Requests;

namespace Shared.Abstractions;

public interface IGameHubServer
{

    Task JoinGameRound(string gameId , string playerId);
    Task ApplyToTender(ApplyToTenderCommand applyToTenderCommand);
    Task EnrollInTraning(EnrollInTrainingCommand enrollInTrainingCommand);
    Task SubmitDecisionsCommand(SubmitDecisionsCommand submitDecisionsCommand);

}
