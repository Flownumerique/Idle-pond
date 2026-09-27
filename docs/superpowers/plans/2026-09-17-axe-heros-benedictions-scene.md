# L'axe héros, les bénédictions et la scène — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rendre IdlePond jouable comme un idle : le héros grandit avec du mana, la Foi achète des bénédictions permanentes, et une scène Phaser montre le héros, ses marques et ses bancs.

**Architecture:** Trois parties indépendantes, dans l'ordre A → B → C. A et B ne touchent que le noyau pur (`tick(etat, dt) → etat`), ses données, sa persistance, le simulateur et deux panneaux React. C ajoute `src/scene/` — une projection pure `vueDeLaScene(etat)` testée en node, et une scène Phaser qui ne lit jamais l'état, seulement la vue. Chaque partie se termine sur une suite verte et un jeu qui tourne.

**Tech Stack:** TypeScript 5.9 strict, Vitest 3, Vite 8, React 19, Phaser 3.90, `break_infinity.js` (`Decimal`), zustand 5, Tailwind 4.

**Spec:** `docs/superpowers/specs/2026-09-17-axe-heros-benedictions-scene-design.md` — le plan argumente depuis le spec ; lire les deux.

## Global Constraints

Recopiées du spec et du dépôt, verbatim. Chaque tâche les inclut implicitement.

- `g = 2.4`, coût de niveau `×1.15`, `f = 1` — reset complet. Seuils cumulés `10 / 25 / 50 / 100` → `×2 / ×4 / ×8 / ×16`, **jamais ×1024**.
- **`D` par palier ne bouge pas.** Après ce plan, `(multiplicateurDePalier() × (1 + BONUS_PAR_NIVEAU_DU_HEROS))³ × DEBIT_RATIO_ESPECE = D³`. `tests/canon.test.ts` le verrouille.
- **Le premier niveau du héros coûte plus que `MANA_A_LA_SORTIE_DE_L_OEUF` (36).** Sinon le joueur naïf grandit avant de convaincre le vairon.
- Le noyau est pur : aucun `Date.now()`, `Math.random`, DOM, ni import hors `noyau/` et `donnees/` dans `src/noyau/` et `src/donnees/`. `tests/architecture.test.ts` reste vert à chaque tâche. **`src/scene/vue.ts` et `src/scene/palette.ts` respectent la même règle** (pas de Phaser).
- **Tout se calcule en un pas pour `dt = 8 h`.** `tests/equivalence-de-pas.test.ts` reste vert. Le niveau du héros et les rangs de bénédiction ne changent qu'à l'ACHAT, jamais pendant un tick.
- Une migration de save **ne supprime jamais un champ**. `VERSION_SAVE` passe de 6 à 7 une seule fois, à la tâche A4 ; B4 complète la même migration.
- La technique baisse les **coûts** et automatise, jamais la production. Une bénédiction monte la **production** et rien d'autre : jamais un coût, jamais un plafond. `tests/canon.test.ts` verrouille les deux sens.
- Lexique : jamais « prestige », « rebirth », « gemme », « perle », « corail », « layer », « zone », « biome », « étage », « strate » dans le code de `src/` (chaînes et commentaires exclus). Jamais « palier », « assise », « couche », « zone » dans un texte AFFICHÉ.
- Code, commentaires, messages de commit en français. Tout paramètre inventé est une constante nommée, commentée `[P] graine`, dans `src/noyau/constantes.ts`.

**Commandes de vérification :**

```bash
npx vitest run                    # toute la suite (153 tests verts au départ, fc1b330)
npx vitest run tests/X.test.ts    # un fichier
npx tsc -b                        # types
npx eslint .                      # lint
npm run build                     # tsc -b && vite build
npm run dev                       # le jeu, http://localhost:5173
```

---

## File Structure

**Créés :**

| Fichier | Responsabilité |
|---|---|
| `src/donnees/benedictions.ts` | Le registre des 22 bénédictions : une globale, une ciblée par espèce. Contenu pur. |
| `src/scene/vue.ts` | `vueDeLaScene(etat): VueDeScene` — projection pure de l'état vers ce que la scène dessine. Pas de Phaser. |
| `src/scene/palette.ts` | Six palettes d'assise, six marques du corps. Données pures. |
| `src/scene/SceneDeLaMare.ts` | La scène Phaser : dessine une `VueDeScene`, ne lit jamais l'état. |
| `src/ui/Scene.tsx` | Monte Phaser dans React, pousse la vue à chaque changement d'état. |
| `src/ui/Heros.tsx` | La carte du héros : niveau, coût, bouton *Grandir*. |
| `src/ui/Benedictions.tsx` | L'écran des bénédictions : la globale, puis une ligne par espèce connue. |
| `tests/heros.test.ts` | L'axe héros : coût, effet, reset, invariants. |
| `tests/benedictions.test.ts` | Les bénédictions : coût en Foi, effet, persistance, frontière. |
| `tests/scene.test.ts` | `vue.ts` et `palette.ts`. |

**Modifiés :**

| Fichier | Ce qui change |
|---|---|
| `src/noyau/types.ts` | `niveauDuHeros` dans `EtatCycle`, `benedictions` dans `EtatPermanent`, `Benediction`, trois termes de production, deux termes de coût, deux sources. |
| `src/noyau/constantes.ts` | Sept graines, le rebudget de `multiplicateurDePalier()`, `VERSION_SAVE = 7`. |
| `src/noyau/economie.ts` | `multiplicateurDuHeros`, `coutDeCroissance`, `debitBeni`, `multiplicateurDeBenediction`, `coutDeBenediction`, détails de captation. |
| `src/noyau/noyau.ts` | Deux actes : `grandir`, `benir`. |
| `src/noyau/eclosion.ts` | `cycleInitial` pose le niveau 1 ; `eclore` écrit `couches`. |
| `src/adaptateurs/persistance.ts` | Migration `6 → 7`. |
| `src/simulateur/simulateur.ts` | Achat `grandir` ; politique de bénédiction après l'éclosion. |
| `src/etat/magasin.ts` | Actions `grandir`, `benir`. |
| `src/ui/App.tsx`, `Mare.tsx`, `Contenance.tsx`, `Captation.tsx`, `format.ts` | Branchement des panneaux, sources de termes. |
| `src/donnees/textes-provisoires.ts` | Noms d'écran des bénédictions et du héros. |
| `tests/canon.test.ts`, `tests/joueur.ts`, `tests/partie-headless.test.ts`, `tests/simulateur.test.ts`, `tests/persistance.test.ts`, `tests/eclosion.test.ts` | Gardes retournés ou étendus. |
| `docs/amendement-v1.1.md`, `docs/PRESEANCE.md`, `README.md` | L'amendement v1.4 et ses conséquences. |

**Interfaces cibles**, référencées par toutes les tâches :

```typescript
// src/noyau/types.ts — ajouts
export type BenedictionId = string
export type PorteeDeBenediction = 'ciblee' | 'globale'
export interface Benediction {
  readonly id: BenedictionId
  readonly portee: PorteeDeBenediction
  /** L'espèce visée. `null` pour la globale. */
  readonly espece: EspeceId | null
}
// TermeDeProduction += 'multiplicateur_heros' | 'multiplicateur_benediction' | 'benediction_globale'
// TermeDeCout       += 'cout_croissance' | 'cout_benediction'
// SourceDeTerme     : { quoi: 'heros'; niveau: number } remplace { quoi: 'heros' }
//                     + { quoi: 'benediction'; rang: number }
// EtatCycle         += readonly niveauDuHeros: number
// EtatPermanent     += readonly benedictions: Readonly<Record<BenedictionId, number>>

// src/noyau/economie.ts — ajouts
export function multiplicateurDuHeros(etat: EtatJeu): number
export function coutDeCroissance(etat: EtatJeu, niveau: number): Decimal      // coût pour passer de `niveau` à `niveau + 1`
export function detailDuHeros(etat: EtatJeu): readonly LigneDeCaptation[]     // signature changée : prend l'état
export function debitBeni(etat: EtatJeu, espece: Espece): Decimal
export function multiplicateurDeBenediction(etat: EtatJeu, espece: Espece): number
export function coutDeBenediction(etat: EtatJeu, benediction: Benediction): Decimal  // en Foi
export function rangDeBenediction(etat: EtatJeu, id: BenedictionId): number

// src/noyau/noyau.ts — ajouts
export function grandir(etat: EtatJeu): EtatJeu
export function benir(etat: EtatJeu, id: BenedictionId): EtatJeu

// src/donnees/benedictions.ts
export const BENEDICTION_GLOBALE_ID = 'benediction-globale'
export const BENEDICTIONS: readonly Benediction[]
export function benedictionParId(id: BenedictionId): Benediction | undefined
export function benedictionCibleeDe(espece: EspeceId): Benediction

// src/scene/vue.ts
export interface VueDEspece { readonly id: EspeceId; readonly rang: number; readonly niveau: number }
export interface VueDePalier { readonly index: number; readonly assise: AssiseId; readonly rangDAssise: number; readonly espece: VueDEspece | null }
export interface VueDuHeros { readonly niveau: number; readonly echelle: number; readonly couches: readonly AssiseId[] }
export interface VueDeScene { readonly paliers: readonly VueDePalier[]; readonly heros: VueDuHeros; readonly eauTroublee: boolean; readonly sature: boolean }
export function vueDeLaScene(etat: EtatJeu): VueDeScene
export function echelleDuHeros(niveau: number): number

// src/scene/palette.ts
export interface PaletteDAssise { readonly fond: number; readonly eau: number; readonly lumiere: number }
export type MarqueId = 'branchies' | 'membranes' | 'luminescence' | 'mineralisation' | 'epaississement' | 'halo'
export const PALETTES: Readonly<Record<AssiseId, PaletteDAssise>>
export const MARQUE_PAR_ASSISE: Readonly<Record<AssiseId, MarqueId>>
export function paletteDe(assise: AssiseId): PaletteDAssise
```

---

# Partie A — l'axe héros

### Task A1 : les types, les graines, et le rebudget de `D`

**Files:**
- Modify: `src/noyau/types.ts` (bloc « Termes de formule », `EtatCycle`, `SourceDeTerme`)
- Modify: `src/noyau/constantes.ts` (§13.2 `multiplicateurDePalier`, §13.3 nouvelles graines)
- Modify: `tests/canon.test.ts` (« le multiplicateur de palier porte la part de D… », `SOURCES_A_VERIFIER`)
- Create: `tests/heros.test.ts`

**Interfaces:**
- Produces: `NIVEAU_DU_HEROS_AU_DEPART`, `BONUS_PAR_NIVEAU_DU_HEROS`, `RATIO_COUT_DE_CROISSANCE` ; `EtatCycle.niveauDuHeros` ; termes `multiplicateur_heros`, `cout_croissance` ; source `{ quoi: 'heros'; niveau }`.

- [ ] **Step 1 : écrire les tests qui échouent**

Créer `tests/heros.test.ts` :

```typescript
/**
 * L'axe héros — spec 2026-09-17 §3.1.
 *
 * Le héros GRANDIT pendant la vie, avec du mana : c'est le quatrième achat du
 * noyau v1.0 §1.2 amendé. Son niveau se reperd à l'éclosion (f = 1), et ce
 * qu'il apporte est un multiplicateur global NOMMÉ, jamais un facteur flottant.
 */
import { describe, expect, it } from 'vitest'
import {
  BONUS_PAR_NIVEAU_DU_HEROS,
  DEBIT_RATIO_ESPECE,
  D_PRODUCTION_PAR_PALIER,
  ESPECE_TOUS_LES_N_PALIERS,
  MANA_A_LA_SORTIE_DE_L_OEUF,
  NIVEAU_DU_HEROS_AU_DEPART,
  RATIO_COUT_DE_CROISSANCE,
  COUT_CREUSER_AU_PALIER_1,
  multiplicateurDePalier,
} from '../src/noyau/constantes'
import { TERMES_DE_COUT, TERMES_DE_PRODUCTION } from '../src/noyau/types'
import { etatInitial } from '../src/noyau/noyau'

describe('A1 — les graines de l’axe héros', () => {
  it('le héros démarre au niveau 1, dans le cycle', () => {
    expect(NIVEAU_DU_HEROS_AU_DEPART).toBe(1)
    expect(etatInitial(1).cycle.niveauDuHeros).toBe(NIVEAU_DU_HEROS_AU_DEPART)
  })

  it('le bonus par niveau est une graine strictement positive et modeste', () => {
    expect(BONUS_PAR_NIVEAU_DU_HEROS).toBeGreaterThan(0)
    expect(BONUS_PAR_NIVEAU_DU_HEROS).toBeLessThan(0.5)
  })

  it('D par palier ne bouge pas : le héros en prend une part, la profondeur le reste', () => {
    // Contrainte globale du plan. Avant ce plan : m_p³ × ratio = D³. Après :
    // (m_p × (1 + b))³ × ratio = D³. Un niveau de héros par palier, et la
    // croissance par palier est inchangée.
    const parTroisPaliers =
      Math.pow(multiplicateurDePalier() * (1 + BONUS_PAR_NIVEAU_DU_HEROS), ESPECE_TOUS_LES_N_PALIERS) *
      DEBIT_RATIO_ESPECE
    expect(parTroisPaliers).toBeCloseTo(Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS), 6)
  })

  it('le premier niveau coûte plus que la charge de l’œuf', () => {
    // Sinon le joueur naïf (moins cher d'abord) grandit avant de convaincre le
    // vairon, et le plancher de cadence du §8.4 tombe dès la première minute.
    expect(COUT_CREUSER_AU_PALIER_1 * RATIO_COUT_DE_CROISSANCE).toBeGreaterThan(MANA_A_LA_SORTIE_DE_L_OEUF)
  })

  it('les termes du héros sont nommés au registre', () => {
    expect(TERMES_DE_PRODUCTION as string[]).toContain('multiplicateur_heros')
    expect(TERMES_DE_COUT as string[]).toContain('cout_croissance')
  })
})
```

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/heros.test.ts`
Expected: FAIL — `BONUS_PAR_NIVEAU_DU_HEROS` n'est pas exporté ; `niveauDuHeros` n'existe pas sur le type.

- [ ] **Step 3 : les types**

Dans `src/noyau/types.ts` :

Ajouter à `TermeDeProduction`, après `'debit_heros'` :

```typescript
  /**
   * Multiplicateur global du niveau du héros — spec 2026-09-17 [D2] :
   * `(1 + BONUS_PAR_NIVEAU_DU_HEROS) ^ (niveau − 1)`. C'est le quatrième achat
   * en mana, et sa part de `D` est retirée au multiplicateur de profondeur.
   */
  | 'multiplicateur_heros'
```

Ajouter `'multiplicateur_heros'` à `TERMES_DE_PRODUCTION`.

Ajouter à `TermeDeCout`, après `'cout_deblocage'` :

```typescript
  /** Faire grandir le héros d'un niveau — spec 2026-09-17 [D3]. Fraction du coût du palier de même rang. */
  | 'cout_croissance'
