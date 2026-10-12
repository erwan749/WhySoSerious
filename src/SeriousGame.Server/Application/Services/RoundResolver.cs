using Server.Application.Abstractions;
using Server.Domain;
using Server.Domain.Enums;

namespace Server.Application.Services;

public class RoundResolver
{
    private readonly IAwardStrategy _awardStrategy;
    private readonly ILogger<RoundResolver> _logger;
    public RoundResolver(IAwardStrategy awardStrategy,ILogger<RoundResolver> logger)
    {
        _awardStrategy = awardStrategy;
        _logger = logger;
    }
    /// <summary>
    /// Attribue tous les appels d'offres du tour : pour chacun, la stratégie désigne la candidature
    /// gagnante, qui devient un contrat actif immobilisant ses consultants ; les autres sont perdues.
    /// </summary>
    public void ResolveTenders(Round round)
    {
        foreach (Tender tender in round.Tenders)
        {
            List<TenderApplication> applications = round.Applications
                .Where(application => application.Tender == tender
                                      && application.Status == ApplicationStatus.Pending)
                .ToList();

            if (applications.Count == 0) continue;

            TenderApplication? winner = _awardStrategy.SelectWinner(tender, applications);

            foreach (TenderApplication application in applications)
            {
                application.Status = application == winner
                    ? ApplicationStatus.Won
                    : ApplicationStatus.Lost;
            }

            if (winner is null)
            {
                _logger.LogInformation(
                    "Tender {TenderName} drew {ApplicationCount} application(s), none admissible",
                    tender.Name, applications.Count);

                continue;
            }

            winner.Company.Contracts.Add(new Contract
            {
                Company = winner.Company,
                Tender = tender,
                // Copie : le contrat et la candidature ont des cycles de vie distincts.
                AssignedConsultants = [.. winner.AssignedConsultants],
                RemainingRounds = tender.RoundsNumber
            });
        }
    }
}
