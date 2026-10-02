# L'axe héros, les bénédictions et la scène — conception

**Date** : 2026-09-17
**Objet** : rendre à IdlePond sa part de JEU. Le joueur doit voir son poisson, le faire grandir avec du mana, et disposer d'un écran d'améliorations permanentes. Trois sous-systèmes, un seul plan : `docs/superpowers/plans/2026-09-17-axe-heros-benedictions-scene.md`.
**Statut** : conception posée par l'analyse du 2026-09-17, décisions prises par défaut et signalées `[D]`. L'auteur peut en renverser une avant que la tâche correspondante commence ; aucune n'est irréversible avant le commit de sa tâche.

---

## 1. Le constat, mesuré le 2026-09-17

L'écran du jeu est entièrement textuel. `src/scene/` ne contient qu'un `.gitkeep`, Phaser est une dépendance importée nulle part, et l'ancienne scène de l'Étang des Merveilles a été supprimée le 5 septembre sans remplacement.

Le joueur dispose de trois achats — creuser, débloquer, améliorer une espèce — et d'une décision, éclore. Rien ne se dépense sur le héros : son débit est une constante (`DEBIT_HEROS = 0,05`), sa contenance monte par séjour passif, et le champ `couches`, prévu pour ses marques corporelles, est initialisé vide et jamais écrit.

Les axes permanents sont soit non construits (arbre de technique, phase 5), soit interdits de monter la production (Foi → miracles, GDD §4.2 ; technique → coûts, noyau §6.3), soit **retirés du code par erreur de lecture** : le noyau v1.0 §4 prévoit les bénédictions achetées en Foi comme « l'écran d'améliorations du jeu », `docs/PRESEANCE.md` donne au noyau le dernier mot sur la mécanique, et le code les a supprimées en citant le GDD, qui n'est plus directif sur ce point.

Conséquence de genre : passé le cycle 13, plus rien ne monte. Un idle sans chiffre qui monte n'est plus un idle (mémoire `idle-incremental-avant-tout`).

## 2. Ce que ce chantier construit

| Sous-système | Ce que le joueur gagne | Ce que le canon reçoit |
|---|---|---|
| **A. L'axe héros** | Un quatrième achat en mana : *grandir*. Le héros monte de niveau pendant la vie, capte mieux et fait mieux produire ce qu'il convainc. Remis à 1 à l'éclosion. | Noyau v1.0 §1.2 passe de trois à **quatre achats**. `D` est rebudgété : une part de la croissance par palier passe du multiplicateur de profondeur au héros. |
| **B. Les bénédictions** | Un écran d'améliorations permanentes payées en Foi : une bénédiction ciblée par espèce (multiplicative), une globale (additive sur le débit de base). | Le noyau v1.0 §4 est appliqué tel quel. Le test de canon qui interdisait le mot « bénédiction » est retourné. |
| **C. La scène** | Une coupe verticale dessinée avec Phaser : les paliers ouverts, les bancs, et le héros — qui grossit avec son niveau et porte une marque par assise traversée. | GDD §15.1 « le corps par accumulation » construit ; `couches` enfin écrit à l'éclosion. |

Le tout tient dans le contrat existant : noyau pur, un pas pour `dt = 8 h`, déterminisme, migration de save sans suppression de champ.

## 3. Décisions

### 3.1 L'axe héros

**[D1] Le niveau du héros vit dans le cycle et se reperd à l'éclosion.** Fiction : il rentre dans l'œuf et en ressort alevin, avec un plafond plus haut. Ce qu'il *est* (contenance, couches) persiste ; ce qu'il a *bâti de lui-même dans cette vie* régresse, comme les galeries. C'est cohérent avec Tier 0 §3 et ça préserve l'invariance d'échelle de l'économie, donc toute la mesure existante.

**[D2] Effet : un multiplicateur global nommé, plus son propre débit.**
- `multiplicateur_heros = (1 + BONUS_PAR_NIVEAU_DU_HEROS) ^ (niveau − 1)`, dans `multiplicateursGlobaux`. Graine `0,15` `[P]`.
- `debit_heros × niveau` pour sa captation propre. Il reste l'amorçage du finding 3 ; il ne devient pas un générateur.

**[D3] Coût : une fraction du coût du palier de même rang.** `coutDeCroissance(niveau) = COUT_CREUSER_AU_PALIER_1 × RATIO_COUT_DE_CROISSANCE × g^(niveau − 1)`. Le joueur optimal paie donc à peu près un niveau de héros par palier, et le multiplicateur de profondeur est **divisé par `(1 + BONUS_PAR_NIVEAU_DU_HEROS)`** pour que `D` par palier ne bouge pas. Graine `RATIO_COUT_DE_CROISSANCE = 0,75` `[P]`, avec une contrainte dure : **le premier niveau coûte plus que la charge de l'œuf** (`36`), sinon le joueur naïf grandit avant de convaincre le vairon et le plancher de cadence du §8.4 tombe.

**[D4] Pas de seuils sur le niveau du héros.** Les seuils 10/25/50/100 restent aux espèces. YAGNI : un multiplicateur géométrique par niveau suffit à faire monter un chiffre à chaque achat, et la scène lit le niveau directement pour l'échelle du corps.

### 3.2 Les bénédictions

