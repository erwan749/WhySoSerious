using Server.Options;

namespace SeriousGame.UnitTests;

public class GameOptionsTests
{
    // Valeurs de repli utilisées quand la section "Game" est absente de la configuration.
    private readonly GameOptions _options = new();

    [Fact]
    public void Defaults_AreAllPositive()
    {
        Assert.True(_options.MinimumPlayers > 0);
        Assert.True(_options.MaximumPlayers >= _options.MinimumPlayers);
        Assert.True(_options.RoundsNumber > 0);
        Assert.True(_options.CompanyInitialTreasury > 0);
        Assert.True(_options.SalaryReferencePerConsultant > 0);
        Assert.True(_options.TrainingBaseCost > 0);
        Assert.True(_options.TrainingRoundsNumber > 0);
    }

    [Fact]
    public void TenderMargin_MakesContractsProfitable()
    {
        // En dessous de 100 %, un contrat rapporterait moins qu'il ne coûte en salaires
        // et plus personne ne candidaterait.
        Assert.True(_options.TenderMarginPercent > 100);
    }

    [Fact]
    public void CatalogueSizes_FillEveryRound()
    {
        // Un tour sans appel d'offres n'aurait aucun intérêt.
        Assert.True(_options.TenderNumberPerPlayer >= 1);
        Assert.True(_options.TrainingNumberPerPlayer >= 1);
    }
}