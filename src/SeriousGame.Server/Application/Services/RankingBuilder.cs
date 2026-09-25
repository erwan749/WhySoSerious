using Server.Domain;
using Shared.Models.Dtos;

namespace Server.Application.Services;

public static class RankingBuilder
{

    public static RankingDto Build(IEnumerable<Company> companies)
    {
        var ranked = companies.OrderByDescending(c => c.Revenue).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        
        var entries = new List<RankingEntryDto>();

        var currentRank = 1;

        for (var i = 0; i < ranked.Count; i++)
        {
            if (i > 0 && ranked[i].Revenue != ranked[i - 1].Revenue) //permet que deux companies partagent le même rang
            {
                currentRank = i + 1;
            }
            entries.Add(new RankingEntryDto
            {
                Rank = currentRank,
                CompanyId =  ranked[i].Id,
                CompanyName = ranked[i].Name,
                Revenue = ranked[i].Revenue,
            });
        }

        return new RankingDto { Entries = entries };
    }
    
}