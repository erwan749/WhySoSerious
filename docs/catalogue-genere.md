# Fonctionnalité — Catalogue généré du tour (US11)

[← Retour à l'index](README.md)

## Contexte

Le backlog prévoyait un référentiel d'appels d'offres saisi à la main, tiré au sort à chaque
tour. Deux problèmes : rien ne garantissait qu'une entreprise puisse couvrir les compétences
exigées — un appel d'offres réclamant du React quand personne n'en fait n'intéresse personne —
et la difficulté était figée dans les données, indépendante de la progression des joueurs.

## Principe

`RoundFactory` bâtit chaque appel d'offres **sur une équipe réellement disponible**. Le tirage
choisit une entreprise de référence, prend ses consultants libres, et déduit d'eux la
main-d'œuvre, les compétences exigées et leur niveau. Un appel d'offres est donc toujours
réalisable par au moins un joueur.

Seuls les noms de projets restent des données (`TenderSeed`). Durée, budget, compétences et
niveaux sont calculés.

## Flux
```
GameFlowService.CreateRound (sous lock (game))
        │
        ▼
        ├─ RoundFactory.Create(game, tenderSeeds, options, random)
        │
        ├─ Nombre d'appels d'offres = TenderNumberPerPlayer × nombre de joueurs
        ├─ Entreprise de référence : tourniquet, départ décalé au hasard
        │
        ├─ Pour chacun : BuildTender
        ├─ consultants libres (Company.GetAvailableConsultants)
        ├─ main-d'œuvre ≤ consultants libres, selon le palier du tour
        ├─ compétences = celles de l'équipe tirée, dédoublonnées
        ├─ niveau exigé = possédé, ou un cran en dessous (plancher Basic)
        ├─ durée ≤ tours restants dans la partie
        ├─ budget = main-d'œuvre × durée × salaire de référence × marge
        └─ nom non encore servi dans la partie
```
## Paliers de difficulté

| Tour | Main-d'œuvre | Durée minimale |
|---|---|---|
| 1 – 2 | 1 | 1 |
| 3 – 4 | 1 à 2 | 2 |
| 5 et + | 2 à 3 | 3 |

La durée est plafonnée à 4 tours, et toujours au nombre de tours restants.

## Fichiers modifiés

| Fichier | Changement |
|---|---|
| `src/SeriousGame.Server/Application/Services/RoundFactory.cs` | Nouveau : tirage du catalogue d'un tour |
| `src/SeriousGame.Server/Application/Services/GameFlowService.cs` | `CreateRound` délègue à la factory au lieu de créer un tour vide |
| `src/SeriousGame.Server/Domain/Company.cs` | `IsConsultantBusy` / `GetAvailableConsultants` : la règle de disponibilité descend du mapper vers le domaine |
| `src/SeriousGame.Server/Application/Mapper.cs` | `ResolveStatus` s'appuie sur la règle du domaine |
| `tests/SeriousGame.UnitTests/RoundFactoryTests.cs` | Nouveau : 7 tests déterministes (`new Random(42)`) |

## Décisions de conception

- **Le catalogue est généré, pas saisi.** C'est ce qui garantit qu'un appel d'offres est
  toujours réalisable, et ce qui fait monter la difficulté toute seule : plus les joueurs
  forment leurs consultants, plus les niveaux exigés grimpent, sans une ligne de donnée en plus.
- **Niveau exigé un cran en dessous, au hasard.** Exiger exactement le niveau possédé rendrait
  l'entreprise de référence seule éligible, et l'attribution (US13) n'aurait jamais deux
  candidatures à départager. Descendre d'un cran ouvre la porte aux concurrents sans rendre le
  contrat infaisable.
- **Trois garde-fous issus de la construction, pas ajoutés après coup** : la main-d'œuvre ne
  dépasse pas les consultants libres, le niveau ne dépasse pas celui possédé, la durée ne
  dépasse pas les tours restants. Ils découlent du fait de partir d'une équipe réelle.
- **`Random` injecté en paramètre**, comme pour `ConsultantFactory` : le tirage devient
  reproductible en test avec `new Random(42)`.
- **Appelée sous `lock (game)`** : `RoundFactory` lit le staff et écrit dans `game.Rounds`.
  Deux joueurs agissant au même instant ne peuvent pas générer deux catalogues pour un tour.

## Limites connues / suite possible

- Les **formations** ne sont pas encore générées (US25) : la collection `Trainings` d'un tour
  reste vide, et l'US08 (inscrire un consultant en formation) n'a rien à proposer d'ici là.
- Une entreprise dont tous les consultants sont occupés ne produit aucun appel d'offres. Un
  tour peut donc être vide — ce n'est pas une erreur, mais le client doit l'afficher
  proprement (US12).
- Deux parties ne se ressemblent jamais : pratique pour la rejouabilité, gênant pour rejouer
  un scénario de bug. Fixer la graine du `Random` serait à envisager si le besoin se présente.