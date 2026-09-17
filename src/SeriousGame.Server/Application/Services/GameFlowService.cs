using Microsoft.AspNetCore.SignalR;
using Server.Application.Abstractions;
using Server.Domain;
using Server.Hubs;
using Shared.Abstractions;
using Shared.Models.Requests;
using Microsoft.Extensions.Logging;

namespace Server.Application.Services;

public class GameFlowService : IGameFlowService
{
    private readonly GameService _gameService;
    private readonly PlayerService _playerService;
    private readonly IHubContext<GameHub , IGameHubClient> _hubContext;
    private readonly ILogger<GameFlowService> _logger;

    public GameFlowService(GameService gameService, PlayerService playerService, IHubContext<GameHub, IGameHubClient> hubContext, ILogger<GameFlowService> logger)
    {
        _gameService = gameService;
        _playerService = playerService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task ApplyToTender(ApplyToTenderCommand command)
    {
        return Task.CompletedTask;
    }

    public Task EnrollInTraining(EnrollInTrainingCommand command)
    {
        return Task.CompletedTask;
    }

    public async Task JoinGameRoom(string gameId, string playerId, string connectionId)
    {
        if (!_playerService.HasPlayer(playerId)) 
        { 
            _logger.LogInformation("No player inside the player service",  playerId); 
            return; 
        }

        var game = _gameService.GetGame(gameId);
        
        if(game == null) 
        {
            _logger.LogInformation("Game does not exist",gameId);
            return;
        }
        if(!game.Players.Any(p => p.Id == playerId)) 
        {
            _logger.LogInformation("Player not in the game",  playerId , gameId); 
            return; 
        }

        await _hubContext.Groups.AddToGroupAsync(connectionId, gameId);


    }

    public Task StartGame(Game game)
    {
        return Task.CompletedTask;
    }

    public Task SubmitDecisions(SubmitDecisionsCommand command)
    {
        return Task.CompletedTask;
    }
}
