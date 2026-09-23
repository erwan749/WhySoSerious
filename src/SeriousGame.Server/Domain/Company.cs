using Server.Domain.Base;
using Server.Domain.Enums;
using Shared.Models.Dtos;
using System.Collections.ObjectModel;

namespace Server.Domain;

public class Company : BaseModel
{
    public required string Name { get; set; }
    public required Player PlayerOwner { get; set; }
    public int Treasury { get; private set; }
    public int Revenue { get; private set; }
    public ICollection<Consultant> Staffs { get; } = [];

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

    /// <summary>
    /// Indique si ce consultant est immobilisé : affecté à un contrat encore actif, ou inscrit à une
    /// formation en cours. Un consultant occupé ne peut ni candidater ni partir en formation.
    /// </summary>
    public bool IsConsultantBusy(Consultant consultant) =>
        Contracts.Any(c => c.Status == ContractStatus.Active && c.AssignedConsultants.Contains(consultant))
        || TrainingEnrollments.Any(e => e.Status == EnrollmentStatus.InProgress && e.Consultant == consultant);

    /// <summary>Consultants du staff mobilisables ce tour : ni en mission, ni en formation.</summary>
    public IReadOnlyList<Consultant> GetAvailableConsultants() =>
        Staffs.Where(consultant => !IsConsultantBusy(consultant)).ToList();

}