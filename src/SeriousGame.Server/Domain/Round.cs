using Server.Domain.Base;

namespace Server.Domain;

public class Round :  BaseModel
{
    public required Game Game { get; init; }
    public int Order { get; init; }
    public bool IsCompleted { get; set; } =  false;

    // Catalogue proposé ce tour.
    public ICollection<Tender> Tenders { get; init; } = [];
    public ICollection<Training> Trainings { get; init; } = [];

    // Décisions des entreprises ce tour, résolues en Won/Lost au calcul de tour.
    public ICollection<TenderApplication> Applications { get; init; } = [];
    //Ids des joueurs du round, US09
    public HashSet<string> SubmittedPlayerIds { get; } = [];
}