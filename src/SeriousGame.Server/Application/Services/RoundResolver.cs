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
    /// <summary>
    /// Fait vivre la partie d'un tour : décrémente la durée restante de chaque contrat actif et de
    /// chaque formation en cours. Un contrat arrivé à terme verse son budget à l'entreprise — en
    /// trésorerie et en chiffre d'affaires — et libère ses consultants ; une formation terminée fait
    /// monter d'un cran la compétence enseignée. Les inscriptions prises à <paramref name="round"/>
    /// sont ignorées : comme un contrat, une formation ne commence qu'au tour suivant.
    /// </summary>
    public void AdvanceContractsAndTrainings(Game game, Round round)
    {
        foreach (Company company in game.Companies)
        {
            foreach (Contract contract in company.Contracts.Where(c => c.Status == ContractStatus.Active))
            {
                contract.RemainingRounds--;

                if (contract.RemainingRounds > 0) continue;

                // Alimente trésorerie et chiffre d'affaires : c'est ce dernier qui classe à la fin.
                company.RecordContractRevenue(contract.Tender.Budget);
                contract.Status = ContractStatus.Completed;
            }

            foreach (TrainingEnrollment enrollment in company.TrainingEnrollments.Where(e => e.Status == EnrollmentStatus.InProgress))
            {
                // Inscription prise à ce tour : elle ne commence qu'au suivant, comme un contrat.
                if (enrollment.StartedAtRound == round.Order) continue;

                enrollment.RemainingRounds--;

                if (enrollment.RemainingRounds > 0) continue;

                ConsultantSkill? consultantSkill = enrollment.Consultant.Skills
                    .FirstOrDefault(skill => skill.Skill.Id == enrollment.Training.Skill.Id);

                if (consultantSkill is null)
                {
                    // Le validateur l'interdit à l'inscription : si on passe ici, c'est un bug ailleurs.
                    _logger.LogWarning(
                        "Training {TrainingName} completed for {ConsultantName}, who no longer has the skill",
                        enrollment.Training.Name, enrollment.Consultant.FullName);
                }
                else
                {
                    consultantSkill.LevelUp();
                }

                enrollment.Status = EnrollmentStatus.Completed;
            }
        }
    }
    /// <summary>
    /// Prélève la masse salariale de chaque entreprise de la partie. Les salaires sont dus pour
    /// tout le staff, qu'il soit en mission, en formation ou sur le banc : c'est ce qui rend
    /// coûteux un consultant inoccupé.
    /// </summary>
    public void PayDaysForStaffs(Game game)
    {
        foreach (Company company in game.Companies)
        {
            var wasBankrupt = company.IsBankrupt;
            var payroll = company.GetPayroll();

            company.RecordSalaryPayment();

            _logger.LogInformation(
                "Company {CompanyName} paid {Payroll} in salaries, treasury is now {Treasury}",
                company.Name, payroll, company.Treasury);

            // Journalisé sur la transition, pas sur l'état : sinon l'avertissement se répéterait
            // à chaque tour jusqu'à la fin de la partie.
            if (!wasBankrupt && company.IsBankrupt)
            {
                _logger.LogWarning(
                    "Company {CompanyName} went bankrupt: treasury is {Treasury}",
                    company.Name, company.Treasury);
            }
        }
    }
}
