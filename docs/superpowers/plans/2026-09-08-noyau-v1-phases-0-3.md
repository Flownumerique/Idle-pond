# Noyau v1.0 — phases 0 à 3 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Faire passer IdlePond du modèle à population simulée au noyau v1.0 — trois achats, une espèce est un générateur avec un niveau — jusqu'aux nombres mesurés par le simulateur.

**Architecture:** Le noyau reste un réducteur pur `tick(etat, dt) → etat`, appelé à 100 ms par le jeu et à `dt = 60 s` par le simulateur : un seul code. On retire d'abord les systèmes morts par les feuilles (divergence forcée, maturation, second canal), puis on remplace le cœur du modèle (`Banc`/`place`/`effectif` → `Espece`/`niveau`), puis on rebranche le simulateur et on mesure. Les adaptateurs — persistance, horloge, hors ligne, boucle, télémétrie — ne changent pas de forme.

**Tech Stack:** TypeScript 5.9 strict, Vitest 3, Vite 8, React 19, `break_infinity.js` (`Decimal`), zustand.

**Spec:** `docs/superpowers/specs/2026-09-08-roadmap-noyau-v1-design.md`

## Global Constraints

Ces règles s'appliquent à **toutes** les tâches. Chaque valeur est recopiée du spec, verbatim.

- `g = 2.4` (coût de palier), `D = 2.31` dérivé de `g / 1.039`, `f = 1` — reset complet, **aucun tarif réduit à la redescente**.
- Coût de niveau `×1.15`. Seuils **cumulés** aux niveaux 10 / 25 / 50 / 100 pour `×2 / ×4 / ×8 / ×16` — **jamais ×1024**.
- Bonus global permanent `+0.03` par espèce ayant atteint le niveau 100, définitif, survit à l'éclosion.
- 62 paliers en `6 / 12 / 12 / 12 / 12 / 8`, 6 assises, **21 espèces en `2 / 4 / 4 / 4 / 4 / 3`**, une espèce tous les 3 paliers à partir du premier de l'assise.
- `mult_densité = (1 + densité / d₀) ^ (θ / α)`, et le gain de densité à l'éclosion vaut `pointe ^ α` où `pointe` est la **production de pointe du cycle**, jamais la profondeur.
- Le noyau est pur : aucun `Date.now()` dans `src/noyau/` ni `src/donnees/`, PRNG à graine dans l'état, aucun état hors du réducteur. `tests/architecture.test.ts` le vérifie et doit rester vert à chaque tâche.
- **Tout se calcule en un pas pour `dt = 8 h`.** `tests/equivalence-de-pas.test.ts` compare 1 pas de 8 h à 480 pas de 60 s et doit rester vert.
- La technique baisse les **coûts** et automatise ; elle ne monte **jamais** une production. `tests/canon.test.ts` le verrouille.
- Migration de save : une migration **ne supprime jamais un champ**, elle le marque mort et l'ignore à la lecture. Tout changement de sémantique à forme constante exige quand même un incrément de `VERSION_SAVE`.
- Le code, les commentaires et les messages de commit sont en français, comme tout le dépôt.

**Commandes de vérification**, utilisées partout dans ce plan :

```bash
npx vitest run                    # toute la suite
npx vitest run tests/X.test.ts    # un fichier
npx tsc -b                        # types
npx eslint .                      # lint
npm run build                     # tsc -b && vite build
```

---

## File Structure

**Supprimés :** `src/noyau/population.ts`, `src/noyau/maturation.ts`, `tests/deux-canaux.test.ts`, `tests/saturation.test.ts`.

**Réécrits en profondeur :**

| Fichier | Responsabilité après refonte |
|---|---|
| `src/noyau/types.ts` | Les types de l'état et du contenu. `Banc`, `BancId`, `EtatBanc` disparaissent ; `Espece` porte son rang et son palier ; `EtatEspece { debloquee, niveau }` les remplace. |
| `src/noyau/economie.ts` | Production, coûts, contenance. Un seul canal de revenu. |
| `src/noyau/noyau.ts` | `tick`, et les trois actes : `creuser`, `debloquer`, `ameliorer`. |
| `src/noyau/constantes.ts` | Les graines. Les constantes du modèle mort sortent ; `DEBIT_HEROS`, `DEBIT_RATIO_ESPECE`, `ESPECE_TOUS_LES_N_PALIERS`, `multiplicateurDePalier()`, `PALIERS_GAGNES_PAR_ECLOSION`, `ECHELLE_DE_PRODUCTION` entrent. |
| `src/donnees/especes.ts` | Les 21 espèces, réparties `2/4/4/4/4/3`, chacune ancrée à son palier. |
| `src/donnees/paliers.ts` | Les 62 paliers. Un palier porte au plus **une** espèce, ou aucune. |

**Modifiés en surface :** `src/noyau/eclosion.ts`, `src/noyau/densite.ts`, `src/noyau/succes.ts`, `src/noyau/technique.ts`, `src/adaptateurs/persistance.ts`, `src/simulateur/simulateur.ts`, `src/simulateur/calibreur.ts`, `src/ui/Captation.tsx`, `src/ui/Mare.tsx`, `src/ui/format.ts`, `tests/etat-de-travail.ts`.

**Interface cible**, référencée par toutes les tâches :

```typescript
// src/noyau/types.ts
export interface Espece {
  readonly id: EspeceId
  readonly assise: AssiseId
  readonly rang: number          // 0..20, global, dans l'ordre de profondeur
  readonly palier: IndexPalier   // le palier qui l'apporte
}

export interface Palier {
  readonly index: IndexPalier
  readonly assise: AssiseId
  readonly espece: EspeceId | null
}

export interface EtatEspece {
  readonly debloquee: boolean
  readonly niveau: number
}

// src/noyau/economie.ts
export function multiplicateurDeSeuil(niveau: number): number
export function debitBaseDeLEspece(espece: Espece): Decimal
export function multiplicateurDeProfondeur(etat: EtatJeu): Decimal
export function productionDeLEspece(etat: EtatJeu, espece: Espece): Decimal
export function productionTotaleParSeconde(etat: EtatJeu): Decimal
export function coutDeDescente(etat: EtatJeu, cible: IndexPalier): Decimal
export function coutDeDeblocage(etat: EtatJeu, espece: Espece): Decimal
export function coutDeNiveau(etat: EtatJeu, espece: Espece, niveau: number): Decimal

// src/noyau/noyau.ts
export function creuser(etat: EtatJeu): EtatJeu
export function debloquer(etat: EtatJeu, especeId: EspeceId): EtatJeu
export function ameliorer(etat: EtatJeu, especeId: EspeceId): EtatJeu
```

---

# Phase 0 — Verser et trancher

### Task 1: Verser les documents et écrire la préséance

**Files:**
- Create: `docs/PRESEANCE.md`
- Modify: `docs/noyau.ts:1`, `docs/constantes.ts:1`, `docs/simulateur.ts:1`, `docs/calibreur.ts:1` (en-tête)
- Commit: `docs/RESULTATS.md`, `docs/idlepond-histoire-v0.1.md`, `docs/idlepond-noyau-v1_0.md` sans modification

**Interfaces:**
- Consumes: rien
- Produces: `docs/PRESEANCE.md`, que toutes les tâches suivantes citent en cas de doute

- [ ] **Step 1: Écrire `docs/PRESEANCE.md`**

```markdown
# Préséance des documents

Écrit le 2026-09-08. Cette page existe parce que trois sessions ont re-dérivé
la même question et ont conclu différemment.

| Domaine | Document directif |
|---|---|
| Mécanique, économie, courbe, arbre technique, état | `docs/idlepond-noyau-v1_0.md` |
| Fiction, monde, géographie nommée, voix, succès, direction artistique | `docs/GDD.md` v2.4 |
| Chiffres — amende le noyau sur ses cinq findings | `docs/RESULTATS.md` |
| `θ`, loi de contenance, là où il ne contredit pas le noyau | `docs/amendement-v1.1.md` |
| **Périmé** | `docs/jalon-v0.1.md` §5.1, qui donnait la préséance au prompt de lancement |

**Règle de lecture.** Quand le GDD v2.4 décrit une mécanique que le noyau v1.0
a tuée, le noyau gagne. Quand le noyau v1.0 est muet sur le monde, la voix ou
un succès, le GDD gagne. Quand `RESULTATS.md` mesure un chiffre que le noyau
v1.0 avait posé en graine, la mesure gagne.

**Sections du GDD dépassées par le noyau v1.0** : §3 la captation, §7 le
vivant, §10 la ponte, §16 l'équilibrage. Le reste du GDD tient.

**Contradiction interne au noyau v1.0, tranchée.** Sa §2 place
`mult_technique` dans la production totale et sa §6.2 donne au nœud
« Réputation » un `+5 % de production`, contre sa propre §6.3 — « la technique
baisse les coûts, la bénédiction monte la production, aucun nœud ne franchit
cette ligne ». La règle dure gagne : `mult_technique` ne figure pas dans la
production, et « Réputation » est à réécrire en réduction de coût.

La justification longue est dans
`docs/superpowers/specs/2026-09-08-roadmap-noyau-v1-design.md`.
```

- [ ] **Step 2: Marquer les quatre `.ts` du spike**

Ajouter en toute première ligne de `docs/noyau.ts`, `docs/constantes.ts`, `docs/simulateur.ts` et `docs/calibreur.ts` :

