# IdlePond — Noyau de jeu

**Version** : 1.0 — 2026-08-24
**Tier** : 2
**Statut** : **remplace intégralement** les quatre mémos précédents, devenus contradictoires entre eux. Ce document est la référence unique pour le noyau mécanique.

| Mémo | Sort |
|---|---|
| `idlepond-arbitrages-v0_1.md` | Obsolète — sauf §1.3 (types) et §1.4 (sauvegarde), repris ici |
| `idlepond-technique-v0_1.md` | Obsolète — l'arbre est réécrit au §6 |
| `idlepond-refonte-captation-v0_1.md` | Obsolète — la refonte est intégrée |
| `idlepond-calibrage-v0_1.md` | Obsolète — la population n'existe plus |

Ne survivent d'eux que ce qui est réécrit ici.

---

# 1. Ce que le jeu est

Un idle incrémental. Le joueur descend dans une eau de plus en plus profonde, y installe des espèces qui produisent du mana, et retourne périodiquement dans l'œuf pour en ressortir capable d'aller plus bas.

## 1.1 La boucle

```
produire du mana → creuser le palier suivant → débloquer une espèce
→ monter ses niveaux → produire plus → creuser plus bas
→ le palier suivant coûte plus que ce qu'on peut contenir
→ retourner dans l'œuf → recommencer plus grand
```

## 1.2 Les trois achats

| Achat | Nature | Coût |
|---|---|---|
| **Creuser** | Ouvre le palier suivant | Jalon, ×2.4 |
| **Débloquer une espèce** | Une fois par espèce | Jalon |
| **Améliorer une espèce** | Répétable — monte son niveau | ×1.15 |

Rien d'autre ne se paie en mana. Il n'y a **ni gestion de population, ni placement, ni capacité de palier** : le contenu de chaque palier est écrit d'avance, et une espèce est un générateur avec un niveau.

## 1.3 Ce que le joueur ne voit pas

La population existe **à l'écran et dans la fiction**, jamais dans l'état. Monter le niveau d'une espèce, c'est son banc qui grossit — mais aucun individu n'est compté, aucune mortalité n'est simulée, aucun repeuplement n'est attendu.

> **Le joueur améliore une espèce. Il ne gère pas un cheptel.**

---

# 2. La production

```
production(espèce) = débit_base × niveau × mult_seuil × mult_bénédictions
production totale   = Σ espèces × mult_densité × mult_technique
```

## 2.1 Les seuils

| Niveau | Multiplicateur sur l'espèce |
|---|---|
| 10 | ×2 |
| 25 | ×4 |
| 50 | ×8 |
| 100 | ×16 |

**Et au niveau 100, l'espèce accorde en plus +3 % de production globale, définitif.**

C'est ce qui empêche les premières assises de devenir un arrondi. Avec `D` = 2.31, un palier situé dix crans en arrière produit 4 000 fois moins ; sans le bonus global, remplir la mare au cycle 15 ne rapporterait rien, et la moitié du contenu textuel s'adresserait à des lieux que plus personne ne regarde.

30 espèces × 3 % ≈ **+90 % en fin de partie**.

## 2.2 La contenance, et pourquoi elle force le reset

Le héros ne peut **stocker** qu'une quantité limitée de mana : sa contenance. Elle ne limite pas sa production ni ses dépenses courantes — elle limite le stock.

Or le coût d'un palier croît de ×2.4. Vient donc le moment où **le palier suivant coûte plus que ce qu'il peut contenir**.

> **La contenance est ce qui force l'éclosion, et c'est ce pour quoi il retourne dans l'œuf.**

Le blocage est **doux** : il peut continuer à jouer indéfiniment, monter des niveaux et accumuler de la Foi. Il ne peut simplement plus descendre.

C'est la seule vraie décision du jeu : **partir maintenant pour la profondeur, ou rester pour la Foi.**

---

# 3. L'éclosion

**Terme retenu.** Remplace « la ponte », qui décrivait l'acte d'une femelle alors que c'est lui qui retourne dans l'œuf. Le mot est déjà au canon — le héros a capté sa mutation *à l'éclosion* : chaque cycle rejoue son origine. Et il nomme le gain, jamais la perte.

## 3.1 Reset complet

| Remis à zéro | Conservé |
|---|---|
| Mana | **Contenance** — le plafond, relevé |
| Paliers creusés | **Densité** — multiplicateur global |
| Espèces débloquées | **Foi et bénédictions** |
| Niveaux d'espèce | **Technique** |

