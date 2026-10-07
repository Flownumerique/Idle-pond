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

Mesuré par `MesurePolitiquesDInsufflationTests` (à la demande, `Explicit`) : quinze cycles,
graine 7, quatre façons de dépenser le Souffle.

| Politique | Rangs achetés | Ciblée max | Cycle 1 | Cycle 5 | Cycle 10 | Cycle 15 | Fond atteint | Pic du cycle 15 |
|---|---|---|---|---|---|---|---|---|
| la moins chère d'abord (actuelle) | 3476 | 158 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 8,8e192 /s |
| la moitié en réserve | 3446 | 157 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 1,2e192 /s |
| la globale seule | 90 | 0 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 8,5e108 /s |
| la plus profonde + la globale | 924 | 157 | 3,1 h | 4,5 h | 7,0 h | 8,9 h | cycle 13 | 3,4e189 /s |

**Les insufflations changent l'échelle des nombres de 84 ordres de grandeur, et le rythme de
zéro.** Durées de cycle, jeu actif (90,9 h) et cycle du fond sont identiques au dixième
d'heure. La raison : le joueur simulé rentre dans l'œuf quand l'acquis de séjour atteint
95 % de son maximum, et l'acquis monte avec le **temps** (`τ₀`), pas avec la production.
C'est le Finding 1 vu de l'autre côté : la boucle est invariante d'échelle, et le Souffle est
une échelle.

**Ce que ça défait.** La roadmap (chantier 2) tenait que « tant que la politique de dépense
n'est pas actionnée, toute mesure d'équilibrage en aval est suspecte ». Pour le rythme,
non : la politique ne le touche pas.

**Ce que ça ouvre — une question de design, pas de calibrage.** Dans cette économie, le
Souffle ne fait rien gagner de perceptible **en temps** : il grossit les chiffres. Si le
joueur doit sentir qu'il progresse (consigne « idle incrémental avant tout »), il faut
qu'une dépense de Souffle raccourcisse quelque chose — par exemple que l'acquis de séjour
monte plus vite, ou que la renaissance devienne rentable plus tôt. À trancher avec l'auteur.

---

## Journal des références de parité

Les références de parité (`Assets/IdlePond/Tests/Reference/`) venaient du TypeScript
archivé. Depuis le **2026-10-07**, elles sont régénérées depuis le C#
(`GenerateurDeReferences`, menu « IdlePond ▸ Parité »), sur décision de l'utilisateur. Une
régénération n'a lieu que pour un changement voulu, noté ici.

| Date | Raison | Ce qui a bougé |
|---|---|---|
| 2026-10-07 | L'assise II, le Gour, livrée jouable (spec `2026-10-07-assise-ii-le-gour-design.md`) | Identifiants `assise-2` → `gour`, `espece-2-2/3/4` → `chabot`, `lamproie`, `ombre` ; `paliersLivres` 6 → 18 ; registre des succès 41 → 54 (seuils des trois espèces, fond du Gour). Les nouvelles remises de coût changent l'économie simulée : sur quinze cycles, densités ×1,09 (5,35e115 → 5,85e115), pics de production et Souffle gagnés en hausse. Aucun autre écart : le reste est identique au TypeScript, à un dernier chiffre de flottant près |

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
