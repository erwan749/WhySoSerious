using Server.Application.Abstractions;
using Server.Domain;
using Server.Domain.Enums;

namespace Server.Application.Services;

/// <summary>
/// Règles de validation d'une candidature à un appel d'offres (US07). Pure, sans dépendance à un
/// hub, un repository ou une connexion réseau — juste les objets du domaine, donc testable seule.
/// </summary>
public static class TenderApplicationValidator
{
    public static CommandResult Validate(Round round, Company company, Tender tender, ICollection<Consultant> consultants)
    {
        if (!round.Tenders.Contains(tender))
        {
            return CommandResult.Fail("Cet appel d'offres n'est pas proposé ce tour.");
        }

        if (consultants.Count == 0)
        {
            return CommandResult.Fail("Il faut affecter au moins un consultant.");
        }

        foreach (var consultant in consultants)
        {
            if (consultant.Company != company)
            {
                return CommandResult.Fail($"{consultant.FullName} n'appartient pas à cette entreprise.");
            }

            if (!ConsultantAvailability.IsFree(round, company, consultant))
            {
                return CommandResult.Fail($"{consultant.FullName} n'est pas disponible ce tour.");
            }
        }

        return CommandResult.Ok();
    }

    public static bool IsFree(Round round, Company company, Consultant consultant)
    {
        var busyOnContract = company.Contracts.Any(c =>
            c.Status == ContractStatus.Active && c.AssignedConsultants.Contains(consultant));

        var busyInTraining = company.TrainingEnrollments.Any(e =>
            e.Status == EnrollmentStatus.InProgress && e.Consultant == consultant);

        var busyOnPendingApplication = round.Applications.Any(a =>
            a.Status == ApplicationStatus.Pending && a.AssignedConsultants.Contains(consultant));

        return !busyOnContract && !busyInTraining && !busyOnPendingApplication;
    }
}