using Client.Options;
using Client.Services.Interfaces;
using Client.State;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Abstractions;
using Shared.Models.Dtos;
using Shared.Models.Requests;

namespace Client.Services;

/// <summary>
/// flux d'une partie en cours : salle de jeu, décisions des joueurs, démarrage
/// </summary>

public class GameServices : IGameServices
{
    private readonly ClientSession _clientSession;
    private readonly ILogger<GameServices> _logger;
    private readonly HubConnection _gameConnection;
    private string? _currentRoundId;
    public string HubUrl { get; }

    public GameServices(IOptions<WebSocketServerOptions> webSocketServerOptions, ClientSession clientSession, ILogger<GameServices> logger)
    {
        _clientSession = clientSession;
        _logger = logger;
        var options = webSocketServerOptions.Value;
        HubUrl = $"{options.Scheme}://{options.Domain}:{options.Port}{HubRoutes.Game}";
        _gameConnection = new HubConnectionBuilder()
             .WithUrl(HubUrl)
             .WithAutomaticReconnect()
             .Build();
        RegisterHandlers();
    }

    public event Action<CompanyDto>? GameStarted;
    public event Action<RoundDto>? RoundStarted;
    public event Action<string>? PlayerSubmitted;
    public event Action<RoundResultDto>? RoundResolved;
    public event Action<RankingDto>? GameEnded;

    public async Task<bool> ConnectAsync()
    {
        try
        {
            await _gameConnection.StartAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec de connexion au hub {HubUrl}", HubUrl);
            return false;
        }
    }

    public Task DisconnectAsync()
    {
       return _gameConnection.StopAsync();
    }

    public Task JoinGameRoomAsync(string gameId)
    {
        return _gameConnection.InvokeAsync(nameof(IGameHubServer.JoinGameRoom), gameId, _clientSession.PlayerId);
    }

    public async Task SubmitDecisionsAsync()
    {
        if (_currentRoundId is null) 
        {
            _logger.LogWarning("Aucun tour en cours, soumission ignorée"); 
            return; 
        }
        var command = new SubmitDecisionsCommand
        {
            PlayerId = _clientSession.PlayerId,
            RoundId = _currentRoundId
        };
        await _gameConnection.InvokeAsync(nameof(IGameHubServer.SubmitDecisions), command);
    }

    private void RegisterHandlers()
    {
        _gameConnection.On<RoundDto>(nameof(IGameHubClient.RoundStarted), round =>
        {
            _currentRoundId = round.Id;
            RoundStarted?.Invoke(round);
        });
        
        _gameConnection.On<ICollection<CompanyDto>>(nameof(IGameHubClient.GameStarted), companies =>
        {
            var myCompany = companies.FirstOrDefault(c => c.OwnerId == _clientSession.PlayerId);
            if (myCompany is not null) GameStarted?.Invoke(myCompany);
        });

        _gameConnection.On<string>(nameof(IGameHubClient.PlayerSubmitted), nickname =>
        {
            PlayerSubmitted?.Invoke(nickname);
        });

        _gameConnection.On<RoundResultDto>(nameof(IGameHubClient.RoundResolved), result =>
        {
            RoundResolved?.Invoke(result);
        });

        _gameConnection.On<RankingDto>(nameof(IGameHubClient.GameEnded), ranking =>
        {
            GameEnded?.Invoke(ranking);
        });
    }
}