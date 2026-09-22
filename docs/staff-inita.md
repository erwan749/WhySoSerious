# Fonctionnalité — Staff initial de consultants (US05)

## Contexte

`GameFlowService.StartGame` (US04) crée une `Company` par `Player`, mais chaque entreprise démarre
sans aucun consultant : `ConsultantSeed` et `ConsultantSkill` étaient prêts depuis l'US03, mais rien
ne les utilisait encore pour peupler le `Staff` des entreprises.

## Principe

Une nouvelle méthode `ConsultantFactory.AssignInitialStaff` distribue un référentiel de 24 cartes
(`AppMemory.ConsultantsSeed`) entre toutes les entreprises d'une partie, juste après leur création.
Le référentiel est mélangé puis réparti en tourniquet (round-robin) : chaque entreprise reçoit
exactement le même nombre de consultants (3), sans jamais recevoir deux fois la même carte.

## Flux

```
GameFlowService.StartGame
        │
        ├─▶ Pour chaque Player : CompanyFactory.Create → game.Companies
        │
        └─▶ ConsultantFactory.AssignInitialStaff(game.Companies, AppMemory.ConsultantsSeed)
                 │
                 ├─▶ Mélange des 24 cartes (Random)
                 ├─▶ Distribution round-robin (3 par entreprise)
                 └─▶ Pour chaque carte : CreateFromSeed → nouveau Consultant + nouvelles ConsultantSkill
                          (jamais les instances du seed : elles sont mutables via LevelUp)
```

## Fichiers modifiés

| Fichier | Changement |
|---|---|
| `src/SeriousGame.Server/Application/Services/ConsultantFactory.cs` | Nouveau : distribution équitable + création d'un `Consultant` à partir d'un `ConsultantSeed` |
| `src/SeriousGame.Server/Infrastructure/AppMemory.cs` | `ConsultantsSeed` : 1 carte d'exemple → 24 cartes réelles |
| `src/SeriousGame.Server/Application/Services/GameFlowService.cs` | `StartGame` appelle `ConsultantFactory.AssignInitialStaff` après la création des `Company` |
| `src/SeriousGame.Server/Application/Mapper.cs` | `ToDto(Consultant)` mappe enfin `Skills` (`TODO US05` posé en US01, résolu) |
| `tests/SeriousGame.UnitTests/ConsultantFactoryTests.cs` | Nouveau : 5 tests couvrant les 3 critères d'acceptation |

## Décisions de conception

- **3 consultants par entreprise, 24 cartes au total** — dimensionné pour tomber juste avec
  `MaximumPlayers` (8 × 3 = 24), sans cas limite à gérer pour une partie complète.
- **Niveau de départ toujours `Level.Zero`** — la variété porte sur *quelles* compétences sont
  possédées, pas sur leur niveau ; `ConsultantSkill.Level` ne progresse que via `LevelUp()`, le fixer
  arbitrairement dans le seed aurait été artificiel.
- **`Random` injecté en paramètre plutôt qu'instancié dans la méthode** — rend `AssignInitialStaff`
  testable de façon déterministe (`new Random(42)` dans les tests).
- **Nouvelle instance de `ConsultantSkill` à chaque `CreateFromSeed`, jamais celle du seed** — évite de
  reproduire le bug déjà rencontré sur `AppMemory.Skills` avant sa scission : un objet mutable partagé
  entre plusieurs `Consultant` ferait fuiter la progression de l'un vers tous les autres.

## Limites connues / suite possible

- Le référentiel de 24 cartes est statique (pas de renouvellement en cours de partie) — suffisant pour
  le staff de départ, mais à revoir si une US future ajoute du recrutement en cours de partie.
- La répartition ne prend en compte que le **nombre** de consultants, pas l'équilibre des compétences
  ou des salaires entre entreprises — un raffinement possible si le playtest montre des débuts trop
  inégaux entre joueurs.