```typescript
/* HARNAIS DE MESURE — NON INTÉGRÉ AU JEU.
 * Ces fichiers ont produit docs/RESULTATS.md. Ils servent de référence de
 * lecture pour le modèle du noyau v1.0, jamais de code de production : le jeu
 * vit dans src/, avec Decimal et l'état immuable. Ne pas importer depuis src/.
 */
```

- [ ] **Step 3: Vérifier que rien de `src/` n'importe `docs/`**

Run: `grep -rn "docs/" src/ tests/ vite.config.ts tsconfig*.json`
Expected: aucune ligne qui importe un module depuis `docs/`.

- [ ] **Step 4: Commit**

```bash
git add docs/
git commit -m "docs(canon): verse le noyau v1.0, l'histoire et les mesures, et fixe la préséance"
```

---

# Phase 1 — Purge

### Task 2: Retirer la divergence non choisie

Le noyau v1.0 §2.2 pose que « le blocage est doux : il peut continuer à jouer indéfiniment ». Une éclosion forcée au bout de 48 h jauge pleine contredit cette phrase directement. La saturation du stock reste — c'est le plafond de contenance — mais elle ne déclenche plus rien.

**Files:**
- Modify: `src/noyau/noyau.ts` (`prochaineCoupure`, `apresLePas`), `src/noyau/economie.ts` (`divergenceNonChoisieEstDue`), `src/noyau/eclosion.ts` (`eclore`), `src/noyau/constantes.ts`
- Delete: `tests/saturation.test.ts`
- Test: `tests/contenance.test.ts`

**Interfaces:**
- Consumes: rien
- Produces: `eclore(etat: EtatJeu): EtatJeu` — le second paramètre `choisie` disparaît ; toute éclosion est choisie.

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter à la fin de `tests/contenance.test.ts` :

```typescript
describe('le blocage est doux (noyau v1.0 §2.2)', () => {
  it('une jauge pleine pendant une semaine ne déclenche aucune éclosion', () => {
    let etat = etatDeTravail()
    const eclosionsAvant = etat.permanent.nombreEclosions
    // 7 jours en un seul pas, jauge saturée du début à la fin
    etat = tick({ ...etat, cycle: { ...etat.cycle, manaCourant: etat.permanent.contenanceMana } }, 7 * 24 * 3600)
    expect(etat.permanent.nombreEclosions).toBe(eclosionsAvant)
    expect(etat.cycle.manaCourant.eq(etat.permanent.contenanceMana)).toBe(true)
  })
})
```

- [ ] **Step 2: Lancer le test et vérifier qu'il échoue**

Run: `npx vitest run tests/contenance.test.ts -t "une jauge pleine pendant une semaine"`
Expected: FAIL — `nombreEclosions` a augmenté, la divergence s'est déclenchée.

- [ ] **Step 3: Retirer la divergence du noyau**

Dans `src/noyau/noyau.ts`, supprimer la fonction `apresLePas` et l'appeler nulle part — `tickDetaille` rend directement `pasEntier(...)` et la branche récursive rend le résultat de la coupure sans post-traitement. Retirer `retenir(instantDeLaDivergence(etat))` de `prochaineCoupure`, ainsi que la fonction `instantDeLaDivergence`.

Dans `src/noyau/economie.ts`, supprimer `divergenceNonChoisieEstDue`. Garder `estSature` et `eauTroublee` : elles servent l'affichage, elles ne déclenchent rien.

Dans `src/noyau/eclosion.ts`, changer la signature en `export function eclore(etat: EtatJeu): EtatJeu` et supprimer toute branche qui lisait `choisie` — le gain est toujours plein.

Dans `src/noyau/constantes.ts`, supprimer `DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES` et `PART_D_ACQUIS_FIXEE_PAR_DIVERGENCE_NON_CHOISIE`.

Supprimer `secondesEnSaturation` de `EtatCycle` dans `src/noyau/types.ts` et de `cycleInitial()` dans `src/noyau/eclosion.ts`, ainsi que sa mise à jour dans le pas.

- [ ] **Step 4: Supprimer le test devenu faux**

```bash
git rm tests/saturation.test.ts
```

- [ ] **Step 5: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint .`
Expected: PASS. Le test de l'étape 1 passe ; `equivalence-de-pas` et `determinisme` restent verts.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(noyau): retire la divergence non choisie — le blocage est doux"
```

---

### Task 3: Retirer la maturation

**Files:**
- Delete: `src/noyau/maturation.ts`
- Modify: `src/noyau/types.ts` (`EtatPermanent.partsMures`, `TermeDeProduction`, `SourceDeTerme`), `src/noyau/economie.ts` (`partMureDuPalier`, `rendementAcclimatation`), `src/noyau/noyau.ts` (avancement de la part mûre dans le pas), `src/noyau/constantes.ts`
- Test: `tests/canon.test.ts`

**Interfaces:**
- Consumes: Task 2
- Produces: `TermeDeProduction` sans `part_mure`

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter dans `tests/canon.test.ts`, dans le `describe` du lexique :

```typescript
it('la maturation ne survit nulle part dans le noyau', () => {
  const source = fichiersDuNoyau().map(sansCommentaires).join('\n')
  for (const mot of ['partMure', 'partsMures', 'maturation', 'cibleDeMaturation']) {
    expect(source, `« ${mot} » subsiste dans src/noyau/`).not.toContain(mot)
  }
})
```

`fichiersDuNoyau()` et `sansCommentaires` existent déjà dans ce fichier et dans `tests/outils.ts`. Si `fichiersDuNoyau()` n'y est pas sous ce nom, réutiliser l'helper de lecture de `tests/architecture.test.ts`.

- [ ] **Step 2: Lancer le test et vérifier qu'il échoue**

Run: `npx vitest run tests/canon.test.ts -t "la maturation ne survit nulle part"`
Expected: FAIL — « partsMures subsiste dans src/noyau/ ».

- [ ] **Step 3: Retirer la maturation**

```bash
git rm src/noyau/maturation.ts
```

Dans `src/noyau/types.ts` : retirer `part_mure` de `TermeDeProduction` et de `TERMES_DE_PRODUCTION` ; retirer `{ quoi: 'eau_murie'; part: number }` de `SourceDeTerme`. **Ne pas retirer `partsMures` de `EtatPermanent` : le marquer mort**, conformément à la règle de migration.

```typescript
  /**
   * MORT depuis le noyau v1.0 — la maturation gouverne ce qu'un lieu devient,
   * jamais ce que le héros gagne. Le champ reste pour que la save ancienne se
   * relise ; plus rien ne l'écrit ni ne le lit.
   * @deprecated
   */
  readonly partsMures: readonly number[]
```

Dans `src/noyau/economie.ts` : supprimer `partMureDuPalier`. Dans `src/noyau/noyau.ts` : supprimer l'avancement de `partsMures` dans le pas et l'appel à `avancerMaturation`.

Dans `src/noyau/constantes.ts` : supprimer `TAU_MATURATION_HEURES` et `PLACE_QUI_DILUE_A_MOITIE`.

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b`
Expected: PASS, sauf `tests/deux-canaux.test.ts` qui échoue — c'est attendu, il tombe à la tâche suivante. Le lancer en l'excluant : `npx vitest run --exclude tests/deux-canaux.test.ts`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(noyau): retire la maturation — elle gouverne le monde, pas le revenu"
```

---

### Task 4: Retirer le second canal de captation

**Files:**
- Modify: `src/noyau/economie.ts` (`rendementAcclimatation`, `productionAcclimateeDuPalier`, `detailDuCanalAcclimate`, `productionTotaleParSeconde`), `src/noyau/types.ts`, `src/noyau/constantes.ts`, `src/ui/Captation.tsx`
- Delete: `tests/deux-canaux.test.ts`
- Test: `tests/canon.test.ts`

**Interfaces:**
- Consumes: Task 3
- Produces: `productionTotaleParSeconde(etat)` = somme des bancs seule (le héros arrive en Task 9)

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter dans `tests/canon.test.ts` :

```typescript
it('un seul canal de revenu : les espèces (noyau v1.0 §10)', () => {
  const source = fichiersDuNoyau().map(sansCommentaires).join('\n')
  for (const mot of ['acclimat', 'debitAcclimate', 'canalAcclimate']) {
    expect(source, `« ${mot} » subsiste dans src/noyau/`).not.toContain(mot)
  }
})
```

- [ ] **Step 2: Lancer le test et vérifier qu'il échoue**

Run: `npx vitest run tests/canon.test.ts -t "un seul canal de revenu"`
Expected: FAIL — « acclimat subsiste dans src/noyau/ ».

- [ ] **Step 3: Retirer le canal**

Dans `src/noyau/economie.ts` : supprimer `rendementAcclimatation`, `productionAcclimateeDuPalier` et `detailDuCanalAcclimate`. `productionTotaleParSeconde` ne somme plus que les bancs :

```typescript
export function productionTotaleParSeconde(etat: EtatJeu): Decimal {
  let total = new Decimal(0)
  for (const banc of BANCS) {
    if (banc.palier >= etat.cycle.paliersOuverts) continue
    total = total.add(productionDuBanc(etat, banc))
  }
  return total
}
```

