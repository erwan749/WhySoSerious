using Client.Resources;
using Client.Services.Interfaces;
using Client.State;
using Client.UI;
using Shared.Models.Dtos;
using System.Reflection;

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
    private readonly List<string> _roundDecisions = [];

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

        _roundDecisions.Clear();

        ShowMyCompanyScreen();
        ShowMarketScreen(round);
        await RunDecisionPhaseAsync(round);

        await RunSubmissionPhaseAsync();

        ConsoleUI.WriteInfo(ClientResources.WaitingForOtherPlayersMessage);
    }
    
    private async Task RunSubmissionPhaseAsync()
    {
        ConsoleUI.WriteHeader(ClientResources.SubmissionHeader);

        if (_roundDecisions.Count == 0)
        {
            ConsoleUI.WriteInfo(ClientResources.NoDecisionsMessage);
        }
        else
        {
            foreach (var decision in _roundDecisions)
            {
                ConsoleUI.WriteInfo($"- {decision}");
            }
        }

        ConsoleUI.WritePrompt(ClientResources.ConfirmSubmissionPrompt);
        Console.ReadLine();

        await _gameServices.SubmitDecisionsAsync();
    }
    
    private async Task RunDecisionPhaseAsync(RoundDto round)
{
    ConsoleUI.WriteHeader(ClientResources.DecisionHeader);

    await RunTenderApplicationStepAsync(round);
    await RunTrainingEnrollmentStepAsync(round);
}

    private async Task RunTenderApplicationStepAsync(RoundDto round)
    {
        if (round.Tenders.Count == 0)
        {
            ConsoleUI.WriteInfo(ClientResources.NoTendersMessage);
            return;
        }

        // Déjà listés et numérotés par l'écran marché : on demande directement le numéro.
        var tenders = round.Tenders.ToList();

        var tenderIndex = ReadIndex(ClientResources.ApplyToTenderPrompt, tenders.Count);
        if (tenderIndex is null) return;

        var tender = tenders[tenderIndex.Value];

        var freeConsultants = GetFreeConsultants();

        if (freeConsultants.Count == 0)
        {
            ConsoleUI.WriteError(ClientResources.NoFreeConsultantError);
            return;
        }

        for (var i = 0; i < freeConsultants.Count; i++)
        {
            ConsoleUI.WriteInfo(string.Format(
                ClientResources.NumberedConsultantLineFormat,
                i + 1,
                freeConsultants[i].FullName,
                ClientResources.StatusFree));
        }

        var consultantIndexes = ReadIndexes(ClientResources.SelectConsultantsPrompt, freeConsultants.Count);

        if (consultantIndexes.Count == 0)
        {
            ConsoleUI.WriteError(ClientResources.NoConsultantSelectedError);
            return;
        }

        var selectedIds = consultantIndexes.Select(index => freeConsultants[index].Id).ToList();

        var error = await _gameServices.ApplyToTenderAsync(tender.Id, selectedIds);

        if (error is null)
        {
            ConsoleUI.WriteInfo(ClientResources.ApplicationSubmittedMessage);
            _roundDecisions.Add(string.Format(ClientResources.TenderDecisionSummaryFormat, tender.Name, selectedIds.Count));
        }
        else
        {
            ConsoleUI.WriteError(error);
        }
    }

    private async Task RunTrainingEnrollmentStepAsync(RoundDto round)
    {
        if (round.Trainings.Count == 0)
        {
            ConsoleUI.WriteInfo(ClientResources.NoTrainingsMessage);
            return;
        }

        var trainings = round.Trainings.ToList();

        // TODO US25 : à retirer quand l'écran marché listera les formations générées.
        for (var i = 0; i < trainings.Count; i++)
        {
            ConsoleUI.WriteInfo(string.Format(
                ClientResources.TrainingDetailFormat,
                i + 1,
                trainings[i].Name,
                trainings[i].Skill.Name,
                trainings[i].Cost,
                trainings[i].RoundsNumber));
        }

        var trainingIndex = ReadIndex(ClientResources.EnrollInTrainingPrompt, trainings.Count);
        if (trainingIndex is null) return;

        var training = trainings[trainingIndex.Value];

        var freeConsultants = GetFreeConsultants();

        if (freeConsultants.Count == 0)
        {
            ConsoleUI.WriteError(ClientResources.NoFreeConsultantError);
            return;
        }

        for (var i = 0; i < freeConsultants.Count; i++)
        {
            ConsoleUI.WriteInfo(string.Format(
                ClientResources.NumberedConsultantLineFormat,
                i + 1,
                freeConsultants[i].FullName,
                ClientResources.StatusFree));
        }

        var consultantIndex = ReadIndex(ClientResources.SelectConsultantPrompt, freeConsultants.Count);

        if (consultantIndex is null)
        {
            ConsoleUI.WriteError(ClientResources.NoConsultantSelectedError);
            return;
        }

        var consultant = freeConsultants[consultantIndex.Value];

        var error = await _gameServices.EnrollInTrainingAsync(training.Id, consultant.Id);

        if (error is null)
        {
            ConsoleUI.WriteInfo(ClientResources.EnrollmentSubmittedMessage);
            _roundDecisions.Add(string.Format(ClientResources.TrainingDecisionSummaryFormat, consultant.FullName, training.Name));
        }
        else
        {
            ConsoleUI.WriteError(error);
        }
    }

    private List<ConsultantDto> GetFreeConsultants() =>
        _session.MyCompany?.Staff.Where(c => c.Status == ConsultantStatus.Free).ToList() ?? [];

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
                ? string.Join(", ", consultant.Skills.Select(s =>
                    string.Format(ClientResources.SkillWithLevelFormat, s.Skill.Name, s.Level)))
                : ClientResources.NoSkillsLabel;

            ConsoleUI.WriteInfo($"   {string.Format(ClientResources.ConsultantSkillsFormat, skillsLabel)}");
        } 

        ConsoleUI.WritePrompt(ClientResources.PressEnterToContinuePrompt);
        Console.ReadLine();
    }
    /// <summary>
    /// Affiche le catalogue du tour — appels d'offres et formations — puis attend que le joueur ait
    /// lu. N'appelle pas le serveur : tout vient du RoundDto reçu avec RoundStarted.
    /// </summary>
    private void ShowMarketScreen(RoundDto round)
    {
        ConsoleUI.WriteHeader(ClientResources.MarketHeader);
        ConsoleUI.WriteHeader(ClientResources.TendersSectionHeader);
        if(round.Tenders.Count == 0)
        {
            ConsoleUI.WriteInfo(ClientResources.NoTendersMessage);
        }
        else
        {
            var number = 1;

            foreach (var tender in round.Tenders) 
            { 
                ConsoleUI.WriteInfo(string.Format(
                    ClientResources.TenderDetailFormat,
                    number,
                    tender.Name,
                    tender.Budget,
                    tender.RoundsNumber,
                    tender.RequiredConsultants));


                ConsoleUI.WriteInfo(string.Format(
                    ClientResources.TenderRequiredSkillsFormat,
                    FormatRequiredSkills(tender.RequiredSkills)));

                number++;
            }
        }
        //TODO Training

        ConsoleUI.WritePrompt(ClientResources.PressEnterToContinuePrompt);
        Console.ReadLine();
    }
    /// <summary>Compétences exigées en une ligne lisible, ou le libellé « aucune » si la liste est vide.</summary>
    private static string FormatRequiredSkills(ICollection<RequiredSkillDto> requiredSkills) =>
        requiredSkills.Count > 0
            ? string.Join(", ", requiredSkills.Select(required =>
                string.Format(ClientResources.SkillWithLevelFormat, required.Skill.Name, required.Level)))
            : ClientResources.NoSkillsLabel;
    /// <summary>
    /// Lit un numéro entre 1 et <paramref name="itemCount"/> et renvoie l'index correspondant.
    /// Renvoie null si le joueur passe son tour (entrée vide) ou saisit une valeur hors bornes.
    /// </summary>
    private static int? ReadIndex(string prompt, int itemCount)
    {
        ConsoleUI.WritePrompt(prompt);
        var input = ConsoleUI.ReadPrompt();

        if (string.IsNullOrWhiteSpace(input)) return null;

        if (!int.TryParse(input, out var number) || number < 1 || number > itemCount)
        {
            ConsoleUI.WriteError(ClientResources.InvalidNumberError);
            return null;
        }

        return number - 1;
    }

    /// <summary>
    /// Lit plusieurs numéros séparés par des virgules et renvoie les index correspondants, sans
    /// doublon. Renvoie une liste vide si l'un des numéros est invalide.
    /// </summary>
    private static List<int> ReadIndexes(string prompt, int itemCount)
    {
        ConsoleUI.WritePrompt(prompt);
        var input = ConsoleUI.ReadPrompt() ?? "";

        var indexes = new List<int>();

        foreach (var part in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var number) || number < 1 || number > itemCount)
            {
                ConsoleUI.WriteError(ClientResources.InvalidNumberError);
                return [];
            }

            // « 1,1 » ne doit pas affecter deux fois le même consultant.
            if (!indexes.Contains(number - 1)) indexes.Add(number - 1);
        }

        return indexes;
    }
}

