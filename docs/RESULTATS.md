# Simulateur IdlePond — ce qu'il a trouvé

```bash
npx tsx src/simulateur.ts   # une passe avec les constantes courantes
npx tsx src/calibreur.ts    # résout ALPHA, THETA et l'échelle
```

Le simulateur et le jeu importeront **le même noyau** (`src/noyau.ts`) et **le
même fichier de constantes**. Le harnais n'ajoute aucune règle : il remplace
seulement le joueur par une politique d'achat.

---

## Finding 1 — la boucle est invariante d'échelle

C'est le résultat principal, et il invalide une hypothèse du noyau v1.0.

Avec un reset complet (`f` = 1), des coûts géométriques (`g` = 2.4) et une
production géométrique (`D` = 2.31), **chaque cycle est le même problème à une
échelle près**. Sa durée est donc constante par construction.

| | Attendu (noyau v1.0) | Mesuré |
|---|---|---|
| Croissance par cycle | ×1.18 | **×0.94 à ×1.00** |
| Durée totale | 182 h | **37,6 h** |
| Cycle le plus long | — | 8 % du total |

Le résultat est **robuste** : il ne bouge pas quand on fait varier `ALPHA`
(0,35 à 0,65), `THETA` (0,05 à 1,2), ni la rampe de marge de contenance
(constante, +1, +2 paliers par éclosion). Le calibreur pousse systématiquement
`THETA` contre sa borne basse — il cherche à *ralentir* le jeu et n'y arrive
pas.

> **La cible de 182 h avec une croissance de 1,18 n'est pas atteignable par
> réglage. Elle est structurellement exclue.**

## Finding 2 — la croissance vient du joueur, pas de l'économie

La politique **relâchée** ne joue pas plus mal : elle joue moins souvent —
un relevé toutes les 4 h au lieu d'un achat continu.

| | Optimale | Relâchée |
|---|---|---|
| Cycle 1 | 3,0 h | 16,1 h |
| Cycle 13 | 2,7 h | 80,0 h |
| Croissance | ×0,94 | **×1,18** |
| Total | 37,6 h | **~600 h** |

C'est exactement la croissance de 1,18 que le noyau v1.0 postulait — mais elle
n'est pas dans l'économie. Elle vient de ce qu'un joueur absent **plafonne sa
contenance et perd la production au-delà**, de plus en plus souvent à mesure
que les cycles demandent plus.

> **Les 182 h n'étaient ni du temps actif ni du temps calendaire : elles
> confondaient les deux.**

Les deux chiffres à retenir désormais :

- **temps actif ≈ 38 h** — la quantité réelle de jeu que le contenu porte ;
- **temps calendaire ≈ 25 jours** — ce que vit un joueur à deux relevés par
  jour.

## Finding 3 — le noyau v1.0 n'avait pas d'amorçage

Production nulle au départ → mana nul → aucune espèce jamais débloquée. La
partie ne démarrait pas.

Corrigé par `DEBIT_HEROS` : le héros capte l'ambiant tout seul. C'est sa
mutation, donc c'est canonique, et le terme devient négligeable dès la
première espèce.

## Finding 4 — 21 espèces, pas 24

La règle « une espèce tous les 3 paliers, à partir du premier de chaque
assise » appliquée à 6/12/12/12/12/8 donne **2 + 4 + 4 + 4 + 4 + 3 = 21**.
Le chiffre de 24 du noyau v1.0 était une estimation, pas un décompte.

## Finding 5 — la loi de l'exposant de densité

Le gain de densité vaut `pointe^ALPHA` et le besoin croît en `g^paliers`. Pour
que la densité suive exactement le besoin, il faut que l'exposant du
multiplicateur vaille `1 / ALPHA`.

- exposant `< 1/ALPHA` → la densité prend du retard, les cycles s'allongent ;
- exposant `= 1/ALPHA` → compensation exacte, cycles plats ;
- exposant `> 1/ALPHA` → surcompensation, les cycles raccourcissent.