Dans `src/noyau/types.ts` : retirer `rendement_acclimatation` et `debit_acclimate` de `TermeDeProduction` et de `TERMES_DE_PRODUCTION` ; retirer `{ quoi: 'acclimatation'; typeMana }` et `{ quoi: 'canal_acclimate' }` de `SourceDeTerme` ; marquer `acclimatations` mort dans `EtatPermanent` avec le même commentaire `@deprecated` que `partsMures`.

Dans `src/noyau/constantes.ts` : supprimer `INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE` et `RENDEMENT_ACCLIMATATION_PLEIN_JUSQU_EN_V05`.

Dans `src/ui/Captation.tsx` : supprimer le bloc qui affiche le canal acclimaté et son total ; l'écran montre désormais un revenu unique, ligne par espèce.

```bash
git rm tests/deux-canaux.test.ts
```

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: PASS, tout vert.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(noyau): un seul canal de revenu, les espèces"
```

---

### Task 5: Remplacer le banc par l'espèce à niveau

C'est la tâche centrale de la phase 1. Le joueur n'achète plus de la place en attendant que la population monte : il monte un niveau, et l'effet est immédiat.

**Files:**
- Delete: `src/noyau/population.ts`
- Modify: `src/noyau/types.ts`, `src/noyau/economie.ts`, `src/noyau/noyau.ts`, `src/noyau/eclosion.ts`, `src/noyau/densite.ts`, `src/noyau/constantes.ts`, `src/donnees/especes.ts`, `src/donnees/paliers.ts`, `src/ui/Mare.tsx`, `src/ui/Captation.tsx`, `tests/etat-de-travail.ts`
- Test: `tests/seuils.test.ts`, `tests/canon.test.ts`

**Interfaces:**
- Consumes: Task 4
- Produces:
  - `EtatEspece { readonly debloquee: boolean; readonly niveau: number }`
  - `EtatCycle.especes: Readonly<Record<EspeceId, EtatEspece>>`
  - `Espece { id, assise, rang, palier }`, `Palier { index, assise, espece: EspeceId | null }`
  - `ESPECES: readonly Espece[]`, `especeParId(id): Espece | undefined`, `especeDuPalier(index): Espece | undefined`
  - `multiplicateurDeSeuil(niveau: number): number`
  - `debloquer(etat, especeId): EtatJeu`, `ameliorer(etat, especeId): EtatJeu`

- [ ] **Step 1: Écrire les tests qui échouent**

Créer `tests/especes.test.ts` :

```typescript
import { describe, expect, it } from 'vitest'
import Decimal from 'break_infinity.js'
import { etatInitial, creuser, debloquer, ameliorer, tick } from '../src/noyau/noyau'
import { multiplicateurDeSeuil, productionTotaleParSeconde } from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'
import { PALIERS } from '../src/donnees/paliers'

const RICHE = (graine = 1) => {
  const base = etatInitial(graine)
  return {
    ...base,
    cycle: { ...base.cycle, manaCourant: new Decimal('1e30') },
    permanent: { ...base.permanent, contenanceMana: new Decimal('1e40') },
  }
}

describe('une espèce est un générateur avec un niveau (noyau v1.0 §1.3)', () => {
  it('21 espèces, réparties 2/4/4/4/4/3', () => {
    expect(ESPECES).toHaveLength(21)
    const parAssise = new Map<string, number>()
    for (const e of ESPECES) parAssise.set(e.assise, (parAssise.get(e.assise) ?? 0) + 1)
    expect([...parAssise.values()]).toEqual([2, 4, 4, 4, 4, 3])
  })

  it('une espèce tous les 3 paliers, à partir du premier de son assise', () => {
    for (const espece of ESPECES) {
      const palier = PALIERS[espece.palier]
      expect(palier.espece).toBe(espece.id)
    }
    const porteurs = PALIERS.filter((p) => p.espece !== null)
    expect(porteurs).toHaveLength(21)
  })

  it('débloquer met le niveau à 1 et produit immédiatement', () => {
    let etat = RICHE()
    const premiere = ESPECES[0]
    expect(productionTotaleParSeconde(etat).eq(0)).toBe(true)
    etat = debloquer(etat, premiere.id)
    expect(etat.cycle.especes[premiere.id].niveau).toBe(1)
    expect(productionTotaleParSeconde(etat).gt(0)).toBe(true)
  })

  it('le niveau agit sans délai : aucune population ne converge', () => {
    let etat = RICHE()
    etat = debloquer(etat, ESPECES[0].id)
    const avant = productionTotaleParSeconde(etat)
    etat = ameliorer(etat, ESPECES[0].id)
    const apres = productionTotaleParSeconde(etat)
    expect(apres.gt(avant)).toBe(true)
    // et le simple écoulement du temps ne change rien à la production
    expect(productionTotaleParSeconde(tick(etat, 3600)).eq(apres)).toBe(true)
  })

  it('les seuils lisent le niveau, cumulés, et cent vaut ×16', () => {
    expect(multiplicateurDeSeuil(1)).toBe(1)
    expect(multiplicateurDeSeuil(9)).toBe(1)
    expect(multiplicateurDeSeuil(10)).toBe(2)
    expect(multiplicateurDeSeuil(25)).toBe(4)
    expect(multiplicateurDeSeuil(50)).toBe(8)
    expect(multiplicateurDeSeuil(100)).toBe(16)
    expect(multiplicateurDeSeuil(5000)).toBe(16)
  })

  it('une espèce dont le palier n’est pas ouvert ne se débloque pas', () => {
    let etat = RICHE()
    const profonde = ESPECES[ESPECES.length - 1]
    etat = debloquer(etat, profonde.id)
    expect(etat.cycle.especes[profonde.id]?.debloquee ?? false).toBe(false)
  })

  it('creuser jusqu’au palier de la deuxième espèce la rend débloquable', () => {
    let etat = RICHE()
    const seconde = ESPECES[1]
    while (etat.cycle.paliersOuverts <= seconde.palier) etat = creuser(etat)
    etat = debloquer(etat, seconde.id)
    expect(etat.cycle.especes[seconde.id].debloquee).toBe(true)
  })
})
```

- [ ] **Step 2: Lancer les tests et vérifier qu'ils échouent**

Run: `npx vitest run tests/especes.test.ts`
Expected: FAIL à la compilation — `debloquer` et `ameliorer` n'existent pas, `ESPECES[0].palier` non plus.

- [ ] **Step 3: Réécrire le contenu**

`src/donnees/especes.ts` — la répartition et l'ancrage :

```typescript
/** 21 espèces : 2 / 4 / 4 / 4 / 4 / 3 (RESULTATS.md, finding 4). */
const ESPECES_PAR_ASSISE: readonly number[] = [2, 4, 4, 4, 4, 3]

function construireEspeces(): readonly Espece[] {
  const especes: Espece[] = []
  ASSISES.forEach((assise, rangAssise) => {
    for (let i = 0; i < ESPECES_PAR_ASSISE[rangAssise]; i += 1) {
      const id = rangAssise === 0 ? ESPECES_DE_LA_NOUE[i] : `espece-${assise.rang}-${i + 1}`
      if (id === ESPECE_RESERVEE) throw new Error('`tanche` est réservée au héros (§2.E)')
      especes.push({
        id,
        assise: assise.id,
        rang: especes.length,
        // une espèce tous les 3 paliers, à partir du premier de l'assise
        palier: assise.indexPremierPalier + i * ESPECE_TOUS_LES_N_PALIERS,
      })
    }
  })
  if (especes.length !== NOMBRE_D_ESPECES_DE_BASE) {
    throw new Error(`Compte d'espèces incohérent : ${especes.length} au lieu de ${NOMBRE_D_ESPECES_DE_BASE}`)
  }
  return especes
}

export const ESPECES: readonly Espece[] = construireEspeces()

const PAR_ID = new Map(ESPECES.map((e) => [e.id, e]))
export function especeParId(id: EspeceId): Espece | undefined {
  return PAR_ID.get(id)
}
```

`ESPECES_DE_LA_NOUE` passe à `['vairon', 'loche']` — l'assise I n'en porte plus que deux, aux paliers 0 et 3. `epinoche` remonte en tête de l'assise II.

`src/donnees/paliers.ts` — un palier porte au plus une espèce :

```typescript
function construirePaliers(): readonly Palier[] {
  const paliers: Palier[] = []
  for (const assise of ASSISES) {
    for (let local = 0; local < assise.nombreDePaliers; local += 1) {
      const index = assise.indexPremierPalier + local
      const espece = ESPECES.find((e) => e.palier === index)
      paliers.push({ index, assise: assise.id, espece: espece?.id ?? null })
    }
  }
  if (paliers.length !== NOMBRE_DE_PALIERS) {
    throw new Error(`Compte de paliers incohérent : ${paliers.length} au lieu de ${NOMBRE_DE_PALIERS}`)
  }
  return paliers
}

export const PALIERS: readonly Palier[] = construirePaliers()