**Tout se repaie au prix d'origine.** `f` = 1. `[P5]` fermé : il n'y a pas de tarif réduit à la redescente, comme dans n'importe quel idle.

## 3.2 Les deux gains

**La contenance** monte en fonction de ce qui a été atteint pendant la vie. C'est ce qui autorise à descendre plus bas au cycle suivant.

**La densité** monte de `profondeur_atteinte ^ α`, ne redescend jamais, et donne un multiplicateur global :

```
mult_densité = (1 + densité / d₀) ^ 0.5
```

Diégétiquement : l'eau se souvient de tout ce qu'elle a porté, la roche ne se souvient de rien. C'est pour ça que les galeries s'effondrent et que le multiplicateur reste.

## 3.3 Les deux monnaies de prestige ne se marchent pas dessus

| | Comment elle monte | Ce qu'elle fait |
|---|---|---|
| **Densité** | Automatiquement, à chaque éclosion | Multiplie la production. Aucun choix |
| **Foi** | Produite pendant la vie, encaissée à l'éclosion | S'achète en bénédictions. Choix du joueur |

---

# 4. La Foi et les bénédictions

## 4.1 Le principe

La Foi monte du peuple vers lui, et redescend sur le peuple en bénédiction. **Il devient un dieu en faisant ce qu'un dieu fait.**

Les bénédictions sont **permanentes** et survivent à l'éclosion. Elles s'achètent **dans l'œuf**, sur un écran dédié — qui devient donc l'écran d'améliorations du jeu, comme le veut la convention du genre.

## 4.2 Ciblée et globale

| Portée | Forme | Domine |
|---|---|---|
| **Ciblée** | **Multiplicateur** sur une espèce nommée, empilable | Tard |
| **Globale** | **Additif** sur le débit de base de toutes les espèces, présentes et futures | Tôt |

L'additif écrase quand les débits sont minuscules et devient négligeable une fois les multiplicateurs décollés. **Le croisement se produit tout seul : aucun paramètre à régler.**

Un ratio d'efficacité réduit ne marchait pas — avec `D` = 2.31 aucune espèce ne dépasse 33 % du revenu, donc le global aurait gagné dans tous les cas.

## 4.3 Les miracles — **gelés**

La question de leur articulation avec les bénédictions n'est pas tranchée et ne bloque rien. À reprendre quand le noyau tourne. `[P26]` **gelé**, pas fermé.

---

# 5. Géographie et contenu

## 5.1 Le compte

| | |
|---|---|
| Assises | 6 |
| Paliers | **62** — 6 / 12 / 12 / 12 / 12 / 8 |
| Éclosions | 15 |
| Espèces de base | **24** — 4 par assise |
| Divergences par temple | ~6, écrites d'avance |

Étang des Merveilles portait 38 espèces sur 11 zones. **Le budget de contenu est prouvé faisable par l'historique du projet** — c'est le seul chiffre de ce document qui repose sur une donnée réelle plutôt que sur une déduction.

## 5.2 Le rythme dans une assise de 12 paliers

| Palier | Ce qui arrive |
|---|---|
| 1, 4, 7, 10 | **Une nouvelle espèce** |
| autres | De la place — coût et production |

Un palier sur trois apporte du contenu, les deux autres du volume. L'assise I à 6 paliers en porte 2, aux paliers 1 et 4.

Sur les trois paliers séparant deux espèces, le total est ×12.3. **Une espèce nouvelle, à pleine puissance, produit donc environ 1,3 fois tout ce qui existait avant elle** — courbe de générateurs classique.

## 5.3 Répartition plate

4 paliers par cycle, constant. La croissance de la durée est portée par l'écart `g/D` (§7) ; un nombre de paliers croissant l'accélérerait une seconde fois et ferait exploser le cycle 15.

---

# 6. La technique

Un arbre de six branches, payé en **points gagnés par l'usage**, jamais en mana ni en Foi. Un arbre payé en monnaie redeviendrait le Corail de Prestige que l'Annexe B a supprimé.

## 6.1 Le point

```
points(branche) = floor( A · ln(1 + compteur / B) )
```

Le logarithme normalise des compteurs incommensurables, donne les rendements décroissants natifs du genre, se calcule en un pas — et **tue le farm** : rester dix cycles dans la même assise n'ouvre plus de points. La seule façon d'en gagner est de faire des choses nouvelles, donc de descendre.

Rendement total sur une partie ≈ 180 points, coût des 30 nœuds ≈ 167. **Le joueur finit par tout avoir, mais pas avant l'assise V.** Le choix est l'ordre, pas la liste.

