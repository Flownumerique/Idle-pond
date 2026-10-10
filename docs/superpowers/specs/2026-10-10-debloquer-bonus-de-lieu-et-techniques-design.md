# L'onglet « Débloquer » — bonus de lieu et techniques — design

Écrit le 2026-10-10, à partir de la maquette de l'utilisateur (canevas « IdlePond — Écran
principal mobile », onglet Débloquer : sous-onglets Lieux, Bonus de lieu, Techniques). Ce que
l'utilisateur a demandé : « dans Débloquer je veux d'autres types d'améliorations, avec des
bonus de zone ». Puis : la mécanique complète, pas seulement l'écran.

## Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| Un bonus peut-il monter la production ? | **Oui**, la règle n° 3 du contrat est amendée : un bonus de lieu monte la production des espèces de son lieu |
| Les techniques de l'onglet | **Au mana**, comme la maquette, et non l'arbre à points du noyau v1.0 §6 |
| Les bonus à la renaissance | **Perdus**, comme les quatre autres achats au mana |
| L'ordre | Le noyau d'abord (compilé et testé hors Unity), l'interface ensuite |

## §1. La mécanique

- **Un registre**, `RegistreDesBonus` : chaque `Bonus` a un genre (réduction de coût,
  production, confort, verbe), un terme nommé, une part par rang, un rang maximal, une
  maîtrise requise et un palier de prix. `Assise` nomme son lieu ; null, c'est une technique.
- **La maîtrise** d'un lieu : ses paliers ouverts dans la vie courante (0 à son nombre de
  paliers). Pour une technique, la maîtrise requise se lit sur les paliers ouverts en tout.
- **Le prix** : `coût_base(palier de prix) × COUT_DE_BONUS_RELATIF × RATIO_COUT_DE_BONUS ^ rang`.
- **Les effets** : une réduction compose `(1 − part)^rang` sur ce qui se paie dans le lieu (ou
  partout, pour une technique) ; une production `(1 + part)^rang` sur les espèces du lieu, par
  le terme `MultiplicateurDeLieu`, qui a sa ligne dans le détail de captation ; un confort
  `1 + part × rang` (la patience : l'absence comptée) ; un verbe ouvre une capacité.
- **Les verbes** (`Automatismes`) jouent après chaque pas de jeu, jamais dans le tick : le pas
  reste homogène, le hors ligne ne les fait pas tourner. La main sûre monte le cran le moins
  cher s'il coûte moins d'un dixième du mana ; la sonde creuse dès que la roche le permet.
- **L'état** : `EtatCycle.Bonus`, rangs par identifiant, dans l'ordre du registre. Sauvegardé
  sous `cycle.bonus` ; une save plus ancienne se lit sans bonus. Pas de nouvelle version de save.
- **La parité** : un facteur neutre n'est jamais multiplié. Les parties de référence, qui
  n'achètent aucun bonus, se rejouent au bit près.

## §2. Le contenu

Huit bonus de lieu (quatre pour la Noue, quatre pour le Gour) et sept techniques, dont deux
verbes. Toutes les valeurs sont des graines `[P]` : ni le simulateur ni une vraie partie ne
les ont mesurées. Le simulateur n'achète aucun bonus.

## §3. L'interface

Un cinquième bouton au dock, **Débloquer**, entre Espèces et L'œuf ; sa pastille, couleur du
mana, compte les bonus achetables. Le tiroir porte trois puces : **Lieux** (chaque lieu
atteint, sa maîtrise et ce qu'elle ouvre ; le suivant fermé, avec sa condition ; « ??? »
au-delà du livré), **Bonus de lieu** (groupés par lieu atteint) et **Techniques**. Chaque
rangée : une icône par genre, le nom, l'étiquette du genre, l'effet d'un rang, la barre des
rangs, et Acheter (éteint tant qu'on ne peut pas payer), « max » au dernier rang, ou ce qui
l'ouvrira.

Le reste de la maquette (fusion Toi + Espèces en « Améliorer », mare à toucher, popups
d'options et de fiche) n'est pas porté : la mare à toucher serait une mécanique neuve.

## §4. Ce qui reste

- **La mesure** : faire acheter les bonus par une politique du simulateur, puis régler les
  graines.
- **L'arbre de technique du noyau v1.0 §6** (points gagnés par l'usage, permanent) reste
  vide et n'est pas touché. Les techniques de l'onglet en sont distinctes ; le chantier n° 3
  de la roadmap est à rediscuter avec cette décision.
