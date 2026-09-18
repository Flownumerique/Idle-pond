# Le renommage transverse — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Appliquer le lexique du Codex au code — la Foi devient le Souffle, les bénédictions deviennent les insufflations, l'éclosion-mécanique devient la renaissance — identifiants, textes affichés et format de sauvegarde compris.

**Architecture:** Cinq tâches. Trois renomment un concept chacune, à travers tout le dépôt d'un seul tenant, parce que TypeScript rend un renommage atomique : on ne peut pas renommer `EtatPermanent.foi` sans corriger le même coup tous ses lecteurs. La quatrième écrit la migration `v7 → v8` en une fois, avec la connaissance des trois renommages. La cinquième retourne le balayage lexical du canon et réécrit les textes et les documents.

**Tech Stack:** TypeScript 5.9 strict, Vitest 3, Vite 8, React 19, Phaser 3.90, `break_infinity.js` (`Decimal`), zustand 5, Tailwind 4.

**Spec:** `docs/CODEX.md` — §5 (le lexique intégral) donne chaque cible de renommage ; §7 (les exceptions figées) donne ce qui ne bouge pas. Lire les deux avant de commencer.

## Global Constraints

- Le noyau est pur : aucun `Date.now()`, `Math.random`, DOM, ni import hors `noyau/` et `donnees/` dans `src/noyau/` et `src/donnees/`. `tests/architecture.test.ts` reste vert à chaque tâche.
- **Tout se calcule en un pas pour `dt = 8 h`.** `tests/equivalence-de-pas.test.ts` reste vert. Aucune tâche ici ne touche à une formule : c'est un renommage, la mécanique ne bouge pas d'un chiffre.
- Une migration de save **ne supprime jamais un champ** — elle en ajoute, elle en renomme, elle ne retire rien d'utile.
- **`D` par palier ne bouge pas.** `tests/canon.test.ts` le verrouille. Aucune tâche ici ne touche `multiplicateurDePalier()`.
- Code, commentaires, messages de commit en français.
- Les titres de test qui contiennent une apostrophe l'écrivent **courbe** (`’`, U+2019) à l'intérieur d'une chaîne à guillemets simples. C'est la convention établie du dépôt, vérifiée à chaque tâche des plans précédents.

### Trois pièges, à lire avant la première ligne

**1. `fois` et `uneFois` contiennent « foi ».** Un remplacement global `foi → souffle` casserait « grandi 3 fois », `tour()` et ses appels. **Ne jamais faire de remplacement non ancré.** Renommer identifiant par identifiant, avec les frontières de mot.

**2. Les migrations 1 à 6 gardent l'ancien vocabulaire.** `src/adaptateurs/persistance.ts` lit des sauvegardes v1 à v7 qui contiennent littéralement `foi`, `benedictions`, `nombreEclosions`. `MIGRATIONS[2]` fait même `delete reste.benedictions`. **Ces entrées ne se renomment pas** — elles décriraient un format qui n'a jamais existé. `canon.test.ts` exempte déjà ce fichier du balayage des mots morts, pour exactement cette raison.

**3. Trois identifiants de succès portent `eclosion` et ne bougent pas** (Codex §7) :
`franchissement-premiere-eclosion`, `franchissement-deuxieme-eclosion`, `franchissement-troisieme-eclosion`. Le registre des succès est immuable au canon et ces identifiants sont dans les sauvegardes des joueurs.

### Une fenêtre de développement, assumée

Entre la tâche 1 et la tâche 4, `VERSION_SAVE` vaut encore 7 pendant que les champs d'état changent de nom : une sauvegarde existante se recharge donc **avec des valeurs par défaut** sur les champs renommés. C'est voulu — la migration est écrite une seule fois, en tâche 4, avec la vue complète. **Ne pas jouer sérieusement sur ce dépôt pendant le chantier.**

---

## File Structure

**Renommés (fichiers) :**

| Avant | Après | Tâche |
|---|---|---|
| `src/donnees/benedictions.ts` | `src/donnees/insufflations.ts` | 2 |
| `src/ui/Benedictions.tsx` | `src/ui/Insufflations.tsx` | 2 |
| `tests/benedictions.test.ts` | `tests/insufflations.test.ts` | 2 |
| `src/noyau/eclosion.ts` | `src/noyau/renaissance.ts` | 3 |
| `src/ui/Eclosion.tsx` | `src/ui/Renaissance.tsx` | 3 |
| `tests/eclosion.test.ts` | `tests/renaissance.test.ts` | 3 |