Coûts par nœud : **5 / 12 / 25 / 45 / 80**, identiques dans toutes les branches. Les compteurs survivent à l'éclosion et ne se redistribuent jamais.

## 6.2 Les six branches

**Creusement** — compteur : mana dépensé à creuser

| # | Nœud | Effet |
|---|---|---|
| 1 | Étaiement | Coût de creusement −10 % |
| 2 | Drainage | Coût de creusement −20 % |
| 3 | **La file** | *verbe* `file_de_descente` |
| 4 | Percement guidé | Coût de creusement −30 % |
| 5 | **Galeries connues** | *verbe* `creusement_auto` sur les paliers déjà ouverts dans une vie précédente |

**Amélioration** — compteur : mana dépensé en niveaux

| # | Nœud | Effet |
|---|---|---|
| 1 | Répétition | Coût de niveau −8 % |
| 2 | **La main sûre** | *verbe* `achat_auto` — achète en continu le niveau le moins cher |
| 3 | Geste appris | Coût de niveau −15 % |
| 4 | Élan | Toute espèce débloquée démarre au niveau 5 |
| 5 | **Le banc suit** | *verbe* `achat_auto_max` — vise le prochain seuil |

**Recrutement** — compteur : espèces débloquées, cumulées sur toutes les vies

| # | Nœud | Effet |
|---|---|---|
| 1 | Approche | Coût de déblocage −15 % |
| 2 | **Les habitudes tiennent** | *verbe* `deblocage_auto` pour une espèce déjà connue |
| 3 | Mémoire des gestes | Coût de déblocage −30 % |
| 4 | Réputation | +5 % de production par espèce débloquée dans l'assise courante |
| 5 | **Le refuge est connu** | Coût de déblocage −50 % |

**Entretien** — compteur : heures hors ligne **effectivement créditées**

| # | Nœud | Effet |
|---|---|---|
| 1 | La relève | Plafond hors ligne 6 h → 12 h |
| 2 | Veille des bancs | Plafond 12 h → 24 h |
| 3 | **Compter les bancs** | *verbe* `lecture_debits` |
| 4 | Le peuple continue | Les achats automatiques tournent hors ligne |
| 5 | **Ce qui s'est passé pendant** | *verbe* — rapport de retour détaillé |

> Le compteur lit les heures **créditées**, jamais le temps écoulé. Sans ça, avancer son horloge farme l'arbre permanent — et c'est la seule protection nécessaire (§8.2).

**Construction** — compteur : temples et portails bâtis

| # | Nœud | Effet |
|---|---|---|
| 1 | Fondations | Coût de temple −20 % |
| 2 | Tunnels | Coût de portail −25 % |
| 3 | **Signalétique** | *verbe* `navigation_directe` |
| 4 | Réouverture | Coût de réouverture −50 % |
| 5 | Collecteurs | Charge alliée par réponse +30 % |

**Éclosion** — compteur : éclosions, et densité acquise

| # | Nœud | Effet |
|---|---|---|
| 1 | Pondre à l'heure | `α` +0.05 |
| 2 | **Lire l'eau** | *verbe* — le gain d'éclosion prévu s'affiche en permanence |
| 3 | Ce qui reste | Densité conservée +10 % |
| 4 | Coquille épaisse | Contenance de départ améliorée |
| 5 | **Le chemin connu** | *verbe* `retour_rapide` — rachète automatiquement ce qui était acquis |

Le nœud 5 est le capstone du jeu. C'est le classique « re-achat de fin de partie » qui rend les dernières éclosions indolores.

## 6.3 Trois règles dures

**Une capacité a exactement une source.** Les verbes viennent de l'arbre et des succès ; un même `CapaciteId` ne doit jamais être atteignable par les deux. La source est déclarée dans la donnée.

**Le budget de verbes est commun.** L'arbre en porte 10 ; il en reste 5 pour les succès sur 15 éclosions, à la cadence d'environ un par éclosion.

**La frontière avec les bénédictions.** La technique baisse les **coûts** et automatise. La bénédiction monte la **production**. Aucun nœud ne franchit cette ligne : un nœud de technique qui augmenterait un rendement est à réécrire en réduction de coût.

---

# 7. Les nombres

