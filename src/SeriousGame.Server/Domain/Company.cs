using Server.Domain.Base;

namespace Server.Domain;

public class Company : BaseModel
{
    public required string Name { get; set; }
    public required Player PlayerOwner { get; set; }
    public int Treasury { get; private set; }
    public int Revenue { get; private set; }
    public ICollection<Consultant> Staff { get; } = [];

    /// <summary>Trésorerie de départ, fixée une seule fois à la création de l'entreprise.</summary>
    public required int InitialTreasury
    {
        init => Treasury = value;
    }

    // État persistant entre tours : appels d'offres en cours d'exécution et consultants en formation.
    // Un consultant est « occupé » s'il figure dans un Contract actif ou un TrainingEnrollment en cours.
    public ICollection<Contract> Contracts { get; } = [];
    public ICollection<TrainingEnrollment> TrainingEnrollments { get; } = [];

    /// <summary>
    /// Encaisse le budget d'un contrat terminé : alimente à la fois la trésorerie (disponible pour
    /// payer salaires/formations) et le chiffre d'affaires (Revenue, qui sert au classement final).
    /// </summary>
    public void RecordContractRevenue(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Le chiffre d'affaires ne peut pas être négatif.");
        Treasury += amount;
        Revenue += amount;
    }
    
    
    public void Deposit(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Le montant déposé ne peut pas être négatif.");
        Treasury += amount;
    }

    public void Withdraw(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Le montant retiré ne peut pas être négatif.");
        Treasury -= amount;
    }
}