**[D5] Deux formes, celles du noyau v1.0 §4.2, rien de plus.**
- *Ciblée*, une par espèce : `multiplicateur_benediction = (1 + BENEDICTION_CIBLEE_PAR_RANG) ^ rang`. Graine `0,5` `[P]`.
- *Globale*, unique : `debit_beni = debit_base + BENEDICTION_GLOBALE_PAR_RANG × rang`, sur toutes les espèces présentes et futures. Graine `0,05` `[P]`. Elle écrase tôt (le vairon capte `0,2`), devient négligeable tard : le croisement se fait tout seul, comme le noyau le dit.
- Le coût d'un niveau d'espèce reste indexé sur le débit de base **non béni** : bénir ne renchérit rien.

**[D6] Coût en Foi, géométrique.** `coutDeBenediction(rang) = base × RATIO_COUT_DE_BENEDICTION ^ rang`, base `3` pour une ciblée, `2` pour la globale, ratio `4` `[P]`. Échelle de référence : le premier cycle rapporte ~5 Foi, le deuxième ~1 600.

**[D7] Achetables à tout moment, pas seulement « dans l'œuf ».** Le noyau §4.1 dit « dans l'œuf, sur un écran dédié ». Un état « dans l'œuf » où rien ne produit pénaliserait un joueur qui s'absente à ce moment, contre le pilier *ne jamais punir l'absence*. La Foi n'est de toute façon **créditée** qu'à l'éclosion : l'écran est ouvert tout le temps, son contenu ne change qu'en rentrant dans l'œuf. Amendement au noyau §4.1, consigné.

**[D8] Le GDD §4.2 (« la Foi n'achète que des miracles ») est dépassé sur ce point**, par la règle de `PRESEANCE.md` : mécanique → noyau v1.0. Les miracles restent gelés (`[P26]`) et coexisteront avec les bénédictions le jour où ils seront tranchés.

### 3.3 La scène

**[D9] Phaser 3, dans `src/scene/`, derrière une projection pure.** `scene/vue.ts` transforme `EtatJeu` en `VueDeScene` — une structure de données sans Phaser, testable en node. La scène Phaser ne lit jamais l'état : elle reçoit la vue. Le noyau n'importe rien de `scene/` ; `architecture.test.ts` le vérifie déjà.

**[D10] Graphismes provisoires dessinés par code, aucun PNG.** GDD §15.3 : l'anatomie du corps de base « bloque le premier sprite définitif » et §15.4 met l'imagerie de l'Étang des Merveilles hors registre. Les 42 spritesheets de `public/anim` ne sont pas réutilisées. Le héros est une ellipse et une queue ; les bancs sont des ellipses plus petites, colorées par rang ; les marques sont des primitives. Ce que la scène **montre** — profondeur, lumière qui baisse, taille du héros, marques, bancs qui grossissent — est ce que la DA reprendra avec de vrais sprites sans toucher à `vue.ts`.

**[D11] Les marques sont visuelles, sans effet chiffré.** Une par assise traversée pendant la vie, écrite dans `couches` à l'éclosion, dans l'ordre des assises. Correspondance GDD §15.1 : Noue → branchies, II → membranes, III → luminescence, IV → minéralisation, V → épaississement, VI → halo `[P]` (le GDD n'en nomme que cinq).

**[D12] Échelle du héros : `1 + 0,25 × log₂(niveau)`.** Niveau 1 → ×1, 16 → ×2, 256 → ×3. Lisible à chaque achat au début, bornée à la fin.

> Remplacé le 2026-10-02 par les quatre stades dessinés de la spec DA pixel art (§3) : 21 / 28 / 42 / 63 px aux niveaux 1 / 4 / 16 / 256.

## 4. Ce que ce chantier ne fait pas

- **Le contenu des assises II à VI.** Le jeu livré reste la Noue et ses six paliers (`PALIERS_LIVRES`). Débloquer « de nouveaux poissons et ainsi de suite » sur la durée est un chantier de contenu, à planifier après celui-ci. La scène est écrite pour 62 paliers et 6 palettes dès maintenant, pour ne pas être réécrite.
- **L'arbre de technique** (phase 5), **les miracles** (`[P26]`), **le découplage des chapitres** (phase 4).
- **Des sprites définitifs.** Bloqués par le GDD §15.3.

## 5. Risques

- **Le plancher de cadence.** Un quatrième achat pas cher tôt déplace les premières minutes. `plancher-de-cadence.test.ts` est le garde ; la tâche qui introduit `grandir` le relance et ajuste `RATIO_COUT_DE_CROISSANCE` par pas de `0,25` si besoin, en consignant la valeur.
- **La forme de la courbe.** Le rebudget de `D` suppose un niveau de héros par palier ; le joueur optimal en achète un peu moins. La tâche de mesure relance `simuler(14)` et consigne les durées ; un écart de plus de 20 % sur le cycle 1 (cible 3 h) rouvre `RATIO_COUT_DE_CROISSANCE` et `BONUS_PAR_NIVEAU_DU_HEROS`.
- **La Foi mal échelonnée.** Si les bénédictions sont toutes achetées dès le cycle 3 ou jamais avant le 10, `RATIO_COUT_DE_BENEDICTION` bouge. C'est une graine, elle est là pour ça.
- **Phaser en test.** Aucun test unitaire ne monte Phaser (environnement node). La scène est vérifiée par capture Playwright ; `vue.ts` et `palette.ts` portent les tests.