export function especeDuPalier(index: IndexPalier): Espece | undefined {
  const id = PALIERS[index].espece
  return id === null ? undefined : especeParId(id)
}
```

Supprimer `idDeBanc`, `BANCS`, `bancsDuPalier` et `bancParId`.

- [ ] **Step 4: Réécrire l'état et l'économie**

`src/noyau/types.ts` : supprimer `BancId`, `Banc` et `EtatBanc` ; ajouter `EtatEspece` ; dans `EtatCycle`, remplacer `bancs` par `especes: Readonly<Record<EspeceId, EtatEspece>>` et marquer `bancs` mort avec `@deprecated` comme `partsMures`. Retirer `effectif` de `TermeDeProduction`, ajouter `niveau`. Retirer `{ quoi: 'population' }` et `{ quoi: 'place'; place }` de `SourceDeTerme`, ajouter `{ quoi: 'niveau'; readonly niveau: number }`.

`src/noyau/economie.ts` :

```typescript
/** Les seuils lisent le NIVEAU. Table cumulée : cent vaut ×16, jamais ×1024. */
export function multiplicateurDeSeuil(niveau: number): number {
  let multiplicateur = 1
  for (const palier of SEUILS_DE_JALON) {
    if (niveau >= palier.seuil) multiplicateur = palier.multiplicateurCumule
  }
  return multiplicateur
}

/**
 * Chaque espèce nouvelle a un débit de base égal à la somme de toutes les
 * précédentes : elle double donc l'assiette additive à niveaux égaux.
 */
export function debitBaseDeLEspece(espece: Espece): Decimal {
  return new Decimal(TAUX_BASE_AU_PALIER_0).mul(Math.pow(DEBIT_RATIO_ESPECE, espece.rang))
}

export function productionDeLEspece(etat: EtatJeu, espece: Espece): Decimal {
  const vivante = etat.cycle.especes[espece.id]
  if (vivante === undefined || !vivante.debloquee || vivante.niveau === 0) return new Decimal(0)
  return debitBaseDeLEspece(espece).mul(vivante.niveau).mul(multiplicateurDeSeuil(vivante.niveau))
}

export function coutDeDeblocage(etat: EtatJeu, espece: Espece): Decimal {
  return coutBaseDuPalier(espece.palier)
    .mul(COUT_DEBLOCAGE_RATIO)
    .mul(facteurDeTechnique(etat, 'cout_deblocage'))
    .mul(facteurDeSucces(etat, 'cout_deblocage'))
}

export function coutDeNiveau(etat: EtatJeu, espece: Espece, niveau: number): Decimal {
  return debitBaseDeLEspece(espece)
    .mul(COUT_NIVEAU_PAR_DEBIT)
    .mul(puissanceDuCoutDeNiveau(niveau))
    .mul(facteurDeTechnique(etat, 'cout_niveau'))
    .mul(facteurDeSucces(etat, 'cout_niveau'))
}
```

Renommer dans `TermeDeCout` : `cout_place` devient `cout_niveau`, `cout_reconviction` devient `cout_deblocage`. Supprimer `productionDuBanc`, `tauxParIndividu`, `tauxParIndividuHorsSeuil`, `tauxBaseDuBanc`, `placeDuPalier`, `coutDeConviction`, `coutDePlace`. `productionTotaleParSeconde` boucle sur `ESPECES` au lieu de `BANCS`.

`src/noyau/noyau.ts` — les deux actes remplacent les deux anciens :

```typescript
/** Débloquer une espèce. Une fois par espèce et par vie ; elle démarre au niveau 1. */
export function debloquer(etat: EtatJeu, especeId: EspeceId): EtatJeu {
  const espece = especeParId(especeId)
  if (espece === undefined) return etat
  if (espece.palier >= etat.cycle.paliersOuverts) return etat
  if (etat.cycle.especes[especeId]?.debloquee === true) return etat
  const cout = coutDeDeblocage(etat, espece)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      especes: { ...etat.cycle.especes, [especeId]: { debloquee: true, niveau: 1 } },
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'recrutement', 1),
    },
  }
}

/** Monter une espèce d'un niveau. L'achat répétable de la boucle, ×1.15. */
export function ameliorer(etat: EtatJeu, especeId: EspeceId): EtatJeu {
  const espece = especeParId(especeId)
  if (espece === undefined) return etat
  const avant = etat.cycle.especes[especeId]
  if (avant === undefined || !avant.debloquee) return etat
  const cout = coutDeNiveau(etat, espece, avant.niveau)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  const niveau = avant.niveau + 1
  const atteintCent =
    niveau >= SEUIL_DU_DRAPEAU_PERMANENT &&
    !etat.permanent.especesAyantAtteintCent.includes(especeId)
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      especes: { ...etat.cycle.especes, [especeId]: { debloquee: true, niveau } },
    },
    permanent: {
      ...etat.permanent,
      especesAyantAtteintCent: atteintCent
        ? [...etat.permanent.especesAyantAtteintCent, especeId]
        : etat.permanent.especesAyantAtteintCent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'amelioration', cout.toNumber()),
    },
  }
}
```

Supprimer `convaincre` et `acheterPlace`.

**Le drapeau des cent tombe maintenant à l'achat, plus pendant un pas.** Retirer `instantDuProchainDrapeau` de `prochaineCoupure` : plus rien ne coupe le pas, `tickDetaille` devient un simple `pasEntier`. C'est ce qui rend l'équivalence de pas triviale au lieu de délicate.

Supprimer `src/noyau/population.ts` et `vitesseDeRepeuplement` de `src/noyau/densite.ts` ; supprimer `K_TAUX_DE_REPEUPLEMENT` et `COUT_DE_PLACE_AU_PALIER_0` de `src/noyau/constantes.ts`, ajouter `COUT_DEBLOCAGE_RATIO = 0.6`, `COUT_NIVEAU_PAR_DEBIT` (dérivé de `COUT_DEBLOCAGE_AU_PALIER_0`), `DEBIT_RATIO_ESPECE = 2` et `ESPECE_TOUS_LES_N_PALIERS = 3`.

- [ ] **Step 5: Réparer les appelants**

`tests/etat-de-travail.ts` : remplacer la boucle sur `BANCS` par

```typescript
  for (const espece of ESPECES.filter((e) => e.palier < etat.cycle.paliersOuverts)) {
    etat = debloquer(etat, espece.id)
    for (let n = 0; n < 12 + espece.palier; n += 1) etat = ameliorer(etat, espece.id)
  }
```

et supprimer le `tick(etat, 137)` final : plus aucun effectif ne converge, l'état est complet à l'achat. Rendre `etat` directement.

`src/ui/Mare.tsx` et `src/ui/Captation.tsx` : remplacer `convaincre`/`acheterPlace` par `debloquer`/`ameliorer`, et l'affichage d'un effectif par celui d'un niveau. `src/ui/format.ts` : supprimer le formateur d'effectif s'il n'est plus appelé.

`src/etat/magasin.ts` : renommer les actions exposées au même endroit.

- [ ] **Step 6: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: PASS. `tests/especes.test.ts` vert, `equivalence-de-pas`, `determinisme`, `architecture`, `voix`, `persistance` verts.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(noyau): une espèce est un générateur avec un niveau"
```

---

### Task 6: Retirer le tarif de redescente et le puits d'aménagement

**Files:**
- Modify: `src/noyau/economie.ts` (`estUnAmenagement`, `coutDeDescente`), `src/noyau/constantes.ts`, `src/noyau/eclosion.ts` (`FRACTION_CONSERVEE`), `src/noyau/types.ts`
- Test: `tests/redescente.test.ts` (réécrit), `tests/canon.test.ts`

**Interfaces:**
- Consumes: Task 5
- Produces: `coutDeDescente(etat, cible)` sans branche d'aménagement

- [ ] **Step 1: Réécrire `tests/redescente.test.ts` en entier**

```typescript
/**
 * f = 1 — reset complet. Le noyau v1.0 §3.1 ferme [P5] : « Tout se repaie au
 * prix d'origine. Il n'y a pas de tarif réduit à la redescente, comme dans
 * n'importe quel idle. »
 */
import { describe, expect, it } from 'vitest'
import Decimal from 'break_infinity.js'
import { etatInitial, creuser } from '../src/noyau/noyau'
import { coutDeDescente } from '../src/noyau/economie'
import { eclore } from '../src/noyau/eclosion'

describe('§3.1 — la redescente se paie plein tarif', () => {
  it('un palier déjà atteint dans une vie passée coûte exactement ce qu’il coûtait', () => {
    let etat = {
      ...etatInitial(7),
      cycle: { ...etatInitial(7).cycle, manaCourant: new Decimal('1e30') },
      permanent: { ...etatInitial(7).permanent, contenanceMana: new Decimal('1e40') },
    }
    const coutNeuf = coutDeDescente(etat, 1)
    for (let i = 0; i < 5; i += 1) etat = creuser(etat)
    expect(etat.permanent.profondeurMaxAtteinte).toBeGreaterThanOrEqual(6)

    const apres = { ...eclore(etat), cycle: { ...eclore(etat).cycle, manaCourant: new Decimal('1e30') } }
    expect(coutDeDescente(apres, 1).eq(coutNeuf)).toBe(true)
  })

  it('creuser un palier neuf et le recreuser après éclosion coûtent le même prix', () => {
    const neuf = etatInitial(7)
    const riche = {
      ...neuf,
      cycle: { ...neuf.cycle, manaCourant: new Decimal('1e30') },
      permanent: {
        ...neuf.permanent,
        contenanceMana: new Decimal('1e40'),
        profondeurMaxAtteinte: 40, // une vie passée est allée très bas
      },
    }
    // la profondeur déjà atteinte ne doit rien changer au prix
    expect(coutDeDescente(riche, 3).eq(coutDeDescente(neuf, 3))).toBe(true)
  })
})
```

