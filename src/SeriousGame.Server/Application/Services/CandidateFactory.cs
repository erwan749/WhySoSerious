namespace Server.Application.Services;

using Server.Domain;
using Server.Options;
    
/// <summary>Tire les candidats au recrutement d'un tour, avec salaire et compétences aléatoires.</summary>
public static class CandidateFactory
{
    public static ICollection<Candidate> Build(
        Game game,
        IReadOnlyList<ConsultantSeed> candidateSeeds,
        IReadOnlyList<Skill> skillCatalog,
        GameOptions options,
        Random random)
    {
        var usedNames = game.Rounds
            .SelectMany(round => round.Candidates)
            .Select(candidate => candidate.FullName)
            .ToHashSet();

        var pool = candidateSeeds
            .Where(seed => !usedNames.Contains($"{seed.Firstname} {seed.Lastname}"))
            .ToList();

        if (pool.Count < options.CandidateNumberPerRound) pool = candidateSeeds.ToList();

        var candidates = new List<Candidate>();

        foreach (var seed in pool.OrderBy(_ => random.Next()).Take(options.CandidateNumberPerRound))
        {
            var candidate = new Candidate
            {
                Firstname = seed.Firstname,
                Lastname = seed.Lastname,
                SalaryRequirement = ConsultantFactory.DrawSalary(random)
            };

            foreach (var consultantSkill in ConsultantFactory.DrawSkills(skillCatalog, random))
            {
                candidate.Skills.Add(consultantSkill);
            }

            candidates.Add(candidate);
        }

        return candidates;
    }
}