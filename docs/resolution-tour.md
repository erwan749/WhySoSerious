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
  ## Avancement des contrats et des formations (US14)

### Ordre de résolution

Quand le dernier joueur valide ses décisions, `GameFlowService.SubmitDecisions`
exécute la résolution dans cet ordre, à l'intérieur d'un `lock (game)` :

1. **Avancement** — `RoundResolver.AdvanceContractsAndTrainings(game, round)`
2. **Attribution** — `RoundResolver.ResolveTenders(round)`
3. *(à venir US15)* paiement des salaires
4. Envoi du bilan aux clients

Cet ordre n'est pas arbitraire : c'est lui qui implémente la règle
« ce qu'on prend à un tour démarre au tour suivant ».

### Règle « démarre au tour suivant »

| Élément | Créé par | Quand | Garde-fou |
|---|---|---|---|
| Contrat | `ResolveTenders` | **après** l'avancement | aucun : l'ordre suffit |
| Formation | validation du joueur | **avant** l'avancement | `StartedAtRound` |

Un contrat est créé par l'attribution, qui tourne *après* l'avancement : il ne
peut donc pas être décrémenté au tour où il est signé. Aucun champ
supplémentaire n'est nécessaire.

Une inscription en formation, elle, est créée pendant la phase de décision du
tour courant : elle existe déjà quand l'avancement s'exécute. On mémorise donc
le tour d'inscription dans `TrainingEnrollment.StartedAtRound` et l'avancement
ignore les inscriptions dont `StartedAtRound == round.Order`.

### Avancement des contrats

Pour chaque contrat `Active` de chaque entreprise :

- `RemainingRounds` est décrémenté ;
- s'il atteint `0`, le contrat passe à `Completed` et l'entreprise encaisse le
  budget de l'appel d'offres via `Company.RecordContractRevenue(int)`, qui
  crédite **à la fois** la trésorerie et le chiffre d'affaires (le CA sert au
  classement final, la trésorerie aux dépenses).

Les consultants ne sont pas « libérés » explicitement : la disponibilité est
calculée à la demande par `Company.IsConsultantBusy`, qui ne regarde que les
contrats `Active` et les formations `InProgress`. Le passage à `Completed`
rend donc les consultants réaffectables dès le tour suivant.

### Avancement des formations

Pour chaque inscription `InProgress` dont `StartedAtRound != round.Order` :

- `RemainingRounds` est décrémenté ;
- s'il atteint `0`, l'inscription passe à `Completed` et la compétence du
  consultant monte d'un niveau via `ConsultantSkill.LevelUp()`.

`LevelUp()` porte l'invariant de domaine (pas de montée au-delà de `Expert`) :
le niveau a un `private set`, il n'est modifiable que par cette méthode.

### Limite connue

Les chiffres affichés en cours de partie (écran « Mon entreprise ») ne
reflètent pas encore ces évolutions : le client garde l'instantané
`CompanyDto` reçu au `GameStarted`, et `RoundResultDto` est encore envoyé
vide. Le classement final, construit côté serveur, est quant à lui juste.
La mise à jour du bilan en cours de partie est le périmètre de l'**US16**.

## Paiement des salaires (US15)

### Place dans la résolution

Le prélèvement est la troisième et dernière étape de la résolution d'un tour :

1. avancement des contrats et formations
2. attribution des appels d'offres
3. **paiement des salaires**
4. envoi du bilan

L'ordre est voulu : un contrat qui s'achève a déjà versé son budget quand le
prélèvement a lieu. L'inverse ferait tomber une entreprise dans le rouge le
tour même où un contrat l'a payée.

### Règle

Chaque entreprise paie, à chaque tour, le salaire de **tout** son staff —
en mission, en formation ou sur le banc. C'est le contrepoids économique du
jeu : un consultant inoccupé coûte autant qu'un consultant productif, donc il
faut le placer.

Le salaire n'est pas une constante de configuration : il est porté par le
consultant (`Consultant.SalaryRequirement`), tiré entre 3 000 € et 5 000 € par
`ConsultantFactory` à la création de la partie. La masse salariale d'une
entreprise est donc dérivée à la demande par `Company.GetPayroll()`, jamais
stockée — un champ stocké devrait être maintenu à chaque recrutement (US24)
et à chaque licenciement.

### Faillite

Si la trésorerie passe sous zéro après le prélèvement, l'entreprise est
déclarée en faillite via `Company.IsBankrupt`. Deux choix de conception :

- **la trésorerie n'est pas bornée à zéro** : elle devient négative, ce qui
  rend l'ampleur du déficit visible dans les journaux ;
- **la faillite est définitive** : un encaissement ultérieur ne relève pas
  l'entreprise. C'est un événement, pas un état transitoire — sinon le
  classement final dépendrait de l'ordre des opérations du dernier tour.

Le drapeau est levé par `Company.RecordSalaryPayment()` elle-même, pas par le
service appelant : la règle « faillite = trésorerie négative après paiement »
appartient au domaine, comme l'écriture de la trésorerie, dont le `set` reste
privé.

### Conséquence de jeu : non implémentée

Lever le drapeau n'a, à ce stade, **aucun effet sur le déroulement de la
partie**. L'entreprise en faillite continue de jouer, et le classement final
reste trié sur le seul chiffre d'affaires.

La règle retenue avec le client est : une entreprise en faillite est éliminée,
et la partie s'arrête quand il reste moins de deux entreprises solvables — à
deux joueurs, une faillite met donc fin à la partie immédiatement. Cette règle
est le périmètre de l'**US26**, sortie de l'US15 parce qu'elle touche la
boucle de tour, la condition de fin de partie, le critère de tri du classement
et le client.

Un point d'attention pour l'US26 : la condition de résolution d'un tour
compare `round.SubmittedPlayerIds.Count` à `game.Players.Count`. Un joueur
éliminé ne soumettant plus, le dénominateur devra devenir le nombre de joueurs
encore en jeu, sans quoi la partie se figerait sans erreur visible.