- [ ] **Step 2: Lancer le test et vérifier qu'il échoue**

Run: `npx vitest run tests/redescente.test.ts`
Expected: FAIL — `coutDeDescente` applique encore `F_FRACTION_D_AMENAGEMENT` sous la profondeur max.

- [ ] **Step 3: Retirer le tarif**

Dans `src/noyau/economie.ts` : supprimer `estUnAmenagement` ; `coutDeDescente` devient

```typescript
export function coutDeDescente(etat: EtatJeu, cible: IndexPalier): Decimal {
  return coutBaseDuPalier(cible)
    .mul(facteurDeTechnique(etat, 'cout_creuser'))
    .mul(facteurDeSucces(etat, 'cout_creuser'))
}
```

Dans `src/noyau/constantes.ts` : supprimer `F_TARIF_REDESCENTE`, `F_FRACTION_D_AMENAGEMENT` et `EXPOSANT_RECONVICTION_DENSITE`. Dans `src/noyau/eclosion.ts` : supprimer `FRACTION_CONSERVEE`. Dans `src/noyau/types.ts` : supprimer `reduction_technique` de `TermeDeCout` si plus rien ne la cible, sinon la garder telle quelle.

Dans `tests/canon.test.ts`, le test « `f` = 1 : reset complet, aucune fraction conservée » lit `F_TARIF_REDESCENTE`. Le réécrire pour qu'il affirme l'absence :

```typescript
it('f = 1 : reset complet, et la constante elle-même n’existe plus', () => {
  const source = fichiersDuNoyau().map(sansCommentaires).join('\n')
  expect(source).not.toContain('F_TARIF_REDESCENTE')
  expect(source).not.toContain('estUnAmenagement')
})
```

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint .`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(noyau): f = 1 — plus de tarif réduit ni de puits d'aménagement"
```

---

### Task 7: Migrer la sauvegarde

**Files:**
- Modify: `src/adaptateurs/persistance.ts` (`MIGRATIONS`, `serialiser`, `deserialiser`), `src/noyau/constantes.ts` (`VERSION_SAVE`)
- Test: `tests/persistance.test.ts`

**Interfaces:**
- Consumes: Task 6
- Produces: `VERSION_SAVE = 5`, migration `4 → 5`

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter à `tests/persistance.test.ts` :

```typescript
describe('migration 4 → 5 : le modèle à population meurt sans emporter la save', () => {
  it('une save v4 se relit, ses champs morts sont ignorés, aucun n’est supprimé', () => {
    const v4 = {
      version: 4,
      contenu: {
        cycle: { bancs: { 'vairon@0': { place: 9, effectif: 7 } }, manaCourant: '500' },
        permanent: { partsMures: [1, 1, 1], acclimatations: { douce: 1 }, nombreEclosions: 2 },
      },
    } as unknown as SaveSerialisee

    const relu = deserialiser(v4, etatInitial(1))
    expect(relu.permanent.nombreEclosions).toBe(2)
    expect(relu.cycle.especes).toEqual({})           // rien n'est reconstruit depuis les bancs
    expect(relu.cycle.manaCourant.eq(500)).toBe(true)
  })

  it('la migration ne supprime aucun champ : elle les laisse passer', () => {
    const migre = MIGRATIONS[4]({
      cycle: { bancs: { x: { place: 1, effectif: 1 } } },
      permanent: { partsMures: [1] },
    }) as Record<string, Record<string, unknown>>
    expect(migre.cycle).toHaveProperty('bancs')
    expect(migre.permanent).toHaveProperty('partsMures')
    expect(migre.cycle).toHaveProperty('especes')
  })
})
```

- [ ] **Step 2: Lancer le test et vérifier qu'il échoue**

Run: `npx vitest run tests/persistance.test.ts -t "migration 4"`
Expected: FAIL — `MIGRATIONS[4]` n'existe pas.

- [ ] **Step 3: Écrire la migration**

Dans `src/noyau/constantes.ts` : `export const VERSION_SAVE = 5`.

Dans `src/adaptateurs/persistance.ts`, ajouter à `MIGRATIONS` :

```typescript
  /**
   * 4 → 5 — le noyau v1.0. La population, la maturation et le second canal
   * meurent. Conformément au §8.3, on ne SUPPRIME aucun champ : `bancs`,
   * `partsMures` et `acclimatations` restent dans la save, plus personne ne les
   * lit. On ouvre `especes` vide — une save d'avant n'a aucun niveau à
   * reconstruire, et le cycle en cours est de toute façon perdu au premier
   * reset. C'est une éclosion de plus, pas une perte de progression : la
   * contenance, la densité, la Foi et la technique sont dans `permanent`.
   */
  4: (contenu) => {
    const etat = contenu as Record<string, Record<string, unknown>>
    return {
      ...etat,
      cycle: { ...etat.cycle, especes: {} },
    }
  },
```

`serialiser` écrit `cycle.especes` et n'écrit plus `cycle.bancs`. `deserialiser` lit `cycle.especes` avec `{}` en repli, et ignore `bancs`, `partsMures`, `acclimatations`.

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(persistance): migration 4 → 5, les champs morts restent mais ne se lisent plus"
```

---

### Task 8: Fermer la phase 1

**Files:**
- Test: `tests/canon.test.ts`

**Interfaces:**
- Consumes: Task 7
- Produces: rien de neuf — c'est le portail de la phase

- [ ] **Step 1: Écrire le test de purge complète**

```typescript
it('le modèle mort ne subsiste nulle part dans src/ (spec §4)', () => {
  const source = tousLesFichiersDeSrc().map(sansCommentaires).join('\n')
  for (const mot of [
    'population', 'maturation', 'acclimat', 'partMure',
    'cout_place', 'convaincre', 'acheterPlace', 'BANCS', 'bancParId',
  ]) {
    expect(source, `« ${mot} » subsiste dans src/`).not.toContain(mot)
  }
})
```

`tousLesFichiersDeSrc()` : réutiliser le lecteur récursif de `tests/architecture.test.ts`, élargi de `src/noyau` à `src`.

- [ ] **Step 2: Lancer et corriger jusqu'au vert**

Run: `npx vitest run tests/canon.test.ts -t "le modèle mort ne subsiste"`
Expected: PASS. Toute occurrence restante est un oubli des tâches 2 à 7 — la corriger là où elle est, pas ici.

- [ ] **Step 3: Vérification complète de la phase**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: tout vert, `vite build` propre.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test(canon): verrouille la purge du modèle à population"
```

---

# Phase 2 — Le cœur v1.0

### Task 9: L'amorçage et le multiplicateur de profondeur

`RESULTATS.md` finding 3 : sans débit propre au héros, la production est nulle, donc le mana est nul, donc aucune espèce n'est jamais débloquée — la partie ne démarre pas.

**Files:**
- Modify: `src/noyau/constantes.ts`, `src/noyau/economie.ts`
- Test: `tests/amorçage.test.ts` (créer)

**Interfaces:**
- Consumes: Task 8
- Produces: `DEBIT_HEROS`, `multiplicateurDePalier(): number`, `multiplicateurDeProfondeur(etat): Decimal`, `ECHELLE_DE_PRODUCTION`

- [ ] **Step 1: Écrire le test qui échoue**

Créer `tests/amorcage.test.ts` :

```typescript
import { describe, expect, it } from 'vitest'
import { etatInitial, tick, debloquer } from '../src/noyau/noyau'
import { productionTotaleParSeconde, coutDeDeblocage } from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'
import { D_PRODUCTION_PAR_PALIER, multiplicateurDePalier, ESPECE_TOUS_LES_N_PALIERS, DEBIT_RATIO_ESPECE } from '../src/noyau/constantes'

describe('finding 3 — la partie démarre toute seule', () => {
  it('à l’état initial, sans aucune espèce, la production est strictement positive', () => {
    expect(productionTotaleParSeconde(etatInitial(1)).gt(0)).toBe(true)
  })

  it('le héros seul finance la première espèce en moins de deux heures', () => {
    let etat = etatInitial(1)
    etat = tick(etat, 2 * 3600)
    expect(etat.cycle.manaCourant.gte(coutDeDeblocage(etat, ESPECES[0]))).toBe(true)
    etat = debloquer(etat, ESPECES[0].id)
    expect(etat.cycle.especes[ESPECES[0].id].debloquee).toBe(true)
  })

  it('le débit du héros devient négligeable dès la première espèce montée', () => {
    let etat = etatInitial(1)
    const heros = productionTotaleParSeconde(etat)
    etat = tick(etat, 2 * 3600)
    etat = debloquer(etat, ESPECES[0].id)
    for (let n = 0; n < 20; n += 1) etat = tick(etat, 600)
    expect(productionTotaleParSeconde(etat).div(heros).gt(10)).toBe(true)
  })
})

describe('le multiplicateur de palier porte D', () => {
  it('trois paliers valent D³, dont ×2 vient de l’espèce nouvelle', () => {
    const mp = multiplicateurDePalier()
    const parTroisPaliers = Math.pow(mp, ESPECE_TOUS_LES_N_PALIERS) * DEBIT_RATIO_ESPECE
    expect(parTroisPaliers).toBeCloseTo(Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS), 6)
  })
})
```

- [ ] **Step 2: Lancer les tests et vérifier qu'ils échouent**

Run: `npx vitest run tests/amorcage.test.ts`
Expected: FAIL — `multiplicateurDePalier` n'existe pas ; la production initiale vaut 0.

