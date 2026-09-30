using Server.Application.Abstractions;
using Server.Domain;
using Server.Domain.Enums;

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
        var consultantSkill = consultant.Skills.FirstOrDefault(s => s.Skill == training.Skill);

        if (consultantSkill is null)
        {
            return CommandResult.Fail($"{consultant.FullName} ne maîtrise pas {training.Skill.Name}.");
        }

        if (consultantSkill.Level == Level.Expert)
        {
            return CommandResult.Fail($"{consultant.FullName} est déjà au niveau maximum en {training.Skill.Name}.");
        }
        return CommandResult.Ok();
    }
}