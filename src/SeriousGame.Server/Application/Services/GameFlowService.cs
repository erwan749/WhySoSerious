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

    public Task<CommandResult> ApplyToTender(ApplyToTenderCommand command)
    {
        var game = _gameService.GetGameByRoundId(command.RoundId);
        var round = game?.Rounds.FirstOrDefault(r => r.Id == command.RoundId);
        var company = game?.Companies.FirstOrDefault(c => c.PlayerOwner.Id == command.PlayerId);
        var tender = round?.Tenders.FirstOrDefault(t => t.Id == command.TenderId);

        if (game is null || round is null || company is null || tender is null)
        {
            _logger.LogWarning(
                "Invalid ApplyToTender: round {RoundId}, player {PlayerId}, tender {TenderId}",
                command.RoundId, command.PlayerId, command.TenderId);
            return Task.FromResult(CommandResult.Fail("Candidature invalide."));
        }

        lock (game)
        {
            var consultants = company.Staffs.Where(c => command.ConsultantIds.Contains(c.Id)).ToList();

            if (consultants.Count != command.ConsultantIds.Count)
            {
                return Task.FromResult(CommandResult.Fail("Un ou plusieurs consultants sont introuvables."));
            }

            var validation = TenderApplicationValidator.Validate(round, company, tender, consultants);

            if (!validation.Success)
            {
                return Task.FromResult(validation);
            }

            round.Applications.Add(new TenderApplication
            {
                Round = round,
                Company = company,
                Tender = tender,
                AssignedConsultants = consultants
            });
        }

        return Task.FromResult(CommandResult.Ok());
    }

    public Task<CommandResult> EnrollInTraining(EnrollInTrainingCommand command)
    {
        var game = _gameService.GetGameByRoundId(command.RoundId);
        var round = game?.Rounds.FirstOrDefault(r => r.Id == command.RoundId);
        var company = game?.Companies.FirstOrDefault(c => c.PlayerOwner.Id == command.PlayerId);
        var training = round?.Trainings.FirstOrDefault(t => t.Id == command.TrainingId);
        var consultant = company?.Staffs.FirstOrDefault(c => c.Id == command.ConsultantId);

        if (game is null || round is null || company is null || training is null || consultant is null)
        {
            _logger.LogWarning(
                "Invalid EnrollInTraining: round {RoundId}, player {PlayerId}, training {TrainingId}, consultant {ConsultantId}",
                command.RoundId, command.PlayerId, command.TrainingId, command.ConsultantId);
            return Task.FromResult(CommandResult.Fail("Inscription invalide."));
        }

        lock (game)
        {
            var validation = TrainingEnrollmentValidator.Validate(round, company, training, consultant);

            if (!validation.Success)
            {
                return Task.FromResult(validation);
            }

            company.Withdraw(training.Cost);

            company.TrainingEnrollments.Add(new TrainingEnrollment
            {
                Company = company,
                Consultant = consultant,
                Training = training,
                RemainingRounds = training.RoundsNumber
            });
        }

        return Task.FromResult(CommandResult.Ok());
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

        ConsultantFactory.AssignInitialStaff(game.Companies.ToList(), _appMemory.ConsultantsSeed, _appMemory.Skills, Random.Shared);

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
    /// <summary>
    /// delegue la construction du catalogue a RoundFactory
    /// </summary>

    private Round CreateRound(Game game) => RoundFactory.Create(game, _appMemory.TenderSeeds, _gameOptions, Random.Shared);

    private Task StartRound(Game game, Round round)
    {
        return _hubContext.Clients.Group(game.Id).RoundStarted(Mapper.ToDto(round));
    }
}