| Terme | Valeur | Statut |
|---|---|---|
| **`g`** — coût de palier | ×2.4 | Fixé |
| **`D`** — production par palier | ×2.31 = `g / 1.04` | Dérivé |
| **`f`** — tarif de redescente | **1** | **Fermé** — reset complet |
| Coût de niveau | ×1.15 | Fixé |
| Durée du cycle 1 | 3 h | Fixé |
| Croissance par cycle | ×1.18 | Fixé |
| Durée totale | ~182 h | Cible |
| Bonus global au niveau 100 | +3 % | Graine |
| **`α`** — gain de densité | 0.6 | **À mesurer** |
| Exposant du multiplicateur de densité | 0.5 | **À mesurer** |
| Plafond hors ligne | 6 h → 24 h | Fixé |
| `(A, B)` × 6 | — | **À mesurer** |

## 7.1 Pourquoi `D` n'est pas égal à `g`

Si `D` = `g`, ouvrir un palier prend toujours le même temps et la durée d'un cycle est plate — ce qui contredit la croissance à 1.18. Il faut que la production croisse **légèrement moins vite** que le coût :

```
g / D = 1.18 ^ (1 / paliers par cycle)
```

À 4,4 paliers par cycle : `g/D` = 1.039.

> **Un écart de 4 %, et il produit toute la forme de la courbe.**

`D` se mesure sur la production **totale d'un palier à pleine puissance**, jamais par espèce — sinon le nombre d'espèces par palier fait dériver le ratio sans que personne ne le voie.

## 7.2 La courbe

| Assise | Cycles | Durée | Part |
|---|---|---|---|
| I | 1 | 3 h | 2 % |
| II | 2–4 | 13 h | 7 % |
| III | 5–7 | 21 h | 12 % |
| IV | 8–10 | 35 h | 19 % |
| V | 11–13 | 57 h | 31 % |
| VI | 14–15 | 53 h | 29 % |

L'ancienne courbe du §16.2 multipliait la durée d'un cycle par **300** entre le premier et le dernier. Ce n'est pas la structure d'un idle : ce sont les **nombres** qui doivent exploser, pas le temps de mur entre deux resets. C'était aussi ce qui mettait mécaniquement 78 % de la partie dans les deux dernières assises.

Le retournement du §13.2 — *ce n'est pas le monde, c'est ce continent* — est dans l'échange **final**. La durée ne règle donc pas le confort : **elle règle combien de joueurs verront le meilleur moment du jeu.**

---

# 8. État et sauvegarde

## 8.1 Le schéma

```typescript
interface GameState {
  saveVersion: number;
  mana: Decimal;
  contenance: Decimal;          // le plafond de stock
  densite: Decimal;             // multiplicateur global, jamais remis à zéro
  foi: Decimal;
  eclosions: number;

  paliersOuverts: PalierId[];
  especes: Record<EspeceId, { debloquee: boolean; niveau: number }>;

  benedictions: Record<BenedictionId, number>;
  techniques: Record<BrancheId, { compteur: Decimal; noeuds: NoeudId[] }>;
  capacites: CapaciteId[];
  succes: Record<SuccesId, { obtenu: boolean; registre: PalierDeVoix }>;

  tempsJoue: number;            // compteur monotone, indépendant de l'horloge
  dernierCredit: number;
  graineAleatoire: number;
}
```

**Retirés** : `capacite`, `population`, `effectif`, `ageMoyen`, `chargeDePalier`, `vive`, `mure`.

## 8.2 Trois règles

**Le cœur est pur et headless** : `tick(state, dt) → state`, aucun `Date.now()` à l'intérieur, PRNG à graine stocké dans l'état, aucun état hors du reducer. Le jeu l'appelle à 100 ms, le simulateur à `dt = 60 s` sur quinze cycles. Un seul code.

**Toute mécanique du cœur se calcule en un pas pour `dt = 8 h`.** Ce qui n'y entre pas est du décor narratif : ça peut exister à l'écran, ça ne consomme pas de tick.

**Hors ligne** : `Δ = min(now − dernierCredit, plafond)` si positif, `0` si l'horloge recule. Rien d'autre — ni drapeau, ni rollback, ni message. Le plafond *est* la protection, et un jeu dont le pilier est *ne jamais punir l'absence* ne peut pas accuser un joueur d'être revenu trop tôt.

## 8.3 Sauvegarde

Nouvelle clé `idlepond:save`, distincte de celle d'Étang des Merveilles, qui est **laissée intacte et jamais écrasée**. Aucune migration : ce n'est pas le même jeu. `saveVersion` dès la v0, chaîne `n → n+1` en place tant qu'elle est vide.

Une migration ajoute ou renomme un champ ; **elle n'en supprime jamais un**, elle le marque mort. Et un changement de sémantique à champ constant exige un incrément de version même si la forme ne bouge pas.

## 8.4 Types à définir

