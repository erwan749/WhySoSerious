using Client.Resources;
using Client.Services.Interfaces;
using Client.State;
using Client.UI;
using Shared.Models.Dtos;

namespace Client.Game;

/// <summary>
/// Boucle de partie côté client : ne pilote plus rien elle-même, elle se connecte au hub /game,
/// rejoint la salle de la partie puis réagit aux événements du serveur (RoundStarted,
/// PlayerSubmitted, RoundResolved, GameEnded). Instanciée par App pour la durée d'une seule
/// partie, comme ConsoleAnimator, pas résolue depuis le conteneur DI.
/// </summary>
public class GameLoop
{
    private readonly ClientSession _session;
    private readonly IGameServices _gameServices;

    // Complété par le handler GameEnded : c'est ce qui fait sortir RunAsync.
    private readonly TaskCompletionSource _gameEndedSignal =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public GameLoop(ClientSession session, IGameServices gameServices)
    {
        _session = session;
        _gameServices = gameServices;
    }

    public async Task RunAsync()
    {
        var game = _session.CurrentGame;
        if (game is null) return;

        SubscribeToGameEvents();

        try
        {
            var isConnected = await _gameServices.ConnectAsync();

            if (!isConnected)
            {
                ConsoleUI.WriteError(string.Format(ClientResources.FailedToConnectError, _gameServices.HubUrl));
                return;
            }

            // Le serveur n'ouvre le tour 1 que lorsque tous les joueurs ont rejoint la salle.
            await _gameServices.JoinGameRoomAsync(game.Id);

            await _gameEndedSignal.Task;
        }
        finally
        {
            UnsubscribeFromGameEvents();
            await _gameServices.DisconnectAsync();
        }
    }

    private void SubscribeToGameEvents()
    {
        _gameServices.GameStarted += OnGameStarted;
        _gameServices.RoundStarted += OnRoundStarted;
        _gameServices.PlayerSubmitted += OnPlayerSubmitted;
        _gameServices.RoundResolved += OnRoundResolved;
        _gameServices.GameEnded += OnGameEnded;
    }

    private void UnsubscribeFromGameEvents()
    {
        _gameServices.GameStarted -= OnGameStarted;
        _gameServices.RoundStarted -= OnRoundStarted;
        _gameServices.PlayerSubmitted -= OnPlayerSubmitted;
        _gameServices.RoundResolved -= OnRoundResolved;
        _gameServices.GameEnded -= OnGameEnded;
    }
    
    private void OnGameStarted(CompanyDto company)
    {
        _session.SetMyCompany(company);
        ConsoleUI.WriteInfo(string.Format(ClientResources.CompanyAssignedFormat, company.Name));
    }

    private void OnRoundStarted(RoundDto round)
    {
        // Les handlers tournent sur un thread SignalR : le rendu attend une saisie clavier,
        // donc il part sur une tâche à part pour ne pas bloquer la réception des messages suivants.
        _ = Task.Run(() => PlayRoundAsync(round));
    }

    private async Task PlayRoundAsync(RoundDto round)
    {
        ConsoleUI.WriteHeader(string.Format(ClientResources.RoundHeaderFormat, round.Order, round.TotalRounds));

        // TODO US12 / US07-US09 : écrans réels du marché et collecte des décisions.
        RenderPhase(TurnPhase.MarketAnalysis);
        ShowMyCompanyScreen();
        await RunDecisionPhaseAsync(round);

        ConsoleUI.WriteInfo(PlaceholderFor(TurnPhase.Submission));
        await _gameServices.SubmitDecisionsAsync();

        ConsoleUI.WriteInfo(ClientResources.WaitingForOtherPlayersMessage);
    }
    