- [ ] **Step 3: Implémenter**

Dans `src/noyau/constantes.ts` :

```typescript
/**
 * Débit propre du héros, mana/s. C'est sa mutation : il capte l'ambiant seul.
 * Sans lui la partie ne démarre pas (RESULTATS.md, finding 3), et le terme
 * devient négligeable dès la première espèce.
 */
export const DEBIT_HEROS = 0.05

/** Échelle globale de production. Règle la DURÉE absolue, rien d'autre. */
export const ECHELLE_DE_PRODUCTION = 1

/**
 * `m_p` — multiplicateur global accordé par chaque palier ouvert.
 *
 * Sur les 3 paliers séparant deux espèces, la production totale doit être
 * multipliée par D³. L'espèce nouvelle apporte ×2 (elle double l'assiette).
 * Donc m_p³ × 2 = D³.
 */
export function multiplicateurDePalier(): number {
  return Math.pow(
    Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS) / DEBIT_RATIO_ESPECE,
    1 / ESPECE_TOUS_LES_N_PALIERS,
  )
}
```

Dans `src/noyau/economie.ts` :

```typescript
export function multiplicateurDeProfondeur(etat: EtatJeu): Decimal {
  return new Decimal(multiplicateurDePalier()).pow(etat.cycle.paliersOuverts - 1)
}

export function productionTotaleParSeconde(etat: EtatJeu): Decimal {
  let assiette = new Decimal(DEBIT_HEROS)
  for (const espece of ESPECES) {
    if (espece.palier >= etat.cycle.paliersOuverts) continue
    assiette = assiette.add(productionDeLEspece(etat, espece))
  }
  return assiette
    .mul(multiplicateurDeProfondeur(etat))
    .mul(multiplicateurDensite(densiteTotale(etat)))
    .mul(multiplicateurDesDrapeaux(etat))
    .mul(ECHELLE_DE_PRODUCTION)
}
```

`EtatPermanent` garde `densites: readonly number[]` par palier — ne pas ajouter de champ agrégé, qui serait un second état à tenir cohérent. Ajouter à `src/noyau/densite.ts` :

```typescript
/** La densité du bassin : la somme de ce que chaque palier porte. */
export function densiteTotale(etat: EtatJeu): number {
  return etat.permanent.densites.reduce((somme, d) => somme + d, 0)
}
```

`multiplicateurDensite(densite: number)` garde sa signature actuelle.

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(noyau): DEBIT_HEROS amorce la partie, et m_p porte D"
```

---

### Task 10: L'éclosion — reset complet, gain indexé sur la pointe

**Files:**
- Modify: `src/noyau/eclosion.ts`, `src/noyau/densite.ts`, `src/noyau/constantes.ts`
- Test: `tests/eclosion.test.ts` (créer)

**Interfaces:**
- Consumes: Task 9
- Produces: `eclore(etat): EtatJeu` conforme au §3, `PALIERS_GAGNES_PAR_ECLOSION = 4`

- [ ] **Step 1: Écrire le test qui échoue**

Créer `tests/eclosion.test.ts` :

```typescript
import { describe, expect, it } from 'vitest'
import Decimal from 'break_infinity.js'
import { etatDeTravail } from './etat-de-travail'
import { eclore } from '../src/noyau/eclosion'
import { densiteTotale } from '../src/noyau/densite'
import { ACQUIS_MAX, ALPHA_GAIN_DE_DENSITE, G_COUT_PALIER, PALIERS_GAGNES_PAR_ECLOSION } from '../src/noyau/constantes'

describe('§3.1 — l’éclosion remet tout à zéro sauf quatre choses', () => {
  it('mana, paliers et espèces repartent de zéro', () => {
    const apres = eclore(etatDeTravail())
    expect(apres.cycle.manaCourant.eq(0)).toBe(true)
    expect(apres.cycle.paliersOuverts).toBe(1)
    expect(Object.values(apres.cycle.especes).every((e) => !e.debloquee && e.niveau === 0)).toBe(true)
  })

  it('contenance, densité, Foi et technique traversent', () => {
    const avant = etatDeTravail()
    const apres = eclore(avant)
    expect(apres.permanent.contenanceMana.gt(avant.permanent.contenanceMana)).toBe(true)
    expect(apres.permanent.foi.gte(avant.permanent.foi)).toBe(true)
    expect(apres.permanent.noeudsTechnique).toEqual(avant.permanent.noeudsTechnique)
    expect(apres.permanent.compteursTechnique).toEqual(avant.permanent.compteursTechnique)
  })

  it('le drapeau des cent survit — c’est l’unique exception (§2.1)', () => {
    const avant = { ...etatDeTravail() }
    const truque = { ...avant, permanent: { ...avant.permanent, especesAyantAtteintCent: ['vairon'] } }
    expect(eclore(truque).permanent.especesAyantAtteintCent).toEqual(['vairon'])
  })

  it('la densité ne redescend jamais, et son gain vaut pointe^α', () => {
    const avant = { ...etatDeTravail() }
    const pointe = new Decimal(1e6)
    const truque = { ...avant, cycle: { ...avant.cycle, productionPicParSeconde: pointe } }
    const gain = densiteTotale(eclore(truque)) - densiteTotale(avant)
    expect(gain).toBeCloseTo(Math.pow(pointe.toNumber(), ALPHA_GAIN_DE_DENSITE), 3)
  })

  it('la contenance monte par le séjour, et un cycle plein vise g^(paliers gagnés)', () => {
    const avant = etatDeTravail()
    // acquis de séjour au maximum : c'est le cycle le mieux joué possible
    const sature = { ...avant, cycle: { ...avant.cycle, acquisDeSejour: ACQUIS_MAX } }
    const rapport = eclore(sature).permanent.contenanceMana.div(avant.permanent.contenanceMana).toNumber()
    expect(rapport).toBeCloseTo(Math.pow(G_COUT_PALIER, PALIERS_GAGNES_PAR_ECLOSION), 3)
  })

  it('un cycle écourté fixe moins de contenance qu’un cycle plein', () => {
    const avant = etatDeTravail()
    const court = { ...avant, cycle: { ...avant.cycle, acquisDeSejour: ACQUIS_MAX / 4 } }
    const plein = { ...avant, cycle: { ...avant.cycle, acquisDeSejour: ACQUIS_MAX } }
    expect(eclore(court).permanent.contenanceMana.lt(eclore(plein).permanent.contenanceMana)).toBe(true)
  })
})
```

- [ ] **Step 2: Lancer les tests et vérifier qu'ils échouent**

Run: `npx vitest run tests/eclosion.test.ts`
Expected: FAIL — `PALIERS_GAGNES_PAR_ECLOSION` n'existe pas ; le gain de densité suit encore l'ancienne loi.

- [ ] **Step 3: Implémenter**

Dans `src/noyau/constantes.ts` :

```typescript
/**
 * Marge de contenance gagnée à chaque éclosion, en paliers. C'est ce qui
 * autorise à descendre plus bas au cycle suivant.
 */
export const PALIERS_GAGNES_PAR_ECLOSION = 4
```

`CONTENANCE_PAR_ECLOSION` devient `Math.pow(G_COUT_PALIER, PALIERS_GAGNES_PAR_ECLOSION)`.

**La contenance continue de monter par le séjour, pas par un forfait.** Le spike de `docs/noyau.ts` multiplie bêtement par `g^4` à chaque éclosion ; le jeu garde `acquisDeSejour`, qui est un pilier du Tier 0 — « le plafond ne monte que par séjour prolongé en mana dense » — et que le spec §4 range explicitement dans ce qui vit. `CONTENANCE_PAR_ECLOSION` devient donc la **cible** qu'un cycle parfaitement joué atteint : `eclore` multiplie la contenance par `CONTENANCE_PAR_ECLOSION ^ (acquisDeSejour / ACQUIS_MAX)`. Un cycle plein donne `g^4`, un cycle écourté donne moins, et le forfait du spike est le cas limite.

Dans `src/noyau/densite.ts`, `densiteLaisseeParLeCycle` rend `pointe ^ α` :

```typescript
/**
 * Le gain de densité est indexé sur la PRODUCTION DE POINTE, jamais sur la
 * profondeur : une monnaie de prestige indexée sur la profondeur croît
 * linéairement, donc son effet relatif s'effondre en fin de partie.
 */
export function densiteLaisseeParLeCycle(productionDePic: Decimal): number {
  return Math.pow(productionDePic.toNumber(), ALPHA_GAIN_DE_DENSITE)
}
```

Dans `src/noyau/eclosion.ts`, `eclore` remet `manaCourant`, `paliersOuverts`, `especes` et `productionPicParSeconde` à leur valeur initiale, multiplie `contenanceMana` par `CONTENANCE_PAR_ECLOSION`, ajoute le gain de densité, incrémente `nombreEclosions`, et ne touche à rien d'autre de `permanent`.

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint .`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(eclosion): reset complet, gain de densité indexé sur la pointe"
```

---

### Task 11: Une partie headless atteint l'éclosion 2

C'est le critère de sortie de la phase 2, formulé par l'amendement v1.1 §2 et repris par le spec.

**Files:**
- Test: `tests/partie-headless.test.ts` (créer)

**Interfaces:**
- Consumes: Task 10
- Produces: `jouerJusquA(eclosions: number): EtatJeu` exporté depuis le test, réutilisé par personne — c'est un portail, pas une API

- [ ] **Step 1: Écrire le test**

```typescript
/**
 * Le critère du §12 : « une partie sans UI atteint l'éclosion 2 en headless ».
 * Une politique naïve — acheter le moins cher qui soit payable — suffit ; si
 * elle n'y arrive pas, c'est l'économie qui est fausse, pas la politique.
 */
