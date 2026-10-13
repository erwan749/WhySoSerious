using Server.Application.Abstractions;
using Server.Domain;

namespace Server.Application.Services;

/// <summary>
/// Critère d'attribution « meilleure couverture » : parmi les candidatures recevables, assez de
/// consultants engagés et toutes les compétences exigées couvertes au niveau demandé, l'emporte
/// celle dont l'équipe dépasse le plus les niveaux exigés. Récompense donc la formation des
/// consultants, pas le nombre de personnes engagées, puisque le score retient le meilleur niveau
/// par compétence. À score égal, la candidature la plus ancienne gagne, pour que le résultat d'un
/// tour soit toujours reproductible.
/// </summary>
public sealed class BestCoverageAwardStrategy : IAwardStrategy
{
    public TenderApplication? SelectWinner(Tender tender, IReadOnlyList<TenderApplication> applications)
    {
        return applications.Where(a => TenderCoverage.IsAdmissible(tender, a)).OrderByDescending(a => TenderCoverage.CoverageScore(tender, a)).FirstOrDefault();
    }
}