```

Ajouter `'cout_croissance'` à `TERMES_DE_COUT`.

Dans `EtatCycle`, après `acquisDeSejour` :

```typescript
  /**
   * Le niveau du héros dans CETTE vie — spec 2026-09-17 [D1]. Part à 1, monte
   * à l'achat, se reperd à l'éclosion : il ressort de l'œuf alevin. Ce qu'il
   * EST (contenance, couches) persiste ; ce qu'il a bâti de lui-même régresse.
   */
  readonly niveauDuHeros: number
```

Remplacer la source `{ readonly quoi: 'heros' }` par :

```typescript
  /** Source de `debit_heros` et de `multiplicateur_heros` : son niveau dans cette vie. */
  | { readonly quoi: 'heros'; readonly niveau: number }
```

- [ ] **Step 4 : les constantes**

Dans `src/noyau/constantes.ts`, remplacer `multiplicateurDePalier()` par :

```typescript
export function multiplicateurDePalier(): number {
  const parPalier = Math.pow(
    Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS) / DEBIT_RATIO_ESPECE,
    1 / ESPECE_TOUS_LES_N_PALIERS,
  )
  // Spec 2026-09-17 [D3] : le héros grandit d'un niveau par palier sous le
  // joueur optimal, et chaque niveau multiplie tout par (1 + b). Cette part
  // sort du multiplicateur de profondeur pour que `D` par palier ne bouge pas.
  return parPalier / (1 + BONUS_PAR_NIVEAU_DU_HEROS)
}
```

Et ajouter, dans §13.3, juste avant `/* ─── Graines d'échelle économique`  :

```typescript
/* ─── L'axe héros — spec 2026-09-17 §3.1 ────────────────────────────────────
 * Le quatrième achat en mana. Le héros grandit pendant la vie ; son niveau se
 * reperd à l'éclosion, comme les galeries. Ce qu'il apporte est un
 * multiplicateur global nommé (`multiplicateur_heros`), et sa part de `D` est
 * retirée au multiplicateur de profondeur — voir `multiplicateurDePalier`.
 */

/** Le héros sort de l'œuf au niveau 1. Pas une graine : le niveau 0 n'existe pas. */
export const NIVEAU_DU_HEROS_AU_DEPART = 1

/**
 * [P] graine — ce que chaque niveau du héros ajoute à TOUTE la production.
 * `(1 + b) ^ (niveau − 1)`. À 0,15, un niveau par palier vaut un sixième de la
 * croissance par palier ; le reste vient de la profondeur. À mesurer.
 */
export const BONUS_PAR_NIVEAU_DU_HEROS = 0.15

/**
 * [P] graine — coût de croissance, en fraction du coût du palier de même rang :
 * `coût(niveau n → n+1) = COUT_CREUSER_AU_PALIER_1 × ratio × g^(n − 1)`.
 *
 * Borné par le bas : le premier niveau (60 × 0,75 = 45) DOIT coûter plus que la
 * charge de l'œuf (36), sinon le joueur naïf grandit avant de convaincre le
 * vairon et le plancher de cadence tombe. `tests/heros.test.ts` le vérifie.
 * Si le plancher tombe quand même, monter par pas de 0,25 et consigner.
 */
export const RATIO_COUT_DE_CROISSANCE = 0.75
```

`multiplicateurDePalier` lit `BONUS_PAR_NIVEAU_DU_HEROS`, déclaré plus bas dans le fichier : c'est une fonction, elle n'est appelée qu'après l'initialisation du module, donc l'ordre de déclaration ne pose pas de problème. Les tables d'`echelles.ts` appellent `multiplicateurDePalier()` à leur propre chargement, qui suit celui de `constantes.ts` : idem.

- [ ] **Step 5 : réparer les compilations cassées par `niveauDuHeros`**

`EtatCycle` a un champ de plus : `cycleInitial()` dans `src/noyau/eclosion.ts` doit le poser. Ajouter l'import `NIVEAU_DU_HEROS_AU_DEPART` et, dans l'objet rendu par `cycleInitial`, après `acquisDeSejour: 0,` :

```typescript
    niveauDuHeros: NIVEAU_DU_HEROS_AU_DEPART,
```

`sourceDuTerme` dans `src/ui/format.ts`, cas `'heros'` :

```typescript
    case 'heros':
      return source.niveau <= 1 ? 'toi, qui captes seul' : `toi, grandi ${source.niveau - 1} fois`
```

`detailDuHeros()` dans `src/noyau/economie.ts` construit `{ quoi: 'heros' }` : le compilateur le refuse maintenant. Correction minimale ici, la vraie réécriture est en A2 :

```typescript
export function detailDuHeros(etat: EtatJeu): readonly LigneDeCaptation[] {
  return [{ terme: 'debit_heros', valeur: DEBIT_HEROS, source: { quoi: 'heros', niveau: etat.cycle.niveauDuHeros } }]
}
```

Et son appelant `src/ui/Contenance.tsx` : `detailDuHeros()[0].source` → `detailDuHeros(etat)[0].source`.

- [ ] **Step 6 : mettre à jour le test de canon**

Dans `tests/canon.test.ts`, `SOURCES_A_VERIFIER` : remplacer `{ quoi: 'heros' }` par `{ quoi: 'heros', niveau: 1 }, { quoi: 'heros', niveau: 7 }`.

Remplacer le test « le multiplicateur de palier porte la part de D que le bestiaire ne porte pas » :

```typescript
  it('le multiplicateur de palier et le héros portent ensemble la part de D que le bestiaire ne porte pas', () => {
    // Spec 2026-09-17 [D3] : un niveau de héros par palier, et sa part sort de
    // `m_p`. Ce qui doit tenir est le produit des deux, pas `m_p` seul.
    const parTroisPaliers =
      Math.pow(multiplicateurDePalier() * (1 + BONUS_PAR_NIVEAU_DU_HEROS), ESPECE_TOUS_LES_N_PALIERS) *
      DEBIT_RATIO_ESPECE
    expect(parTroisPaliers).toBeCloseTo(Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS), 6)
  })
```

Ajouter `BONUS_PAR_NIVEAU_DU_HEROS` à l'import depuis `../src/noyau/constantes`.

- [ ] **Step 7 : vérifier**

Run: `npx vitest run tests/heros.test.ts tests/canon.test.ts && npx tsc -b`
Expected: PASS, et `tsc` propre.

Run: `npx vitest run`
Expected: **quelques tests rouges** dans `tests/simulateur.test.ts` (durées de cycle) et `tests/contenance.test.ts` sont possibles : `multiplicateurDePalier()` a baissé de 15 % et le héros ne compense pas encore (A3). C'est attendu ; A5 remet la mesure d'aplomb. Noter les noms des tests rouges dans le message de commit.

- [ ] **Step 8 : commit**

```bash
git add src/noyau/types.ts src/noyau/constantes.ts src/noyau/eclosion.ts src/noyau/economie.ts src/ui/format.ts src/ui/Contenance.tsx tests/heros.test.ts tests/canon.test.ts
git commit -m "feat(heros): le niveau du héros entre dans le cycle, et D est rebudgété

Spec 2026-09-17 [D1]-[D3]. Trois graines, un champ de cycle, deux termes nommés.
Le multiplicateur de profondeur cède (1 + b) au héros ; D par palier est inchangé
et le test de canon le vérifie sur le produit des deux.

Tests rouges attendus jusqu'à A5 : <lister ici>."
```

---

### Task A2 : l'économie du héros

**Files:**
- Modify: `src/noyau/economie.ts`
- Modify: `tests/heros.test.ts`

**Interfaces:**
- Consumes: A1.
- Produces: `multiplicateurDuHeros(etat)`, `coutDeCroissance(etat, niveau)`, `productionDuHeros(etat)` (lit le niveau), `detailDuHeros(etat)` (deux lignes), `multiplicateursGlobaux` inclut le héros.

- [ ] **Step 1 : les tests**

Ajouter à `tests/heros.test.ts` :

```typescript
import Decimal from 'break_infinity.js'
import {
  coutDeCroissance,
  detailDuHeros,
  multiplicateurDuHeros,
  multiplicateursGlobaux,
  productionDuHeros,
  productionTotaleParSeconde,
} from '../src/noyau/economie'
import { G_COUT_PALIER, DEBIT_HEROS } from '../src/noyau/constantes'
import { etatDeTravail } from './etat-de-travail'
import type { EtatJeu } from '../src/noyau/types'

function auNiveau(etat: EtatJeu, niveauDuHeros: number): EtatJeu {
  return { ...etat, cycle: { ...etat.cycle, niveauDuHeros } }
}

describe('A2 — ce que le niveau du héros vaut', () => {
  it('au niveau 1 le multiplicateur vaut exactement 1', () => {
    expect(multiplicateurDuHeros(auNiveau(etatInitial(1), 1))).toBe(1)
  })

  it('chaque niveau multiplie par (1 + b), et c’est un terme global', () => {
    const base = etatDeTravail()
    const un = auNiveau(base, 1)
    const cinq = auNiveau(base, 5)
    expect(multiplicateurDuHeros(cinq)).toBeCloseTo(Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, 4), 12)
    const rapport = multiplicateursGlobaux(cinq).div(multiplicateursGlobaux(un)).toNumber()
    expect(rapport).toBeCloseTo(Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, 4), 9)
    // La production TOTALE suit le même rapport, espèces comprises.
    const total = productionTotaleParSeconde(cinq).div(productionTotaleParSeconde(un)).toNumber()
    expect(total).toBeGreaterThan(rapport) // > : son débit propre monte aussi avec le niveau
  })

  it('son débit propre monte avec le niveau, multiplicateurs compris', () => {
    const un = auNiveau(etatInitial(1), 1)
    const trois = auNiveau(etatInitial(1), 3)
    expect(productionDuHeros(un).eq(new Decimal(DEBIT_HEROS).mul(multiplicateursGlobaux(un)))).toBe(true)
    expect(productionDuHeros(trois).eq(new Decimal(DEBIT_HEROS).mul(3).mul(multiplicateursGlobaux(trois)))).toBe(true)
  })

  it('le coût de croissance suit g, en fraction du coût du palier de même rang', () => {
    const etat = etatInitial(1)
    const premier = coutDeCroissance(etat, 1)
    expect(premier.toNumber()).toBeCloseTo(COUT_CREUSER_AU_PALIER_1 * RATIO_COUT_DE_CROISSANCE, 9)
    expect(coutDeCroissance(etat, 4).div(coutDeCroissance(etat, 3)).toNumber()).toBeCloseTo(G_COUT_PALIER, 9)
  })

  it('le détail du héros nomme ses deux termes, et sa source porte le niveau', () => {
    const lignes = detailDuHeros(auNiveau(etatInitial(1), 4))
    expect(lignes.map((l) => l.terme)).toEqual(['debit_heros', 'multiplicateur_heros'])
    expect(lignes[0].source).toEqual({ quoi: 'heros', niveau: 4 })
    expect(lignes[1].valeur).toBeCloseTo(Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, 3), 12)
  })
})
```

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/heros.test.ts`
Expected: FAIL — `multiplicateurDuHeros` et `coutDeCroissance` ne sont pas exportés.

- [ ] **Step 3 : l'implémentation**

Dans `src/noyau/economie.ts` :

Ajouter aux imports depuis `./constantes` : `BONUS_PAR_NIVEAU_DU_HEROS`, `RATIO_COUT_DE_CROISSANCE`.

Après `multiplicateurDeProfondeur`, ajouter :

```typescript
/**
 * Multiplicateur global du niveau du héros — spec 2026-09-17 [D2].
 *
 * `(1 + b) ^ (niveau − 1)` : au niveau 1 il vaut exactement 1, et un cycle
 * dont le héros n'a jamais grandi produit ce qu'il produisait avant ce terme.
 * Sa part de `D` est retirée au multiplicateur de profondeur, pas ajoutée
 * par-dessus : voir `multiplicateurDePalier` dans `constantes.ts`.
 */
export function multiplicateurDuHeros(etat: EtatJeu): number {
  return Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, Math.max(0, etat.cycle.niveauDuHeros - 1))
}
```

Dans `multiplicateursGlobaux`, ajouter `.mul(multiplicateurDuHeros(etat))` avant `.mul(multiplicateurDesDrapeaux(etat))`.

Remplacer `productionDuHeros` :

```typescript
export function productionDuHeros(etat: EtatJeu): Decimal {
  return new Decimal(DEBIT_HEROS).mul(etat.cycle.niveauDuHeros).mul(multiplicateursGlobaux(etat))
}
```

Remplacer `detailDuHeros` :

```typescript
export function detailDuHeros(etat: EtatJeu): readonly LigneDeCaptation[] {
  const source = { quoi: 'heros', niveau: etat.cycle.niveauDuHeros } as const
  return [
    { terme: 'debit_heros', valeur: DEBIT_HEROS, source },
    { terme: 'multiplicateur_heros', valeur: multiplicateurDuHeros(etat), source },
  ]
}
```

Dans `detailDeCaptation`, ajouter une ligne après `multiplicateur_densite` :

```typescript
    {
      terme: 'multiplicateur_heros',
      valeur: multiplicateurDuHeros(etat),
      source: { quoi: 'heros', niveau: etat.cycle.niveauDuHeros },
    },
```

Dans la section Coûts, après `coutDeNiveau` :

```typescript
/**
 * Ce que coûte de faire grandir le héros de `niveau` à `niveau + 1` — spec
 * 2026-09-17 [D3] : une fraction du coût du palier de même rang, donc `g^(n−1)`.
 * Le joueur optimal en paie à peu près un par palier, et c'est ce qui autorise
 * le rebudget de `D` dans `multiplicateurDePalier`.
 */
export function coutDeCroissance(etat: EtatJeu, niveau: number): Decimal {
  return puissanceDeG(Math.max(0, niveau - 1))
    .mul(COUT_CREUSER_AU_PALIER_1)
    .mul(RATIO_COUT_DE_CROISSANCE)
    .mul(facteurDeCout(etat, 'cout_croissance'))
}
```

- [ ] **Step 4 : vérifier**

Run: `npx vitest run tests/heros.test.ts tests/canon.test.ts tests/architecture.test.ts && npx tsc -b`
Expected: PASS.

- [ ] **Step 5 : commit**

```bash
git add src/noyau/economie.ts tests/heros.test.ts
git commit -m "feat(heros): multiplicateur du héros, coût de croissance, détail nommé

multiplicateursGlobaux porte le héros ; son débit propre suit son niveau ; le
détail de captation attribue les deux termes à sa source."
```

---

### Task A3 : l'acte `grandir`, et l'éclosion qui le reperd

**Files:**
- Modify: `src/noyau/noyau.ts`
- Modify: `tests/heros.test.ts`
- Modify: `tests/eclosion.test.ts`

**Interfaces:**
- Produces: `grandir(etat): EtatJeu`.

- [ ] **Step 1 : les tests**

Ajouter à `tests/heros.test.ts` :

