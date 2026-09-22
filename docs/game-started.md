# Fonctionnalité — Notifier le joueur de son entreprise (`GameStarted`)

## Contexte

`GameFlowService.StartGame` (US04) crée bien une `Company` par `Player` côté serveur, mais rien
n'informait le client de l'entreprise qui lui avait été attribuée : `IGameHubClient` n'exposait que
`RoundStarted`, `PlayerSubmitted`, `RoundResolved` et `GameEnded`. Le nom de l'entreprise
(généré par `CompanyFactory` sous la forme `"{Nickname} Consulting"`) restait donc invisible.

## Principe

Un nouvel événement `GameStarted` diffuse la liste des `Company` de la partie à tous les clients dès
que le dernier joueur a rejoint la salle de jeu — le même instant où le premier `Round` démarre.
Chaque client filtre ensuite la liste reçue pour ne garder que **sa propre** entreprise
(`OwnerId == PlayerId`) et l'affiche.

## Flux

```
Tous les joueurs ont rejoint /game (JoinGameRoom)
                │
                ▼
GameFlowService.JoinGameRoom détecte "everyoneIsHere"
                │
                ├─▶ GameStarted(companies)   ── diffusé au groupe, AVANT RoundStarted
                │        │
                │        ▼
                │   GameServices filtre par OwnerId == PlayerId
                │        │
                │        ▼
                │   GameLoop.OnGameStarted → affiche "Ton entreprise : {Nom}"
                │
                └─▶ RoundStarted(round)      ── premier tour
```

## Fichiers modifiés

| Fichier | Changement |
|---|---|
| `src/SeriousGame.Shared/Abstractions/IGameHubClient.cs` | Ajout de `Task GameStarted(ICollection<CompanyDto> companies)` |
| `src/SeriousGame.Server/Application/Services/GameFlowService.cs` | `JoinGameRoom` diffuse `GameStarted` avant `StartRound`, quand `everyoneIsHere` |
| `src/SeriousGame.Client/Services/Interfaces/IGameServices.cs` | Ajout de `event Action<CompanyDto>? GameStarted` |
| `src/SeriousGame.Client/Services/GameServices.cs` | Souscrit à `GameStarted` côté hub, filtre la `Company` du joueur courant, relaie l'événement |
| `src/SeriousGame.Client/Game/GameLoop.cs` | S'abonne/désabonne à `GameStarted`, affiche le nom via `ConsoleUI` |
| `src/SeriousGame.Client/Resources/ClientResources.resx` | Nouvelle entrée `CompanyAssignedFormat` (`"Ton entreprise : {0}"`) |

## Décisions de conception

- **Diffusion de la liste complète des `Company`, filtrage côté client** — plutôt qu'un événement
  ciblé par joueur. Ça laisse la porte ouverte à un futur écran "entreprises concurrentes" sans
  redéfinir le contrat, et évite d'envoyer un message SignalR par joueur individuellement.
- **Émis depuis `JoinGameRoom`, pas depuis `StartGame`** — au moment de `StartGame`, aucun client
  n'a encore rejoint le groupe SignalR de la partie (`AddToGroupAsync` n'a pas eu lieu). `JoinGameRoom`
  est le premier point où l'on sait que tout le monde est connecté au hub `/game`.
- **Filtrage dans `GameServices`, pas dans `GameLoop`** — seule la couche réseau connaît
  `ClientSession.PlayerId` et le format DTO ; `GameLoop` ne doit recevoir que ce qui le concerne
  directement.
- **Émis avant `RoundStarted`** — garantit que le joueur voit le nom de son entreprise avant l'écran
  du premier tour.

## Limites connues / suite possible

- Le nom de l'entreprise reste **généré automatiquement** (`"{Nickname} Consulting"`), non
  personnalisable par le joueur — point resté ouvert depuis l'US Company.
- L'affichage actuel est un simple message console (`ConsoleUI.WriteInfo`) ; un futur écran dédié
  (US06 — "Consulter mon entreprise") pourra réutiliser le même `CompanyDto` déjà transporté.
