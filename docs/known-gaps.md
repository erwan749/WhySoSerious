# Limitations connues

[← Retour à l'index](README.md)

Volontairement hors périmètre, documenté ici plutôt que corrigé en silence :

- **Pas d'authentification/autorisation.** Une revue de sécurité automatisée a trouvé de vrais problèmes ici (`PlayerId`/`clientId` fournis par le client et acceptés sans vérification, pas de `[Authorize]` sur les hubs) — suivi séparément, non corrigé par le travail DTO/layering décrit dans [architecture.md](architecture.md).
- Concurrence : les collections d'`AppMemory` (`Players`/`Games`) sont désormais des `ConcurrentDictionary`, mais les **opérations composées** ne sont pas encore synchronisées — `GameService.JoinGame` fait un « trouver-puis-muter » (une partie peut être supprimée entre les deux), et `Game.Players` reste une `List` mutée par des joins/leaves simultanés sur la même partie. À traiter avec les règles de `JoinGame` (verrouillage par agrégat).
- La **formation ne peut faire progresser qu'une compétence déjà possédée** : `ConsultantSkill.LevelUp` avance d'un cran, rien ne permet d'accorder une compétence nouvelle. Un consultant ne peut donc pas se reconvertir, seulement se spécialiser. À trancher avec le client (US25).
## Écran « Mon entreprise » périmé en cours de partie

`ClientSession.MyCompany` n'est alimenté qu'une fois, au `GameStarted`
(`GameLoop.OnGameStarted`). Le serveur envoie `RoundResultDto` sans remplir
`Companies`. Conséquence : statut des consultants, trésorerie et chiffre
d'affaires restent figés à l'écran pendant toute la partie, alors que le
serveur les met bien à jour (confirmé par le classement final).

Correction prévue : US16 (remplir `RoundResultDto.Companies` et appeler
`SetMyCompany` sur `RoundResolved`).

## Faillite sans conséquence

`Company.IsBankrupt` est levé par le paiement des salaires mais n'a aucun
effet : pas d'élimination, pas de fin de partie anticipée, et le classement
final ignore le drapeau. Périmètre de l'US26.

## Trésorerie initiale non équilibrée

`CompanyInitialTreasury` vaut 1 000 000 en production pour 5 tours et une
masse salariale de l'ordre de 15 000 € par tour : la pression économique est
nulle et la faillite inatteignable. La valeur de développement (30 000) rend
la règle observable. À arbitrer avec le client.