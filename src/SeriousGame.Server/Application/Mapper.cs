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

    public static ConsultantDto ToDto(Consultant consultant) => new()
    {
        Id = consultant.Id,
        FullName = consultant.FullName,
        SalaryRequirement = consultant.SalaryRequirement,
        Skills = [], // TODO US05 : mapper consultant.Skills une fois ConsultantSkill en place
        // IsBusy = false à faire seulement à parit de l'US06// TODO US06 : calculé depuis les Contracts / TrainingEnrollments actifs
    };

    public static CompanyDto ToDto(Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        OwnerId = company.PlayerOwner.Id,
        Treasury = company.Treasury,
        Revenue = company.Revenue,
        Staff = company.Staff.Select(ToDto).ToList()
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
        RoundsNumber = tender.RoundsNumber
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
}