```typescript
import { grandir, eclore, tick } from '../src/noyau/noyau'
import { comparerAToleranceFlottante } from './outils'

describe('A3 — grandir', () => {
  it('paie le coût, monte d’un niveau, crédite le compteur Amélioration', () => {
    const avant = { ...etatInitial(1), cycle: { ...etatInitial(1).cycle, manaCourant: new Decimal(1000) } }
    const cout = coutDeCroissance(avant, avant.cycle.niveauDuHeros)
    const apres = grandir(avant)
    expect(apres.cycle.niveauDuHeros).toBe(2)
    expect(apres.cycle.manaCourant.eq(avant.cycle.manaCourant.sub(cout))).toBe(true)
    expect(apres.permanent.compteursTechnique.amelioration).toBeCloseTo(
      avant.permanent.compteursTechnique.amelioration + cout.toNumber(),
      9,
    )
  })

  it('refuse sans rien changer si le mana manque', () => {
    const pauvre = { ...etatInitial(1), cycle: { ...etatInitial(1).cycle, manaCourant: new Decimal(1) } }
    expect(grandir(pauvre)).toBe(pauvre)
  })

  it('le niveau se reperd à l’éclosion : il ressort alevin', () => {
    const grandi = auNiveau(etatDeTravail(), 9)
    expect(eclore(grandi).cycle.niveauDuHeros).toBe(NIVEAU_DU_HEROS_AU_DEPART)
  })

  it('le niveau ne bouge jamais pendant un tick — le pas reste homogène', () => {
    const depart = auNiveau(etatDeTravail(), 6)
    let petits = depart
    for (let i = 0; i < 480; i += 1) petits = tick(petits, 60)
    const grand = tick(depart, 8 * 3600)
    expect(grand.cycle.niveauDuHeros).toBe(6)
    comparerAToleranceFlottante(petits, grand)
  })
})
```

Dans `tests/eclosion.test.ts`, test « mana, paliers, espèces, pointe, durée et acquis repartent de l'œuf » : ajouter au bloc `avecCycle(...)` de départ `niveauDuHeros: 4,` et, après `expect(apres.acquisDeSejour).toBe(0)` :

```typescript
    expect(apres.niveauDuHeros).toBe(1)
```

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/heros.test.ts`
Expected: FAIL — `grandir` n'est pas exporté.

- [ ] **Step 3 : l'implémentation**

Dans `src/noyau/noyau.ts`, ajouter `coutDeCroissance` à l'import depuis `./economie`, et après `ameliorer` :

```typescript
/**
 * Faire grandir le héros d'un niveau — le quatrième achat, spec 2026-09-17.
 *
 * Même forme que les trois autres : payable ou rien ne change. Le compteur
 * crédité est celui d'Amélioration : c'est du mana dépensé en niveaux, et
 * l'arbre n'a pas de branche « héros ». Le niveau agit à l'instant où il est
 * payé, jamais pendant un pas.
 */
export function grandir(etat: EtatJeu): EtatJeu {
  const cout = coutDeCroissance(etat, etat.cycle.niveauDuHeros)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      niveauDuHeros: etat.cycle.niveauDuHeros + 1,
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'amelioration', cout.toNumber()),
    },
  }
}
```

L'éclosion reperd déjà le niveau : `eclore` remplace `cycle` par `cycleInitial()`, qui pose `NIVEAU_DU_HEROS_AU_DEPART` depuis A1. Rien à écrire.

- [ ] **Step 4 : vérifier**

Run: `npx vitest run tests/heros.test.ts tests/eclosion.test.ts tests/equivalence-de-pas.test.ts tests/determinisme.test.ts`
Expected: PASS.

- [ ] **Step 5 : commit**

```bash
git add src/noyau/noyau.ts tests/heros.test.ts tests/eclosion.test.ts
git commit -m "feat(heros): l'acte grandir — le quatrième achat, reperdu à l'éclosion"
```

---

### Task A4 : la save — version 7

**Files:**
- Modify: `src/noyau/constantes.ts` (`VERSION_SAVE`)
- Modify: `src/adaptateurs/persistance.ts` (`MIGRATIONS[6]`)
- Modify: `tests/persistance.test.ts`

- [ ] **Step 1 : les tests**

Dans `tests/persistance.test.ts`, remplacer le test « la save porte la version courante après migration — 4 → 5, puis 5 → 6 » :

```typescript
  it('la save porte la version courante après migration — 4 → 5, 5 → 6, puis 6 → 7', () => {
    const migre = deserialiser({ versionSave: 4, contenu: {} } as unknown as SaveSerialisee, etatInitial(0))
    expect(migre.versionSave).toBe(7)
  })
```

Et ajouter un `describe` :

```typescript
describe('migration 6 → 7 : le héros a un niveau, la Foi a un débouché', () => {
  it('une save v6 se réveille au niveau 1, sans bénédiction, et ne perd rien', () => {
    const v6 = {
      versionSave: 6,
      contenu: {
        cycle: { manaCourant: '500', paliersOuverts: 4, especes: { vairon: { debloquee: true, niveau: 12 } } },
        permanent: { nombreEclosions: 2, foi: '40' },
      },
    } as unknown as SaveSerialisee
    const relu = deserialiser(v6, etatInitial(1))
    expect(relu.cycle.niveauDuHeros).toBe(1)
    expect(relu.cycle.especes.vairon.niveau).toBe(12)
    expect(relu.permanent.foi.eq(40)).toBe(true)
    expect(relu.permanent.benedictions).toEqual({})
  })

  it('la migration écrit les deux champs neufs et ne supprime rien', () => {
    const migre = MIGRATIONS[6]({
      cycle: { manaCourant: '1' },
      permanent: { foi: '2', unChampInconnu: true },
    }) as Record<string, Record<string, unknown>>
    expect(migre.cycle).toHaveProperty('niveauDuHeros', 1)
    expect(migre.permanent).toHaveProperty('benedictions')
    expect(migre.permanent).toHaveProperty('unChampInconnu', true)
  })
})
```

`benedictions` n'existe pas encore sur `EtatPermanent` : ce test compile parce que `deserialiser` rend un `EtatJeu`, et TypeScript refusera `relu.permanent.benedictions`. **Ajouter dès maintenant le champ** dans `src/noyau/types.ts`, `EtatPermanent`, après `heuresHorsLigneCreditees` :

```typescript
  /**
   * Rang acheté de chaque bénédiction — noyau v1.0 §4, spec 2026-09-17 [D5].
   * Permanent : c'est l'écran d'améliorations du jeu. Vide tant que B1 n'a pas
   * rempli le registre.
   */
  readonly benedictions: Readonly<Record<BenedictionId, number>>
```

avec, en tête du bloc Identifiants : `export type BenedictionId = string`. Et dans `etatInitial` (`src/noyau/noyau.ts`), après `heuresHorsLigneCreditees: 0,` : `benedictions: {},`.

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/persistance.test.ts`
Expected: FAIL — `MIGRATIONS[6]` est `undefined` ; la version vaut 6.

- [ ] **Step 3 : l'implémentation**

`src/noyau/constantes.ts` : `export const VERSION_SAVE = 7`.

`src/adaptateurs/persistance.ts`, dans `MIGRATIONS`, après l'entrée `5` :

```typescript
  /**
   * 6 → 7 — l'axe héros et les bénédictions (spec 2026-09-17).
   *
   * Deux champs NEUFS, aucun retiré : `cycle.niveauDuHeros` part à 1 — une
   * save en cours de vie reprend avec un héros qui n'a pas encore grandi, ce
   * qui est vrai — et `permanent.benedictions` part vide. Le spread de
   * `deserialiser` les comblerait depuis le repli, mais une sémantique nouvelle
   * exige son incrément de version (noyau v1.0 §8.3), et l'écrire ici rend
   * l'intention lisible dans la chaîne.
   */
  6: (contenu) => {
    const brut = (contenu ?? {}) as Record<string, unknown>
    const cycle = (brut.cycle ?? {}) as Record<string, unknown>
    const permanent = (brut.permanent ?? {}) as Record<string, unknown>
    return {
      ...brut,
      cycle: { ...cycle, niveauDuHeros: 1 },
      permanent: { ...permanent, benedictions: {} },
    }
  },
```

- [ ] **Step 4 : vérifier**

Run: `npx vitest run tests/persistance.test.ts tests/determinisme.test.ts && npx tsc -b`
Expected: PASS.

- [ ] **Step 5 : commit**

```bash
git add src/noyau/constantes.ts src/noyau/types.ts src/noyau/noyau.ts src/adaptateurs/persistance.ts tests/persistance.test.ts
git commit -m "feat(persistance): migration 6 → 7 — niveau du héros et bénédictions"
```

---

### Task A5 : le simulateur et les joueurs de test achètent le héros

**Files:**
- Modify: `src/simulateur/simulateur.ts`
- Modify: `tests/joueur.ts`
- Modify: `tests/partie-headless.test.ts`
- Modify: `tests/simulateur.test.ts`
- Modify: `src/noyau/constantes.ts` (table de `NOMBRE_D_ECLOSIONS_VISE`, si les chiffres bougent)

**Interfaces:**
- Produces: `Achat` accepte `{ type: 'grandir'; cout; gain }`.

- [ ] **Step 1 : les tests**

Dans `tests/simulateur.test.ts`, ajouter dans le `describe('le simulateur tourne sur le noyau v1.0')` :

```typescript
  it('le joueur optimal fait grandir le héros, à peu près une fois par palier', () => {
    // Spec [D3] : le coût suit g comme le palier, donc le rapport coût/gain des
    // deux achats reste comparable tout le long. On ne demande pas l'égalité —
    // le gain d'un palier vaut (m_p − 1), celui d'un niveau vaut b — mais un
    // héros laissé au niveau 1 signifierait que l'achat n'est jamais rentable,
    // et le rebudget de D serait faux.
    let niveauMax = 0
    const resultat = simuler(3, undefined, 1, (etat) => {
      niveauMax = Math.max(niveauMax, etat.cycle.niveauDuHeros)
    })
    const paliersDuCycle1 = resultat.cycles[0].paliersOuverts
    expect(niveauMax).toBeGreaterThanOrEqual(Math.floor(paliersDuCycle1 / 2))
    expect(niveauMax).toBeLessThanOrEqual(paliersDuCycle1 + 2)
  })
```

Et dans le test existant « le gain de chaque achat est la production qu'il ajoute réellement », étendre `appliquer`/la boucle pour couvrir `'grandir'` : là où le test applique un achat selon son type, ajouter la branche `if (achat.type === 'grandir') suivant = grandir(courant)` (importer `grandir` depuis `../src/noyau/noyau`). Lire le test avant de le modifier : il compare `achat.gain` à `productionTotaleParSeconde(après) − productionTotaleParSeconde(avant)` pour chaque achat de `achatsDisponibles(etat)`.

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/simulateur.test.ts -t "grandir"`
Expected: FAIL — `niveauMax` vaut 1.

- [ ] **Step 3 : le simulateur**

Dans `src/simulateur/simulateur.ts` :

Importer `grandir` depuis `../noyau/noyau`, `coutDeCroissance` depuis `../noyau/economie`, et `BONUS_PAR_NIVEAU_DU_HEROS`, `DEBIT_HEROS` depuis `../noyau/constantes`.

Étendre `Achat` :

```typescript
export type Achat =
  | { readonly type: 'creuser'; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'grandir'; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'debloquer'; readonly espece: Espece; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'niveau'; readonly espece: Espece; readonly cout: Decimal; readonly gain: Decimal }
```

Dans `achatsDisponibles`, après le bloc `creuser` et avant la boucle sur les espèces :

```typescript
  // Grandir — spec 2026-09-17 [D2]. Après l'achat, TOUTE la production est
  // multipliée par (1 + b), et le débit propre du héros passe de n à n + 1 :
  //   P' = (S + D·(n+1)) · M · (1 + b)  avec  P = (S + D·n) · M
  //   P' − P = P·b + D·M·(1 + b)
  // où M est `multiplicateursGlobaux` de l'état courant (héros compris).
  {
    const cout = coutDeCroissance(etat, etat.cycle.niveauDuHeros)
    if (!horsDePortee(cout)) {
      const propre = new Decimal(DEBIT_HEROS).mul(multiplicateurs()).mul(1 + BONUS_PAR_NIVEAU_DU_HEROS)
      achats.push({
        type: 'grandir',
        cout,
        gain: productionTotale().mul(BONUS_PAR_NIVEAU_DU_HEROS).add(propre),
      })
    }
  }
```

Dans `appliquer` :

```typescript
function appliquer(etat: EtatJeu, achat: Achat): EtatJeu {
  if (achat.type === 'creuser') return creuser(etat)
  if (achat.type === 'grandir') return grandir(etat)
  if (achat.type === 'debloquer') return debloquer(etat, achat.espece.id)
  return ameliorer(etat, achat.espece.id)
}
```

Mettre à jour le commentaire de `Achat` : « Les quatre achats ».

- [ ] **Step 4 : les joueurs de test**

`tests/joueur.ts`, dans `depenser`, après la ligne `retenir(coutDeDescente(...), creuser)` :

```typescript
    retenir(coutDeCroissance(courant, courant.cycle.niveauDuHeros), grandir)
```

avec `grandir` importé depuis `../src/noyau/noyau` et `coutDeCroissance` depuis `../src/noyau/economie`.

`tests/partie-headless.test.ts`, dans `optionsPayables`, après le bloc `creuser` :

```typescript
  {
    const cout = coutDeCroissance(etat, etat.cycle.niveauDuHeros)
    if (etat.cycle.manaCourant.gte(cout)) options.push({ cout, appliquer: grandir })
  }
```

mêmes imports.

- [ ] **Step 5 : vérifier, et mesurer**

Run: `npx vitest run`
Expected: PASS sur toute la suite. Deux tests sont les juges de la courbe et peuvent être rouges :

- `tests/plancher-de-cadence.test.ts` — si le premier succès dépasse 120 s ou un trou dépasse 300 s dans la première demi-heure : monter `RATIO_COUT_DE_CROISSANCE` de 0,25 (0,75 → 1,0 → 1,25), relancer, garder la première valeur verte et l'écrire dans le commentaire de la constante.
- `tests/simulateur.test.ts` « le premier cycle dure environ trois heures » et « la saturation ne gèle pas la partie » — si rouge, lire la durée mesurée dans le message d'échec. Un écart de plus de 20 % sur le cycle 1 rouvre `BONUS_PAR_NIVEAU_DU_HEROS` (0,15 → 0,10) ; consigner.

Puis relancer la mesure de référence et **recopier la table** dans le commentaire de `NOMBRE_D_ECLOSIONS_VISE` (`constantes.ts`) si les chiffres ont bougé de plus de 5 % :

```bash
npx tsx -e "
import { simuler } from './src/simulateur/simulateur.ts'
const r = simuler(14, undefined, 1)
for (const c of r.cycles) console.log(c.index + 1, c.paliersOuverts, (c.dureeEcouleeSecondes / 3600).toFixed(2) + ' h')
console.log('actif', (r.secondesActives / 3600).toFixed(1), 'h')
"
```

- [ ] **Step 6 : commit**

```bash
git add src/simulateur/simulateur.ts tests/joueur.ts tests/partie-headless.test.ts tests/simulateur.test.ts src/noyau/constantes.ts
git commit -m "feat(simulateur): le joueur optimal fait grandir le héros

