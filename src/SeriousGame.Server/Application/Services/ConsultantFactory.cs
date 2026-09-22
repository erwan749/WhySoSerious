using Server.Domain;

namespace Server.Application.Services;

/// <summary>
/// Distribue les consultants de départ entre les entreprises d'une partie, à partir du référentiel
/// de cartes (AppMemory.ConsultantsSeed). Répartition équitable : même nombre de consultants par
/// entreprise, pioche mélangée pour ne pas toujours donner les mêmes cartes aux mêmes entreprises.
/// </summary>
public static class ConsultantFactory
{
    public const int ConsultantsPerCompany = 3;

    public static void AssignInitialStaff(IReadOnlyList<Company> companies, IReadOnlyList<ConsultantSeed> seeds, Random random)
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
            company.Staff.Add(CreateFromSeed(shuffledSeeds[i], company));
        }
    }

    public static Consultant CreateFromSeed(ConsultantSeed seed, Company company)
    {
        var consultant = new Consultant
        {
            Firstname = seed.Firstname,
            Lastname = seed.Lastname,
            Company = company
        };

        consultant.SetSalaryRequirement(seed.SalaryRequirement);

        foreach (var seedSkill in seed.Skills)
        {
            consultant.Skills.Add(new ConsultantSkill { Skill = seedSkill.Skill });
        }

        return consultant;
    }
}