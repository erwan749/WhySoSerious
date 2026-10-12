using Server.Domain;

namespace Server.Application.Services;

public static class TenderCoverage
{
    /// <summary>
    /// Pour chaque compétence présente dans l'équipe engagée, retourne le <see cref="ConsultantSkill"/>
    /// de plus haut niveau : une seule entrée par compétence, sans doublon.
    /// </summary>
    private static List<ConsultantSkill> GetConsultantsSkills(TenderApplication application) => 
        application.AssignedConsultants.SelectMany(c => c.Skills).GroupBy(s => s.Skill.Id).Select(g => g.MaxBy(s => s.Level)!).ToList();

    /// <summary>
    /// Une candidature est recevable si elle engage assez de consultants et si chaque compétence
    /// exigée est couverte, au niveau demandé ou au-dessus, par au moins un consultant engagé.
    /// </summary>
    public static bool IsAdmissible(Tender tender, TenderApplication application)
    {
        if (!tender.RequiredSkills.Any() || 
            tender.RequiredConsultants > application.AssignedConsultants.Count) 
            return false;

        List<ConsultantSkill> skills = GetConsultantsSkills(application);

        return tender.RequiredSkills.All(rs => skills.Any(cs => cs.Skill.Id == rs.Skill.Id && cs.Level >= rs.Level));
    }

    /// <summary>
    /// Somme des dépassements de niveau sur les compétences exigées : plus l'équipe engagée est
    /// qualifiée au-delà du minimum, plus le score est élevé.
    /// </summary>
    public static int CoverageScore(Tender tender, TenderApplication application)
    {
        List<ConsultantSkill> skills = GetConsultantsSkills(application);

        return tender.RequiredSkills.Sum(rs => skills.First(cs => cs.Skill.Id == rs.Skill.Id).Level- rs.Level);
    }
}
