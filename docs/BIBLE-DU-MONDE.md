# IdlePond — la bible du monde

**Version** : 0.3 — 2026-11-23
**Statut** : espace des **propositions** de monde, avant validation par l'auteur. Ce qui est
validé passe dans `docs/CODEX.md` (fiction) et `docs/da/gabarits.md` (dessin), et sort d'ici.

**Historique.** La v0.1 (magies, tribus, quinze espèces à venir, peuples, panthéon, faune) a été
validée en entier le 2026-11-22 et fait foi au Codex v1.3, §2 ; son texte reste dans
l'historique git (commit `2a74e3b`). La v0.2 (la dérivation des six espèces livrées) a été
validée le 2026-11-23 et fait foi au Codex v1.4 ; son texte reste au commit `cf80bce`.

---

## Dernière proposition validée

**Les couleurs de la Noue, du Gour et du héros** (v0.3) ont été validées le 2026-11-23, avec
des membranes fines — un trait de bleu entre deux rayons, l'orange des nageoires reste. Elles
font foi au Codex v1.5 (§2, « Les lieux et le héros à l'écran ») et dans `docs/da/gabarits.md`
(palettes) ; elles sont dans le jeu, à l'échelle actuelle (240 px). Maquette :
`docs/da/apercu-lieux-v2.png`. Le texte de la proposition reste dans l'historique git (commit
`cc13364`).

---

## Proposition en cours — le héros qui s'éveille (v0.4)

**Maquette** : `docs/da/apercu-eveil-v1.png` — au fond du Gour, sous sa lumière propre.

**L'idée** : il commence **ordinaire** — l'alevin d'une ponte ordinaire (Codex, acte I). Son
corps gagne du détail avec sa **puissance** (son niveau, dans le cycle), et du merveilleux avec
son **éveil** (ses renaissances, qui ne se perdent jamais). La puissance se reperd à chaque
renaissance ; l'éveil reste. Un joueur voit donc d'un coup d'œil les deux : où il en est de
ce cycle, et ce qu'il est devenu.

**Sa lumière propre** (déjà dans le jeu) : elle complète celle de sa bande jusqu'à 1, pour qu'il
reste d'or partout. Plus la mare est sombre, plus son halo se voit sur l'eau autour de lui.

### La puissance — le niveau (les quatre stades déjà dessinés)

| Stade | Niveau | Ce qui s'ajoute |
|---|---|---|
| 0 · alevin | 1 à 3 | rien : le poisson d'or, simple |
| 1 | 4 à 15 | **les écailles** : des arcs ambre tramés sur le flanc |
| 2 | 16 à 255 | **la ligne d'or** : des points clairs le long du flanc, de la queue aux branchies |
| 3 | 256 et plus | **les bouts de lumière** : le bout de la queue pâlit, la matière devient lumière |

### L'éveil — les renaissances

| Éveil | Renaissances (à régler) | Ce qui s'ajoute | Couleurs |
|---|---|---|---|
| 0 | 0 | rien | — |
| I | 1 | **les runes de mana** : un à trois signes sur le flanc, là où il a pris le mana des morts | `#4FCDCD` mana, `#9FFFF0` |
| II | 3 | **le voile** : trois rubans qui prolongent la queue et ondulent, et des mèches au-dessus de la dorsale, de plus en plus clairsemés en s'éloignant | `#FFF4D0` or pâle, `#BFF4FF` bleu d'âme |
| III | 6 | **les étincelles** : de petites croix de lumière qui tournent autour de lui, plus nombreuses à chaque renaissance | `#FFFFFF`, `#FFE070`, `#BFF4FF` |
| IV | 10 | **la couronne** : trois pointes de lumière au-dessus de la tête ; son halo s'élargit | `#FFE070`, `#FFF8E0`, `#FFFFFF` |

**En jeu** : le voile ondule et les étincelles tournent (animés, toujours au pixel entier) ; les
runes et la couronne sont fixes. Le halo de sa lumière s'élargit d'un cran par éveil. Rien n'est
demi-transparent : l'« éthéré » est une trame, comme les rayons.

**Le lien au Codex** : l'éveil IV arrive vers la fin de l'axe des renaissances, quand il devient
**le demi-dieu** (§4). La couronne n'est pas une royauté — il ne veut pas régner — mais la lumière
de celui qui franchit et revient.

### Ce que l'auteur doit trancher

Les quatre détails de puissance ; les quatre éveils et leur ordre ; les seuils de renaissances
(1, 3, 6, 10 proposés, pour un parcours visé de 14) ; les couleurs (mana et bleu d'âme).
