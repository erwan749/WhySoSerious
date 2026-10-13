using Server.Domain;

namespace Server.Application.Abstractions;

public interface IAwardStrategy
{

    /// <summary>
    /// Désigne la candidature gagnante parmi celles qui visent <paramref name="tender"/>,
    /// ou null si aucune n'est recevable.
    /// </summary>
    TenderApplication? SelectWinner(Tender tender, IReadOnlyList<TenderApplication> applications);

}