**Modifiés, à toutes les tâches :** `src/noyau/types.ts`, `src/noyau/constantes.ts`, `src/noyau/economie.ts`, `src/noyau/noyau.ts`, `src/simulateur/simulateur.ts`, `src/etat/magasin.ts`, `src/ui/App.tsx`, `src/ui/format.ts`, et les tests qui les touchent.

**Modifié une seule fois :** `src/adaptateurs/persistance.ts` (tâche 4 — la migration et les deux fonctions de (dé)sérialisation), `tests/canon.test.ts` (tâche 5), `src/index.css` (tâche 5).

---

# Task 1 : le Souffle

**Files:**
- Modify: `src/noyau/types.ts`, `src/noyau/constantes.ts`, `src/noyau/economie.ts`, `src/noyau/eclosion.ts`, `src/noyau/noyau.ts`, `src/noyau/succes.ts`, `src/donnees/echelles.ts`, `src/donnees/succes/actes.ts`, `src/simulateur/simulateur.ts`, `src/adaptateurs/telemetrie.ts`, `src/adaptateurs/persistance.ts` (lignes de (dé)sérialisation **seulement**), `src/ui/App.tsx`, `src/ui/Eclosion.tsx`, `src/ui/Heros.tsx`, `src/ui/Scene.tsx`, `src/ui/format.ts`, `src/ui/Benedictions.tsx`
- Modify (tests): `tests/persistance.test.ts`, `tests/benedictions.test.ts`, `tests/eclosion.test.ts`, `tests/simulateur.test.ts`, `tests/etat-de-travail.ts`, et tout test que `tsc` signalera

**Interfaces:**
- Produces: `EtatPermanent.souffle`, `MesureDeCycle.souffleGagne`, `SOUFFLE_BASE`, `SOUFFLE_EXPOSANT`, `SOUFFLE_COUT_DE_BENEDICTION_CIBLEE`, `SOUFFLE_COUT_DE_BENEDICTION_GLOBALE`, `gainDeSoufflePrevu()`

- [ ] **Step 1 : la table de renommage**

Appliquer exactement ces correspondances, **avec frontières de mot**. Rien d'autre ne change.

| Avant | Après |
|---|---|
| `EtatPermanent.foi` | `EtatPermanent.souffle` |
| `MesureDeCycle.foiGagnee` | `MesureDeCycle.souffleGagne` |
| `FOI_BASE` | `SOUFFLE_BASE` |
| `FOI_EXPOSANT` | `SOUFFLE_EXPOSANT` |
| `FOI_COUT_DE_BENEDICTION_CIBLEE` | `SOUFFLE_COUT_DE_BENEDICTION_CIBLEE` |
| `FOI_COUT_DE_BENEDICTION_GLOBALE` | `SOUFFLE_COUT_DE_BENEDICTION_GLOBALE` |
| `gainDeFoiPrevu` | `gainDeSoufflePrevu` |

Les deux constantes qui portent encore `BENEDICTION` sont renommées une seconde fois en
tâche 2. C'est voulu : chaque tâche laisse un état qui compile.

**NE PAS TOUCHER** : `fois`, `uneFois`, et les entrées `MIGRATIONS[0]` à `MIGRATIONS[6]`.
Dans `persistance.ts`, seules les lignes de `serialiser` et `deserialiser` changent :

```typescript
// serialiser(), dans permanent :
        souffle: serialiserDecimal(etat.permanent.souffle),
// serialiser(), dans telemetrie.cycles.map :
          souffleGagne: serialiserDecimal(c.souffleGagne),
// deserialiser(), dans permanent :
        souffle: deserialiserDecimal(permanent.souffle, repli.permanent.souffle),
// deserialiser(), dans telemetrie.cycles.map :
        souffleGagne: deserialiserDecimal(c.souffleGagne, new Decimal(0)),
```

- [ ] **Step 2 : les textes affichés qui disent « Foi »**

Trois endroits, et ils sont les seuls à écrire le mot au joueur :

`src/ui/App.tsx` — la ligne du bandeau :
```tsx
            <dt className="text-jour-tu">Souffle</dt>
            <dd className="font-chiffre text-foi tabular-nums">{montant(etat.permanent.souffle)}</dd>
```

`src/ui/Eclosion.tsx` — deux phrases :
```tsx
          <dt>Souffle que le vivant a laissé</dt>
```
```tsx
        Rester plus longtemps fait monter le Souffle. Partir maintenant fait descendre plus bas.
```

La classe Tailwind `text-foi` / `border-foi` ne change **pas ici** — c'est une couleur,
elle est renommée en tâche 5 avec `src/index.css`.

- [ ] **Step 3 : vérifier que tout compile et que la suite est verte**

