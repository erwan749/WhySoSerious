using Microsoft.AspNetCore.SignalR;
using Shared.Abstractions;
using Server.Application.Abstractions;
using Shared.Models.Requests;

namespace Server.Hubs;

/// <summary>
/// adaptateur SignalR fin, délègue à IGameFlowService.
/// </summary>

public sealed class GameHub : Hub<IGameHubClient>, IGameHubServer
{
    private readonly IGameFlowService _gameFlowService;

    public GameHub(IGameFlowService gameFlowService)
    {
        _gameFlowService = gameFlowService;
    }

    public Task JoinGameRoom(string gameId, string playerId)
    {
        return _gameFlowService.JoinGameRoom(gameId, playerId, Context.ConnectionId);
    }

    public async Task ApplyToTender(ApplyToTenderCommand applyToTenderCommand)
    {
        var result = await _gameFlowService.ApplyToTender(applyToTenderCommand);

        if (!result.Success)
        {
            throw new HubException(result.Error);
        }
    }

    public async Task EnrollInTraining(EnrollInTrainingCommand enrollInTrainingCommand)
    {
        var result = await _gameFlowService.EnrollInTraining(enrollInTrainingCommand);

        if (!result.Success)
        {
            throw new HubException(result.Error);
        }
    }

    public  Task SubmitDecisions(SubmitDecisionsCommand submitDecisionsCommand)
    {
        return _gameFlowService.SubmitDecisions(submitDecisionsCommand);
    }
}