    private async Task RunDecisionPhaseAsync(RoundDto round)
    {
        ConsoleUI.WriteHeader(ClientResources.DecisionHeader);

        if (round.Tenders.Count == 0)
        {
            ConsoleUI.WriteInfo(ClientResources.NoTendersMessage);
            return;
        }

        foreach (var availableTender in round.Tenders)
        {
            ConsoleUI.WriteInfo(string.Format(ClientResources.TenderLineFormat, availableTender.Name, availableTender.Budget));
        }

        ConsoleUI.WritePrompt(ClientResources.ApplyToTenderPrompt);
        var tenderName = ConsoleUI.ReadPrompt();

        if (tenderName is null) return;

        var tender = round.Tenders.FirstOrDefault(t => t.Name.Equals(tenderName, StringComparison.OrdinalIgnoreCase));

        if (tender is null)
        {
            ConsoleUI.WriteError(ClientResources.TenderNotFoundError);
            return;
        }

        var freeConsultants = _session.MyCompany?.Staff.Where(c => c.Status == ConsultantStatus.Free).ToList() ?? [];

        if (freeConsultants.Count == 0)
        {
            ConsoleUI.WriteError(ClientResources.NoFreeConsultantError);
            return;
        }

        foreach (var consultant in freeConsultants)
        {
            ConsoleUI.WriteInfo(string.Format(ClientResources.ConsultantLineFormat, consultant.FullName, ClientResources.StatusFree));
        }

        ConsoleUI.WritePrompt(ClientResources.SelectConsultantsPrompt);
        var input = ConsoleUI.ReadPrompt() ?? "";
        var selectedNames = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var selectedIds = freeConsultants
            .Where(c => selectedNames.Any(n => n.Equals(c.FullName, StringComparison.OrdinalIgnoreCase)))
            .Select(c => c.Id)
            .ToList();

        if (selectedIds.Count == 0)
        {
            ConsoleUI.WriteError(ClientResources.NoConsultantSelectedError);
            return;
        }

        var error = await _gameServices.ApplyToTenderAsync(tender.Id, selectedIds);

        ConsoleUI.WriteInfo(error is null ? ClientResources.ApplicationSubmittedMessage : $"❌ {error}");
    }

    private static void OnPlayerSubmitted(string nickname)
    {
        ConsoleUI.WriteInfo(string.Format(ClientResources.PlayerSubmittedFormat, nickname));
    }

    private static void OnRoundResolved(RoundResultDto result)
    {
        // TODO US16 : bilan réel du tour (contrats gagnés/perdus, trésorerie, classement).
        ConsoleUI.WriteInfo(PlaceholderFor(TurnPhase.Resolution));
    }

    private void OnGameEnded(RankingDto ranking)
    {
        // TODO US17 : affichage du classement final.
        ConsoleUI.WriteHeader(ClientResources.GameOverHeader);
        ConsoleUI.WriteInfo(ClientResources.GameOverMessage);

        _gameEndedSignal.TrySetResult();
    }

    private static void RenderPhase(TurnPhase phase)
    {
        ConsoleUI.WriteInfo(PlaceholderFor(phase));
        ConsoleUI.WritePrompt(ClientResources.PressEnterToContinuePrompt);
        Console.ReadLine();
    }

    private static string PlaceholderFor(TurnPhase phase) => phase switch
    {
        TurnPhase.MarketAnalysis => ClientResources.MarketAnalysisPlaceholder,
        TurnPhase.Simulation => ClientResources.SimulationPlaceholder,
        TurnPhase.Submission => ClientResources.SubmissionPlaceholder,
        TurnPhase.Resolution => ClientResources.ResolutionPlaceholder,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
    private void ShowMyCompanyScreen()
    {
        var company = _session.MyCompany;

        if (company is null)
        {
            ConsoleUI.WriteError(ClientResources.NoCompanyError);
            return;
        }

        ConsoleUI.WriteHeader(string.Format(ClientResources.MyCompanyHeaderFormat, company.Name));
        ConsoleUI.WriteInfo(string.Format(ClientResources.TreasuryFormat, company.Treasury));
        ConsoleUI.WriteInfo(string.Format(ClientResources.RevenueFormat, company.Revenue));

        foreach (var consultant in company.Staff)
        {
            var statusLabel = consultant.Status switch
            {
                ConsultantStatus.Free => ClientResources.StatusFree,
                ConsultantStatus.OnMission => ClientResources.StatusOnMission,
                ConsultantStatus.InTraining => ClientResources.StatusInTraining,
                _ => throw new ArgumentOutOfRangeException()
            };

            ConsoleUI.WriteInfo(string.Format(ClientResources.ConsultantLineFormat, consultant.FullName, statusLabel));

            var skillsLabel = consultant.Skills.Count > 0
                ? string.Join(", ", consultant.Skills.Select(s => s.Skill.Name))
                : ClientResources.NoSkillsLabel;

            ConsoleUI.WriteInfo($"   {string.Format(ClientResources.ConsultantSkillsFormat, skillsLabel)}");
        } 

        ConsoleUI.WritePrompt(ClientResources.PressEnterToContinuePrompt);
        Console.ReadLine();
    }
}
