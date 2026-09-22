using System.Collections.Concurrent;
using Server.Domain;
using Server.Domain.Enums;

namespace Server.Infrastructure;

/// <summary>
/// État en mémoire du serveur, partagé entre tous les appels de hub (enregistré en singleton).
/// Les collections sont concurrentes : plusieurs connexions SignalR peuvent muter Players/Games
/// en parallèle. Clé = Id de l'entité, donc réenregistrer une même identité remplace au lieu de dupliquer.
/// </summary>
public class AppMemory
{
    public ConcurrentDictionary<string, Player> Players { get; } = new();
    public ConcurrentDictionary<string, Game> Games { get; } = new();

    // Référentiel de compétences (donnée de seed, en lecture seule).
    public IReadOnlyList<Skill> Skills { get; } =
    [
        new Skill { Id = 1, Name = "HTML" },
        new Skill { Id = 2, Name = "CSS" },
        new Skill { Id = 3, Name = "JavaScript" },
        new Skill { Id = 4, Name = "TypeScript" },
        new Skill { Id = 5, Name = "React" },
        new Skill { Id = 6, Name = "Angular" },
        new Skill { Id = 7, Name = "Vue.js" },
        new Skill { Id = 8, Name = "Node.js" },
        new Skill { Id = 9, Name = "Express.js" },
        new Skill { Id = 10, Name = "ASP.NET Core" },
        new Skill { Id = 11, Name = "Ruby on Rails" },
        new Skill { Id = 12, Name = "Django" },
        new Skill { Id = 13, Name = "Flask" },
        new Skill { Id = 14, Name = "PHP" },
        new Skill { Id = 15, Name = "Laravel" },
        new Skill { Id = 16, Name = "Spring Boot" },
        new Skill { Id = 17, Name = "SQL" },
        new Skill { Id = 18, Name = "NoSQL" },
        new Skill { Id = 19, Name = "GraphQL" },
        new Skill { Id = 20, Name = "REST APIs" }
    ];

    // Un exemple de chaque type de donnée de seed, pour valider le format retenu (US03).
    // Tender/Training sont immuables et réutilisables tels quels entre parties (comme Skill).
    // ConsultantSeed est un blueprint, jamais un Consultant réel (voir ConsultantSeed.cs).
    // Le catalogue complet suivra en US10 (Piste B) et les consultants de départ en US05 (Piste A).
    public IReadOnlyList<Tender> TendersSeed { get; }
    public IReadOnlyList<Training> TrainingsSeed { get; }
    public IReadOnlyList<ConsultantSeed> ConsultantsSeed { get; }

