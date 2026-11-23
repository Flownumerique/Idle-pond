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

**Références de l'auteur** : `docs/da/reference/` — `da-1-cycle.png` (de l'œuf à l'adulte),
`da-2-croissance.png` (la croissance, les nageoires qui deviennent des voiles),
`da-3-eveil.png` (les quatre éveils). Elles font la direction ; les maquettes en pixels en sont
tirées.

**Inspirations** : `docs/da/inspirations/_parcours.png` (le parcours du héros, de l'œuf à
l'aura) et `_planche.png` (les quatorze sujets) — les références découpées proprement et passées
en pixel art détaillé, sans la contrainte des gabarits ; découpes HD dans `hd/`, pixels dans
`pixels/`, produits par `outils/da/inspirations.py`. C'est la cible de détail du dessin.

**Premier essai à l'échelle du jeu** : `docs/da/apercu-sprites-v04.png`, un PNG par sujet dans
`docs/da/sprites-v04/`, produits par `outils/da/pixeliser.py` (réduction, alpha tranché, palette
du héros, lissage, contour). **C'est une base à reprendre à la main, pas un dessin fini** :
le grain des illustrations reste visible, l'œuf et la larve sont trop petits pour la méthode,
et l'anneau, les étincelles et l'aura — de la lumière douce dans les références — ne survivent
pas à l'alpha tranché : ils seront dessinés et animés en jeu, à part du corps.

**Dans le prototype** (2026-11-23, à l'essai, pas encore validé) : le corps des quatre stades
vient de la lignée `da-2`, avec son œil, les marques de la Noue et du Gour replacées sur ce
corps, et les quatre éveils en calques (runes ; rubans en deux images ; anneau et étoiles qui
tournent ; aura), seuils 1, 3, 6, 10. Tout est produit par `outils/da/heros.py` et se refait
d'une commande. Pas encore : l'œuf et la larve à la renaissance ; l'échelle 360 px.

**L'idée** : il commence **ordinaire** — l'alevin d'une ponte ordinaire (Codex, acte I). Son
corps gagne du détail avec sa **puissance** (son niveau, dans le cycle), et du merveilleux avec
son **éveil** (ses renaissances, qui ne se perdent jamais). La puissance se reperd à chaque
renaissance ; l'éveil reste. Un joueur voit donc d'un coup d'œil les deux : où il en est de
ce cycle, et ce qu'il est devenu.

**Sa lumière propre** (déjà dans le jeu) : elle complète celle de sa bande jusqu'à 1, pour qu'il
reste d'or partout. Plus la mare est sombre, plus son halo se voit sur l'eau autour de lui.

### Le visage

Grand œil sombre avec un éclat, petit sourire, joue rose : il a un visage dès l'alevin
(`da-2`). C'est ce qui en fait un personnage et non un poisson parmi d'autres.

### La naissance — l'œuf et la larve (`da-1`, les deux premiers)

L'œuf translucide, l'embryon enroulé autour de sa réserve d'or ; puis la larve pâle, presque
transparente, la réserve sous le ventre. **Proposé** : ils ne sont pas des stades de jeu ; ils
se jouent **à chaque renaissance**, en courte animation — l'œuf s'ouvre, la larve nage,
l'alevin apparaît. La renaissance devient une éclosion.

### La puissance — le niveau (`da-2`)

| Stade | Niveau | Ce qui s'ajoute |
|---|---|---|
| 0 · alevin | 1 à 3 | rond, simple, le visage, de petites nageoires |
| 1 | 4 à 15 | **les écailles** et **la ligne d'or** (des points dorés le long du flanc) |
| 2 | 16 à 255 | **les nageoires s'allongent** et s'effrangent |
| 3 | 256 et plus | **la queue en voile** : longue, ample, en plis, comme un combattant |

`da-1` propose une autre lignée adulte, plus fine et plus réaliste ; `da-2` est retenue ici
parce que la croissance s'y lit mieux. À trancher.

### L'éveil — les renaissances (`da-3`)

| Éveil | Renaissances (à régler) | Ce qui s'ajoute | Couleurs |
|---|---|---|---|
| 0 | 0 | rien | — |
| I | 1 | **les runes** : une spirale turquoise sur le flanc, des traits turquoise sur la dorsale, les nageoires et derrière l'œil | `#1EA8B8`, `#7FE6E8` |
| II | 3 | **le voile** : des rubans crème et turquoise qui sortent de la queue et ondulent derrière lui | `#FFF4D0`, `#7FE6E8` |
| III | 6 | **les étincelles** : des étoiles d'or à quatre branches qui tournent sur un anneau de lumière | `#FFE070`, `#FFFBEA` |
| IV | 10 | **l'aura** : un ovale de lumière dorée l'enveloppe, les étoiles se rassemblent au-dessus de sa tête ; son halo s'élargit | `#FFE070`, `#F8D070` |

**En jeu** : le corps (stade + runes) est un sprite ; le voile, l'anneau, les étoiles et l'aura
sont des calques à part, animés — le voile ondule, les étoiles tournent, toujours au pixel
entier. Rien n'est demi-transparent : l'aura est une trame et une lumière, pas un flou.

**Le lien au Codex** : l'éveil IV arrive vers la fin de l'axe des renaissances, quand il devient
**le demi-dieu** (§4). L'aura n'est pas une royauté — il ne veut pas régner — mais la lumière de
celui qui franchit et revient.

### Ce que ça demande au code

- **Des cadres plus grands** : la queue en voile et les rubans dépassent les gabarits actuels
  (65 × 31 au stade 3 ; les tirages font 70 × 45 et 80 × 39). Le passage à 360 px, déjà validé,
  est le bon moment pour les agrandir.
- **Les marques des lieux et les runes se partagent les nageoires** : le bleu des membranes du
  Gour et le turquoise des runes sont voisins. À distinguer — runes sur le flanc seulement, ou
  membranes dans un autre bleu.

### Ce que l'auteur doit trancher

La lignée de croissance (`da-2` ou `da-1`) ; l'œuf et la larve joués à la renaissance ; les
quatre éveils et leurs seuils (1, 3, 6, 10 proposés, pour un parcours visé de 14) ; le partage
des nageoires entre marques et runes ; faire ce travail avant ou avec le passage à 360 px.