```typescript
type Condition =
  | { type: 'niveau'; espece: EspeceId; seuil: number }
  | { type: 'palierOuvert'; palier: PalierId }
  | { type: 'eclosion'; compte: number }
  | { type: 'densite'; seuil: Decimal }
  | { type: 'acte'; acte: ActeId }
  | { type: 'et' | 'ou'; membres: Condition[] };

type TermeDeFormule =
  | 'debit_base' | 'mult_seuil' | 'mult_benediction' | 'mult_densite'
  | 'cout_creuser' | 'cout_niveau' | 'cout_deblocage'
  | 'contenance' | 'plafond_hors_ligne' | 'alpha';

type CapaciteId =
  | 'achat_auto' | 'achat_auto_max' | 'creusement_auto' | 'deblocage_auto'
  | 'file_de_descente' | 'retour_rapide' | 'navigation_directe'
  | 'lecture_debits' | 'lecture_eclosion' | 'rapport_retour';
```

`TermeDeFormule` sert **deux fois** : l'effet chiffré d'un succès, et le détail de captation. Un seul registre, donc la contrepartie de l'effet silencieux est gratuite.

---

# 9. Registre `[P]`

| # | État |
|---|---|
| P5 | **Fermé** — `f` = 1, reset complet |
| P20 | **Fermé** — 182 h, 15 éclosions |
| P25 | **Mort** — plus de population simulée |
| P29 | **Mort** — plus de maturation dans le revenu |
| P26 | **Gelé** — les miracles, à reprendre plus tard |
| P6 | **Mort** — plus de repeuplement |
| P7, P13–P19, P21, P24, P27 | Ouverts, non bloquants |
| P8, P9 | Ouverts — bloquent l'écriture, pas le code |
| P10, P11 | Portages ↑T0 à soumettre |

## 9.1 Vocabulaire — tranché

**Le reset s'appelle une éclosion.** Voir §3.

**La géographie a trois registres, et ils ne se recouvrent pas :**

| Registre | Mot | Où |
|---|---|---|
| Conception | **assise** | GDD, code, `AssiseId` |
| Travail oral | *étage* | Nulle part par écrit |
| Joueur | **noms propres** — la mare, les galeries noyées, le récif, la mer relique | UI |

> **L'interface ne nomme jamais les couches génériquement. Elle les montre.**

Le joueur ne lit pas « Assise III ». Il voit une coupe verticale, il descend, la lumière baisse, et chaque couche porte le nom qu'un habitant lui donnerait. C'est l'application au vocabulaire lui-même de la règle du §1.3 : le peuple nomme sans comprendre.

Conséquence : **zéro renommage dans le dépôt**, et aucun mot générique à choisir puisque aucun n'est affiché.

**`palier` reste visible** — c'est ce qu'on creuse, et le bouton doit le dire. Le mot porte déjà le double sens de marche et de seuil franchi.

---

# 10. Ce qui est mort

| Système | Raison |
|---|---|
| Deux canaux de captation | Un seul revenu : les espèces |
| Plafond de maturation, `part_mûre`, seaux `vive`/`mure` | Réparaient un problème créé par le second canal |
| Population, mortalité, legs, `k`, `t₉₀`, `k_effectif` | Une espèce est un générateur avec un niveau |
| Capacité de palier, `Pmax` | Sans placement, elle n'a pas d'objet |
| Choix d'espèce et de placement | Le contenu de chaque palier est écrit d'avance |
| `affinité`, `habitabilité` comme variables | Deviennent des outils d'auteur, hors formules |
| Tarif réduit à la redescente | `f` = 1 |
| Ratio global/ciblé des bénédictions | Remplacé par une différence de nature |

La maturation reste **vraie dans le monde** : la mer relique a mûri jusqu'à l'élémentaire, la mare est bloquée au degré 2, la pierre condense, un temple détermine la faune qui diverge. Elle gouverne ce qu'un lieu peut *devenir* — jamais ce que le héros gagne.

---

# 11. Prochaines étapes

1. Écrire `constantes.ts` avec les graines du §7.
2. Écrire le cœur pur : `tick`, les trois achats, l'éclosion.
3. Écrire le simulateur : même code, `dt = 60 s`, 15 cycles, quatre courbes en sortie.
4. Ajuster `α`, l'exposant de densité et les six `(A, B)` jusqu'à ce que la courbe tienne.
5. Puis seulement : le prototype jouable sur l'assise I, télémétrie incluse.

Le vocabulaire et le nom du reset sont tranchés (§9.1). Plus rien ne bloque l'écriture du code.
