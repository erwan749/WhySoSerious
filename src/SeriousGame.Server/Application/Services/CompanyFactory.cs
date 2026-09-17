using Server.Domain;
using Server.Options;

namespace Server.Application.Services;

/// <summary>
/// Assemble une Company à partir de son propriétaire et des paramètres de partie
/// (pattern Factory, sur le modèle de GameFactory). Reste une fonction pure.
/// </summary>
public static class CompanyFactory
{
    public static Company Create(Player owner, GameOptions options)
    {
        return new Company
        {
            Name = GenerateDefaultName(owner),
            PlayerOwner = owner,
            InitialTreasury = options.CompanyInitialTreasury
        };
    }

    private static string GenerateDefaultName(Player owner) => $"{owner.Nickname} Consulting";
}