Run: `npx tsc -b && npx eslint . && npx vitest run`
Expected: `tsc` et `eslint` propres. La suite montre **196 verts sur 197** — le rouge est
`plancher-de-cadence.test.ts`, parqué au canon et hors périmètre. Tout autre rouge est une
faute de renommage : `tsc` l'aura déjà dit.

Run: `grep -rn "\bfoi\b\|\bFoi\b\|foiGagnee\|FOI_" src/ tests/ --include="*.ts" --include="*.tsx" | grep -v "adaptateurs/persistance.ts"`
Expected: aucune ligne. Si une sort, c'est un identifiant oublié.

- [ ] **Step 4 : commit**

```bash
git add -A
git commit -m "refactor(lexique): la Foi devient le Souffle

Codex §5. Champs d'état, constantes, fonction de gain, textes affichés.
Les migrations 1 à 6 de persistance.ts gardent « foi » : elles lisent des
sauvegardes qui l'employaient. La migration v7 → v8 arrive en tâche 4."
```

---

# Task 2 : les insufflations

**Files:**
- Rename: `src/donnees/benedictions.ts` → `src/donnees/insufflations.ts`, `src/ui/Benedictions.tsx` → `src/ui/Insufflations.tsx`, `tests/benedictions.test.ts` → `tests/insufflations.test.ts`
- Modify: `src/noyau/types.ts`, `src/noyau/constantes.ts`, `src/noyau/economie.ts`, `src/noyau/noyau.ts`, `src/donnees/noeuds-technique.ts`, `src/donnees/textes-provisoires.ts`, `src/simulateur/simulateur.ts`, `src/etat/magasin.ts`, `src/ui/App.tsx`, `src/ui/format.ts`, `src/adaptateurs/persistance.ts` ((dé)sérialisation seulement)
- Modify (tests): `tests/canon.test.ts`, `tests/simulateur.test.ts`, et tout test signalé par `tsc`

**Interfaces:**
- Consumes: tâche 1 (`SOUFFLE_COUT_DE_BENEDICTION_*`, renommés ici une seconde fois)
- Produces: `insuffler()`, `Insufflation`, `InsufflationId`, `PorteeDInsufflation`, `INSUFFLATIONS`, `INSUFFLATION_GLOBALE_ID`, `insufflationParId()`, `insufflationCibleeDe()`, `coutDInsufflation()`, `rangDInsufflation()`, `multiplicateurDInsufflation()`, `debitInsuffle()`, `insufflerAuMieux()`, `EtatPermanent.insufflations`

- [ ] **Step 1 : la table de renommage**

| Avant | Après |
|---|---|
| `benir` | `insuffler` |
| `Benediction` | `Insufflation` |
| `BenedictionId` | `InsufflationId` |
| `PorteeDeBenediction` | `PorteeDInsufflation` |
| `BENEDICTIONS` | `INSUFFLATIONS` |
| `BENEDICTION_GLOBALE_ID` | `INSUFFLATION_GLOBALE_ID` |
| `benedictionParId` | `insufflationParId` |
| `benedictionCibleeDe` | `insufflationCibleeDe` |
| `BENEDICTION_CIBLEE_PAR_RANG` | `INSUFFLATION_CIBLEE_PAR_RANG` |
| `BENEDICTION_GLOBALE_PAR_RANG` | `INSUFFLATION_GLOBALE_PAR_RANG` |
| `SOUFFLE_COUT_DE_BENEDICTION_CIBLEE` | `SOUFFLE_COUT_D_INSUFFLATION_CIBLEE` |
| `SOUFFLE_COUT_DE_BENEDICTION_GLOBALE` | `SOUFFLE_COUT_D_INSUFFLATION_GLOBALE` |
| `RATIO_COUT_DE_BENEDICTION` | `RATIO_COUT_D_INSUFFLATION` |
| `coutDeBenediction` | `coutDInsufflation` |
| `rangDeBenediction` | `rangDInsufflation` |
| `multiplicateurDeBenediction` | `multiplicateurDInsufflation` |
| `debitBeni` | `debitInsuffle` |
| `benirAuMieux` | `insufflerAuMieux` |
| `EtatPermanent.benedictions` | `EtatPermanent.insufflations` |
| `surBenediction` | `surInsufflation` |
| `TEXTE_DE_LA_BENEDICTION_GLOBALE` | `TEXTE_DE_L_INSUFFLATION_GLOBALE` |
| `TEXTE_DE_BENEDICTION_CIBLEE` | `TEXTE_D_INSUFFLATION_CIBLEE` |
| terme `'multiplicateur_benediction'` | `'multiplicateur_insufflation'` |
| terme `'benediction_globale'` | `'insufflation_globale'` |
| terme `'cout_benediction'` | `'cout_insufflation'` |

