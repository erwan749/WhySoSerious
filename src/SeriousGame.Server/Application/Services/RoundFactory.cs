using Server.Domain;
using Server.Domain.Enums;
using Server.Options;

namespace Server.Application.Services;

/// <summary>
/// Elle construit le catalogue d'un tour
/// appels d'offres et formations
/// à partir du staff réellement en jeu, de façon qu'un appel d'offres soit toujours réalisable par au moins une entreprise, 
/// et que la difficulté suive la montée en compétence des joueurs.
/// </summary>

public static class RoundFactory
{
    // Paliers de difficulté : numéro du tour à partir duquel les exigences augmentent.
    private const int IntermediateTierFirstRound = 3;
    private const int AdvancedTierFirstRound = 5;

    // Main-d'œuvre exigée, par palier.
    private const int StarterMinConsultants = 1;
    private const int StarterMaxConsultants = 1;
    private const int IntermediateMinConsultants = 1;
    private const int IntermediateMaxConsultants = 2;
    private const int AdvancedMinConsultants = 2;
    private const int AdvancedMaxConsultants = 3;

    // Durée d'un appel d'offres, en tours. Un contrat long est rentable mais immobilise l'équipe.
    private const int StarterMinDuration = 1;
    private const int IntermediateMinDuration = 2;
    private const int AdvancedMinDuration = 3;
    private const int MaxDuration = 4;

    /// <summary>
    /// Crée le tour suivant de la partie, catalogue compris, et l'ajoute à <paramref name="game"/>.
    /// </summary>
    public static Round Create(
        Game game,
        IReadOnlyList<TenderSeed> tenderSeeds,
        GameOptions options,
        Random random)
    {
        var round = new Round
        {
            Game = game,
            Order = game.Rounds.Count + 1
        };

        var companies = game.Companies.ToList();

        var usedNames = game.Rounds
            .SelectMany(r => r.Tenders)
            .Select(t => t.Name)
            .ToHashSet();

        var tier = TierFor(round.Order);
        var remainingRounds = game.RoundsNumber - round.Order + 1;
        var tenderCount = options.TenderNumberPerPlayer * game.Players.Count;

        var companyOffset = companies.Count == 0 ? 0 : random.Next(companies.Count);

        for (var i = 0; i < tenderCount && companies.Count > 0 && remainingRounds > 0; i++)
        {
            var company = companies[(companyOffset + i) % companies.Count];

            var tender = BuildTender(company, tier, remainingRounds, tenderSeeds, usedNames, options, random);

            if (tender is null) continue;

            usedNames.Add(tender.Name);
            round.Tenders.Add(tender);
        }


        game.Rounds.Add(round);

        return round;
    }

    /// <summary>
    /// Bâtit un appel d'offres sur une équipe réellement disponible chez <paramref name="company"/> :
    /// la main-d'œuvre ne dépasse jamais ses consultants libres, et les compétences exigées sont les
    /// leurs. Renvoie null si l'entreprise n'a personne à proposer.
    /// </summary>
    private static Tender? BuildTender(
        Company company,
        (int MinConsultants, int MaxConsultants, int MinDuration) tier,
        int remainingRounds,
        IReadOnlyList<TenderSeed> tenderSeeds,
        IReadOnlySet<string> usedNames,
        GameOptions options,
        Random random)
    {
        var availableConsultants = company.GetAvailableConsultants()
            .Where(consultant => consultant.Skills.Count > 0)
            .ToList();

        if (availableConsultants.Count == 0) return null;

        var requiredConsultants = Math.Min(
            random.Next(tier.MinConsultants, tier.MaxConsultants + 1),
            availableConsultants.Count);

        var team = availableConsultants
            .OrderBy(_ => random.Next())
            .Take(requiredConsultants)
            .ToList();

        var requiredSkills = team
            .Select(consultant => consultant.Skills.ElementAt(random.Next(consultant.Skills.Count)))
            // Deux consultants sur la même compétence ne font qu'une exigence, au niveau le plus haut.
            .GroupBy(consultantSkill => consultantSkill.Skill)
            .Select(group => new RequiredSkill
            {
                Skill = group.Key,
                Level = RequiredLevelFor(group.Max(consultantSkill => consultantSkill.Level), random)
            })
            .ToList();

        // En fin de partie, un contrat court vaut mieux qu'aucun contrat.
        var maxDuration = Math.Min(MaxDuration, remainingRounds);
        var duration = maxDuration <= tier.MinDuration
            ? maxDuration
            : random.Next(tier.MinDuration, maxDuration + 1);

        var tender = new Tender
        {
            Name = DrawName(tenderSeeds, usedNames, random),
            RequiredConsultants = requiredConsultants,
            RoundsNumber = duration,
            // Division en dernier : faite avant, l'arrondi entier avalerait la marge.
            Budget = requiredConsultants * duration
                     * options.SalaryReferencePerConsultant
                     * options.TenderMarginPercent / 100
        };

        foreach (var requiredSkill in requiredSkills)
        {
            tender.RequiredSkills.Add(requiredSkill);
        }

        return tender;
    }

    /// <summary>
    /// Niveau exigé : celui que le consultant possède, ou un cran en dessous, jamais sous Basic.
    /// Descendre d'un cran laisse une chance aux concurrents de candidater aussi.
    /// </summary>
    private static Level RequiredLevelFor(Level ownedLevel, Random random)
    {
        if (ownedLevel <= Level.Basic) return Level.Basic;

        return random.Next(2) == 0 ? ownedLevel : ownedLevel - 1;
    }

    /// <summary>Tire un nom non encore servi ; réutilise la réserve si elle est épuisée.</summary>
    private static string DrawName(
        IReadOnlyList<TenderSeed> tenderSeeds,
        IReadOnlySet<string> usedNames,
        Random random)
    {
        var available = tenderSeeds.Where(seed => !usedNames.Contains(seed.Name)).ToList();
        var pool = available.Count > 0 ? available : tenderSeeds;

        return pool[random.Next(pool.Count)].Name;
    }
    /// <summary>
    /// Bornes d'exigence applicables au tour <paramref name="roundOrder"/> : plus la partie avance,
    /// plus les appels d'offres réclament de monde et de temps.
    /// </summary>
    private static (int MinConsultants, int MaxConsultants, int MinDuration) TierFor(int roundOrder)
    {
        if (roundOrder >= AdvancedTierFirstRound)
        {
            return (AdvancedMinConsultants, AdvancedMaxConsultants, AdvancedMinDuration);
        }

        if (roundOrder >= IntermediateTierFirstRound)
        {
            return (IntermediateMinConsultants, IntermediateMaxConsultants, IntermediateMinDuration);
        }

        return (StarterMinConsultants, StarterMaxConsultants, StarterMinDuration);
    }

}