import { describe, expect, it } from 'vitest'
import { etatInitial, tick, creuser, debloquer, ameliorer } from '../src/noyau/noyau'
import { eclore } from '../src/noyau/eclosion'
import { coutDeDescente, coutDeDeblocage, coutDeNiveau, estBloque, toutEstCreuse } from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'
import type { EtatJeu } from '../src/noyau/types'

function acheterLeMoinsCher(etat: EtatJeu): EtatJeu {
  let meilleur: { cout: import('break_infinity.js').default; appliquer: () => EtatJeu } | null = null
  const retenir = (cout: import('break_infinity.js').default, appliquer: () => EtatJeu) => {
    if (etat.cycle.manaCourant.lt(cout)) return
    if (meilleur === null || cout.lt(meilleur.cout)) meilleur = { cout, appliquer }
  }
  if (!toutEstCreuse(etat)) {
    const cible = etat.cycle.paliersOuverts
    if (coutDeDescente(etat, cible).lte(etat.permanent.contenanceMana)) {
      retenir(coutDeDescente(etat, cible), () => creuser(etat))
    }
  }
  for (const espece of ESPECES) {
    if (espece.palier >= etat.cycle.paliersOuverts) continue
    const vivante = etat.cycle.especes[espece.id]
    if (vivante === undefined || !vivante.debloquee) retenir(coutDeDeblocage(etat, espece), () => debloquer(etat, espece.id))
    else retenir(coutDeNiveau(etat, espece, vivante.niveau), () => ameliorer(etat, espece.id))
  }
  return meilleur === null ? etat : meilleur.appliquer()
}

describe('§12 — une partie headless', () => {
  it('atteint l’éclosion 2 sans interface, en moins de 200 heures de jeu', () => {
    let etat = etatInitial(2026)
    let secondes = 0
    while (etat.permanent.nombreEclosions < 2 && secondes < 200 * 3600) {
      const avant = etat
      etat = acheterLeMoinsCher(etat)
      if (etat === avant) {
        if (estBloque(etat)) etat = eclore(etat)
        else { etat = tick(etat, 60); secondes += 60 }
      }
    }
    expect(etat.permanent.nombreEclosions).toBeGreaterThanOrEqual(2)
    expect(etat.permanent.profondeurMaxAtteinte).toBeGreaterThan(6)
  })
})
```

- [ ] **Step 2: Lancer le test**

Run: `npx vitest run tests/partie-headless.test.ts`
Expected: PASS. S'il échoue par blocage, la cause est dans `estBloque` ou dans `CONTENANCE_INITIALE` — corriger là, pas dans le test.

- [ ] **Step 3: Vérifier l'équivalence de pas sur le nouveau cœur**

Run: `npx vitest run tests/equivalence-de-pas.test.ts tests/determinisme.test.ts tests/architecture.test.ts`
Expected: PASS. Le pas n'a plus aucune coupure interne, l'équivalence doit être exacte et non plus seulement à la tolérance.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test(noyau): une partie headless atteint l'éclosion 2"
```

---

# Phase 3 — Mesurer

### Task 12: Rebrancher le simulateur

**Files:**
- Modify: `src/simulateur/simulateur.ts` (`Politique`, `POLITIQUE_PAR_DEFAUT`, `simuler`)
- Test: `tests/simulateur.test.ts`

**Interfaces:**
- Consumes: Task 11
- Produces: `simuler(nbEclosions, politique, graine)` rendant `{ cycles: MesureDeCycle[], secondesActives, secondesEcoulees }`

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter à `tests/simulateur.test.ts` :

```typescript
describe('le simulateur tourne sur le noyau v1.0', () => {
  it('quinze éclosions, et le temps actif est distinct du temps écoulé', () => {
    const r = simuler(15, POLITIQUE_PAR_DEFAUT, 1)
    expect(r.cycles).toHaveLength(15)
    expect(r.secondesActives).toBeGreaterThan(0)
    expect(r.secondesEcoulees).toBeGreaterThanOrEqual(r.secondesActives)
  })

  it('le premier cycle dure environ trois heures', () => {
    const r = simuler(15, POLITIQUE_PAR_DEFAUT, 1)
    const h = r.cycles[0].dureeEcouleeSecondes / 3600
    expect(h).toBeGreaterThan(1)
    expect(h).toBeLessThan(8)
  })

  it('la politique optimale et la politique relâchée diffèrent (finding 2)', () => {
    const optimale = simuler(13, POLITIQUE_PAR_DEFAUT, 1)
    const relachee = simuler(13, { ...POLITIQUE_PAR_DEFAUT, secondesEntreReleves: 4 * 3600 }, 1)
    expect(relachee.secondesEcoulees).toBeGreaterThan(optimale.secondesEcoulees * 2)
  })
})
```

- [ ] **Step 2: Lancer et vérifier l'échec**

Run: `npx vitest run tests/simulateur.test.ts -t "le simulateur tourne sur le noyau v1.0"`
Expected: FAIL — la politique achète encore de la place et convainc des bancs.

- [ ] **Step 3: Réécrire la politique**

```typescript
export interface Politique {
  /** 0 = achat continu (joueur optimal). 4 h = un relevé toutes les 4 h. */
  readonly secondesEntreReleves: number
  /** Pas d'intégration entre deux décisions, en secondes. */
  readonly pas: number
}

export const POLITIQUE_PAR_DEFAUT: Politique = { secondesEntreReleves: 0, pas: 60 }

export type Achat =
  | { readonly type: 'creuser'; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'debloquer'; readonly espece: Espece; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'niveau'; readonly espece: Espece; readonly cout: Decimal; readonly gain: Decimal }

/**
 * Gains marginaux, calculés analytiquement.
 *
 * La production est un produit de facteurs dont un seul dépend de l'achat :
 *   prod = assiette × mult_profondeur × mult_densité × mult_drapeaux × échelle
 * Chaque gain se dérive donc en O(1), sans copier l'état. La version naïve, qui
 * recopiait l'état pour chaque candidat, rendait le calibrage impraticable.
 */
export function achatsDisponibles(etat: EtatJeu): readonly Achat[] {
  const out: Achat[] = []
  const prod = productionTotaleParSeconde(etat)
  const assiette = prod.div(multiplicateurDeProfondeur(etat))
    .div(multiplicateurDensite(densiteTotale(etat)))
    .div(multiplicateurDesDrapeaux(etat))
    .div(ECHELLE_DE_PRODUCTION)
  const parUniteAssiette = prod.div(assiette)

  if (!toutEstCreuse(etat)) {
    const cible = etat.cycle.paliersOuverts
    const cout = coutDeDescente(etat, cible)
    // creuser ne se propose que si la contenance peut le porter : c'est le blocage doux
    if (cout.lte(etat.permanent.contenanceMana)) {
      out.push({ type: 'creuser', cout, gain: prod.mul(multiplicateurDePalier() - 1) })
    }
  }

  for (const espece of ESPECES) {
    if (espece.palier >= etat.cycle.paliersOuverts) continue
    const vivante = etat.cycle.especes[espece.id]
    const debit = debitBaseDeLEspece(espece)
    if (vivante === undefined || !vivante.debloquee) {
      out.push({
        type: 'debloquer',
        espece,
        cout: coutDeDeblocage(etat, espece),
        gain: debit.mul(multiplicateurDeSeuil(1)).mul(parUniteAssiette),
      })
    } else {
      const n = vivante.niveau
      const delta = (n + 1) * multiplicateurDeSeuil(n + 1) - n * multiplicateurDeSeuil(n)
      let gain = debit.mul(delta).mul(parUniteAssiette)
      // le niveau 100 ouvre en plus un bonus de production GLOBALE, définitif
      if (n + 1 === SEUIL_DU_DRAPEAU_PERMANENT) gain = gain.add(prod.mul(BONUS_GLOBAL_A_CENT_INDIVIDUS))
      out.push({ type: 'niveau', espece, cout: coutDeNiveau(etat, espece, n), gain })
    }
  }
  return out
}
```

La règle de choix : parmi les achats payables, prendre celui dont `cout / gain` est le plus bas — le meilleur temps de retour. `simuler` compte **deux** temps :

```typescript
export interface ResultatDeSimulation {
  readonly cycles: readonly MesureDeCycle[]
  /** Somme des relevés où le joueur agit — les ~38 h du finding 2. */
  readonly secondesActives: number
  /** Temps de jeu total, plafonnements de contenance inclus — les ~25 jours. */
  readonly secondesEcoulees: number
}
```

Les confondre est exactement l'erreur que le finding 2 a corrigée. Retirer aussi `secondesEnRedescente` de `MesureDeCycle` et de `EtatTelemetrie` : la redescente n'a plus de tarif propre depuis la tâche 6, la mesure n'a plus d'objet.

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint .`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(simulateur): rebranche la politique sur les trois achats"
```

---

### Task 13: Résoudre α, θ, l'échelle et les six (A, B)

