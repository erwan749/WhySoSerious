using Server.Domain;
using Server.Domain.Enums;

namespace Server.Application.Services;

/// <summary>
/// Règle partagée : un consultant est disponible pour une nouvelle décision (candidature ou
/// formation) ce tour s'il n'est ni en mission active, ni déjà en formation, ni déjà engagé dans
/// une candidature en attente de résolution sur ce même tour.
/// </summary>
public static class ConsultantAvailability
{
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