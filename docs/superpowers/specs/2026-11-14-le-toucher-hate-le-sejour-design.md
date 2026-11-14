# Le toucher hâte le séjour — design (court)

Écrit le 2026-11-14, à partir du Finding 10 de `RESULTATS.md` : le toucher ne compte qu'avant le
premier blocage de la première vie. Ensuite la contenance décide, et un toucher remplit une mare
déjà pleine. L'utilisateur veut qu'il **gagne un rôle**.

## Intention

Ce qu'on attend pendant 94 à 98 % d'une vie, ce n'est pas du mana, c'est l'acquis de séjour qui
fait monter ce qu'on peut porter. Le toucher agit désormais là-dessus : **toucher la mare, c'est
s'y faire plus vite**. Le joueur attentif raccourcit ses vies ; celui qui ne touche jamais garde
le rythme mesuré, et le jeu reste un idle (intention de la spec du 2026-10-11 : « un coup de
pouce »).

Non-buts : toucher ne donne pas de Souffle, ne touche pas au pic, ne passe pas la contenance.
Anti-but : un toucher qui ferait sauter la vie entière (l'acquis sature, donc c'est exclu par
construction).

## Approches considérées

| Approche | Pour | Contre | |
|---|---|---|---|
| **Hâter le séjour** | Le seul levier qui change le rythme (Finding 8) ; se voit sur la jauge ; sature seul | Le jeu actif raccourcit les vies : à doser | **Retenue par l'utilisateur** |
| Mare pleine → Souffle | Vise exactement le cas mort | Conversion à régler sur une échelle exponentielle ; récompense l'autoclicker | Écartée |
| Le remous (multiplicateur de production temporaire) | Simple à lire | Le pic, donc le Souffle, se gonfle d'une rafale avant de rentrer | Écartée |

## Ce qui a été décidé

- **Dosage** (utilisateur) : un toucher vaut `SECONDES_DE_SEJOUR_PAR_TOUCHER = 0,25` s de séjour.
  À deux touchers par seconde, le séjour monte ×1,5 ; à la borne (10/s), ×3,5.
- **L'amélioration du toucher** (utilisateur) : chaque rang ajoute une part fixe au séjour d'un
  toucher, et elle devient **bornée**, comme celle de séjour. Elle garde son effet sur le mana.
  Graines : `+10 %` de séjour par rang, `10` rangs au plus.

## Le mécanisme

- `Reducteur.Toucher(etat, fois)` fait deux choses, dans cet ordre :
  1. l'acquis avance de `fois × SejourDuToucher(etat)` secondes, par **la même courbe que le
     temps** (`A∞ + (A − A∞)·exp(−s/τ)`, `τ` amélioration de séjour comprise). L'acquis sature,
     donc toucher ne dépasse jamais le plafond d'une vie ; n touchers d'un coup valent toujours
     n touchers un à un ;
  2. le mana gagne la valeur des touchers, plafonnée par la contenance **après** l'avance.
  Rien d'autre ne bouge : ni le temps de jeu, ni la durée du cycle, ni le pic.
- `Economie.SejourDuToucher(etat) = SECONDES_DE_SEJOUR_PAR_TOUCHER × (1 + 0,1 × rang)`.
- `AmeliorationAuMaximum` couvre la portée `Toucher` (rang max 10). `donnees.json` suit.
- Un toucher n'est plus jamais « refusé » tant que l'acquis n'est pas exactement au plafond :
  `Partie.Toucher` le compte dans la cadence et émet le changement.

## L'écran

- L'envol garde `+gain` en mana quand il y en a. **Mare pleine** : il dit ce que le toucher a
  ajouté à ce qu'on peut porter, `+N à porter` (contenance après − avant). *(Appel de Claude.)*
- La fiche du héros, tuile « Par toucher » : la valeur en mana, et en dessous le séjour d'un
  toucher. *(Appel de Claude.)*
- Le texte de l'amélioration dit les deux effets.

## Le simulateur et la mesure

- Le simulateur, qui sait toucher (`Politique.ToucherParSeconde`), applique le même
  `Reducteur.Toucher` : rien à écrire en double.
- `MesureDuToucherTests` est relancée : durée des vies et jeu actif sur quinze vies, par cadence.
  Le Finding 10 est complété avec les nouveaux chiffres.
- Parité : la politique historique ne touche pas et n'achète pas l'amélioration du toucher ; les
  parties de référence ne bougent pas. Seul `donnees.json` change (rang max).

## Laissé au constructeur

Le libellé exact des textes à l'écran (passés par `LexiqueTests`), l'endroit du test du plancher de
cadence si le joueur scripté, qui touche toutes les deux secondes, en change le chiffre.
