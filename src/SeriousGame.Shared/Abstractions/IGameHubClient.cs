using Shared.Models.Dtos;
namespace Shared.Abstractions;

/// <summary>
/// événement que le serveur envoie
/// pendant une partie
/// </summary>

public interface IGameHubClient
{
    Task RoundStarted(RoundDto roundDto);
    Task PlayerSubmitted(string nickName);
    Task RoundResolved(RoundResultDto roundResultDto);
    Task GameEnded(RankingDto rankingDto);
}
