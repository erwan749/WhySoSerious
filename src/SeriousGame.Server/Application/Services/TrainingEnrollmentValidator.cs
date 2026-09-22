using Server.Application.Abstractions;
using Server.Domain;

namespace Server.Application.Services;

/// <summary>
/// Règles de validation d'une inscription à une formation (US08). Pure, testable sans dépendance.
/// </summary>
public static class TrainingEnrollmentValidator
{
    public static CommandResult Validate(Round round, Company company, Training training, Consultant consultant)
    {
        if (!round.Trainings.Contains(training))
        {
            return CommandResult.Fail("Cette formation n'est pas proposée ce tour.");
        }

        if (consultant.Company != company)
        {
            return CommandResult.Fail($"{consultant.FullName} n'appartient pas à cette entreprise.");
        }

        if (!ConsultantAvailability.IsFree(round, company, consultant))
        {
            return CommandResult.Fail($"{consultant.FullName} n'est pas disponible ce tour.");
        }

        if (company.Treasury < training.Cost)
        {
            return CommandResult.Fail("Trésorerie insuffisante pour financer cette formation.");
        }

        return CommandResult.Ok();
    }
}