D'où le paramètre `THETA` : l'exposant vaut `THETA / ALPHA`, et `THETA` est la
**part du besoin que la densité compense**. C'est le seul bouton qui agisse
sur la forme de la courbe.

---

## Finding 6 — le Souffle ne change pas le rythme (2026-10-07)

Mesuré par `MesurePolitiquesDAmeliorationTests` (à la demande, `Explicit`) : quinze cycles,
graine 7, quatre façons de dépenser le Souffle.

| Politique | Rangs achetés | Ciblée max | Cycle 1 | Cycle 5 | Cycle 10 | Cycle 15 | Fond atteint | Pic du cycle 15 |
|---|---|---|---|---|---|---|---|---|
| la moins chère d'abord (actuelle) | 3476 | 158 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 8,8e192 /s |
| la moitié en réserve | 3446 | 157 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 1,2e192 /s |
| la globale seule | 90 | 0 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 8,5e108 /s |
| la plus profonde + la globale | 924 | 157 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 3,4e189 /s |

**Les améliorations de renaissance changent l’échelle des nombres de 84 ordres de grandeur, et le rythme de
zéro.** Durées de cycle, jeu actif (90,9 h) et cycle du fond sont identiques au dixième
d'heure. La raison : le joueur simulé rentre dans l'œuf quand l'acquis de séjour atteint
95 % de son maximum, et l'acquis monte avec le **temps** (`τ₀`), pas avec la production.
C'est le Finding 1 vu de l'autre côté : la boucle est invariante d'échelle, et le Souffle est
une échelle.