La forme `D` pour « d' » suit la convention du dépôt (`debitBaseDeLEspece`, `nomDeLEspece`).

- [ ] **Step 2 : les identifiants de donnée, qui sont des clefs de sauvegarde**

Dans `src/donnees/insufflations.ts` :

```typescript
export const INSUFFLATION_GLOBALE_ID = 'insufflation-globale'
```
```typescript
    id: `insufflation-${espece.id}`,
```

**Ce sont les clefs du registre `permanent.insufflations`.** Une sauvegarde existante les
porte sous l'ancienne forme (`benediction-vairon`) : la tâche 4 les réécrit. Noter ici
qu'elles changent, pour que la migration n'oublie pas.

- [ ] **Step 3 : les textes affichés**

`src/donnees/textes-provisoires.ts` — le commentaire et les deux blocs :

```typescript
/**
 * Ce que l'écran dit d'une insufflation — un verbe, ce que ça fait. La globale
 * a un nom à elle ; une ciblée prend le nom de son espèce.
 */
export const TEXTE_DE_L_INSUFFLATION_GLOBALE = {
  nom: 'Insuffler l’eau',
  effet: 'tout ce qui vit ici capte un peu plus, et tout ce qui viendra',
} as const

export const TEXTE_D_INSUFFLATION_CIBLEE = {
  effet: 'ils te donnent moitié plus, à chaque fois',
} as const
```

`src/ui/Insufflations.tsx` — le titre et le bouton :

```tsx
        <h2 className="font-texte text-lg text-jour-doux">Ce que tu insuffles</h2>
```
```tsx
        <span className="font-chiffre text-sm text-foi tabular-nums">{montant(souffle)} de Souffle</span>
```
```tsx
          const nom = b.id === INSUFFLATION_GLOBALE_ID ? TEXTE_DE_L_INSUFFLATION_GLOBALE.nom : `Insuffler ${nomDeLEspece(b.espece as string)}`
```
```tsx
                Insuffler
                <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(prix)} de Souffle</span>
```

- [ ] **Step 4 : le test de canon qui nomme les termes**

`tests/canon.test.ts` porte un `describe` sur la frontière technique/bénédiction et deux
tests qui citent les termes. Renommer le `describe` et les termes cités :

```typescript
describe("noyau v1.0 §4 — le Souffle achète des insufflations, et rien d'autre ne monte la production", () => {
```

et dans le test des termes :

```typescript
      const terme = insufflation.portee === 'ciblee' ? 'multiplicateur_insufflation' : 'insufflation_globale'
```

Lire le fichier avant d'éditer : les titres y sont à guillemets **doubles** avec apostrophe
droite, contrairement au reste du dépôt. Suivre la convention locale du fichier.

- [ ] **Step 5 : vérifier**

Run: `npx tsc -b && npx eslint . && npx vitest run`
Expected: propres, 196/197.

Run: `grep -rniE "b[ée]n[ée]diction|b[ée]nir|debitBeni" src/ tests/ --include="*.ts" --include="*.tsx" | grep -v "adaptateurs/persistance.ts"`
Expected: aucune ligne.

- [ ] **Step 6 : commit**

```bash
git add -A
git commit -m "refactor(lexique): les bénédictions deviennent les insufflations

Codex §5. Types, registre, économie, politique de simulateur, écran, textes.
Les identifiants de donnée passent de « benediction-* » à « insufflation-* » :
ce sont des clefs de sauvegarde, la tâche 4 les réécrit."
```

---

# Task 3 : la renaissance

**Files:**
- Rename: `src/noyau/eclosion.ts` → `src/noyau/renaissance.ts`, `src/ui/Eclosion.tsx` → `src/ui/Renaissance.tsx`, `tests/eclosion.test.ts` → `tests/renaissance.test.ts`
- Modify: `src/noyau/types.ts`, `src/noyau/constantes.ts`, `src/noyau/noyau.ts`, `src/noyau/economie.ts`, `src/noyau/succes.ts`, `src/simulateur/simulateur.ts`, `src/etat/magasin.ts`, `src/ui/App.tsx`, `src/adaptateurs/persistance.ts` ((dé)sérialisation seulement)
- Modify (tests): tout test signalé par `tsc`

**Interfaces:**
- Produces: `renaitre()`, `EtatPermanent.nombreRenaissances`, `NOMBRE_DE_RENAISSANCES_VISE`, `CONTENANCE_PAR_RENAISSANCE`, `doitRenaitre()`, `Politique.fractionDeSaturationPourRenaitre`

