# Limitations connues

[← Retour à l'index](README.md)

Volontairement hors périmètre, documenté ici plutôt que corrigé en silence :

- **Pas d'authentification/autorisation.** Une revue de sécurité automatisée a trouvé de vrais problèmes ici (`PlayerId`/`clientId` fournis par le client et acceptés sans vérification, pas de `[Authorize]` sur les hubs) — suivi séparément, non corrigé par le travail DTO/layering décrit dans [architecture.md](architecture.md).
- Concurrence : les collections d'`AppMemory` (`Players`/`Games`) sont désormais des `ConcurrentDictionary`, mais les **opérations composées** ne sont pas encore synchronisées — `GameService.JoinGame` fait un « trouver-puis-muter » (une partie peut être supprimée entre les deux), et `Game.Players` reste une `List` mutée par des joins/leaves simultanés sur la même partie. À traiter avec les règles de `JoinGame` (verrouillage par agrégat).
- La **formation ne peut faire progresser qu'une compétence déjà possédée** : `ConsultantSkill.LevelUp` avance d'un cran, rien ne permet d'accorder une compétence nouvelle. Un consultant ne peut donc pas se reconvertir, seulement se spécialiser. À trancher avec le client (US25).