using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Server.Application.Abstractions;
using Server.Domain;
using Server.Hubs;
using Shared.Abstractions;
using Shared.Models.Dtos;
using Shared.Models.Requests;
using Microsoft.Extensions.Options;
using Server.Infrastructure;
using Server.Options;

namespace Server.Application.Services;

public class GameFlowService : IGameFlowService
{
    private readonly GameService _gameService;
    private readonly PlayerService _playerService;
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly ILogger<GameFlowService> _logger;
    private readonly GameOptions _gameOptions;
    private readonly AppMemory _appMemory;

    public GameFlowService(
        GameService gameService,
        PlayerService playerService,
        IHubContext<GameHub, IGameHubClient> hubContext,
        ILogger<GameFlowService> logger,
        IOptions<GameOptions> gameOptions,
        AppMemory appMemory)
    {
        _gameService = gameService;
        _playerService = playerService;
        _hubContext = hubContext;
        _logger = logger;
        _gameOptions = gameOptions.Value;
        _appMemory = appMemory;
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
            _logger.LogWarning("Unknown player {PlayerId}", playerId);
            return;
        }

        var game = _gameService.GetGame(gameId);

        if (game is null)
        {
            _logger.LogWarning("Game {GameId} does not exist", gameId);
            return;
        }

        if (!game.Players.Any(p => p.Id == playerId))
        {
            _logger.LogWarning("Player {PlayerId} is not in game {GameId}", playerId, gameId);
            return;
        }

        await _hubContext.Groups.AddToGroupAsync(connectionId, gameId);

        Round? firstRound = null;

        // Verrou par partie : deux joueurs qui rejoignent la salle en même temps ne doivent pas
        // créer deux fois le tour 1. Pas d'await ici, l'envoi SignalR se fait après le lock.
        lock (game)
        {
            game.PlayersInGameRoom.Add(playerId);

            var everyoneIsHere = game.IsInProgress
                && game.PlayersInGameRoom.Count == game.Players.Count
                && game.Rounds.Count == 0;

            if (everyoneIsHere)
            {
                firstRound = CreateRound(game);
            }
        }

        if (firstRound is not null)
        {
            await _hubContext.Clients.Group(game.Id).GameStarted(game.Companies.Select(Mapper.ToDto).ToList());
            await StartRound(game, firstRound);
        }
    }

    public Task StartGame(Game game)
    {
        foreach (var player in game.Players)
        {
            var company = CompanyFactory.Create(player, _gameOptions);
            game.Companies.Add(company);
        }

        ConsultantFactory.AssignInitialStaff(game.Companies.ToList(), _appMemory.ConsultantsSeed, Random.Shared);

        return Task.CompletedTask;
    }

    public async Task SubmitDecisions(SubmitDecisionsCommand command)
    {
        var game = _gameService.GetGameByRoundId(command.RoundId);

        if (game is null)
        {
            _logger.LogWarning("No game found for round {RoundId}", command.RoundId);
            return;
        }

        var round = game.Rounds.FirstOrDefault(r => r.Id == command.RoundId);
        var player = game.Players.FirstOrDefault(p => p.Id == command.PlayerId);

        if (round is null || player is null)
        {
            _logger.LogWarning("Invalid submission: round {RoundId}, player {PlayerId}", command.RoundId, command.PlayerId);
            return;
        }

        bool everyoneSubmitted;

        lock (game)
        {
            round.SubmittedPlayerIds.Add(command.PlayerId);

            everyoneSubmitted = !round.IsCompleted
                && round.SubmittedPlayerIds.Count == game.Players.Count;

            // Marqué tout de suite dans le verrou : deux soumissions simultanées ne doivent pas
            // résoudre le tour deux fois.
            if (everyoneSubmitted) round.IsCompleted = true;
        }

        await _hubContext.Clients.Group(game.Id).PlayerSubmitted(player.Nickname);

        if (!everyoneSubmitted) return;

        // TODO US13-US16 : attribution des appels d'offres, avancement des contrats et formations,
        // salaires, puis bilan réel du tour.
        var result = new RoundResultDto
        {
            RoundId = round.Id,
            RoundOrder = round.Order
        };

        await _hubContext.Clients.Group(game.Id).RoundResolved(result);

        if (game.Rounds.Count >= game.RoundsNumber)
        {
            // TODO US17 : classement réel par chiffre d'affaires.
            await _hubContext.Clients.Group(game.Id).GameEnded(new RankingDto());
            return;
        }

        Round nextRound;

        lock (game)
        {
            nextRound = CreateRound(game);
        }

        await StartRound(game, nextRound);
    }

    // TODO US11 : le catalogue du tour (appels d'offres, formations) sera généré par RoundFactory.
    private static Round CreateRound(Game game)
    {
        var round = new Round
        {
            Game = game,
            Order = game.Rounds.Count + 1
        };

        game.Rounds.Add(round);

        return round;
    }

    private Task StartRound(Game game, Round round)
    {
        return _hubContext.Clients.Group(game.Id).RoundStarted(Mapper.ToDto(round));
    }
}
