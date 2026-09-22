# Fonctionnalité — Salaires et compétences aléatoires par partie

## Contexte

`AppMemory` est un singleton créé une seule fois au démarrage du serveur. Le référentiel
`ConsultantsSeed` (US05) fixait salaire et compétences directement sur chaque carte : toutes les
parties lancées pendant la même exécution du serveur donnaient donc exactement le même staff de
départ, dans le même ordre de tirage. Randomiser *dans* `AppMemory` n'aurait rien changé, puisqu'il
n'est construit qu'une fois — il fallait déplacer le tirage aléatoire dans le code qui s'exécute à
chaque partie.

## Principe

`ConsultantSeed` ne porte plus que des noms (prénom/nom). `ConsultantFactory`, qui s'exécute à chaque
`GameFlowService.StartGame`, tire pour chaque consultant un salaire aléatoire (3 000 à 5 000, par pas
de 100) et 1 à 2 compétences aléatoires piochées dans `AppMemory.Skills`. Le `Random` déjà injecté
(`Random.Shared`) garantit un tirage différent à chaque partie.

## Flux

```
GameFlowService.StartGame
        │
        └─▶ ConsultantFactory.AssignInitialStaff(companies, ConsultantsSeed, Skills, random)
                 │
                 ├─▶ Mélange des 24 noms, distribution round-robin (3 par entreprise)
                 │
                 └─▶ Pour chaque nom : CreateFromSeed(seed, skillCatalog, company, random)
                          ├─▶ SalaireRequirement = random.Next(30,51) × 100
                          └─▶ 1 ou 2 compétences tirées dans skillCatalog,
                              chacune enveloppée dans une NOUVELLE ConsultantSkill
```

## Fichiers modifiés

| Fichier | Changement |
|---|---|
| `src/SeriousGame.Server/Domain/ConsultantSeed.cs` | `SalaryRequirement`/`Skills` retirés — ne reste que `Firstname`/`Lastname` |
| `src/SeriousGame.Server/Application/Services/ConsultantFactory.cs` | `AssignInitialStaff`/`CreateFromSeed` prennent un `skillCatalog` et tirent salaire + compétences au hasard |
| `src/SeriousGame.Server/Infrastructure/AppMemory.cs` | `ConsultantsSeed` : 24 cartes réduites à des noms seuls |
| `src/SeriousGame.Server/Application/Services/GameFlowService.cs` | `StartGame` passe `_appMemory.Skills` en plus à `AssignInitialStaff` |
| `tests/SeriousGame.UnitTests/ConsultantFactoryTests.cs` | Tests adaptés à la nouvelle signature (plage de salaire, nombre de compétences, non-partage d'instances) |

## Décisions de conception

- **`ConsultantSeed` réduit à un simple nom** — le salaire et les compétences n'ont plus de raison
  d'être fixés à l'avance dans un référentiel statique dès lors qu'ils doivent varier à chaque partie ;
  les garder sur `ConsultantSeed` aurait été une donnée inutile, jamais lue telle quelle.
- **Salaire arrondi à la centaine (`random.Next(30,51) × 100`)** plutôt qu'un entier brut entre 3000 et
  5000 — plus lisible à l'écran pour le joueur, sans changer la logique de tirage.
- **1 à 2 compétences, jamais 0** — un consultant sans aucune compétence ne pourrait jamais candidater
  à un appel d'offres ni justifier un salaire ; le minimum de 1 garantit qu'il reste utile dès le départ.
- **Toujours une nouvelle instance de `ConsultantSkill`, même si deux consultants tirent la même
  compétence** — `ConsultantSkill` est mutable (`LevelUp`) ; le même raisonnement que la scission de
  `Skill` (bug corrigé avant l'US03) s'applique ici : deux consultants ne doivent jamais partager la
  même instance, même en cas de coïncidence sur la compétence tirée.

## Limites connues / suite possible

- Le tirage est uniforme (chaque compétence a la même probabilité d'être choisie) — si le jeu doit un
  jour favoriser certaines compétences plus rares ou plus demandées, il faudra pondérer le tirage.
- Le nombre de compétences par consultant (1 à 2) et la plage de salaire (3 000-5 000) sont des
  constantes dans `ConsultantFactory` — à externaliser vers `GameOptions` si le besoin de les ajuster
  sans recompiler se présente.
