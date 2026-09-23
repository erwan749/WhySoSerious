using Server.Domain;

namespace Server.Application.Services;

/// <summary>
/// Distribue les consultants de départ entre les entreprises d'une partie, à partir des noms de
/// AppMemory.ConsultantsSeed. Salaire et compétences sont tirés au hasard à chaque partie, pour que
/// deux parties successives ne donnent jamais le même staff de départ.
/// </summary>
public static class ConsultantFactory
{
    public const int ConsultantsPerCompany = 3;
    private const int MinSalaryStep = 30; // ×100 → 3 000
    private const int MaxSalaryStep = 50; // ×100 → 5 000
    private const int MinSkillsPerConsultant = 1;
    private const int MaxSkillsPerConsultant = 2;
    private const int MinLevelUpOnStart = 1;
    private const int MaxLevelUpOnStart = 2;


    public static void AssignInitialStaff(
        IReadOnlyList<Company> companies,
        IReadOnlyList<ConsultantSeed> seeds,
        IReadOnlyList<Skill> skillCatalog,
        Random random)
    {
        var requiredCount = companies.Count * ConsultantsPerCompany;

        if (seeds.Count < requiredCount)
        {
            throw new InvalidOperationException(
                $"Pas assez de consultants dans le référentiel ({seeds.Count}) pour {companies.Count} entreprises ({requiredCount} requis).");
        }

        var shuffledSeeds = seeds.OrderBy(_ => random.Next()).Take(requiredCount).ToList();

        for (var i = 0; i < requiredCount; i++)
        {
            var company = companies[i % companies.Count];
            company.Staffs.Add(CreateFromSeed(shuffledSeeds[i], skillCatalog, company, random));
        }
    }

    public static Consultant CreateFromSeed(ConsultantSeed seed, IReadOnlyList<Skill> skillCatalog, Company company, Random random)
    {
        var consultant = new Consultant
        {
            Firstname = seed.Firstname,
            Lastname = seed.Lastname,
            Company = company
        };

        consultant.SetSalaryRequirement(random.Next(MinSalaryStep, MaxSalaryStep + 1) * 100);

        var skillCount = random.Next(MinSkillsPerConsultant, MaxSkillsPerConsultant + 1);
        var randomSkills = skillCatalog.OrderBy(_ => random.Next()).Take(skillCount);

        foreach (var skill in randomSkills)
        {
            var cSkill = new ConsultantSkill { Skill = skill };
            var randomLevel = random.Next(MinLevelUpOnStart, MaxLevelUpOnStart + 1);
            for (var i = 0; i < randomLevel; i++)
            {
                cSkill.LevelUp();
            }
            consultant.Skills.Add(cSkill);

        }

        return consultant;
    }
}