Achat 'grandir' avec son gain marginal exact ; les deux joueurs de test le
connaissent. Mesuré sur 14 cycles : <cycle 1 en h, total actif en h>."
```

---

### Task A6 : la carte du héros à l'écran

**Files:**
- Create: `src/ui/Heros.tsx`
- Modify: `src/etat/magasin.ts`
- Modify: `src/ui/App.tsx`
- Modify: `src/ui/Contenance.tsx`

- [ ] **Step 1 : le magasin**

Dans `src/etat/magasin.ts` : importer `grandir` depuis `../noyau/noyau` ; ajouter `grandir(): void` à l'interface `Magasin` après `ameliorer` ; et dans `create`, après `ameliorer: ...` :

```typescript
      grandir: () => set({ etat: grandir(get().etat) }),
```

- [ ] **Step 2 : la carte**

Créer `src/ui/Heros.tsx` :

```tsx
/**
 * Toi — le héros. Le quatrième achat (spec 2026-09-17).
 *
 * Même gabarit que la carte d'une espèce dans `Mare.tsx` : un nom, ce que ça
 * donne par seconde, un bouton avec son coût. Le héros n'a pas de nom propre
 * (GDD : « aucun nom propre définitif ») ; l'écran dit « toi ».
 */
import type { EtatJeu } from '../noyau/types'
import { contenance, coutDeCroissance, multiplicateurDuHeros, productionDuHeros } from '../noyau/economie'
import { cout, montant } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly surCroissance: () => void
}

export function Heros({ etat, surCroissance }: Props) {
  const niveau = etat.cycle.niveauDuHeros
  const prix = coutDeCroissance(etat, niveau)
  const payable = etat.cycle.manaCourant.gte(prix) && prix.lte(contenance(etat))
  const bonus = Math.round((multiplicateurDuHeros(etat) - 1) * 100)

  return (
    <section className="rounded-lg border border-foi/40 bg-eau-fond/40 p-3">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-texte text-base">toi</span>
        <span className="font-chiffre text-xs text-jour-tu tabular-nums">
          {niveau === 1 ? 'alevin' : `grandi ${niveau - 1} fois`}
        </span>
      </div>
      <div className="mt-1 flex items-baseline gap-4 text-sm text-jour-doux">
        <span className="font-chiffre text-mana tabular-nums">+{montant(productionDuHeros(etat))} / s</span>
        {bonus > 0 ? (
          <span className="font-chiffre text-jour-tu tabular-nums">et tout ce que tu convaincs donne +{bonus} %</span>
        ) : null}
      </div>
      <button
        type="button"
        disabled={!payable}
        onClick={surCroissance}
        className="mt-2 w-full rounded-md border border-foi/60 px-3 py-1.5 text-sm transition-colors enabled:hover:bg-foi/10 disabled:cursor-not-allowed disabled:opacity-40"
      >
        Grandir
        <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(prix)}</span>
      </button>
    </section>
  )
}
```

- [ ] **Step 3 : brancher**

Dans `src/ui/App.tsx` : importer `Heros` ; dans `<main>`, entre `<Contenance etat={etat} />` et le bloc `Captation`, ajouter :

```tsx
          <Heros etat={etat} surCroissance={() => useMagasin.getState().grandir()} />
```

Dans `src/ui/Contenance.tsx`, la ligne « dont … / s » lit `detailDuHeros(etat)[0].source` (déjà corrigé en A1). Rien d'autre.

- [ ] **Step 4 : vérifier dans le jeu**

Run: `npx tsc -b && npx eslint . && npm run build`
Expected: propres.

Run: `npm run dev`, ouvrir `http://localhost:5173`, profil vierge (vider `localStorage` clé `idlepond` si besoin). Attendu : la carte « toi » sous la jauge, bouton *Grandir* à 45, grisé tant que le mana est à 36 ; convaincre le vairon, attendre, grandir ; le « +… / s » total monte et la carte dit « grandi 1 fois ». Prendre une capture pour le rapport de tâche.

- [ ] **Step 5 : commit**

```bash
git add src/ui/Heros.tsx src/etat/magasin.ts src/ui/App.tsx
git commit -m "feat(ui): la carte du héros — grandir, et voir ce que ça donne"
```

---

# Partie B — les bénédictions

### Task B1 : le registre, les types, et le test de canon retourné

**Files:**
- Create: `src/donnees/benedictions.ts`
- Modify: `src/noyau/types.ts`
- Modify: `src/noyau/constantes.ts`
- Modify: `tests/canon.test.ts`
- Create: `tests/benedictions.test.ts`

**Interfaces:**
- Produces: `Benediction`, `BENEDICTIONS`, `benedictionParId`, `benedictionCibleeDe`, `BENEDICTION_GLOBALE_ID` ; termes `multiplicateur_benediction`, `benediction_globale`, `cout_benediction` ; source `{ quoi: 'benediction'; rang }` ; graines `BENEDICTION_CIBLEE_PAR_RANG`, `BENEDICTION_GLOBALE_PAR_RANG`, `FOI_COUT_DE_BENEDICTION_CIBLEE`, `FOI_COUT_DE_BENEDICTION_GLOBALE`, `RATIO_COUT_DE_BENEDICTION`.

- [ ] **Step 1 : retourner le test de canon**

Dans `tests/canon.test.ts`, le `describe('GDD §4.2 — la Foi n’achète que des miracles')` devient `describe('noyau v1.0 §4 — la Foi achète des bénédictions, et rien d’autre ne monte la production')`. Y **supprimer** les tests « plus aucun terme de production n'est atteignable par un achat » et « aucune source de bénédiction ne subsiste dans le code », et le long commentaire qui les précède, remplacé par :

```typescript
  /**
   * RETOURNÉ une seconde fois, le 2026-09-17. Le 2026-09-08 ce bloc avait
   * supprimé les bénédictions au nom du GDD §4.2 ; le soir même la préséance
   * est passée au noyau v1.0 pour la mécanique (`docs/PRESEANCE.md`), et le
   * noyau §4 fait des bénédictions « l'écran d'améliorations du jeu ». Le code
   * avait gardé la suppression. Spec 2026-09-17 [D8].
   *
   * Ce qui reste vrai, et vérifié : la technique et les succès ne montent
   * jamais une production ; une bénédiction ne fait QUE cela.
   */
```

Ajouter dans ce `describe` :

```typescript
  it('une bénédiction ne cible qu’un terme de production, jamais un coût ni un plafond', () => {
    for (const benediction of BENEDICTIONS) {
      const terme = benediction.portee === 'ciblee' ? 'multiplicateur_benediction' : 'benediction_globale'
      expect(TERMES_DE_PRODUCTION as string[]).toContain(terme)
      expect(TERMES_DE_COUT as string[]).not.toContain(terme)
      expect(TERMES_DE_CONFORT as string[]).not.toContain(terme)
    }
  })

  it('une bénédiction ciblée par espèce, une globale, et pas une de plus', () => {
    const ciblees = BENEDICTIONS.filter((b) => b.portee === 'ciblee')
    const globales = BENEDICTIONS.filter((b) => b.portee === 'globale')
    expect(ciblees.map((b) => b.espece)).toEqual(ESPECES.map((e) => e.id))
    expect(globales).toHaveLength(1)
    expect(globales[0].espece).toBeNull()
  })
```

avec `BENEDICTIONS` importé depuis `../src/donnees/benedictions`. Dans `SOURCES_A_VERIFIER`, ajouter `{ quoi: 'benediction', rang: 0 }, { quoi: 'benediction', rang: 3 }`.

Le test « aucun succès ne monte une production » reste tel quel.

- [ ] **Step 2 : les tests de registre**

Créer `tests/benedictions.test.ts` :

```typescript
/**
 * Les bénédictions — noyau v1.0 §4, spec 2026-09-17 §3.2.
 *
 * Permanentes, payées en Foi, et l'unique chose que la Foi achète tant que les
 * miracles sont gelés. Ciblée : multiplicateur sur une espèce. Globale :
 * additif sur le débit de base de toutes les espèces, présentes et futures.
 */
import { describe, expect, it } from 'vitest'
import { BENEDICTION_GLOBALE_ID, BENEDICTIONS, benedictionCibleeDe, benedictionParId } from '../src/donnees/benedictions'
import {
  BENEDICTION_CIBLEE_PAR_RANG,
  BENEDICTION_GLOBALE_PAR_RANG,
  FOI_COUT_DE_BENEDICTION_CIBLEE,
  FOI_COUT_DE_BENEDICTION_GLOBALE,
  RATIO_COUT_DE_BENEDICTION,
} from '../src/noyau/constantes'
import { ESPECES } from '../src/donnees/especes'

describe('B1 — le registre', () => {
  it('la globale est trouvable par son identifiant, et chaque espèce a sa ciblée', () => {
    expect(benedictionParId(BENEDICTION_GLOBALE_ID)?.portee).toBe('globale')
    for (const espece of ESPECES) {
      const ciblee = benedictionCibleeDe(espece.id)
      expect(ciblee.portee).toBe('ciblee')
      expect(ciblee.espece).toBe(espece.id)
      expect(benedictionParId(ciblee.id)).toBe(ciblee)
    }
    expect(BENEDICTIONS).toHaveLength(ESPECES.length + 1)
  })

  it('les graines sont positives, et le coût croît', () => {
    expect(BENEDICTION_CIBLEE_PAR_RANG).toBeGreaterThan(0)
    expect(BENEDICTION_GLOBALE_PAR_RANG).toBeGreaterThan(0)
    expect(FOI_COUT_DE_BENEDICTION_CIBLEE).toBeGreaterThan(0)
    expect(FOI_COUT_DE_BENEDICTION_GLOBALE).toBeGreaterThan(0)
    expect(RATIO_COUT_DE_BENEDICTION).toBeGreaterThan(1)
  })
})
```

- [ ] **Step 3 : vérifier qu'ils échouent**

Run: `npx vitest run tests/benedictions.test.ts tests/canon.test.ts`
Expected: FAIL — module `benedictions` introuvable.

- [ ] **Step 4 : les types**

Dans `src/noyau/types.ts` :

Après `export type BenedictionId = string` (posé en A4), ajouter :

```typescript
export type PorteeDeBenediction = 'ciblee' | 'globale'

/**
 * Une bénédiction — noyau v1.0 §4.2. Deux formes, deux natures : la ciblée
 * MULTIPLIE une espèce nommée, la globale ADDITIONNE au débit de base de
 * toutes. L'additif écrase tôt et s'efface tard ; le croisement se fait seul.
 */
export interface Benediction {
  readonly id: BenedictionId
  readonly portee: PorteeDeBenediction
  /** L'espèce visée. `null` pour la globale. */
  readonly espece: EspeceId | null
}
```

`TermeDeProduction` : ajouter après `'multiplicateur_heros'` :

```typescript
  /** Bénédiction ciblée sur l'espèce : `(1 + c) ^ rang`. Noyau v1.0 §4.2. */
  | 'multiplicateur_benediction'
  /** Bénédiction globale : `+ k × rang` sur le débit de base de chaque espèce. */
  | 'benediction_globale'
```

et les deux dans `TERMES_DE_PRODUCTION`.

`TermeDeCout` : ajouter `| 'cout_benediction'` avec le commentaire `/** Une bénédiction, en Foi. Un terme de coût comme un autre : la technique pourra le viser. */`, et dans `TERMES_DE_COUT`.

`SourceDeTerme` : ajouter `| { readonly quoi: 'benediction'; readonly rang: number }`.

- [ ] **Step 5 : les constantes**

Dans `src/noyau/constantes.ts`, après le bloc de l'axe héros :

```typescript
/* ─── Les bénédictions — noyau v1.0 §4, spec 2026-09-17 §3.2 ────────────────
 * L'écran d'améliorations permanentes du jeu, payé en Foi. Toutes ces valeurs
 * sont des graines : le premier cycle rapporte ~5 Foi, le deuxième ~1 600, et
 * c'est contre cette échelle qu'elles seront réfutées.
 */

/** [P] graine — une ciblée multiplie son espèce par `(1 + c)` à chaque rang. */
export const BENEDICTION_CIBLEE_PAR_RANG = 0.5

/**
 * [P] graine — la globale ajoute `k × rang` au débit de base de CHAQUE espèce,
 * en mana/s par niveau. Le vairon capte 0,2 : à 0,05 le premier rang lui
 * donne +25 %, et il ne donne plus rien de visible à la dixième espèce.
 */
export const BENEDICTION_GLOBALE_PAR_RANG = 0.05

/** [P] graine — coût du premier rang, en Foi. */
export const FOI_COUT_DE_BENEDICTION_CIBLEE = 3
export const FOI_COUT_DE_BENEDICTION_GLOBALE = 2

/** [P] graine — chaque rang coûte ce facteur de plus que le précédent. */
export const RATIO_COUT_DE_BENEDICTION = 4
```

- [ ] **Step 6 : le registre**

Créer `src/donnees/benedictions.ts` :

```typescript
/**
 * IdlePond — le registre des bénédictions.
 *
 * Contenu pur, engendré depuis les espèces : une ciblée par espèce, une
 * globale. Aucune valeur ici — les graines vivent dans `constantes.ts`, les
 * formules dans `economie.ts`. Les identifiants entrent dans les saves : figés.
 */
import type { Benediction, BenedictionId, EspeceId } from '../noyau/types'
import { ESPECES } from './especes'

export const BENEDICTION_GLOBALE_ID = 'benediction-globale'

function construire(): readonly Benediction[] {
  const globale: Benediction = { id: BENEDICTION_GLOBALE_ID, portee: 'globale', espece: null }
  const ciblees = ESPECES.map<Benediction>((espece) => ({
    id: `benediction-${espece.id}`,
    portee: 'ciblee',
    espece: espece.id,
  }))
  return [globale, ...ciblees]
}

export const BENEDICTIONS: readonly Benediction[] = construire()

export function benedictionParId(id: BenedictionId): Benediction | undefined {
  return BENEDICTIONS.find((b) => b.id === id)
}

/** La ciblée d'une espèce. Lance si l'espèce n'existe pas : le registre est engendré depuis elles. */
export function benedictionCibleeDe(espece: EspeceId): Benediction {
  const ciblee = BENEDICTIONS.find((b) => b.portee === 'ciblee' && b.espece === espece)
  if (ciblee === undefined) throw new Error(`Aucune bénédiction ciblée pour l’espèce ${espece}`)
  return ciblee
}
```

`src/ui/format.ts`, `sourceDuTerme`, ajouter le cas :

```typescript
    case 'benediction':
      return source.rang === 0 ? 'rien de béni' : `béni ${source.rang} fois`
```

- [ ] **Step 7 : vérifier**

Run: `npx vitest run tests/benedictions.test.ts tests/canon.test.ts tests/architecture.test.ts && npx tsc -b`
Expected: PASS. Le test « aucun terme périmé » reste vert : « bénédiction » n'est pas dans sa liste.

- [ ] **Step 8 : commit**

```bash
git add src/donnees/benedictions.ts src/noyau/types.ts src/noyau/constantes.ts src/ui/format.ts tests/canon.test.ts tests/benedictions.test.ts
git commit -m "feat(benedictions): le registre, les termes nommés, et le canon retourné

Noyau v1.0 §4 appliqué par la préséance de docs/PRESEANCE.md. Le test qui
interdisait le mot est remplacé par celui qui borne ce qu'une bénédiction
peut viser : la production, et rien d'autre."
```