**Contre-épreuve : renaître dès qu'on est bloqué.** Même mesure, `FractionDeSaturationPourRenaitre
= 0`. Les cycles dégénèrent (≈ 0 h chacun après le premier, le fond n'est jamais atteint,
1,0 h de jeu actif en quinze cycles) : le blocage tombe presque aussitôt après chaque retour
dans l'œuf. C'est donc bien l'acquis de séjour, borné par le temps, qui donne sa longueur au
cycle — et aucune politique d’amélioration n’y touche.

**Ce que ça défait.** La roadmap (chantier 2) tenait que « tant que la politique de dépense
n'est pas actionnée, toute mesure d'équilibrage en aval est suspecte ». Pour le rythme,
non : la politique ne le touche pas.

**Ce que ça ouvre — une question de design, pas de calibrage.** Dans cette économie, le
Souffle ne fait rien gagner de perceptible **en temps** : il grossit les chiffres. Si le
joueur doit sentir qu'il progresse (consigne « idle incrémental avant tout »), il faut
qu'une dépense de Souffle raccourcisse quelque chose — par exemple que l'acquis de séjour
monte plus vite, ou que la renaissance devienne rentable plus tôt. À trancher avec l'auteur.

---

## Finding 7 — les bonus ne changent pas le rythme non plus, et ils ne sont pas un choix (2026-11-13)

Mesuré par `MesureDesBonusTests` (à la demande, `Explicit`) : six cycles, graine 7, contenu
borné aux 18 paliers livrés. Le joueur simulé prend le bonus ouvert le moins cher s'il coûte
au plus une part donnée de son mana (`Politique.PartDuManaPourUnBonus`) ; les deux verbes sont
laissés de côté, le simulateur achetant déjà en continu.

| Politique | Cycle 1 | Cycle 2 | Cycle 3 | Paliers (cycles 1 / 2 / 3) | Rangs tenus (cycles 1 / 2 / 3) | Pic du cycle 3 |
|---|---|---|---|---|---|---|
| aucun bonus | 3,12 h | 3,48 h | 3,72 h | 10 / 15 / 18 | 0 | 2,7e19 /s |
| au plus 10 % du mana | 3,15 h | 3,55 h | 3,73 h | 11 / 16 / 18 | 41 / 60 / 69 | 1,9e21 /s |
| au plus 50 % du mana | 3,15 h | 3,55 h | 3,73 h | 11 / 16 / 18 | 51 / 66 / 70 | 3,7e21 /s |
| dès qu'il est payable | 3,13 h | 3,55 h | 3,73 h | 11 / 16 / 18 | 54 / 67 / 70 | 3,8e21 /s |

**Trois constats.**

- **Un palier de plus par vie, et c'est tout.** Les bonus font gagner un palier aux cycles 1 et
  2 ; les durées ne bougent pas d'un dixième d'heure. Même cause qu'au Finding 6 : la vie dure
  ce que dure l'acquis de séjour.
- **Tout est acheté, à chaque vie.** Le registre porte 70 rangs hors verbes ; le joueur en tient
  41 à 54 dès la première vie et 69 ou 70 à la troisième. Aux prix actuels (`COUT_DE_BONUS_RELATIF
  = 1`, `RATIO_COUT_DE_BONUS = 2`), un bonus n'est pas une décision : c'est une case à cocher de
  plus à chaque redescente.
- **Les nombres, eux, bougent** : le pic est multiplié par 3 à la première vie, par ~140 à la
  troisième.

**Lecture de la part bloquée.** La mesure rend aussi la part de la vie passée sans pouvoir
creuser : 84 % au cycle 1 (74 % en achetant tout), 94 à 98 % ensuite. Ces chiffres sont gonflés
par la borne des 18 paliers — une fois tout creusé, la vie entière compte comme bloquée — et ne
se comparent pas à la table de l'amendement v1.3, mesurée sur 62 paliers.

**Décidé le 2026-11-13 : des prix plus raides.** `RATIO_COUT_DE_BONUS` passe de 2 à 6 (un rang
de plus coûte à peu près deux paliers de plus). Même mesure, rangs tenus à la fin des vies
1 / 2 / 3, sur 70 :

| Ratio | Acheteur prudent (10 % du mana) | Acheteur avide (dès que payable) |
|---|---|---|
| 2 | 41 / 60 / 69 | 54 / 67 / 70 |
| **6** | **23 / 43 / 58** | **32 / 54 / 65** |

La première vie en tient moins de la moitié : il y a un ordre à choisir. **Limite, mesurée :**
dès la quatrième vie les 70 rangs sont tenus quand même, parce que les prix sont ancrés sur des
paliers livrés et que la production les dépasse. Aucun ratio ne l'empêche ; ce sont les lieux
suivants qui redonneront des rangs chers. Les durées ne bougent toujours pas.

---

## Finding 8 — l'amélioration de séjour est le premier achat qui change le rythme (2026-11-13)

Décision de l'auteur, après les Findings 6 et 7 : une amélioration de renaissance de plus,
**le séjour**, qui raccourcit le temps caractéristique de l'acquis (`τ × (1 − 0,1)^rang`, dix
rangs au plus, donc `τ × 0,35` au dernier). Elle ne monte aucune production et ne baisse aucun
coût. Son rang ne change qu'à l'achat : la forme exponentielle de l'acquis reste exacte en un
pas, et un test d'équivalence de pas le garde.

Mesuré par `MesurePolitiquesDAmeliorationTests`, quinze cycles, graine 7, politique « le séjour
d'abord, puis la moins chère » :

| | Cycle 1 | Cycle 5 | Cycle 10 | Cycle 15 | Fond atteint | Jeu actif |
|---|---|---|---|---|---|---|
| sans le séjour (les quatre politiques du Finding 6) | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 90,9 h |
| le séjour d'abord, ratio de prix 1 000 | 3,1 h | 2,7 h | 2,4 h | 3,1 h | cycle 13 | 40,7 h |
| le séjour d'abord, ratio de prix 100 000 (retenu) | 3,1 h | 3,3 h | 2,4 h | 3,1 h | cycle 13 | 44,8 h |

**Ce que ça montre.** Pour la première fois, une dépense de Souffle se lit en heures : la
descente complète passe de 91 h à 45 h, et le fond reste au cycle 13. Le ratio de prix ne
déplace que le moment où les rangs tombent (plus tard à 100 000) ; la durée totale tient à la
part par rang et au rang maximal.

**Ce que ça ouvre.** Avec les dix rangs, les vies redeviennent presque plates (2,4 à 3,3 h) :
la croissance voulue par `CROISSANCE_DU_SEJOUR_PAR_PALIER` est en grande partie rachetée. Deux
réglages restent à trancher avec l'auteur : la **durée totale visée** (45 h, 91 h, ou entre les
deux — une part de 5 % par rang donnerait `τ × 0,60`), et si les dernières vies doivent rester
plus longues que les premières.

**Parité.** Les politiques historiques du simulateur n'achètent pas le séjour : les parties de
référence se rejouent au bit près. Seul `donnees.json` gagne l'entrée du registre.

---

## Finding 9 — l'arbre de technique, mesuré (2026-11-13)

Les trente nœuds du noyau v1.0 §6.2 sont transcrits (`NoeudsTechnique.cs`) et appris par
l'usage, sans achat. Les calendriers des deux branches bornées et les couples (A, B) des trois
autres ont été posés par le calcul (`Technique.cs`) ; `MesureDeLArbreTests` dit à quelle vie
chaque nœud tombe au simulateur, graine 7.

| Branche | Rang 1 | Rang 2 | Rang 3 | Rang 4 | Rang 5 |
|---|---|---|---|---|---|
| Creusement | vie 1 | vie 1 | vie 2 (la file) | vie 5 | **vie 11** (galeries connues) |
| Amélioration | vie 1 | vie 1 (la main sûre) | vie 4 | vie 10 (l'élan) | **jamais en 15 vies** (tout le monde suit) |
| Recrutement | vie 1 | vie 2 (les habitudes tiennent) | vie 3 | vie 4 | vie 5 |
| Renaissance | vie 1 | vie 2 (lire l'eau) | vie 4 | vie 6 | vie 9 (le chemin connu) |
| Entretien | — | — | — | — | — |
| Construction | — | — | — | — | — |

Sur le contenu livré (18 paliers, 8 vies) : les deux derniers nœuds de Creusement et
d'Amélioration et le dernier de Renaissance ne tombent pas ; le reste comme ci-dessus, « le
refuge est connu » à la vie 6 au lieu de 5.

**Ce que ça montre.**

- **Le rythme ne bouge presque pas.** Quinze vies : 92,0 h de jeu actif contre 90,9 h avant
  l'arbre ; 3,2 / 4,5 / 7,1 / 8,9 h aux vies 1, 5, 10 et 15 ; fond à la vie 13. Avec le séjour
  d'abord : 43,9 h contre 44,8 h. L'arbre baisse des coûts et automatise : il ne raccourcit
  rien, c'est l'acquis de séjour qui donne sa longueur à une vie (Findings 6 à 8).
- **Deux verbes arrivent trop tard.** « Galeries connues » à la vie 11 et « Tout le monde suit »
  jamais : ce sont pourtant ceux qui soulagent la redescente. Les couples (A, B) de Creusement
  (3,8 ; 60) et d'Amélioration (1,9 ; 140) sont à resserrer.
- **Entretien n'est pas mesuré** : le simulateur ne s'absente jamais. Ses moments (6 h, 23 h,
  65 h, 184 h, 640 h d'absence créditée) ne viennent que du calcul.
- **Construction est muette** : rien ne crédite son compteur tant que temples et portails
  n'existent pas.

**Ce que la mesure ne dit pas.** Le simulateur achète en continu et ne fait pas tourner les
verbes : il mesure QUAND un nœud tombe, pas ce que le verbe change pour un joueur. Ça, seule
une vraie partie le dira (`telemetrie.jsonl`).

---

## Journal des références de parité

Les références de parité (`Assets/IdlePond/Tests/Reference/`) venaient du TypeScript
archivé. Depuis le **2026-10-07**, elles sont régénérées depuis le C#
(`GenerateurDeReferences`, menu « IdlePond ▸ Parité »), sur décision de l'utilisateur. Une
régénération n'a lieu que pour un changement voulu, noté ici.

| Date | Raison | Ce qui a bougé |
|---|---|---|
| 2026-10-07 | L'assise II, le Gour, livrée jouable (spec `2026-10-07-assise-ii-le-gour-design.md`) | Identifiants `assise-2` → `gour`, `espece-2-2/3/4` → `chabot`, `lamproie`, `ombre` ; `paliersLivres` 6 → 18 ; registre des succès 41 → 54 (seuils des trois espèces, fond du Gour). Les nouvelles remises de coût changent l'économie simulée : sur quinze cycles, densités ×1,09 (5,35e115 → 5,85e115), pics de production et Souffle gagnés en hausse. Aucun autre écart : le reste est identique au TypeScript, à un dernier chiffre de flottant près |
| 2026-11-13 | L'arbre de technique entier (Finding 9) : trente nœuds, six branches ; la main sûre et la sonde quittent l'onglet « Débloquer » pour l'arbre | `parties.json` et `hors-ligne.json` régénérés : `noeudsTechnique` se remplit, les remises de creusement, de cran et de conviction, « rentrer à l'heure » (`α` 0,60 → 0,65) et la coquille épaisse déplacent mana, compteurs et densités. Quinze cycles : 331 320 s actives contre 327 300 (+1,2 %). `constantes.json` modifié À LA MAIN, il ne se régénère pas : `BUDGET_DE_VERBES_ARBRE` 10 → 11 (« Le peuple continue » compte comme verbe ; le total reste 15) |
| 2026-11-13 | L'arbre de technique s'ouvre : branche Recrutement, cinq nœuds appris par l'usage (`NoeudsTechnique.cs`, calendrier 3 / 6 / 12 / 20 / 30 espèces convaincues) | `parties.json` et `hors-ligne.json` : `noeudsTechnique` n'est plus vide, les remises de conviction (−15 / −30 / −50 %) et de cran (−5 %) déplacent les compteurs et les densités d'environ 0,5 % sur quinze cycles (5,878e115 → 5,847e115). Durées de cycle inchangées au dixième d'heure (3,1 / 4,5 / 7,0 / 8,9 h, fond au cycle 13) |
| 2026-11-13 | L'amélioration de séjour entre au registre (Finding 8) | `donnees.json` seul : une entrée `amelioration-sejour`, portée `sejour`. `parties.json`, `hors-ligne.json` et les autres sont régénérés identiques |
| 2026-10-08 | Les insufflations deviennent les améliorations de renaissance (Codex v1.1) | Renommage seul, sans régénération : clef `insufflations` → `ameliorations`, identifiants `insufflation-*` → `amelioration-*`, constantes `*_INSUFFLATION_*` → `*_AMELIORATION_*`, termes `cout_insufflation`, `insufflation_globale`. Aucun chiffre ne bouge |

---

## Ce qu'il reste à décider

Le contenu — 62 paliers, 21 espèces, 15 chapitres — porte **38 h de jeu
actif**. Trois sorties :

1. **Accepter 38 h.** C'est une durée honnête pour un idle narratif, et le
   premier cycle tient en 3 h.
2. **Découpler les chapitres des éclosions.** 45 éclosions de ~2,7 h font
   120 h, avec un chapitre toutes les 3 éclosions. Aucun contenu perdu, et la
   cadence narrative devient réglable indépendamment de l'économie.
3. **Allonger le cycle 1.** 12 h par cycle × 15 donne 180 h — mais un premier
   cycle de 12 h est hors format pour une découverte.

La 2 est la seule qui donne la durée voulue sans casser le premier cycle.