    public AppMemory()
    {
        TendersSeed =
        [
            new Tender
            {
                Name = "Refonte du site vitrine",
                Budget = 15_000,
                RoundsNumber = 2,
                RequiredSkills = { new RequiredSkill { Skill = Skills[2], Level = Level.Intermediate } } // JavaScript
            }
        ];

        TrainingsSeed =
        [
            new Training
            {
                Name = "Initiation à React",
                Skill = Skills[4], // React
                Cost = 800,
                RoundsNumber = 1
            }
        ];

        ConsultantsSeed =
        [
            /*new ConsultantSeed
            {
                Firstname = "Ada",
                Lastname = "Lovelace",
                SalaryRequirement = 4_000,
                Skills = [ new ConsultantSkill { Skill = Skills[2] } ] // JavaScript, niveau Zero par défaut
            }*/
            
            new ConsultantSeed { Firstname = "Alice", Lastname = "Martin", SalaryRequirement = 3_200, Skills = [ new ConsultantSkill { Skill = Skills[0] }, new ConsultantSkill { Skill = Skills[1] } ] }, // HTML, CSS
            new ConsultantSeed { Firstname = "Bruno", Lastname = "Costa", SalaryRequirement = 3_500, Skills = [ new ConsultantSkill { Skill = Skills[2] } ] }, // JavaScript
            new ConsultantSeed { Firstname = "Chloé", Lastname = "Nguyen", SalaryRequirement = 4_200, Skills = [ new ConsultantSkill { Skill = Skills[3] }, new ConsultantSkill { Skill = Skills[4] } ] }, // TypeScript, React
            new ConsultantSeed { Firstname = "David", Lastname = "Okafor", SalaryRequirement = 3_800, Skills = [ new ConsultantSkill { Skill = Skills[5] } ] }, // Angular
            new ConsultantSeed { Firstname = "Elena", Lastname = "Rossi", SalaryRequirement = 3_600, Skills = [ new ConsultantSkill { Skill = Skills[6] } ] }, // Vue.js
            new ConsultantSeed { Firstname = "Farid", Lastname = "Haddad", SalaryRequirement = 4_000, Skills = [ new ConsultantSkill { Skill = Skills[7] }, new ConsultantSkill { Skill = Skills[8] } ] }, // Node.js, Express.js
            new ConsultantSeed { Firstname = "Grace", Lastname = "Kim", SalaryRequirement = 4_500, Skills = [ new ConsultantSkill { Skill = Skills[9] } ] }, // ASP.NET Core
            new ConsultantSeed { Firstname = "Hugo", Lastname = "Meyer", SalaryRequirement = 3_700, Skills = [ new ConsultantSkill { Skill = Skills[10] } ] }, // Ruby on Rails
            new ConsultantSeed { Firstname = "Inès", Lastname = "Dubois", SalaryRequirement = 3_900, Skills = [ new ConsultantSkill { Skill = Skills[11] } ] }, // Django
            new ConsultantSeed { Firstname = "Jonas", Lastname = "Lindqvist", SalaryRequirement = 3_400, Skills = [ new ConsultantSkill { Skill = Skills[12] } ] }, // Flask
            new ConsultantSeed { Firstname = "Karim", Lastname = "Belkacem", SalaryRequirement = 3_100, Skills = [ new ConsultantSkill { Skill = Skills[13] } ] }, // PHP
            new ConsultantSeed { Firstname = "Léa", Lastname = "Fontaine", SalaryRequirement = 3_300, Skills = [ new ConsultantSkill { Skill = Skills[14] } ] }, // Laravel
            new ConsultantSeed { Firstname = "Marco", Lastname = "Silva", SalaryRequirement = 4_600, Skills = [ new ConsultantSkill { Skill = Skills[15] } ] }, // Spring Boot
            new ConsultantSeed { Firstname = "Nadia", Lastname = "Petrova", SalaryRequirement = 3_000, Skills = [ new ConsultantSkill { Skill = Skills[16] } ] }, // SQL
            new ConsultantSeed { Firstname = "Omar", Lastname = "El-Amin", SalaryRequirement = 3_400, Skills = [ new ConsultantSkill { Skill = Skills[17] } ] }, // NoSQL
            new ConsultantSeed { Firstname = "Priya", Lastname = "Sharma", SalaryRequirement = 4_300, Skills = [ new ConsultantSkill { Skill = Skills[18] }, new ConsultantSkill { Skill = Skills[19] } ] }, // GraphQL, REST APIs
            new ConsultantSeed { Firstname = "Quentin", Lastname = "Roy", SalaryRequirement = 3_300, Skills = [ new ConsultantSkill { Skill = Skills[0] }, new ConsultantSkill { Skill = Skills[2] } ] }, // HTML, JavaScript
            new ConsultantSeed { Firstname = "Rania", Lastname = "Saidi", SalaryRequirement = 3_900, Skills = [ new ConsultantSkill { Skill = Skills[1] }, new ConsultantSkill { Skill = Skills[4] } ] }, // CSS, React
            new ConsultantSeed { Firstname = "Samuel", Lastname = "Weiss", SalaryRequirement = 4_400, Skills = [ new ConsultantSkill { Skill = Skills[3] }, new ConsultantSkill { Skill = Skills[7] } ] }, // TypeScript, Node.js
            new ConsultantSeed { Firstname = "Tariq", Lastname = "Aziz", SalaryRequirement = 4_100, Skills = [ new ConsultantSkill { Skill = Skills[5] }, new ConsultantSkill { Skill = Skills[16] } ] }, // Angular, SQL
            new ConsultantSeed { Firstname = "Uma", Lastname = "Reddy", SalaryRequirement = 4_200, Skills = [ new ConsultantSkill { Skill = Skills[6] }, new ConsultantSkill { Skill = Skills[18] } ] }, // Vue.js, GraphQL
            new ConsultantSeed { Firstname = "Victor Hugo", Lastname = "Alves", SalaryRequirement = 3_800, Skills = [ new ConsultantSkill { Skill = Skills[8] }, new ConsultantSkill { Skill = Skills[17] } ] }, // Express.js, NoSQL
            new ConsultantSeed { Firstname = "Wei", Lastname = "Zhang", SalaryRequirement = 4_700, Skills = [ new ConsultantSkill { Skill = Skills[9] }, new ConsultantSkill { Skill = Skills[19] } ] }, // ASP.NET Core, REST APIs
            new ConsultantSeed { Firstname = "Yasmin", Lastname = "Koné", SalaryRequirement = 3_600, Skills = [ new ConsultantSkill { Skill = Skills[11] }, new ConsultantSkill { Skill = Skills[13] } ] } // Django, PHP

        ];
    }
}