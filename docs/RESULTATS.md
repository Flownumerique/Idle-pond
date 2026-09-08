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