---

### Task B2 : l'économie des bénédictions

**Files:**
- Modify: `src/noyau/economie.ts`
- Modify: `tests/benedictions.test.ts`

**Interfaces:**
- Produces: `rangDeBenediction`, `debitBeni`, `multiplicateurDeBenediction`, `coutDeBenediction` ; `debitDeLEspece` lit les deux ; `detailDeCaptation` porte deux lignes de plus.

- [ ] **Step 1 : les tests**

Ajouter à `tests/benedictions.test.ts` :

```typescript
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../src/noyau/types'
import { etatInitial } from '../src/noyau/noyau'
import { etatDeTravail } from './etat-de-travail'
import {
  coutDeBenediction,
  coutDeNiveau,
  debitBaseDeLEspece,
  debitBeni,
  detailDeCaptation,
  multiplicateurDeBenediction,
  productionDeLEspece,
  rangDeBenediction,
} from '../src/noyau/economie'

function benie(etat: EtatJeu, rangs: Record<string, number>): EtatJeu {
  return { ...etat, permanent: { ...etat.permanent, benedictions: { ...etat.permanent.benedictions, ...rangs } } }
}

describe('B2 — ce qu’une bénédiction vaut', () => {
  const vairon = ESPECES[0]

  it('sans bénédiction : rang 0, débit béni = débit de base, multiplicateur 1', () => {
    const etat = etatDeTravail()
    expect(rangDeBenediction(etat, BENEDICTION_GLOBALE_ID)).toBe(0)
    expect(debitBeni(etat, vairon).eq(debitBaseDeLEspece(vairon))).toBe(true)
    expect(multiplicateurDeBenediction(etat, vairon)).toBe(1)
  })

  it('la globale ajoute k × rang au débit de base de toute espèce', () => {
    const etat = benie(etatDeTravail(), { [BENEDICTION_GLOBALE_ID]: 3 })
    for (const espece of ESPECES.slice(0, 3)) {
      const attendu = debitBaseDeLEspece(espece).add(BENEDICTION_GLOBALE_PAR_RANG * 3)
      expect(debitBeni(etat, espece).eq(attendu)).toBe(true)
    }
  })

  it('la ciblée multiplie SON espèce par (1 + c)^rang, et aucune autre', () => {
    const etat = benie(etatDeTravail(), { [benedictionCibleeDe(vairon.id).id]: 2 })
    expect(multiplicateurDeBenediction(etat, vairon)).toBeCloseTo(Math.pow(1 + BENEDICTION_CIBLEE_PAR_RANG, 2), 12)
    expect(multiplicateurDeBenediction(etat, ESPECES[1])).toBe(1)
  })

  it('la production de l’espèce lit les deux', () => {
    const nue = etatDeTravail()
    const etat = benie(nue, { [BENEDICTION_GLOBALE_ID]: 1, [benedictionCibleeDe(vairon.id).id]: 1 })
    const rapport = productionDeLEspece(etat, vairon).div(productionDeLEspece(nue, vairon)).toNumber()
    const attendu =
      (debitBaseDeLEspece(vairon).toNumber() + BENEDICTION_GLOBALE_PAR_RANG) /
      debitBaseDeLEspece(vairon).toNumber() *
      (1 + BENEDICTION_CIBLEE_PAR_RANG)
    expect(rapport).toBeCloseTo(attendu, 9)
  })

  it('bénir ne renchérit pas le niveau : le coût suit le débit NON béni', () => {
    const nue = etatDeTravail()
    const etat = benie(nue, { [BENEDICTION_GLOBALE_ID]: 5 })
    expect(coutDeNiveau(etat, vairon, 7).eq(coutDeNiveau(nue, vairon, 7))).toBe(true)
  })

  it('le coût en Foi est géométrique, et la globale et la ciblée ont chacune leur base', () => {
    const etat = etatInitial(1)
    const globale = benedictionParId(BENEDICTION_GLOBALE_ID)!
    const ciblee = benedictionCibleeDe(vairon.id)
    expect(coutDeBenediction(etat, globale).toNumber()).toBeCloseTo(FOI_COUT_DE_BENEDICTION_GLOBALE, 9)
    expect(coutDeBenediction(etat, ciblee).toNumber()).toBeCloseTo(FOI_COUT_DE_BENEDICTION_CIBLEE, 9)
    const deuxRangs = benie(etat, { [ciblee.id]: 2 })
    expect(coutDeBenediction(deuxRangs, ciblee).toNumber()).toBeCloseTo(
      FOI_COUT_DE_BENEDICTION_CIBLEE * Math.pow(RATIO_COUT_DE_BENEDICTION, 2),
      9,
    )
  })

  it('le détail de captation nomme les deux termes, chacun à sa source', () => {
    const etat = benie(etatDeTravail(), { [BENEDICTION_GLOBALE_ID]: 2, [benedictionCibleeDe(vairon.id).id]: 1 })
    const lignes = detailDeCaptation(etat, vairon)
    const globale = lignes.find((l) => l.terme === 'benediction_globale')
    const ciblee = lignes.find((l) => l.terme === 'multiplicateur_benediction')
    expect(globale?.valeur).toBeCloseTo(BENEDICTION_GLOBALE_PAR_RANG * 2, 12)
    expect(globale?.source).toEqual({ quoi: 'benediction', rang: 2 })
    expect(ciblee?.valeur).toBeCloseTo(1 + BENEDICTION_CIBLEE_PAR_RANG, 12)
    expect(ciblee?.source).toEqual({ quoi: 'benediction', rang: 1 })
  })
})
```

`Decimal` sert dès B3 (`new Decimal(100)`) ; ESLint ne le signale donc qu'entre B2 et B3, sur ce fichier de test seulement.

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/benedictions.test.ts`
Expected: FAIL — `debitBeni` non exporté.

- [ ] **Step 3 : l'implémentation**

Dans `src/noyau/economie.ts` :

Imports : `Benediction`, `BenedictionId` depuis `./types` ; `BENEDICTION_CIBLEE_PAR_RANG`, `BENEDICTION_GLOBALE_PAR_RANG`, `FOI_COUT_DE_BENEDICTION_CIBLEE`, `FOI_COUT_DE_BENEDICTION_GLOBALE`, `RATIO_COUT_DE_BENEDICTION` depuis `./constantes` ; `BENEDICTION_GLOBALE_ID`, `benedictionCibleeDe` depuis `../donnees/benedictions`.

Après `debitBaseDeLEspece`, ajouter :

```typescript
/* ─── Bénédictions — noyau v1.0 §4.2 ────────────────────────────────────────*/

export function rangDeBenediction(etat: EtatJeu, id: BenedictionId): number {
  return etat.permanent.benedictions[id] ?? 0
}

/**
 * Le débit de base d'une espèce, augmenté de la bénédiction GLOBALE : additif,
 * « sur le débit de base de toutes les espèces, présentes et futures ». Il
 * domine quand les débits sont minuscules et s'efface une fois les
 * multiplicateurs décollés — aucun ratio à régler.
 *
 * Le coût d'un niveau ne le lit PAS : il lit `debitBaseDeLEspece`. Bénir ne
 * renchérit rien.
 */
export function debitBeni(etat: EtatJeu, espece: Espece): Decimal {
  const rang = rangDeBenediction(etat, BENEDICTION_GLOBALE_ID)
  if (rang === 0) return debitBaseDeLEspece(espece)
  return debitBaseDeLEspece(espece).add(BENEDICTION_GLOBALE_PAR_RANG * rang)
}

/** La bénédiction CIBLÉE de l'espèce : `(1 + c) ^ rang`, empilable, 1 à rang 0. */
export function multiplicateurDeBenediction(etat: EtatJeu, espece: Espece): number {
  return Math.pow(1 + BENEDICTION_CIBLEE_PAR_RANG, rangDeBenediction(etat, benedictionCibleeDe(espece.id).id))
}
```

Remplacer `debitDeLEspece` :

```typescript
function debitDeLEspece(etat: EtatJeu, espece: Espece): Decimal {
  const vivante = etat.cycle.especes[espece.id]
  if (vivante === undefined || !vivante.debloquee || vivante.niveau === 0) return new Decimal(0)
  return debitBeni(etat, espece)
    .mul(vivante.niveau)
    .mul(multiplicateurDeSeuil(vivante.niveau))
    .mul(multiplicateurDeBenediction(etat, espece))
}
```

Dans `detailDeCaptation`, ajouter après la ligne `taux_base` :

```typescript
    {
      terme: 'benediction_globale',
      valeur: BENEDICTION_GLOBALE_PAR_RANG * rangDeBenediction(etat, BENEDICTION_GLOBALE_ID),
      source: { quoi: 'benediction', rang: rangDeBenediction(etat, BENEDICTION_GLOBALE_ID) },
    },
```

et après `multiplicateur_jalon` :

```typescript
    {
      terme: 'multiplicateur_benediction',
      valeur: multiplicateurDeBenediction(etat, espece),
      source: { quoi: 'benediction', rang: rangDeBenediction(etat, benedictionCibleeDe(espece.id).id) },
    },
```

Dans la section Coûts, après `coutDeCroissance` :

```typescript
/**
 * Ce que coûte le rang suivant d'une bénédiction, EN FOI — spec 2026-09-17
 * [D6]. Géométrique : `base × ratio ^ rang`. `cout_benediction` est un terme
 * de coût nommé, donc la technique et les succès pourront le viser.
 */
export function coutDeBenediction(etat: EtatJeu, benediction: Benediction): Decimal {
  const base = benediction.portee === 'globale' ? FOI_COUT_DE_BENEDICTION_GLOBALE : FOI_COUT_DE_BENEDICTION_CIBLEE
  return new Decimal(base)
    .mul(Decimal.pow(RATIO_COUT_DE_BENEDICTION, rangDeBenediction(etat, benediction.id)))
    .mul(facteurDeCout(etat, 'cout_benediction'))
}
```

- [ ] **Step 4 : le simulateur suit l'assiette bénie**

`achatsDisponibles` dans `src/simulateur/simulateur.ts` calcule le gain d'un déblocage et d'un niveau avec `debitBaseDeLEspece(espece)`. Remplacer ces deux occurrences par `debitBeni(etat, espece).mul(multiplicateurDeBenediction(etat, espece))` (importer les deux depuis `../noyau/economie`). Le test « le gain de chaque achat est la production qu'il ajoute réellement » ne le verra pas tant que la fixture n'a aucune bénédiction : ajouter dans ce test un second passage sur `benie(etatDeTravail(), { 'benediction-globale': 2, 'benediction-vairon': 1 })` — recopier la petite fonction `benie` de `tests/benedictions.test.ts`.

- [ ] **Step 5 : vérifier**

Run: `npx vitest run tests/benedictions.test.ts tests/simulateur.test.ts tests/equivalence-de-pas.test.ts && npx tsc -b`
Expected: PASS.

- [ ] **Step 6 : commit**

```bash
git add src/noyau/economie.ts src/simulateur/simulateur.ts tests/benedictions.test.ts tests/simulateur.test.ts
git commit -m "feat(benedictions): débit béni, multiplicateur ciblé, coût en Foi, détail nommé"
```

---

### Task B3 : l'acte `benir`, et ce qui traverse l'éclosion

**Files:**
- Modify: `src/noyau/noyau.ts`
- Modify: `tests/benedictions.test.ts`
- Modify: `tests/eclosion.test.ts`

- [ ] **Step 1 : les tests**

Ajouter à `tests/benedictions.test.ts` :

```typescript
import { benir, eclore } from '../src/noyau/noyau'

describe('B3 — bénir', () => {
  const vairon = ESPECES[0]

  it('paie la Foi, monte le rang d’un', () => {
    const riche = { ...etatInitial(1), permanent: { ...etatInitial(1).permanent, foi: new Decimal(100) } }
    const globale = benedictionParId(BENEDICTION_GLOBALE_ID)!
    const prix = coutDeBenediction(riche, globale)
    const apres = benir(riche, globale.id)
    expect(rangDeBenediction(apres, globale.id)).toBe(1)
    expect(apres.permanent.foi.eq(riche.permanent.foi.sub(prix))).toBe(true)
    // Le mana n'est pas touché : la Foi n'est pas une seconde monnaie de mana.
    expect(apres.cycle.manaCourant.eq(riche.cycle.manaCourant)).toBe(true)
  })

  it('refuse sans rien changer si la Foi manque, ou si l’identifiant est inconnu', () => {
    const pauvre = etatInitial(1)
    expect(pauvre.permanent.foi.eq(0)).toBe(true)
    expect(benir(pauvre, BENEDICTION_GLOBALE_ID)).toBe(pauvre)
    const riche = { ...pauvre, permanent: { ...pauvre.permanent, foi: new Decimal(100) } }
    expect(benir(riche, 'benediction-qui-n-existe-pas')).toBe(riche)
  })

  it('le rang traverse l’éclosion — c’est permanent', () => {
    const avecUneCiblee = benir(
      { ...etatDeTravail(), permanent: { ...etatDeTravail().permanent, foi: new Decimal(1000) } },
      benedictionCibleeDe(vairon.id).id,
    )
    expect(rangDeBenediction(avecUneCiblee, benedictionCibleeDe(vairon.id).id)).toBe(1)
    expect(eclore(avecUneCiblee).permanent.benedictions).toEqual(avecUneCiblee.permanent.benedictions)
  })

  it('les rangs sont sérialisés dans l’ordre du registre, pas de l’achat', () => {
    // Deux parties qui bénissent les mêmes choses dans un ordre différent
    // doivent produire la même chaîne de save (déterminisme).
    const riche = { ...etatInitial(1), permanent: { ...etatInitial(1).permanent, foi: new Decimal(1e6) } }
    const a = benir(benir(riche, 'benediction-loche'), BENEDICTION_GLOBALE_ID)
    const b = benir(benir(riche, BENEDICTION_GLOBALE_ID), 'benediction-loche')
    expect(Object.keys(a.permanent.benedictions)).toEqual(Object.keys(b.permanent.benedictions))
  })
})
```

Dans `tests/eclosion.test.ts`, `describe('§3.1 — ce qui traverse l’éclosion')`, ajouter :

```typescript
  it('les bénédictions traversent', () => {
    const avant = avecPermanent(etatDeTravail(), { benedictions: { 'benediction-globale': 2, 'benediction-vairon': 1 } })
    expect(eclore(avant).permanent.benedictions).toEqual(avant.permanent.benedictions)
  })
```

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/benedictions.test.ts`
Expected: FAIL — `benir` non exporté.

- [ ] **Step 3 : l'implémentation**

Dans `src/noyau/noyau.ts` : importer `coutDeBenediction` depuis `./economie`, `BENEDICTIONS`, `benedictionParId` depuis `../donnees/benedictions`, `BenedictionId` depuis `./types`. Après `grandir` :