- [ ] **Step 1 : la table de renommage**

| Avant | Après |
|---|---|
| `eclore` | `renaitre` |
| `EtatPermanent.nombreEclosions` | `EtatPermanent.nombreRenaissances` |
| `NOMBRE_D_ECLOSIONS_VISE` | `NOMBRE_DE_RENAISSANCES_VISE` |
| `CONTENANCE_PAR_ECLOSION` | `CONTENANCE_PAR_RENAISSANCE` |
| `doitEclore` | `doitRenaitre` |
| `fractionDeSaturationPourEclore` | `fractionDeSaturationPourRenaitre` |
| `surEclosion` | `surRenaissance` |
| composant `Eclosion` | composant `Renaissance` |

**Ce qui ne bouge PAS :**

- Les trois identifiants de succès `franchissement-*-eclosion` (Codex §7). Leurs clefs dans
  `TEXTES_DE_SUCCES`, leur type `SuccesId`, leur entrée dans `src/donnees/succes/franchissements.ts`.
- Le mot « éclosion » dans la prose du commentaire de `src/noyau/renaissance.ts` **quand il
  parle de la naissance du héros** — Codex §7 : le mot est rétréci, pas banni.

- [ ] **Step 2 : l'en-tête du module renommé**

`src/noyau/renaissance.ts` porte encore un en-tête qui se contredit (« Le héros ENTRE dans
l'œuf. Jamais "ponte" »). Le réécrire :

```typescript
/**
 * IdlePond — la renaissance.
 *
 * Le héros rentre dans son œuf et se refait. Il ne pond pas, il n'est pas
 * réincarné : c'est le même corps qui ressort, avec une couche de plus. Codex §4.
 *
 * §6.5, et rien de plus :
 *   f = 1 — reset complet du peuplement et de la géométrie, aucune fraction
 *           conservée.
 *   Conservé : densité, arbre de technique, succès, couches, contenance, et le
 *              drapeau des cent — l'unique exception.
 *   Perdu    : espèces débloquées et leurs niveaux, paliers ouverts, mana
 *              courant.
 *   Le mana expire vers l'ambiant — il n'est pas détruit (Tier 0 §5).
 */
```

- [ ] **Step 3 : la note périmée de `types.ts`**

L'en-tête de `src/noyau/types.ts` dit encore que `bénédiction` a disparu et que le
renommage est « en attente ». C'est ce chantier qui le solde. Remplacer ce paragraphe par :

```typescript
 * Le lexique est celui du Codex v1.0 (2026-09-18) : le Souffle, les
 * insufflations, la renaissance. Les trois identifiants de succès en
 * `eclosion` sont figés au canon et font exception (Codex §7).
```

- [ ] **Step 4 : vérifier**

Run: `npx tsc -b && npx eslint . && npx vitest run`
Expected: propres, 196/197.

Run: `grep -rniE "\beclore\b|\béclore\b|nombreEclosions|ECLOSIONS_VISE|PAR_ECLOSION|surEclosion|doitEclore" src/ tests/ --include="*.ts" --include="*.tsx"`
Expected: aucune ligne.

Run: `grep -rn "franchissement-premiere-eclosion" src/ | head -3`
Expected: **au moins une ligne** — la preuve que l'exception figée a bien été respectée.

- [ ] **Step 5 : commit**

```bash
git add -A
git commit -m "refactor(lexique): l'éclosion-mécanique devient la renaissance

Codex §4 et §5. Le héros ne pond pas et n'est pas réincarné : il se refait.
« Éclosion » garde son sens de naissance, à l'acte I, et les trois
identifiants de succès qui le portent restent figés (Codex §7)."
```

---

# Task 4 : la migration v7 → v8

**Files:**
- Modify: `src/noyau/constantes.ts` (`VERSION_SAVE`), `src/adaptateurs/persistance.ts` (`MIGRATIONS[7]`)
- Modify: `tests/persistance.test.ts`

**Interfaces:**
- Consumes: les trois tâches précédentes — les champs cibles s'appellent déjà `souffle`, `insufflations`, `nombreRenaissances`, `souffleGagne`
- Produces: `MIGRATIONS[7]`, `VERSION_SAVE = 8`

- [ ] **Step 1 : les tests**

Dans `tests/persistance.test.ts`, remplacer le test de chaîne de versions :

```typescript
  it('la save porte la version courante après migration — 4 → 5, 5 → 6, 6 → 7, puis 7 → 8', () => {
    const migre = deserialiser({ versionSave: 4, contenu: {} } as unknown as SaveSerialisee, etatInitial(0))
    expect(migre.versionSave).toBe(8)
  })
```

Et ajouter un `describe` :

```typescript
describe('migration 7 → 8 : le lexique du Codex entre dans les sauvegardes', () => {
  it('une save v7 se réveille avec le Souffle, les insufflations et les renaissances', () => {
    const v7 = {
      versionSave: 7,
      contenu: {
        cycle: { manaCourant: '500', paliersOuverts: 4 },
        permanent: {
          foi: '1600',
          benedictions: { 'benediction-globale': 3, 'benediction-vairon': 2 },
          nombreEclosions: 5,
          densites: [1, 1, 1, 1, 1, 1],
        },
        telemetrie: { cycles: [{ index: 0, foiGagnee: '40' }] },
      },
    } as unknown as SaveSerialisee
    const relu = deserialiser(v7, etatInitial(1))
    expect(relu.permanent.souffle.eq(1600)).toBe(true)
    expect(relu.permanent.nombreRenaissances).toBe(5)
    expect(relu.permanent.insufflations).toEqual({
      'insufflation-globale': 3,
      'insufflation-vairon': 2,
    })
  })

  it('la migration ne perd aucun champ qu’elle ne connaît pas', () => {
    const migre = MIGRATIONS[7]({
      permanent: { foi: '2', benedictions: {}, nombreEclosions: 1, unChampInconnu: true },
      telemetrie: { cycles: [] },
    }) as Record<string, Record<string, unknown>>
    expect(migre.permanent).toHaveProperty('souffle', '2')
    expect(migre.permanent).toHaveProperty('unChampInconnu', true)
    expect(migre.permanent).not.toHaveProperty('foi')
  })
})
```

- [ ] **Step 2 : vérifier qu'ils échouent**

Run: `npx vitest run tests/persistance.test.ts`
Expected: FAIL — `MIGRATIONS[7]` est `undefined`, la version vaut 7.

- [ ] **Step 3 : l'implémentation**

`src/noyau/constantes.ts` : `export const VERSION_SAVE = 8`.

`src/adaptateurs/persistance.ts`, dans `MIGRATIONS`, après l'entrée `6` :

```typescript
  /**
   * 7 → 8 — le lexique du Codex v1.0 (2026-09-18).
   *
   * Trois champs changent de nom, aucun ne disparaît : `foi` devient
   * `souffle`, `benedictions` devient `insufflations`, `nombreEclosions`
   * devient `nombreRenaissances`. Les clefs du registre sont réécrites aussi
   * (`benediction-vairon` → `insufflation-vairon`) : ce sont des identifiants
   * de donnée, pas des noms d'espèce.
   *
   * Les entrées 1 à 6 gardent l'ancien vocabulaire, et c'est normal : elles
   * lisent des sauvegardes qui l'employaient. Ce fichier est le seul du dépôt
   * qui a le droit de connaître les mots morts.
   */
  7: (contenu) => {
    const brut = (contenu ?? {}) as Record<string, unknown>
    const permanent = (brut.permanent ?? {}) as Record<string, unknown>
    const telemetrie = (brut.telemetrie ?? {}) as Record<string, unknown>
    const { foi, benedictions, nombreEclosions, ...restePermanent } = permanent

    const anciennes = (benedictions ?? {}) as Record<string, number>
    const insufflations: Record<string, number> = {}
    for (const [clef, rang] of Object.entries(anciennes)) {
      insufflations[clef.replace(/^benediction-/, 'insufflation-')] = rang
    }

    const cycles = Array.isArray(telemetrie.cycles) ? telemetrie.cycles : []

    return {
      ...brut,
      permanent: {
        ...restePermanent,
        souffle: foi ?? '0',
        insufflations,
        nombreRenaissances: nombreEclosions ?? 0,
      },
      telemetrie: {
        ...telemetrie,
        cycles: cycles.map((cycle) => {
          const { foiGagnee, ...resteCycle } = (cycle ?? {}) as Record<string, unknown>
          return { ...resteCycle, souffleGagne: foiGagnee ?? '0' }
        }),
      },
    }
  },
```

- [ ] **Step 4 : vérifier**

Run: `npx vitest run tests/persistance.test.ts tests/determinisme.test.ts && npx tsc -b`
Expected: PASS.

Run: `npx vitest run`
Expected: 196/197.

- [ ] **Step 5 : commit**

```bash
git add -A
git commit -m "feat(persistance): migration 7 → 8 — le Souffle, les insufflations, les renaissances

Trois champs renommés, aucun retiré, et les clefs du registre d'insufflations
réécrites. Les migrations 1 à 6 gardent les anciens noms : elles lisent des
formats qui les employaient."
```

---

# Task 5 : le canon, les couleurs et les documents

**Files:**
- Modify: `tests/canon.test.ts`, `src/index.css`, `README.md`, `docs/amendement-v1.1.md`, `docs/ROADMAP.md`
- Modify: tout fichier portant encore `text-foi` / `border-foi`

**Interfaces:**
- Consumes: les quatre tâches précédentes

- [ ] **Step 1 : les mots morts entrent au canon**

Dans `tests/canon.test.ts`, ajouter à la liste `PERIMES` :

```typescript
    { motif: /\bfoi\b/i, quoi: 'foi (dire le Souffle)' },
    { motif: /\bfid[èe]les?\b/i, quoi: 'fidèle (le Souffle n’a pas de fidèles)' },
    { motif: /\bb[ée]n[ée]dictions?\b/i, quoi: 'bénédiction (dire insufflation)' },
    { motif: /\bb[ée]nir\b/i, quoi: 'bénir (dire insuffler)' },
    { motif: /\b[ée]clore\b/i, quoi: 'éclore (dire renaître)' },
    { motif: /\bpontes?\b|\bpondre\b/i, quoi: 'ponte (dire renaissance)' },
```

**`eclosion` n'entre PAS dans cette liste.** Codex §7 : le mot est rétréci, pas banni — il
nomme la naissance du héros à l'acte I, et trois identifiants de succès le portent
légitimement. Le renommage de ses emplois mécaniques a été fait en tâche 3 ; il n'y a pas
de garde automatique possible ici, et c'est assumé.

**Le motif `\bfoi\b` est ancré exprès** : `fois` ne correspond pas, parce que le `s` est un
caractère de mot et fait échouer la frontière. C'est ce qui rend ce motif sûr.

- [ ] **Step 1 bis : exempter `persistance.ts`, sans quoi ce test devient infaisable**

Le balayage `PERIMES` ratisse **tout `src/` sans aucune exemption**, et il ne retire que
les commentaires — pas les chaînes ni le code. Or `MIGRATIONS[1]` à `[7]` contiennent
`permanent.foi`, `delete reste.benedictions` et le motif `/^benediction-/` **en tant que
code**. Sans exemption, ce test échoue dès que les six motifs sont ajoutés, et il n'y a
aucun moyen de le satisfaire : ces lignes doivent exister.

C'est exactement la raison pour laquelle l'autre balayage du fichier — celui du modèle mort
— exempte déjà `persistance.ts` en s'en expliquant : « *c'est le seul fichier qui a le
droit de connaître les anciens noms de champs, parce qu'il lit les vieilles sauvegardes* ».
Étendre la même exemption ici :

```typescript
  it('aucun terme périmé ne subsiste dans le code de src/', () => {
    // `src/adaptateurs/persistance.ts` est exclu, pour la même raison que dans
    // le balayage du modèle mort : ses migrations lisent des sauvegardes
    // écrites avec les anciens noms de champs (`foi`, `benedictions`,
    // `nombreEclosions`). Leur interdire ces mots interdirait la migration.
    const migrations = join('src', 'adaptateurs', 'persistance.ts')
    const fautes: string[] = []
    for (const fichier of fichiersTs(join(RACINE, 'src'))) {
      if (relative(RACINE, fichier) === migrations) continue
      const code = sansCommentaires(readFileSync(fichier, 'utf8'))
      for (const { motif, quoi } of PERIMES) {
        if (motif.test(code)) fautes.push(`${relative(RACINE, fichier)} : ${quoi}`)
      }
    }
    expect(fautes).toEqual([])
  })
```

Vérifier que `join` et `relative` sont déjà importés dans ce fichier — ils le sont, le
balayage du modèle mort les emploie.

- [ ] **Step 2 : la couleur**

`src/index.css` :

```css
  --color-souffle:    oklch(0.83 0.10 80);
```

Puis remplacer `text-foi` → `text-souffle` et `border-foi` → `border-souffle` partout
(`src/ui/Renaissance.tsx`, `src/ui/Insufflations.tsx`, `src/ui/App.tsx`, `src/ui/Heros.tsx`).

Run: `grep -rn "foi" src/index.css src/ui/`
Expected: aucune ligne.

- [ ] **Step 3 : le README**

Section « État » — la phrase des insufflations :

```markdown
Depuis le 2026-09-17 : le héros **grandit** (quatrième achat, en mana), le Souffle
achète des **insufflations** permanentes, et une **scène** dessinée au trait
montre le héros, ses marques et ses bancs. Sans technique : c'est le jalon
v0.4.
```

« Le contrat », règle 3 :

```markdown
3. La technique baisse les **coûts** et automatise ; l'insufflation monte la
   **production**, et c'est la seule chose qu'elle fait. Aucun nœud, **aucun
   succès** ne franchit cette ligne (`tests/canon.test.ts`).
```

Mettre à jour le compte de tests du bloc de commandes avec le chiffre réel de
`npx vitest run`, et ajouter `codex` à la liste des documents cités.

- [ ] **Step 4 : l'amendement v1.5**

Dans `docs/amendement-v1.1.md`, après la sous-section v1.4, ajouter :

```markdown
#### Amendé le 2026-09-18 (v1.5) — le lexique du Codex entre dans le code

Codex : `docs/CODEX.md` v1.0.

Renommage transverse, sans un seul changement de mécanique : la **Foi** devient
le **Souffle** — une énergie distincte du mana, exhalée par tout ce qui vit et
recueillie seulement quand le héros est scellé, et non plus une émotion du
peuple. Les **bénédictions** deviennent les **insufflations**. L'**éclosion**,
comme nom du cycle, devient la **renaissance** : le héros ne pond pas et n'est
pas réincarné, il se refait, et son corps garde le compte.

Sauvegardes migrées **v7 → v8** : `foi` → `souffle`, `benedictions` →
`insufflations` (clefs comprises), `nombreEclosions` → `nombreRenaissances`,
`foiGagnee` → `souffleGagne`. Les migrations 1 à 6 gardent l'ancien
vocabulaire — elles lisent des formats qui l'employaient.

Trois identifiants de succès en `eclosion` restent figés : le registre est
immuable au canon (§16.1) et ils sont dans les sauvegardes des joueurs. C'est
une décision, consignée au Codex §7.

Cette dette était notée dans `src/noyau/types.ts` depuis le 2026-09-08
(« en attendant le renommage transverse »). Elle est soldée.
```

- [ ] **Step 5 : retirer le chantier de la roadmap**

Dans `docs/ROADMAP.md`, supprimer la section « 1. Le renommage transverse » et renuméroter
les suivantes. La roadmap se tient à jour par retrait, pas par coche — son propre pied de
page le dit.

Mettre à jour « Où on en est » : les insufflations et le Souffle y sont nommés
correctement.

- [ ] **Step 6 : vérifier**

Run: `npx vitest run && npx tsc -b && npx eslint . && npm run build`
Expected: tout vert (196/197), tout propre.

Run: `npm run dev`, ouvrir `http://localhost:5173`, profil vierge. Attendu : le bandeau dit
**Souffle**, l'écran des insufflations s'intitule « Ce que tu insuffles » et son bouton dit
« Insuffler », et la couleur dorée du Souffle est inchangée à l'œil. Forcer une renaissance
et vérifier que le compteur monte. Capture pour le rapport de tâche.

- [ ] **Step 7 : commit**

```bash
git add -A
git commit -m "docs(canon): les mots morts entrent au canon, le lexique est appliqué

Six motifs ajoutés au balayage de canon.test.ts : foi, fidèle, bénédiction,
bénir, éclore, ponte. « Éclosion » n'y entre pas — Codex §7, le mot est
rétréci, pas banni. Couleur, README, amendement v1.5, roadmap."
```

---

## Self-review

**Couverture du Codex.** §5 le lexique : les énergies → tâche 1 ; les axes permanents →
tâche 2 ; le héros (renaissance) → tâche 3 ; les mots morts → tâche 5. §7 les exceptions
figées : les trois succès sont protégés en tâche 3 (avec un `grep` qui **exige** leur
présence), et « bénédiction » survivant à l'acte IX ne concerne aucun identifiant. Les
clefs de sauvegarde sont couvertes en tâche 4.

**Cohérence des noms.** `souffle` / `SOUFFLE_*` / `souffleGagne` — identiques des tâches 1
à 5. `insuffler` / `Insufflation` / `INSUFFLATION_*` / `D` pour « d' » — identiques des
tâches 2 à 5. `renaitre` / `nombreRenaissances` / `RENAISSANCE` — identiques 3 à 5. Les
deux constantes `SOUFFLE_COUT_DE_BENEDICTION_*` créées en tâche 1 sont explicitement
renommées en tâche 2 ; c'est le seul identifiant à changer deux fois, et c'est signalé aux
deux endroits.

**Ordre de dépendance.** 1 avant 2 (les constantes de coût portent les deux mots). 1, 2, 3
avant 4 (la migration doit écrire vers des champs qui existent). 4 avant 5 (le balayage des
mots morts échouerait sur un `foi` encore présent). La fenêtre où les sauvegardes ne se
rechargent pas est bornée aux tâches 1 à 3 et signalée en tête.