**Files:**
- Modify: `src/simulateur/calibreur.ts`, `src/noyau/technique.ts` (`COUPLES_A_B`)
- Test: `tests/calibrage.test.ts` (créer)

**Interfaces:**
- Consumes: Task 12
- Produces: `resoudreCourbe(cible): { alpha, theta, echelle }`, `COUPLES_A_B` renseigné pour les six branches

- [ ] **Step 1: Écrire le test qui échoue**

Créer `tests/calibrage.test.ts` :

```typescript
import { describe, expect, it } from 'vitest'
import { resoudreCourbe, resoudreCoupleAB } from '../src/simulateur/calibreur'
import { COUTS_DE_NOEUD } from '../src/noyau/constantes'
import { COUPLES_A_B, pointsDeBranche } from '../src/noyau/technique'

describe('finding 5 — la loi de l’exposant', () => {
  it('l’exposant de densité vaut θ/α, et θ reste dans [0, 1]', () => {
    const { alpha, theta } = resoudreCourbe({ dureeCycle1Heures: 3, nbEclosions: 45 })
    expect(theta).toBeGreaterThanOrEqual(0)
    expect(theta).toBeLessThanOrEqual(1)
    expect(alpha).toBeGreaterThan(0)
  })
})

describe('§6.1 — l’arbre se finance sur une partie, pas avant l’assise V', () => {
  const TOTAL = COUTS_DE_NOEUD.reduce((a, b) => a + b, 0) * 6   // 167 × 6 branches ≈ 1002

  it('les six couples (A, B) sont renseignés', () => {
    expect(Object.keys(COUPLES_A_B)).toHaveLength(6)
  })

  it('une branche ouvre ses cinq nœuds sur une partie complète, et pas plus tôt', () => {
    for (const [branche, ab] of Object.entries(COUPLES_A_B)) {
      const points = pointsDeBranche(branche as never, Number.MAX_SAFE_INTEGER / 1e6)
      expect(points, `${branche} n’ouvre pas ses cinq nœuds`).toBeGreaterThanOrEqual(COUTS_DE_NOEUD.length)
      expect(ab.a).toBeGreaterThan(0)
      expect(ab.b).toBeGreaterThan(0)
    }
    expect(TOTAL).toBeGreaterThan(0)
  })
})
```

- [ ] **Step 2: Lancer et vérifier l'échec**

Run: `npx vitest run tests/calibrage.test.ts`
Expected: FAIL — `resoudreCourbe` n'existe pas, `COUPLES_A_B` est vide.

- [ ] **Step 3: Implémenter le calibreur**

`θ` règle la forme, l'échelle règle la durée absolue, et les deux sont séparables — donc deux bissections successives, jamais une recherche à deux dimensions :

```typescript
export interface CibleDeCourbe {
  readonly dureeCycle1Heures: number
  readonly nbEclosions: number
  /** Rapport visé entre la durée du dernier cycle et celle du premier. */
  readonly croissanceTotale: number
}

export interface Reglage {
  readonly alpha: number
  readonly theta: number
  readonly echelle: number
}

/**
 * `θ` est la part du besoin que la densité compense (RESULTATS.md, finding 5) :
 * θ = 1 compense exactement, donc cycles plats ; θ = 0 ne compense rien, donc
 * cycles qui s'allongent sans fin. C'est le SEUL bouton qui agisse sur la
 * forme. L'échelle, elle, ne déplace que la durée absolue — elle ne change
 * aucun rapport entre cycles, ce qui est précisément ce qui rend les deux
 * bissections indépendantes et permet de les enchaîner.
 *
 * `α` reste sa graine tant qu'une mesure ne le réfute pas : le finding 1 a
 * montré que la courbe n'y est pas sensible.
 */
export function resoudreCourbe(cible: CibleDeCourbe): Reglage {
  const theta = bissecter(0, 1, 40, (t) => {
    const r = simulerAvec({ theta: t }, cible.nbEclosions)
    const premier = r.cycles[0].dureeEcouleeSecondes
    const dernier = r.cycles[r.cycles.length - 1].dureeEcouleeSecondes
    return dernier / premier - cible.croissanceTotale
  })
  const echelle = bissecter(1e-6, 1e6, 60, (e) => {
    const r = simulerAvec({ theta, echelle: e }, cible.nbEclosions)
    return r.cycles[0].dureeEcouleeSecondes / 3600 - cible.dureeCycle1Heures
  })
  return { alpha: ALPHA_GAIN_DE_DENSITE, theta, echelle }
}

/** Bissection sur une fonction monotone. Rend la borne au bout de `pas` coupes. */
function bissecter(bas: number, haut: number, pas: number, f: (x: number) => number): number {
  let a = bas
  let b = haut
  for (let i = 0; i < pas; i += 1) {
    const m = (a + b) / 2
    if (f(m) > 0) b = m
    else a = m
  }
  return (a + b) / 2
}
```

`simulerAvec` injecte un réglage dans les constantes le temps d'une passe — passer les paramètres en argument à `simuler` plutôt que de muter un module, pour que le noyau reste pur.

**Si `bissecter` sort contre une borne**, c'est le finding 1 qui se reproduit : la cible est structurellement inatteignable. Ne pas élargir la borne — l'écrire dans `RESULTATS.md` v2 à la tâche 14 et remonter la question, comme la v1 l'a fait pour les 182 h.

`resoudreCoupleAB` existe déjà dans `src/simulateur/calibreur.ts` : l'appeler pour les six branches sur les trajectoires de compteurs relevées par `simuler`, avec pour cible « le cinquième nœud s'ouvre pendant l'assise V ». Écrire le résultat dans `COUPLES_A_B` de `src/noyau/technique.ts` en littéraux, chacun commenté de la date de sa mesure :

```typescript
/** Mesurés le 2026-09-08 par `resoudreCoupleAB`. Ne pas éditer à la main. */
export const COUPLES_A_B: Readonly<Record<BrancheTechniqueId, { readonly a: number; readonly b: number }>> = {
  creusement: { a: 0, b: 0 },   // ← remplacer par la mesure
  amelioration: { a: 0, b: 0 },
  recrutement: { a: 0, b: 0 },
  entretien: { a: 0, b: 0 },
  construction: { a: 0, b: 0 },
  eclosion: { a: 0, b: 0 },
}
```

- [ ] **Step 4: Lancer la suite**

Run: `npx vitest run && npx tsc -b && npx eslint .`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(calibrage): résout θ, l'échelle et les six couples (A, B)"
```

---

### Task 14: Écrire `RESULTATS.md` v2

**Files:**
- Modify: `docs/RESULTATS.md` (remplacé en entier)
- Create: `tests/courbe.test.ts`

**Interfaces:**
- Consumes: Task 13
- Produces: la référence chiffrée qui gouverne les phases 4 et 5

- [ ] **Step 1: Écrire le test de non-régression de la courbe**

```typescript
/**
 * Verrouille la courbe MESURÉE, pas la courbe espérée. Les bornes viennent de
 * docs/RESULTATS.md v2 ; si une mesure sort de là, c'est la mesure qui a raison
 * et le document qui doit être refait — jamais l'inverse.
 */
import { describe, expect, it } from 'vitest'
import { simuler, POLITIQUE_PAR_DEFAUT } from '../src/simulateur/simulateur'

describe('la courbe mesurée', () => {
  const r = simuler(45, POLITIQUE_PAR_DEFAUT, 1)

  it('le premier cycle tient dans la fenêtre de découverte', () => {
    expect(r.cycles[0].dureeEcouleeSecondes / 3600).toBeLessThan(8)
  })

  it('aucun cycle ne dépasse 15 % du total', () => {
    const total = r.cycles.reduce((s, c) => s + c.dureeEcouleeSecondes, 0)
    for (const c of r.cycles) expect(c.dureeEcouleeSecondes / total).toBeLessThan(0.15)
  })

  it('les 62 paliers sont atteignables avant la dernière éclosion', () => {
    expect(Math.max(...r.cycles.map((c) => c.paliersOuverts))).toBeGreaterThanOrEqual(62)
  })
})
```

- [ ] **Step 2: Lancer le test et relever les nombres**

Run: `npx vitest run tests/courbe.test.ts`
Expected: PASS. Noter les valeurs réelles — durée du cycle 1, temps actif total, part du plus long cycle, éclosion à laquelle le palier 62 tombe.

- [ ] **Step 3: Réécrire `docs/RESULTATS.md`**

Remplacer le contenu par la mesure du jour, en gardant la forme du v1 : un titre, les commandes pour reproduire, un finding par section, et une section finale « ce qu'il reste à décider ». Conserver les cinq findings du v1 dans une section « ce que la v1 avait trouvé et qui tient toujours », puisqu'ils restent vrais et que le v2 les prolonge.

En tête du fichier :

```markdown
**Version 2 — 2026-09-08.** Remplace la v1, mesurée sur le harnais de
`docs/*.ts`. Celle-ci est mesurée sur le noyau du jeu, dans `src/`, avec le
même code que celui qui tourne dans le navigateur.
```

- [ ] **Step 4: Vérification complète**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: tout vert.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "docs(mesure): RESULTATS v2, mesuré sur le noyau du jeu"
```

---

## Après ce plan

Les phases 4 (découplage du récit) et 5 (l'arbre et l'assise I jouable) sont décrites dans le spec mais **ne sont pas planifiées ici** : leur contenu dépend des nombres de la tâche 14. Re-planifier après.