```typescript
/**
 * Bénir — noyau v1.0 §4. Payé en FOI, permanent, et le seul débouché de la Foi
 * tant que les miracles sont gelés ([P26]).
 *
 * La table est reconstruite dans l'ordre du registre, jamais dans l'ordre des
 * achats — même raison que `especesAyantAtteintCent` : l'ordre des clefs d'un
 * objet est celui de l'insertion, et le test de déterminisme compare la chaîne
 * de save.
 */
export function benir(etat: EtatJeu, id: BenedictionId): EtatJeu {
  const benediction = benedictionParId(id)
  if (benediction === undefined) return etat
  const cout = coutDeBenediction(etat, benediction)
  if (etat.permanent.foi.lt(cout)) return etat
  const rangs = { ...etat.permanent.benedictions, [id]: (etat.permanent.benedictions[id] ?? 0) + 1 }
  const benedictions: Record<BenedictionId, number> = {}
  for (const b of BENEDICTIONS) {
    const rang = rangs[b.id]
    if (rang !== undefined && rang > 0) benedictions[b.id] = rang
  }
  return {
    ...etat,
    permanent: {
      ...etat.permanent,
      foi: etat.permanent.foi.sub(cout),
      benedictions,
    },
  }
}
```

L'éclosion ne touche pas `permanent.benedictions` : `eclore` recopie `...etat.permanent`. Rien à écrire.

- [ ] **Step 4 : vérifier**

Run: `npx vitest run tests/benedictions.test.ts tests/eclosion.test.ts tests/determinisme.test.ts tests/persistance.test.ts`
Expected: PASS.

- [ ] **Step 5 : commit**

```bash
git add src/noyau/noyau.ts tests/benedictions.test.ts tests/eclosion.test.ts
git commit -m "feat(benedictions): l'acte bénir — en Foi, permanent, dans l'ordre du registre"
```

---

### Task B4 : le simulateur bénit après l'éclosion

**Files:**
- Modify: `src/simulateur/simulateur.ts`
- Modify: `tests/simulateur.test.ts`

- [ ] **Step 1 : le test**

Ajouter dans `tests/simulateur.test.ts`, `describe('le simulateur tourne sur le noyau v1.0')` :

```typescript
  it('la Foi est dépensée en bénédictions après l’éclosion, et la partie converge toujours', () => {
    // Spec [D6] : l'échelle de Foi (~5 au cycle 1, ~1 600 au cycle 2) doit
    // rendre la première bénédiction payable dès la première éclosion, sans
    // que tout le registre soit acheté avant le cycle 5.
    const resultat = simuler(5, undefined, 1)
    expect(resultat.cycleNonConvergent).toBeNull()
    const rangs = Object.values(resultat.etat.permanent.benedictions)
    expect(rangs.length).toBeGreaterThan(0)
    const total = rangs.reduce((a, b) => a + b, 0)
    expect(total).toBeGreaterThanOrEqual(2)
    expect(total).toBeLessThan(40)
    // Il reste moins de Foi qu'il n'en faut pour la bénédiction la moins chère :
    // la politique dépense, elle ne thésaurise pas.
    const moinsChere = BENEDICTIONS.map((b) => coutDeBenediction(resultat.etat, b)).reduce((a, b) => (a.lt(b) ? a : b))
    expect(resultat.etat.permanent.foi.lt(moinsChere)).toBe(true)
  })
```

avec `BENEDICTIONS` depuis `../src/donnees/benedictions` et `coutDeBenediction` depuis `../src/noyau/economie`.

- [ ] **Step 2 : vérifier qu'il échoue**

Run: `npx vitest run tests/simulateur.test.ts -t "bénédictions"`
Expected: FAIL — aucun rang acheté.

- [ ] **Step 3 : l'implémentation**

Dans `src/simulateur/simulateur.ts`, importer `benir` depuis `../noyau/noyau`, `coutDeBenediction` depuis `../noyau/economie`, `BENEDICTIONS` depuis `../donnees/benedictions`. Ajouter avant `simuler` :

```typescript
/**
 * Ce que le joueur fait de sa Foi : il bénit, la moins chère d'abord, tant
 * qu'il peut payer. Une politique, pas une règle du noyau — la globale et les
 * ciblées ont chacune leur échelle de prix, et le simulateur n'a pas à savoir
 * laquelle rapporte le plus dans une vie qui n'a pas encore commencé.
 *
 * Appelée juste après `eclore` : c'est là que la Foi est créditée. Elle
 * termine d'elle-même — chaque rang multiplie le prix par le ratio.
 */
export function benirAuMieux(etat: EtatJeu): EtatJeu {
  let courant = etat
  for (let garde = 0; garde < 10_000; garde += 1) {
    let choix: { readonly id: string; readonly cout: Decimal } | null = null
    for (const b of BENEDICTIONS) {
      const cout = coutDeBenediction(courant, b)
      if (cout.gt(courant.permanent.foi)) continue
      if (choix === null || cout.lt(choix.cout)) choix = { id: b.id, cout }
    }
    if (choix === null) return courant
    const suivant = benir(courant, choix.id)
    if (suivant === courant) throw new Error(`Le noyau refuse une bénédiction que la politique croyait payable : ${choix.id}`)
    courant = suivant
  }
  throw new Error('La politique de bénédiction ne termine pas')
}
```

Dans `simuler`, remplacer `etat = eclore(etat)` par :

```typescript
    etat = benirAuMieux(eclore(etat))
```

- [ ] **Step 4 : vérifier, et mesurer**

Run: `npx vitest run`
Expected: PASS. Si « le premier cycle dure environ trois heures » ou « la saturation ne gèle pas » tombe, la bénédiction globale a déplacé le cycle 2 et suivants — pas le cycle 1, qui n'a pas de Foi. Lire la durée, et si le cycle 2 est plus court que le cycle 1 de plus de 30 %, monter `RATIO_COUT_DE_BENEDICTION` de 4 à 6 ; consigner.

Relancer la mesure de A5 Step 5 et mettre à jour la table de `NOMBRE_D_ECLOSIONS_VISE` si elle a bougé de plus de 5 %.

- [ ] **Step 5 : commit**

```bash
git add src/simulateur/simulateur.ts tests/simulateur.test.ts src/noyau/constantes.ts
git commit -m "feat(simulateur): la Foi est dépensée en bénédictions après chaque éclosion"
```

---

### Task B5 : l'écran des bénédictions

**Files:**
- Create: `src/ui/Benedictions.tsx`
- Modify: `src/donnees/textes-provisoires.ts`
- Modify: `src/etat/magasin.ts`
- Modify: `src/ui/App.tsx`

- [ ] **Step 1 : les textes**

Dans `src/donnees/textes-provisoires.ts`, après `NOM_DES_ESPECES` :

```typescript
/**
 * Ce que l'écran dit d'une bénédiction — un verbe, ce que ça fait. La globale
 * a un nom à elle ; une ciblée prend le nom de son espèce.
 */
export const TEXTE_DE_LA_BENEDICTION_GLOBALE = {
  nom: 'Bénir l’eau',
  effet: 'tout ce qui vit ici capte un peu plus, et tout ce qui viendra',
} as const

export const TEXTE_DE_BENEDICTION_CIBLEE = {
  effet: 'ils te donnent moitié plus, à chaque fois',
} as const
```

- [ ] **Step 2 : le magasin**

`src/etat/magasin.ts` : importer `benir` et `BenedictionId` ; ajouter `benir(id: BenedictionId): void` à l'interface, et `benir: (id) => set({ etat: benir(get().etat, id) }),`.

- [ ] **Step 3 : l'écran**

Créer `src/ui/Benedictions.tsx` :

```tsx
/**
 * Ce que tu bénis — noyau v1.0 §4.1 : « l'écran d'améliorations du jeu, comme
 * le veut la convention du genre ». Permanent, payé en Foi.
 *
 * Spec 2026-09-17 [D7] : ouvert tout le temps. La Foi n'est créditée qu'à
 * l'éclosion, donc ce que cet écran permet ne change qu'en rentrant dans
 * l'œuf ; mais un joueur qui revient d'une absence ne doit pas trouver une
 * porte fermée.
 *
 * Les ciblées ne sont listées que pour les espèces déjà convaincues au moins
 * une fois dans cette vie ou une autre — on ne bénit pas ce qu'on n'a jamais
 * vu. Le registre entier existe dans la donnée ; l'écran le filtre.
 */
import type { EtatJeu } from '../noyau/types'
import { BENEDICTIONS, BENEDICTION_GLOBALE_ID } from '../donnees/benedictions'
import { coutDeBenediction, rangDeBenediction } from '../noyau/economie'
import { TEXTE_DE_BENEDICTION_CIBLEE, TEXTE_DE_LA_BENEDICTION_GLOBALE } from '../donnees/textes-provisoires'
import { cout, montant, nomDeLEspece } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly surBenediction: (id: string) => void
}

export function Benedictions({ etat, surBenediction }: Props) {
  const foi = etat.permanent.foi
  const connues = new Set<string>([
    ...Object.keys(etat.cycle.especes).filter((id) => etat.cycle.especes[id].debloquee),
    ...etat.permanent.especesAyantAtteintCent,
    ...BENEDICTIONS.filter((b) => b.espece !== null && rangDeBenediction(etat, b.id) > 0).map((b) => b.espece as string),
  ])
  const visibles = BENEDICTIONS.filter((b) => b.portee === 'globale' || (b.espece !== null && connues.has(b.espece)))

  return (
    <section className="space-y-2">
      <div className="flex items-baseline justify-between">
        <h2 className="font-texte text-lg text-jour-doux">Ce que tu bénis</h2>
        <span className="font-chiffre text-sm text-foi tabular-nums">{montant(foi)} de Foi</span>
      </div>
      <ol className="space-y-2">
        {visibles.map((b) => {
          const rang = rangDeBenediction(etat, b.id)
          const prix = coutDeBenediction(etat, b)
          const payable = foi.gte(prix)
          const nom = b.id === BENEDICTION_GLOBALE_ID ? TEXTE_DE_LA_BENEDICTION_GLOBALE.nom : `Bénir ${nomDeLEspece(b.espece as string)}`
          const effet = b.id === BENEDICTION_GLOBALE_ID ? TEXTE_DE_LA_BENEDICTION_GLOBALE.effet : TEXTE_DE_BENEDICTION_CIBLEE.effet
          return (
            <li key={b.id} className="rounded-lg border border-eau-bord bg-eau-fond/40 p-3">
              <div className="flex items-baseline justify-between gap-3">
                <span className="font-texte text-base">{nom}</span>
                <span className="font-chiffre text-xs text-jour-tu tabular-nums">{rang === 0 ? 'jamais' : `${rang} fois`}</span>
              </div>
              <p className="mt-0.5 text-xs text-jour-tu">{effet}</p>
              <button
                type="button"
                disabled={!payable}
                onClick={() => surBenediction(b.id)}
                className="mt-2 w-full rounded-md border border-foi/60 px-3 py-1.5 text-sm text-foi transition-colors enabled:hover:bg-foi/10 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Bénir
                <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(prix)} de Foi</span>
              </button>
            </li>
          )
        })}
      </ol>
    </section>
  )
}
```

- [ ] **Step 4 : brancher**

`src/ui/App.tsx` : importer `Benedictions` ; dans `<aside>`, après `<Succes etat={etat} />` :

```tsx
          <div className="mt-6">
            <Benedictions etat={etat} surBenediction={(id) => useMagasin.getState().benir(id)} />
          </div>
```

`Succes` est `md:sticky` avec une hauteur bornée ; si l'écran des bénédictions passe sous la ligne de flottaison, retirer `md:sticky md:top-6` de la section de `Succes.tsx` — une ligne.

- [ ] **Step 5 : vérifier**

Run: `npx tsc -b && npx eslint . && npx vitest run tests/canon.test.ts && npm run build`
Expected: propres — le test « aucun terme de couche ne sort dans un texte affiché » ne lit pas ces deux textes ; vérifier à l'œil qu'ils ne contiennent ni « palier » ni « couche » (ils ne les contiennent pas).

