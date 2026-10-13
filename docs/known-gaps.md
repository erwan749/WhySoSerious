# Limitations connues

[← Retour à l'index](README.md)

Volontairement hors périmètre, documenté ici plutôt que corrigé en silence :

- **Pas d'authentification/autorisation.** Une revue de sécurité automatisée a trouvé de vrais problèmes ici (`PlayerId`/`clientId` fournis par le client et acceptés sans vérification, pas de `[Authorize]` sur les hubs) — suivi séparément, non corrigé par le travail DTO/layering décrit dans [architecture.md](architecture.md).
- Concurrence : les collections d'`AppMemory` (`Players`/`Games`) sont désormais des `ConcurrentDictionary`, mais les **opérations composées** ne sont pas encore synchronisées — `GameService.JoinGame` fait un « trouver-puis-muter » (une partie peut être supprimée entre les deux), et `Game.Players` reste une `List` mutée par des joins/leaves simultanés sur la même partie. À traiter avec les règles de `JoinGame` (verrouillage par agrégat).
- La **formation ne peut faire progresser qu'une compétence déjà possédée** : `ConsultantSkill.LevelUp` avance d'un cran, rien ne permet d'accorder une compétence nouvelle. Un consultant ne peut donc pas se reconvertir, seulement se spécialiser. À trancher avec le client (US25).
## Le staff des concurrents est diffusé à tous

`GameStarted` et `RoundResolved` envoient la liste complète des `CompanyDto`
au groupe de la partie, staff compris : salaires, compétences et niveaux des
concurrents. C'est le client qui filtre sur `OwnerId`, donc l'information est
lisible en inspectant le trafic.

Corriger demanderait un `CompanyDto` complet pour soi et une vue réduite pour
les autres, soit un changement de signature sur deux événements du hub.

## Code mort hérité de l'échafaudage

`GameLoop.RenderPhase`, `GameLoop.PlaceholderFor`, l'enum `TurnPhase` et les
ressources `MarketAnalysisPlaceholder`, `SimulationPlaceholder` et
`ResolutionPlaceholder` n'ont plus aucun appelant depuis que les écrans réels
remplacent les placeholders. À supprimer dans un commit de nettoyage à part.

## Faillite sans conséquence

`Company.IsBankrupt` est levé par le paiement des salaires mais n'a aucun
effet : pas d'élimination, pas de fin de partie anticipée, et le classement
final ignore le drapeau. Périmètre de l'US26.

## Trésorerie initiale non équilibrée

`CompanyInitialTreasury` vaut 1 000 000 en production pour 5 tours et une
masse salariale de l'ordre de 15 000 € par tour : la pression économique est
nulle et la faillite inatteignable. La valeur de développement (30 000) rend
la règle observable. À arbitrer avec le client.