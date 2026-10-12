# Fonctionnalité — Résolution du tour : attribution des appels d'offres (US13)

[← Retour à l'index](README.md)

## Contexte

Jusqu'ici, une candidature restait `Pending` indéfiniment : aucun contrat n'était créé, aucun
consultant n'était immobilisé, et personne ne gagnait d'argent. Les joueurs décidaient, le serveur
enregistrait, et rien n'en sortait.

## Principe

À la dernière soumission d'un tour, chaque appel d'offres est attribué. Une stratégie désigne la
candidature gagnante, qui devient un `Contract` actif chez son entreprise ; les autres passent à
`Lost`.

Le critère d'attribution est isolé derrière `IAwardStrategy` : c'est une règle de jeu que le client
peut encore changer, pas une mécanique figée.

## Flux
```
GameFlowService.SubmitDecisions (sous lock (game), à la dernière soumission)
    │
    ├─▶ round.IsCompleted = true
    │
    └─▶ RoundResolver.ResolveTenders(round)
    │
    └─▶ Pour chaque Tender du tour :
        ├─ candidatures Pending qui le visent, dans l’ordre d’arrivée
        ├─ IAwardStrategy.SelectWinner → gagnante ou null
        ├─ gagnante → Won, les autres → Lost
        └─ Contract actif chez la gagnante (durée = RoundsNumber du tender)
```
## Le critère retenu : meilleure couverture

**Éligibilité** (`TenderCoverage.IsAdmissible`) : la candidature engage au moins la main-d'œuvre
exigée, et chaque compétence exigée est couverte au niveau demandé ou au-dessus par au moins un
consultant engagé.

**Classement** (`TenderCoverage.CoverageScore`) : pour chaque compétence exigée, l'écart entre le
meilleur niveau engagé et le niveau demandé ; le total forme le score. La candidature au score le
plus élevé l'emporte.

**Départage** : à score égal, la candidature la plus ancienne gagne.

## Fichiers modifiés

| Fichier | Changement |
|---|---|
| `src/SeriousGame.Server/Application/Abstractions/IAwardStrategy.cs` | Nouveau : contrat du critère d'attribution |
| `src/SeriousGame.Server/Application/Services/BestCoverageAwardStrategy.cs` | Nouveau : critère « meilleure couverture » |
| `src/SeriousGame.Server/Application/Services/TenderCoverage.cs` | Nouveau : éligibilité et score de couverture |
| `src/SeriousGame.Server/Application/Services/RoundResolver.cs` | Nouveau : attribution, statuts et création des contrats |
| `src/SeriousGame.Server/Application/Services/GameFlowService.cs` | `SubmitDecisions` résout le tour une fois tout le monde soumis |
| `src/SeriousGame.Server/Program.cs` | Enregistrement de `IAwardStrategy` et `RoundResolver` |
| `tests/SeriousGame.UnitTests/*` | Nouveaux : couverture, stratégie, resolver |

## Décisions de conception

- **Meilleure couverture plutôt que moins-disant ou premier arrivé.** Le moins-disant exigerait un
  champ prix sur `TenderApplication`, donc une modification du contrat commun. Le premier arrivé
  récompenserait la vitesse de frappe. La couverture est le seul critère qui relie l'attribution aux
  décisions du joueur — et qui donne un intérêt à la formation : former un consultant, c'est
  augmenter son score sur les contrats à venir.
- **Départage par ancienneté, pas au hasard.** Un même tour rejoué doit donner le même résultat,
  sinon les tests deviennent ingérables et un joueur ne peut pas comprendre pourquoi il a perdu. Le
  tri repose sur la stabilité documentée d'`OrderByDescending`, verrouillée par un test.
- **L'éligibilité est séparée du critère.** `TenderCoverage` vit à part de la stratégie : avoir assez
  de consultants et couvrir les compétences vaut quel que soit le critère retenu. Une autre stratégie
  la réutilisera sans la dupliquer.
- **La stratégie ne modifie rien.** Elle choisit, le `RoundResolver` applique. Une stratégie qui
  muterait les statuts serait impossible à remplacer sans surprise.
- **Le contrat ferme la boucle.** Dès sa création, ses consultants deviennent occupés au sens de
  `Company.IsConsultantBusy` : `RoundFactory` ne les prendra plus comme équipe de référence, et le
  validateur refusera de les engager ailleurs. Aucune synchronisation à écrire, c'est la même règle
  lue des deux côtés.
- **Résolution sous `lock (game)`**, une fois seulement, à la dernière soumission. Une résolution
  déclenchée à chaque soumission attribuerait les contrats au premier joueur à valider.

## Limites connues / suite possible

- Le critère doit être **confirmé par le client**. Si Michel Girard tranche autrement, c'est une
  nouvelle classe de stratégie et une ligne dans `Program.cs`, pas une reprise du resolver.
- Une candidature qui ne couvre pas l'appel d'offres est acceptée puis perdue, plutôt que refusée à
  la saisie : le joueur peut tenter sa chance. À rediscuter si le playtest montre que c'est perçu
  comme un bug.
- L'avancement des contrats (US14), les salaires (US15) et le bilan du tour (US16) ne sont pas
  encore là : un contrat créé ne se termine jamais et ne rapporte rien. Les points d'insertion sont
  marqués par des TODO dans `SubmitDecisions`.