`npm run dev` : au premier lancement l'écran liste « Bénir l'eau » à 2 de Foi, grisé. Forcer une éclosion (creuser jusqu'au blocage puis « Rentrer ») : la Foi apparaît en haut, le bouton s'active, bénir fait monter le « +… / s » du vairon dans la vie suivante. Capture.

- [ ] **Step 6 : commit**

```bash
git add src/ui/Benedictions.tsx src/donnees/textes-provisoires.ts src/etat/magasin.ts src/ui/App.tsx src/ui/Succes.tsx
git commit -m "feat(ui): l'écran des bénédictions — ce que la Foi achète"
```

---

# Partie C — la scène

### Task C1 : `couches` écrit à l'éclosion

**Files:**
- Modify: `src/noyau/eclosion.ts`
- Modify: `tests/eclosion.test.ts`

- [ ] **Step 1 : les tests**

Dans `tests/eclosion.test.ts`, `describe('§3.1 — ce qui traverse l’éclosion')` :

```typescript
  it('chaque assise traversée dans cette vie laisse une couche, dans l’ordre des assises, une seule fois', () => {
    // GDD §15.1 : « une marque par assise fixée ». `couches` était déclaré et
    // jamais écrit (audit du 2026-09-08). Spec 2026-09-17 [D11].
    const dansLaNoue = avecCycle(etatDeTravail(), { paliersOuverts: 4 })
    expect(dansLaNoue.permanent.couches).toEqual([])
    const uneFois = eclore(dansLaNoue)
    expect(uneFois.permanent.couches).toEqual(['noue'])

    // Deux assises ouvertes, la première déjà marquée : une seule couche neuve.
    const plusBas = avecCycle(avecPermanent(uneFois, { couches: ['noue'] }), { paliersOuverts: 8 })
    expect(eclore(plusBas).permanent.couches).toEqual(['noue', 'assise-2'])

    // L'ordre est celui des assises, pas celui de l'obtention.
    const desordre = avecCycle(avecPermanent(etatDeTravail(), { couches: ['assise-2'] }), { paliersOuverts: 2 })
    expect(eclore(desordre).permanent.couches).toEqual(['noue', 'assise-2'])
  })
```

- [ ] **Step 2 : vérifier qu'il échoue**

Run: `npx vitest run tests/eclosion.test.ts -t "couche"`
Expected: FAIL — `couches` reste `[]`.

- [ ] **Step 3 : l'implémentation**

Dans `src/noyau/eclosion.ts`, importer `ASSISES` depuis `../donnees/assises` et ajouter avant `eclore` :

```typescript
/**
 * Les couches du corps — GDD §15.1, « une marque par assise fixée ».
 *
 * Toute assise dont le premier palier a été ouvert dans cette vie laisse sa
 * marque. Dans l'ordre des assises, jamais dans l'ordre de l'obtention : la
 * divergence « se lit comme une somme d'histoire », et une somme n'a pas
 * d'ordre — mais la save, elle, compare des chaînes.
 */
export function couchesApres(etat: EtatJeu, paliersOuverts: number): readonly AssiseId[] {
  const acquises = new Set(etat.permanent.couches)
  for (const assise of ASSISES) {
    if (assise.indexPremierPalier < paliersOuverts) acquises.add(assise.id)
  }
  return ASSISES.filter((a) => acquises.has(a.id)).map((a) => a.id)
}
```

(`AssiseId` importé depuis `./types`.) Dans `eclore`, dans `permanent: { ... }`, ajouter après `densites,` :

```typescript
      couches: couchesApres(etat, etat.cycle.paliersOuverts),
```

- [ ] **Step 4 : vérifier**

Run: `npx vitest run tests/eclosion.test.ts tests/determinisme.test.ts tests/simulateur.test.ts`
Expected: PASS.

- [ ] **Step 5 : commit**

```bash
git add src/noyau/eclosion.ts tests/eclosion.test.ts
git commit -m "feat(eclosion): chaque assise traversée laisse une couche sur le corps"
```

---

### Task C2 : la projection pure `vue.ts` et la palette

**Files:**
- Create: `src/scene/vue.ts`
- Create: `src/scene/palette.ts`
- Create: `tests/scene.test.ts`
- Modify: `tests/architecture.test.ts`

- [ ] **Step 1 : les tests**

Créer `tests/scene.test.ts` :

```typescript
/**
 * La scène — spec 2026-09-17 §3.3. Ce qui se teste en node : la projection
 * pure de l'état vers ce que Phaser dessine, et les données de palette.
 * Phaser lui-même n'est pas monté ici.
 */
import { readFileSync } from 'node:fs'
import { join, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import { etatInitial, creuser, debloquer, ameliorer } from '../src/noyau/noyau'
import { ASSISES } from '../src/donnees/assises'
import { ESPECES } from '../src/donnees/especes'
import { echelleDuHeros, vueDeLaScene } from '../src/scene/vue'
import { MARQUE_PAR_ASSISE, PALETTES, paletteDe } from '../src/scene/palette'
import { etatDeTravail } from './etat-de-travail'

describe('C2 — la vue de la scène', () => {
  it('liste exactement les paliers ouverts, avec leur assise et leur espèce', () => {
    const etat = etatDeTravail()
    const vue = vueDeLaScene(etat)
    expect(vue.paliers).toHaveLength(etat.cycle.paliersOuverts)
    vue.paliers.forEach((p, i) => {
      expect(p.index).toBe(i)
      expect(ASSISES.some((a) => a.id === p.assise)).toBe(true)
    })
    const avecEspece = vue.paliers.filter((p) => p.espece !== null)
    expect(avecEspece.map((p) => p.espece?.id)).toEqual(
      ESPECES.filter((e) => e.palier < etat.cycle.paliersOuverts && etat.cycle.especes[e.id]?.debloquee).map((e) => e.id),
    )
  })

  it('une espèce non débloquée n’apparaît pas, même si son palier est ouvert', () => {
    let etat = etatInitial(1)
    etat = { ...etat, cycle: { ...etat.cycle, manaCourant: etat.cycle.manaCourant.mul(1e6) } }
    etat = creuser(creuser(creuser(etat)))
    expect(vueDeLaScene(etat).paliers.every((p) => p.espece === null)).toBe(true)
    etat = debloquer(etat, ESPECES[0].id)
    etat = ameliorer(etat, ESPECES[0].id)
    const premier = vueDeLaScene(etat).paliers[0]
    expect(premier.espece).toEqual({ id: ESPECES[0].id, rang: 0, niveau: 2 })
  })

  it('le héros porte son niveau, son échelle et ses couches', () => {
    const etat = {
      ...etatDeTravail(),
      cycle: { ...etatDeTravail().cycle, niveauDuHeros: 16 },
      permanent: { ...etatDeTravail().permanent, couches: ['noue'] },
    }
    const heros = vueDeLaScene(etat).heros
    expect(heros.niveau).toBe(16)
    expect(heros.echelle).toBeCloseTo(2, 9)
    expect(heros.couches).toEqual(['noue'])
  })

  it('l’échelle vaut 1 + 0,25·log₂(niveau)', () => {
    expect(echelleDuHeros(1)).toBe(1)
    expect(echelleDuHeros(2)).toBeCloseTo(1.25, 9)
    expect(echelleDuHeros(256)).toBeCloseTo(3, 9)
  })

  it('l’eau trouble et la saturation passent dans la vue', () => {
    const vue = vueDeLaScene(etatInitial(1))
    expect(vue.eauTroublee).toBe(false)
    expect(vue.sature).toBe(false)
  })

  it('la vue est sérialisable telle quelle — c’est ce qui traverse vers Phaser', () => {
    expect(() => JSON.stringify(vueDeLaScene(etatDeTravail()))).not.toThrow()
  })
})

describe('C2 — la palette', () => {
  it('six palettes, une par assise, et la lumière baisse en descendant', () => {
    expect(Object.keys(PALETTES)).toEqual(ASSISES.map((a) => a.id))
    const lumieres = ASSISES.map((a) => paletteDe(a.id).lumiere)
    for (let i = 1; i < lumieres.length; i += 1) expect(lumieres[i]).toBeLessThan(lumieres[i - 1])
  })

  it('une marque par assise, toutes différentes', () => {
    const marques = ASSISES.map((a) => MARQUE_PAR_ASSISE[a.id])
    expect(new Set(marques).size).toBe(ASSISES.length)
  })

  it('vue.ts et palette.ts n’importent pas Phaser', () => {
    const racine = resolve(__dirname, '..')
    for (const f of ['src/scene/vue.ts', 'src/scene/palette.ts']) {
      expect(readFileSync(join(racine, f), 'utf8')).not.toMatch(/from\s+['"]phaser['"]/)
    }
  })
})
```

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/scene.test.ts`
Expected: FAIL — modules introuvables.

- [ ] **Step 3 : la palette**

Créer `src/scene/palette.ts` :

```typescript
/**
 * IdlePond — palettes et marques, données pures de la scène.
 *
 * GDD §15.2 : « la lumière est la variable de progression la plus lisible.
 * Elle décroît continûment jusqu'à ce que la seule lumière restante soit celle
 * que le mana produit. » Six palettes, une par assise, la lumière qui baisse.
 *
 * GDD §15.1 : « un corps de base sur lequel s'ajoutent des marques, une par
 * assise fixée : branchies, membranes, luminescence, minéralisation,
 * épaississement. » Cinq marques nommées pour six assises : la sixième, le
 * halo, est un [P] — à confirmer par la DA.
 *
 * Aucun PNG : §15.3 bloque le premier sprite définitif sur l'anatomie du corps
 * de base, et §15.4 met l'imagerie de l'Étang des Merveilles hors registre.
 */
import type { AssiseId } from '../noyau/types'
import { ASSISES } from '../donnees/assises'

export interface PaletteDAssise {
  /** La roche derrière l'eau, 0xRRGGBB. */
  readonly fond: number
  /** L'eau elle-même. */
  readonly eau: number
  /** 0 à 1 — ce qu'il reste de jour. Décroît strictement en descendant. */
  readonly lumiere: number
}

export type MarqueId = 'branchies' | 'membranes' | 'luminescence' | 'mineralisation' | 'epaississement' | 'halo'

const PALETTES_PAR_RANG: readonly PaletteDAssise[] = [
  { fond: 0x24312b, eau: 0x2f5f5a, lumiere: 1.0 },
  { fond: 0x1d2a2c, eau: 0x244a52, lumiere: 0.75 },
  { fond: 0x17222a, eau: 0x1b3a4b, lumiere: 0.5 },
  { fond: 0x121a24, eau: 0x152b3d, lumiere: 0.32 },
  { fond: 0x1a1414, eau: 0x2a1c1c, lumiere: 0.18 },
  { fond: 0x0b0d14, eau: 0x0f1424, lumiere: 0.08 },
]

const MARQUES_PAR_RANG: readonly MarqueId[] = [
  'branchies',
  'membranes',
  'luminescence',
  'mineralisation',
  'epaississement',
  'halo',
]

export const PALETTES: Readonly<Record<AssiseId, PaletteDAssise>> = Object.fromEntries(
  ASSISES.map((a, i) => [a.id, PALETTES_PAR_RANG[i]]),
)

export const MARQUE_PAR_ASSISE: Readonly<Record<AssiseId, MarqueId>> = Object.fromEntries(
  ASSISES.map((a, i) => [a.id, MARQUES_PAR_RANG[i]]),
)

export function paletteDe(assise: AssiseId): PaletteDAssise {
  return PALETTES[assise] ?? PALETTES_PAR_RANG[PALETTES_PAR_RANG.length - 1]
}
```

- [ ] **Step 4 : la vue**

Créer `src/scene/vue.ts` :

```typescript
/**
 * IdlePond — la vue de la scène : ce que Phaser dessine, sans Phaser.
 *
 * Projection PURE de l'état (spec 2026-09-17 [D9]). La scène ne lit jamais
 * `EtatJeu` : elle reçoit cette structure, sérialisable, et la dessine. C'est
 * ce qui rend la scène testable en node par ses données, et remplaçable par
 * de vrais sprites sans toucher au noyau.
 *
 * Même contrainte de pureté que le noyau : aucun import hors `noyau/`,
 * `donnees/` et `scene/`. `tests/scene.test.ts` vérifie l'absence de Phaser.
 */
import type { AssiseId, EspeceId, EtatJeu } from '../noyau/types'
import { eauTroublee, estSature } from '../noyau/economie'
import { PALIERS } from '../donnees/paliers'
import { ESPECES } from '../donnees/especes'
import { assiseDuPalier } from '../donnees/assises'

export interface VueDEspece {
  readonly id: EspeceId
  readonly rang: number
  readonly niveau: number
}

export interface VueDePalier {
  readonly index: number
  readonly assise: AssiseId
  /** 1 à 6. */
  readonly rangDAssise: number
  /** L'espèce débloquée que ce palier porte, ou rien. */
  readonly espece: VueDEspece | null
}

export interface VueDuHeros {
  readonly niveau: number
  /** Facteur de taille du corps — spec [D12]. */
  readonly echelle: number
  readonly couches: readonly AssiseId[]
}

export interface VueDeScene {
  readonly paliers: readonly VueDePalier[]
  readonly heros: VueDuHeros
  readonly eauTroublee: boolean
  readonly sature: boolean
}

/** `1 + 0,25 · log₂(niveau)` : ×1 au niveau 1, ×2 à 16, ×3 à 256. */
export function echelleDuHeros(niveau: number): number {
  return 1 + 0.25 * Math.log2(Math.max(1, niveau))
}

export function vueDeLaScene(etat: EtatJeu): VueDeScene {
  const paliers: VueDePalier[] = []
  for (let index = 0; index < etat.cycle.paliersOuverts; index += 1) {
    const palier = PALIERS[index]
    const assise = assiseDuPalier(index)
    let espece: VueDEspece | null = null
    if (palier.espece !== null) {
      const vivante = etat.cycle.especes[palier.espece]
      const definition = ESPECES.find((e) => e.id === palier.espece)
      if (vivante?.debloquee === true && definition !== undefined) {
        espece = { id: definition.id, rang: definition.rang, niveau: vivante.niveau }
      }
    }
    paliers.push({ index, assise: assise.id, rangDAssise: assise.rang, espece })
  }
  return {
    paliers,
    heros: {
      niveau: etat.cycle.niveauDuHeros,
      echelle: echelleDuHeros(etat.cycle.niveauDuHeros),
      couches: etat.permanent.couches,
    },
    eauTroublee: eauTroublee(etat),
    sature: estSature(etat),
  }
}
```

- [ ] **Step 5 : vérifier**

Run: `npx vitest run tests/scene.test.ts tests/architecture.test.ts tests/canon.test.ts && npx tsc -b && npx eslint .`
Expected: PASS. `canon.test.ts` balaie `src/` entier pour le lexique périmé : ni `layer`, ni `zone` dans ces deux fichiers.

- [ ] **Step 6 : commit**

```bash
git add src/scene/vue.ts src/scene/palette.ts tests/scene.test.ts
git commit -m "feat(scene): la vue pure de la scène, et six palettes qui perdent la lumière"
```

---

### Task C3 : la scène Phaser

**Files:**
- Create: `src/scene/SceneDeLaMare.ts`
- Delete: `src/scene/.gitkeep`

Pas de test unitaire : Phaser exige un canvas. La vérification est en C4, dans le navigateur.

- [ ] **Step 1 : la scène**

Créer `src/scene/SceneDeLaMare.ts` :

```typescript
/**
 * IdlePond — la scène : une coupe verticale, le héros, ses bancs.
 *
 * Elle ne connaît pas l'état du jeu. Elle reçoit une `VueDeScene` par
 * l'événement `vue` du jeu Phaser (émis par `ui/Scene.tsx`) et redessine ce
 * qui a changé. Tout est dessiné au trait — aucun sprite, spec [D10] : ce que
 * la DA remplacera, c'est ce fichier, jamais `vue.ts`.
 *
 * Ce qu'elle montre, et rien d'autre :
 *   - une bande par palier ouvert, colorée par son assise, la lumière qui baisse ;
 *   - dans chaque bande qui porte une espèce, un banc dont l'effectif dessiné
 *     croît avec le logarithme du niveau ;
 *   - le héros, dans la bande la plus basse, à l'échelle de son niveau, avec
 *     une marque par couche ;
 *   - l'eau qui se trouble quand la jauge dépasse l'alerte — un effet, pas un
 *     texte (GDD §2.4).
 */
import Phaser from 'phaser'
import type { VueDePalier, VueDeScene } from './vue'
import { MARQUE_PAR_ASSISE, paletteDe, type MarqueId } from './palette'

export const EVENEMENT_VUE = 'vue'

const HAUTEUR_DE_BANDE = 72
const MARGE = 12
const POISSONS_MAX_PAR_BANC = 14

/** Couleur d'un banc, par rang d'espèce : une teinte qui tourne, une clarté qui baisse. */
function couleurDuBanc(rang: number): number {
  const teinte = (rang * 47) % 360
  const couleur = Phaser.Display.Color.HSVToRGB(teinte / 360, 0.35, 0.85 - Math.min(0.5, rang * 0.02))
  return Phaser.Display.Color.GetColor(couleur.r, couleur.g, couleur.b)
}

export class SceneDeLaMare extends Phaser.Scene {
  private fond!: Phaser.GameObjects.Graphics
  private bancs!: Phaser.GameObjects.Group
  private heros!: Phaser.GameObjects.Container
  private trouble!: Phaser.GameObjects.Rectangle
  private derniereVue: string | null = null

  constructor() {
    super('SceneDeLaMare')
  }

  create() {
    this.fond = this.add.graphics()
    this.bancs = this.add.group()
    this.heros = this.add.container(0, 0)
    this.trouble = this.add
      .rectangle(0, 0, this.scale.width, this.scale.height, 0x8a7a3a, 0)
      .setOrigin(0, 0)
    this.game.events.on(EVENEMENT_VUE, this.appliquer, this)
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => this.game.events.off(EVENEMENT_VUE, this.appliquer, this))
    this.scale.on(Phaser.Scale.Events.RESIZE, () => {
      this.trouble.setSize(this.scale.width, this.scale.height)
      this.derniereVue = null
    })
  }

  /** Redessine si la vue a changé. La vue est petite : la comparer en chaîne coûte moins qu'un redessin. */
  appliquer(vue: VueDeScene) {
    const clef = JSON.stringify(vue)
    if (clef === this.derniereVue) return
    this.derniereVue = clef

    const largeur = this.scale.width
    const hauteurTotale = vue.paliers.length * HAUTEUR_DE_BANDE
    this.cameras.main.setBounds(0, 0, largeur, Math.max(hauteurTotale, this.scale.height))
    this.cameras.main.scrollY = Math.max(0, hauteurTotale - this.scale.height)

    this.dessinerLesBandes(vue.paliers, largeur)
    this.dessinerLesBancs(vue.paliers, largeur)
    this.dessinerLeHeros(vue, largeur)

    this.tweens.add({ targets: this.trouble, alpha: vue.eauTroublee ? 0.28 : 0, duration: 900 })
  }

  private dessinerLesBandes(paliers: readonly VueDePalier[], largeur: number) {
    this.fond.clear()
    paliers.forEach((palier, i) => {
      const palette = paletteDe(palier.assise)
      const y = i * HAUTEUR_DE_BANDE
      this.fond.fillStyle(palette.fond, 1)
      this.fond.fillRect(0, y, largeur, HAUTEUR_DE_BANDE)
      this.fond.fillStyle(palette.eau, 0.55 + 0.4 * palette.lumiere)
      this.fond.fillRect(MARGE, y + 2, largeur - 2 * MARGE, HAUTEUR_DE_BANDE - 4)
      // La lumière : un voile clair qui s'amincit en descendant.
      this.fond.fillStyle(0xdde9e6, 0.12 * palette.lumiere)
      this.fond.fillRect(MARGE, y + 2, largeur - 2 * MARGE, 10)
    })
  }

  private dessinerLesBancs(paliers: readonly VueDePalier[], largeur: number) {
    this.bancs.clear(true, true)
    paliers.forEach((palier, i) => {
      if (palier.espece === null) return
      const effectif = Math.min(POISSONS_MAX_PAR_BANC, 1 + Math.floor(Math.log2(Math.max(1, palier.espece.niveau))))
      const couleur = couleurDuBanc(palier.espece.rang)
      const y0 = i * HAUTEUR_DE_BANDE + HAUTEUR_DE_BANDE / 2
      for (let k = 0; k < effectif; k += 1) {
        const x = MARGE + 30 + ((k * 53) % (largeur - 2 * MARGE - 120))
        const y = y0 + ((k * 17) % 28) - 14
        const poisson = this.add.ellipse(x, y, 14, 7, couleur, 0.9)
        this.bancs.add(poisson)
        this.tweens.add({
          targets: poisson,
          x: x + 18 + (k % 3) * 6,
          duration: 1800 + (k % 5) * 300,
          yoyo: true,
          repeat: -1,
          ease: 'Sine.easeInOut',
        })
      }
    })
  }

  private dessinerLeHeros(vue: VueDeScene, largeur: number) {
    this.heros.removeAll(true)
    const bandeDuBas = Math.max(0, vue.paliers.length - 1)
    const x = largeur - MARGE - 60
    const y = bandeDuBas * HAUTEUR_DE_BANDE + HAUTEUR_DE_BANDE / 2
    this.heros.setPosition(x, y)
    this.heros.setScale(vue.heros.echelle)

    const corps = this.add.graphics()
    corps.fillStyle(0xb9c7c2, 1)
    corps.fillEllipse(0, 0, 34, 16)
    corps.fillTriangle(-16, 0, -28, -8, -28, 8)
    corps.fillStyle(0x1b2422, 1)
    corps.fillCircle(9, -2, 1.6)
    this.heros.add(corps)

    for (const assise of vue.heros.couches) {
      this.heros.add(this.marque(MARQUE_PAR_ASSISE[assise]))
    }

    this.tweens.add({ targets: this.heros, y: y - 4, duration: 2200, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' })
  }

  /** Une marque du corps — GDD §15.1. Des primitives, jusqu'à la DA. */
  private marque(id: MarqueId): Phaser.GameObjects.Graphics {
    const g = this.add.graphics()
    switch (id) {
      case 'branchies':
        g.lineStyle(1.2, 0x5d7a74, 1)
        for (let i = 0; i < 3; i += 1) g.lineBetween(2 + i * 2.5, -5, 2 + i * 2.5, 5)
        break
      case 'membranes':
        g.fillStyle(0x7fb3a8, 0.55)
        g.fillTriangle(-4, -7, 6, -12, 10, -6)
        g.fillTriangle(-4, 7, 6, 12, 10, 6)
        break
      case 'luminescence':
        g.fillStyle(0x9ef0e0, 0.5)
        g.fillCircle(-6, 0, 5)
        g.fillStyle(0xd6fff7, 0.9)
        g.fillCircle(-6, 0, 2)
        break
      case 'mineralisation':
        g.fillStyle(0x8c8f7a, 1)
        for (let i = 0; i < 5; i += 1) g.fillRect(-12 + i * 5, -8 + (i % 2) * 2, 2, 2)
        break
      case 'epaississement':
        g.lineStyle(2.5, 0x8a9a95, 0.9)
        g.strokeEllipse(0, 0, 36, 18)
        break
      case 'halo':
        g.lineStyle(1, 0xf1e4a8, 0.7)
        g.strokeCircle(0, 0, 24)
        break
    }
    return g
  }
}
```

- [ ] **Step 2 : vérifier la compilation et le lexique**

Run: `npx tsc -b && npx eslint . && npx vitest run tests/canon.test.ts tests/architecture.test.ts`
Expected: propres. Le balayage lexical de `canon.test.ts` porte sur `src/` entier ; ce fichier n'emploie ni `layer`, ni `zone`, ni `biome`.

Supprimer `src/scene/.gitkeep`.

- [ ] **Step 3 : commit**

```bash
git rm src/scene/.gitkeep
git add src/scene/SceneDeLaMare.ts
git commit -m "feat(scene): la coupe verticale, le héros et ses bancs, au trait"
```

---

### Task C4 : monter la scène dans React

**Files:**
- Create: `src/ui/Scene.tsx`
- Modify: `src/ui/App.tsx`

- [ ] **Step 1 : le composant**

Créer `src/ui/Scene.tsx` :

```tsx
/**
 * Monte Phaser dans React et lui pousse la vue.
 *
 * Un seul `Phaser.Game` par montage, détruit au démontage. La vue est
 * recalculée à chaque changement d'état — dix fois par seconde — et c'est la
 * scène qui décide si elle redessine (elle compare la vue sérialisée). Ce
 * composant ne contient aucune règle de jeu : `vueDeLaScene` est pure.
 */
import { useEffect, useRef } from 'react'
import Phaser from 'phaser'
import type { EtatJeu } from '../noyau/types'
import { vueDeLaScene } from '../scene/vue'
import { EVENEMENT_VUE, SceneDeLaMare } from '../scene/SceneDeLaMare'

export function Scene({ etat }: { readonly etat: EtatJeu }) {
  const conteneur = useRef<HTMLDivElement>(null)
  const jeu = useRef<Phaser.Game | null>(null)

  useEffect(() => {
    if (conteneur.current === null || jeu.current !== null) return
    jeu.current = new Phaser.Game({
      type: Phaser.AUTO,
      parent: conteneur.current,
      transparent: true,
      scale: { mode: Phaser.Scale.RESIZE, width: '100%', height: '100%' },
      scene: [SceneDeLaMare],
    })
    return () => {
      jeu.current?.destroy(true)
      jeu.current = null
    }
  }, [])

  useEffect(() => {
    jeu.current?.events.emit(EVENEMENT_VUE, vueDeLaScene(etat))
  }, [etat])

  return <div ref={conteneur} className="h-72 w-full overflow-hidden rounded-lg border border-eau-bord bg-eau-abysse" />
}
```

La première émission peut précéder `create()` de la scène : la scène ne s'abonne qu'à `create`, donc elle manque cette vue et attend la suivante, 100 ms plus tard. Acceptable ; si un écran vide d'une seconde gêne, la scène peut relire `this.game.registry` — non requis ici.

- [ ] **Step 2 : brancher**

`src/ui/App.tsx` : importer `Scene` ; dans `<main>`, en tout premier, avant `<Contenance etat={etat} />` :

```tsx
          <Scene etat={etat} />
```

- [ ] **Step 3 : vérifier dans le navigateur**

Run: `npx tsc -b && npx eslint . && npm run build && npx vitest run`
Expected: propres, suite verte.

`npm run dev`, profil vierge. Attendu, dans l'ordre :
1. Une bande verte-grise (la Noue), un petit poisson clair à droite : le héros au niveau 1.
2. Convaincre le vairon : un banc d'une ellipse apparaît dans la bande. Monter à 2, 4, 8 crans : 2, 3, 4 poissons.
3. Grandir plusieurs fois : le héros grossit visiblement.
4. Creuser : une deuxième bande, plus sombre, et le héros descend dans la plus basse.
5. Laisser la jauge dépasser 85 % : un voile limoneux monte sur la scène ; il s'efface en dépensant.
6. Rentrer dans l'œuf : une seule bande à nouveau, héros petit, avec trois traits de branchies.

Prendre une capture à l'étape 4 et à l'étape 6 pour le rapport de tâche. Vérifier la console : zéro erreur.

- [ ] **Step 4 : commit**

```bash
git add src/ui/Scene.tsx src/ui/App.tsx
git commit -m "feat(ui): la scène dans l'écran — on voit le héros grandir, descendre, se marquer"
```

---

# Partie D — le canon et les documents

### Task D1 : l'amendement v1.4, la préséance, le README

**Files:**
- Modify: `docs/amendement-v1.1.md`
- Modify: `docs/PRESEANCE.md`
- Modify: `README.md`
- Modify: `src/noyau/constantes.ts` (commentaire de `NOMBRE_D_ECLOSIONS_VISE` si non fait en A5/B4)

- [ ] **Step 1 : l'amendement**

Dans `docs/amendement-v1.1.md`, après la sous-section « Décidé le 2026-09-16 — la descente n'est pas toute la partie » (avant `### 2.C`), ajouter :

```markdown
#### Amendé le 2026-09-17 (v1.4) — le héros grandit, la Foi bénit, la scène montre

Spec : `docs/superpowers/specs/2026-09-17-axe-heros-benedictions-scene-design.md`.

**Le constat.** Après la refonte du 8 au 16 septembre, le joueur disposait de
trois achats et d'une décision, aucun sur le héros, aucune image ; et passé le
cycle 13, plus aucun chiffre ne montait — la Foi n'achetait rien, la technique
ne monte jamais la production. Un idle sans chiffre qui monte n'est plus un
idle.

**Quatre achats, pas trois** (noyau v1.0 §1.2 amendé). *Grandir* : le héros
monte de niveau en mana, `coût = COUT_CREUSER_AU_PALIER_1 × 0,75 × g^(n−1)`,
effet `multiplicateur_heros = 1,15^(n−1)` global, plus son débit propre × n.
Le niveau se reperd à l'éclosion. **`D` par palier est inchangé** : le
multiplicateur de profondeur est divisé par 1,15, et le test de canon porte
sur le produit des deux.

**Les bénédictions sont de retour** (noyau v1.0 §4, par la préséance de
`PRESEANCE.md` — le GDD §4.2 est dépassé sur ce point). Ciblée :
`× 1,5^rang` sur une espèce. Globale : `+ 0,05 × rang` sur le débit de base
de toutes. Coût en Foi `3 × 4^rang` et `2 × 4^rang`. **Achetables à tout
moment**, et non « dans l'œuf » : un état où rien ne produit pénaliserait
l'absence. La Foi n'est de toute façon créditée qu'en rentrant dans l'œuf.

**`couches` est enfin écrit** : une marque par assise traversée dans la vie,
à l'éclosion, dans l'ordre des assises. Visuelle, sans effet chiffré.

**Mesuré après** : <recopier la table de `NOMBRE_D_ECLOSIONS_VISE` : cycle 1,
cycle 5, cycle 13, total actif ; et le niveau maximal du héros au cycle 1>.

Toutes les valeurs ci-dessus sont des graines `[P]`, dans `constantes.ts`.
```

Remplir la ligne « Mesuré après » avec les chiffres relevés en A5 et B4.

- [ ] **Step 2 : la préséance**

Dans `docs/PRESEANCE.md`, après le paragraphe « Sections du GDD dépassées par le noyau v1.0 », ajouter :

```markdown
**Ajouté le 2026-09-17.** GDD §4.2 (« la Foi n'achète que des miracles ») est
dépassé par le noyau v1.0 §4 : la Foi achète des **bénédictions**, permanentes,
qui montent la production. Les miracles restent gelés (`[P26]`). Le noyau v1.0
§1.2 compte désormais **quatre** achats — voir `docs/amendement-v1.1.md`, v1.4.
```

- [ ] **Step 3 : le README**

Dans `README.md`, section « État » : remplacer la phrase « Sans technique ni bénédictions : c'est le jalon v0.4. » par :

```markdown
Depuis le 2026-09-17 : le héros **grandit** (quatrième achat, en mana), la Foi
achète des **bénédictions** permanentes, et une **scène** dessinée au trait
montre le héros, ses marques et ses bancs. Sans technique : c'est le jalon
v0.4.
```

Et dans « Le contrat », règle 3, remplacer « la bénédiction monte la **production** » par « la bénédiction monte la **production**, et c'est la seule chose qu'elle fait ». Mettre à jour le compte de tests dans le bloc de commandes (`npm test # N tests`) avec le chiffre réel de `npx vitest run`.

- [ ] **Step 4 : vérifier**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: tout vert, tout propre.

- [ ] **Step 5 : commit**

```bash
git add docs/amendement-v1.1.md docs/PRESEANCE.md README.md src/noyau/constantes.ts
git commit -m "docs(canon): amendement v1.4 — quatre achats, les bénédictions, la scène"
```

---

## Self-review

**Couverture du spec.** [D1] → A1, A3. [D2] → A2. [D3] → A1, A2, A5. [D4] → aucune tâche, par construction (pas de seuils écrits). [D5] → B1, B2. [D6] → B1, B2. [D7] → B5, D1. [D8] → B1, D1. [D9] → C2, C4. [D10] → C3. [D11] → C1, C2, C3. [D12] → C2. §4 hors périmètre : rien ne touche `PALIERS_LIVRES`, `noeuds-technique.ts`, ni les chapitres. §5 risques : A5 Step 5 et B4 Step 4 portent les ajustements.

**Cohérence des noms.** `niveauDuHeros`, `grandir`, `coutDeCroissance(etat, niveau)`, `multiplicateurDuHeros`, `detailDuHeros(etat)` — identiques de A1 à C2. `benir(etat, id)`, `coutDeBenediction(etat, benediction)`, `rangDeBenediction(etat, id)`, `debitBeni(etat, espece)`, `multiplicateurDeBenediction(etat, espece)`, `BENEDICTION_GLOBALE_ID`, `benedictionCibleeDe(espece)` — identiques de B1 à B5. `vueDeLaScene`, `echelleDuHeros`, `EVENEMENT_VUE`, `paletteDe`, `MARQUE_PAR_ASSISE` — identiques de C2 à C4. `EtatPermanent.benedictions` est ajouté en A4 (la migration en a besoin) et lu dès B1.

**Ordre de dépendance.** A4 précède B1 (le champ et la version). C1 précède C2 (la vue lit `couches`, qui existait déjà mais restait vide). C2 précède C3 et C4. D1 en dernier, avec les chiffres.
