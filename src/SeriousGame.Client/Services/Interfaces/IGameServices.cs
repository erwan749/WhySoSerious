using Shared.Models.Dtos;
using System.Diagnostics.Tracing;

namespace Client.Services.Interfaces;

// Contrat placeholder pour la future logique client du hub /game - vide tant que les actions en jeu
// (tours, appels d'offres, formations) ne sont pas implémentées. Reflète GameServices.
public interface IGameServices
{

    event Action<CompanyDto>? GameStarted;
    event Action<RoundDto>? RoundStarted;
    event Action<string>? PlayerSubmitted;
    event Action<RoundResultDto>? RoundResolved;
    event Action<RankingDto>? GameEnded;

    string HubUrl { get; }

    Task<bool> ConnectAsync();
    Task JoinGameRoomAsync(string gameId);
    Task<string?> ApplyToTenderAsync(string tenderId, ICollection<string> consultantIds);
    Task SubmitDecisionsAsync();
    Task DisconnectAsync();

}
