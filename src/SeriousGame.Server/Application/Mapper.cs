using Server.Domain;
using Server.Domain.Enums;
using Shared.Models.Dtos;

namespace Server.Application;

public static class Mapper
{
    public static PlayerDto ToDto(Player player) => new()
    {
        Id = player.Id,
        Nickname = player.Nickname,
        IsActive = player.IsActive
    };

    public static GameDto ToDto(Game game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        MinimumPlayers = game.MinimumPlayers,
        MaximumPlayers = game.MaximumPlayers,
        RoundsNumber = game.RoundsNumber,
        IsInProgress = game.IsInProgress,
        Owner = ToDto(game.Owner),
        Players = game.Players.Select(ToDto).ToList()
    };
    
    public static SkillDto ToDto(Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name
    };

    public static ConsultantSkillDto ToDto(ConsultantSkill consultantSkill) => new()
    {
        Skill = ToDto(consultantSkill.Skill),
        Level = ToDto(consultantSkill.Level)
    };

    public static ConsultantDto ToDto(Consultant consultant) => new()
    {
        Id = consultant.Id,
        FullName = consultant.FullName,
        SalaryRequirement = consultant.SalaryRequirement,
        Skills = consultant.Skills.Select(ToDto).ToList(),
        Status = ResolveStatus(consultant)
    };

    private static ConsultantStatus ResolveStatus(Consultant consultant)
    {
        var company = consultant.Company;
        
        if (!company.IsConsultantBusy(consultant)) return ConsultantStatus.Free;

        if (company.Contracts.Any(c => c.Status == ContractStatus.Active && c.AssignedConsultants.Contains(consultant)))
        {
            return ConsultantStatus.OnMission;
        }

        if (company.TrainingEnrollments.Any(e => e.Status == EnrollmentStatus.InProgress && e.Consultant == consultant))
        {
            return ConsultantStatus.InTraining;
        }

        return ConsultantStatus.Free;
    }

    public static CompanyDto ToDto(Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        OwnerId = company.PlayerOwner.Id,
        Treasury = company.Treasury,
        Revenue = company.Revenue,
        Staff = company.Staffs.Select(ToDto).ToList()
    };

    public static RequiredSkillDto ToDto(RequiredSkill requiredSkill) => new()
    {
        Skill = ToDto(requiredSkill.Skill),
        Level = ToDto(requiredSkill.Level)
    };

    public static TenderDto ToDto(Tender tender) => new()
    {
        Id = tender.Id,
        Name = tender.Name,
        RequiredSkills = tender.RequiredSkills.Select(ToDto).ToList(),
        Budget = tender.Budget,
        RoundsNumber = tender.RoundsNumber,
        RequiredConsultants = tender.RequiredConsultants
    };

    private static SkillLevel ToDto(Level level) => level switch
    {
        Level.Zero => SkillLevel.Zero,
        Level.Basic => SkillLevel.Basic,
        Level.Intermediate => SkillLevel.Intermediate,
        Level.Advanced => SkillLevel.Advanced,
        Level.Expert => SkillLevel.Expert,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    public static TrainingDto ToDto(Training training) => new()
    {
        Id = training.Id,
        Name = training.Name,
        Skill = ToDto(training.Skill),
        Cost = training.Cost,
        RoundsNumber = training.RoundsNumber
    };

    public static RoundDto ToDto(Round round) => new()
    {
        Id = round.Id,
        Order = round.Order,
        TotalRounds = round.Game.RoundsNumber,
        Tenders = round.Tenders.Select(ToDto).ToList(),
        Trainings = round.Trainings.Select(ToDto).ToList()
    };
    /// <summary>
    /// Projette une formation qui vient de s'achever : le consultant concerné et la compétence qui
    /// a monté d'un cran. La compétence est lue sur la formation, pas sur le consultant — ce dernier
    /// en possède plusieurs et rien n'y indique laquelle vient d'évoluer.
    /// </summary>
    public static TrainingOutcomeDto ToDto(TrainingEnrollment enrollment) => new()
    {
        ConsultantId = enrollment.Consultant.Id,
        ConsultantFullName = enrollment.Consultant.FullName,
        SkillName = enrollment.Training.Skill.Name
    };
    /// <summary>
    /// Projette l'issue d'un appel d'offres. <paramref name="winner"/> vaut <c>null</c> quand
    /// personne ne l'a emporté : aucun candidat, ou aucun candidat admissible. L'appel d'offres
    /// figure quand même au bilan — un marché resté désert est une information de jeu.
    /// </summary>
    private static TenderOutcomeDto ToOutcomeDto(Tender tender, TenderApplication? winner) => new()
    {
        TenderId = tender.Id,
        TenderName = tender.Name,
        WinningCompanyId = winner?.Company.Id
    };
    /// <summary>
    /// Assemble le bilan d'un tour résolu : l'issue de chaque appel d'offres, les formations qui
    /// viennent de s'achever et l'état à jour des entreprises. Les formations sont passées en
    /// paramètre plutôt que relues sur l'état : une inscription clôturée ne garde pas trace du tour
    /// où elle s'est terminée.
    /// </summary>
    public static RoundResultDto ToResultDto(
        Round round,
        IReadOnlyList<TrainingEnrollment> completedTrainings) => new()
        {
            RoundId = round.Id,
            RoundOrder = round.Order,
            TenderOutcomes = round.Tenders
            .Select(tender => ToOutcomeDto(
                tender,
                round.Applications.FirstOrDefault(application =>
                    application.Tender == tender && application.Status == ApplicationStatus.Won)))
            .ToList(),
            CompletedTrainings = completedTrainings.Select(ToDto).ToList(),
            Companies = round.Game.Companies.Select(ToDto).ToList()
        };
}
