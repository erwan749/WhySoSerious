using System.Collections.Concurrent;
using Server.Domain;

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

    // Données de seed en lecture seule. Seuls des noms sont écrits à la main : durée, main-d'œuvre,
    // compétences, niveaux et budget d'un Tender sont générés au tirage du tour, à partir du staff
    // réellement en jeu (voir RoundFactory, US11). Un Seed est un blueprint, jamais une entité réelle.
    public IReadOnlyList<TenderSeed> TenderSeeds { get; }
    public IReadOnlyList<ConsultantSeed> ConsultantsSeed { get; }

    public AppMemory()
    {
        TenderSeeds =
        [
            new TenderSeed { Name = "WhySoSerious" },
            new TenderSeed { Name = "CodeMonkey" },
            new TenderSeed { Name = "BugHunter" },
            new TenderSeed { Name = "NullPointer" },
            new TenderSeed { Name = "StackOverflowed" },
            new TenderSeed { Name = "ItWorksOnMyMachine" },
            new TenderSeed { Name = "CodeAndChill" },
            new TenderSeed { Name = "CtrlAltElite" },
            new TenderSeed { Name = "CommitThis" },
            new TenderSeed { Name = "MergeAndDestroy" },
            new TenderSeed { Name = "GitRekt" },
            new TenderSeed { Name = "404NotFound" },
            new TenderSeed { Name = "Segfault" },
            new TenderSeed { Name = "SpaghettiCode" },
            new TenderSeed { Name = "NoBugJustFeatures" },
            new TenderSeed { Name = "WorksOnMyMachine" },
            new TenderSeed { Name = "sudoMakeMeACoffee" },
            new TenderSeed { Name = "HelloWorldAgain" },
            new TenderSeed { Name = "ShipIt" },
            new TenderSeed { Name = "TrustMeImADev" }
        ];

        ConsultantsSeed =
        [
            new ConsultantSeed { Firstname = "Alice", Lastname = "Martin" },
            new ConsultantSeed { Firstname = "Bruno", Lastname = "Costa" },
            new ConsultantSeed { Firstname = "Chloé", Lastname = "Nguyen" },
            new ConsultantSeed { Firstname = "David", Lastname = "Okafor" },
            new ConsultantSeed { Firstname = "Elena", Lastname = "Rossi" },
            new ConsultantSeed { Firstname = "Farid", Lastname = "Haddad" },
            new ConsultantSeed { Firstname = "Grace", Lastname = "Kim" },
            new ConsultantSeed { Firstname = "Hugo", Lastname = "Meyer" },
            new ConsultantSeed { Firstname = "Inès", Lastname = "Dubois" },
            new ConsultantSeed { Firstname = "Jonas", Lastname = "Lindqvist" },
            new ConsultantSeed { Firstname = "Karim", Lastname = "Belkacem" },
            new ConsultantSeed { Firstname = "Léa", Lastname = "Fontaine" },
            new ConsultantSeed { Firstname = "Marco", Lastname = "Silva" },
            new ConsultantSeed { Firstname = "Nadia", Lastname = "Petrova" },
            new ConsultantSeed { Firstname = "Omar", Lastname = "El-Amin" },
            new ConsultantSeed { Firstname = "Priya", Lastname = "Sharma" },
            new ConsultantSeed { Firstname = "Quentin", Lastname = "Roy" },
            new ConsultantSeed { Firstname = "Rania", Lastname = "Saidi" },
            new ConsultantSeed { Firstname = "Samuel", Lastname = "Weiss" },
            new ConsultantSeed { Firstname = "Tariq", Lastname = "Aziz" },
            new ConsultantSeed { Firstname = "Uma", Lastname = "Reddy" },
            new ConsultantSeed { Firstname = "Victor Hugo", Lastname = "Alves" },
            new ConsultantSeed { Firstname = "Wei", Lastname = "Zhang" },
            new ConsultantSeed { Firstname = "Yasmin", Lastname = "Koné" }
        ];
    }
}