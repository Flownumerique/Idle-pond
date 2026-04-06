# Documentation — Étang des Merveilles

> Jeu idle de pêche magique, React 19 + Phaser 3

---

## Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Stack technique](#stack-technique)
3. [Architecture des fichiers](#architecture-des-fichiers)
4. [Mécanique de jeu](#mécanique-de-jeu)
5. [Système de poissons et biomes](#système-de-poissons-et-biomes)
6. [Système de prestige](#système-de-prestige)
7. [Gemmes et monnaies](#gemmes-et-monnaies)
8. [Corail de Prestige (arbre de recherche)](#corail-de-prestige)
9. [Succès](#succès)
10. [Défis quotidiens](#défis-quotidiens)
11. [Événements narratifs](#événements-narratifs)
12. [Journal et Bestiaire](#journal-et-bestiaire)
13. [État global (Zustand)](#état-global-zustand)
14. [Boucle de jeu](#boucle-de-jeu)
15. [Calcul des bonus](#calcul-des-bonus)
16. [Persistance](#persistance)
17. [Scène Phaser](#scène-phaser)

---

## Vue d'ensemble

**Étang des Merveilles** est un jeu de type *idle/clicker* dans lequel le joueur creuse progressivement un étang magique, achète des poissons qui génèrent de la Mana passivement, et améliore son étang à travers plusieurs couches de mécaniques de progression.

L'étang possède **8 biomes** (profondeurs 0-7) hébergeant au total **16 espèces** de poissons. La progression globale repose sur :
- L'achat et l'amélioration de poissons (production de Mana)
- Le creusage de l'étang (déblocage de biomes et espèces)
- Le prestige (remise à zéro enrichie avec des récompenses permanentes)
- L'arbre de recherche "Corail de Prestige" (bonus permanents payés en Gemmes)

---

## Stack technique

| Couche | Technologie |
|--------|-------------|
| Framework UI | React 19 + TypeScript |
| Build tool | Vite 8 |
| Moteur visuel | Phaser 3 |
| État global | Zustand 5 (avec `persist` middleware) |
| Grands nombres | `break_infinity.js` (classe `Decimal`) |
| Styles | Tailwind CSS 4 |

---

## Architecture des fichiers

```
src/
├── App.tsx                        # Racine : layout, onglets, montage des managers
│
├── components/
│   ├── Shop.tsx                   # Boutique principale (poissons + creusage + prestige)
│   ├── Stats.tsx                  # Affichage mana, revenus, profondeur
│   ├── Research.tsx               # Corail de Prestige (5 branches)
│   ├── PearlMarket.tsx            # Marché des Perles (coûts en Gemmes 💎)
│   ├── PrestigeUpgrades.tsx       # Améliorations de prestige (coûts en Perles 🪸)
│   ├── Achievements.tsx           # Panneau des succès
│   ├── Lore.tsx                   # Journal + Bestiaire
│   ├── Challenges.tsx             # Défis quotidiens
│   ├── Guide.tsx                  # Guide du jeu (statique)
│   ├── BoostOverlay.tsx           # Bouton + état du Boost de Mana
│   ├── PhaserContainer.tsx        # Montage du canvas Phaser dans React
│   ├── UnlockNotification.tsx     # Toast animé lors du déblocage d'un biome
│   ├── EventNotification.tsx      # Toast narratif ambiant (événements de l'étang)
│   └── WelcomeBackNotification.tsx# Toast de reconnexion (gains hors-ligne)
│
├── data/
│   ├── fishTypes.ts               # Définitions des 16 espèces + jalons + helpers
│   ├── achievements.ts            # 49 succès avec conditions (fonctions pures)
│   ├── lore.ts                    # Journal (8 entrées par profondeur) + Bestiaire (16 espèces)
│   ├── research.ts                # 5 branches du Corail de Prestige (21 nœuds)
│   ├── pearlUpgrades.ts           # 6 améliorations du Marché des Perles
│   ├── prestigeUpgrades.ts        # 12 améliorations de prestige (3 niveaux)
│   ├── challenges.ts              # Pool de 15 défis + sélection quotidienne déterministe
│   └── narrativeEvents.ts         # Pool de 30 événements narratifs ambiants
│
├── store/
│   └── useGameStore.ts            # Store Zustand central + toutes les actions
│
├── managers/
│   ├── GameLoopManager.ts         # Singleton : tick 100ms, revenus, gemmes passives, events
│   └── OfflineManager.ts          # Calcul des gains hors-ligne au démarrage
│
├── game/
│   └── scenes/
│       └── PondScene.ts           # Scène Phaser : 8 zones visuelles, sprites poissons
│
└── utils/
    ├── bonuses.ts                 # computeBonuses() — agrège tous les modificateurs
    ├── formatNumber.ts            # Formatage des grands nombres (k/M/B/T/…)
    └── session.ts                 # Suivi de mana gagnée en session (non-persisté)
```

---

## Mécanique de jeu

### Flux principal

```
Acheter poisson → produit Mana/s → accumuler Mana
    ↓
Creuser l'étang (coût en Mana) → débloque nouveau biome + nouvelles espèces
    ↓
Succès débloqués automatiquement → récompenses en Gemmes 💎
    ↓
Gemmes → Corail de Prestige (bonus permanents) + Marché des Perles + Boost
    ↓
Prestige (si profondeur ≥ 2) → remise à zéro + gain Perles 🪸
    ↓
Perles → Améliorations de Prestige (bonus ultra-permanents)
```

### Formule de revenu

```
revenu_tick = Σ(poissons) [
  baseIncome
  × 1.5^(level - 1)          -- multiplicateur de niveau
  × Π(jalons_atteints) ×2    -- jalons propres (×2 par jalon)
]
× getGlobalMultiplier()      -- somme des bonus globaux des jalons de tous les poissons
× globalIncomeMult           -- bonus recherche + perles + prestige
× boostMultiplier            -- ×2 ou ×3 si boost actif
× deepFishIncomeMult         -- bonus Océanologie pour profondeur 4+
```

### Coût de creusage

```
coût = 500 × 10^profondeur_actuelle × pondCostMult
```

### Coût des poissons

```
coût_n = baseCost × fishCostMult × 1.15^n
```
_(n = nombre déjà possédés de cette espèce)_

### Jalons (milestones)

Chaque espèce a 4 jalons aux niveaux **10, 25, 50, 100** :
- **Multiplicateur propre** : ×2 sur la production de ce poisson par jalon
- **Bonus global** : % ajouté à la production globale de tout l'étang

La recherche `bio_4` (Corail de Prestige) réduit le niveau requis de 1 pour chaque jalon.

---

## Système de poissons et biomes

Voir [`POISSONS.md`](./POISSONS.md) pour la liste complète des 16 espèces par zone.

Voir [`BIOMES.md`](./BIOMES.md) pour les descriptions narratives et stats détaillées.

**Règles de déblocage :**
- Espèce disponible si `pondDepth >= fish.requiredDepth`
- Certaines espèces requièrent en plus un nombre minimum de prestiges (`requiredPrestiges`)
- Le Poisson Céleste est limité à **1 exemplaire** (`maxOwned: 1`)

---

## Système de prestige

**Condition :** `pondDepth >= 2`

**Récompense en Perles :**
```
perles = ceil((pondDepth × 5 + floor(log10(mana))) × prestigePearlMult)
```

**Remise à zéro :** Mana, poissons (sauf bonus `prest_keep_fish`), profondeur, boost
**Conservé :** Gemmes, Perles, améliorations de recherche, améliorations de prestige

Les améliorations de prestige permettent de conserver une partie de la progression :
- Mana de départ (5k ou 1M)
- Profondeur de départ (1 ou 2)
- Niveau de départ des poissons (3 ou 10)
- % de poissons conservés (10%)

---

## Gemmes et monnaies

| Monnaie | Symbole | Obtention | Utilisation |
|---------|---------|-----------|-------------|
| **Mana** | — | Poissons (passif) | Acheter poissons, creuser |
| **Gemmes** | 💎 | Succès, Poisson Céleste (+1/min), Corail Mystique/Océanologie | Corail de Prestige, Marché des Perles, Boost |
| **Perles** | 🪸 | Prestige, Défis quotidiens | Améliorations de Prestige |

---

## Corail de Prestige

Arbre de recherche permanent payé en Gemmes 💎, **5 branches** :

| Branche | Couleur | Nœuds | Effet principal |
|---------|---------|-------|-----------------|
| 🧬 Biologie | Émeraude | 5 | +15% à +75% revenu global, jalons anticipés |
| ⛏️ Géologie | Ambre | 4 | −15% à −50% coût de creusage |
| ⚗️ Alchimie | Violet | 4 | Durée boost +5 min, coût −5 💎, ×3 au lieu de ×2 |
| 🔮 Mystique | Cyan | 4 | +25/50% récompenses succès, +1/+2 💎/min passifs |
| 🌊 Océanologie | Teal | 4 | +20/30% global, −20% coût poissons, +50% profonds, +3 💎/min |

Chaque nœud a un prérequis (`requires`) qui force une progression linéaire dans sa branche.

---

## Succès

**49 succès** répartis en 7 catégories :

| Catégorie | Nombre | Récompense |
|-----------|--------|------------|
| Nombre de poissons (1/10/50/100/200/500) | 6 | 5 → 250 💎 |
| Premières espèces (16 espèces) | 16 | 5 → 500 💎 |
| Profondeur atteinte (1-7) | 7 | 10 → 200 💎 |
| Mana accumulée (1k → 1e18) | 6 | 5 → 250 💎 |
| Niveaux de poissons (10/25/50/100 + 5 à max) | 5 | 10 → 300 💎 |
| Prestige (1/3/5/10) | 4 | 50 → 500 💎 |
| Recherche (1/5/10 nœuds) | 3 | 15 → 100 💎 |
| Diversité (banc de rubis, abyssaux, tous biomes) | 3 | 20 → 150 💎 |

Les succès sont vérifiés à chaque tick (100ms) par `checkAchievements()`.
Le multiplicateur de récompenses (`gemmeRewardMult`) s'applique via la branche Mystique.

---

## Défis quotidiens

- **3 défis** sélectionnés chaque jour parmi un pool de 15
- Sélection **déterministe** par date (générateur LCG seedé sur `new Date().toDateString()`)
- Récompense en **Perles 🪸** à la complétion
- Remis à zéro automatiquement chaque jour

---

## Événements narratifs

Des messages d'ambiance apparaissent **toutes les 5 à 10 minutes** (intervalle aléatoire).

- **30 événements** dans le pool (`narrativeEvents.ts`)
- Chaque événement a des conditions : profondeur minimale et/ou type de poisson requis
- Affiché via `EventNotification.tsx` (toast en bas de l'écran, 7 secondes)

---

## Journal et Bestiaire

**Journal** (`unlockedAtDepth`) : 8 entrées de lore débloquées à chaque profondeur.

**Bestiaire** (`unlockedByFishType`) : 16 entrées narratives, une par espèce, débloquées en possédant le poisson concerné. Accessible via le deuxième onglet du Journal.

---

## État global (Zustand)

```typescript
interface GameState {
  mana: Decimal;                      // production principale
  gemmes: number;                     // monnaie secondaire
  perles: number;                     // monnaie de prestige
  poissons: PoissonInstance[];        // tableau de tous les poissons possédés
  pondDepth: number;                  // profondeur actuelle (0-7)
  boostActiveUntil: number;           // timestamp de fin de boost
  lastSaveTime: number;               // pour calcul hors-ligne
  prestiges: number;                  // compteur de prestiges
  unlockedAchievementIds: string[];
  researchUnlocked: string[];
  pearlUpgradesUnlocked: string[];
  prestigeUpgradesUnlocked: string[];
  dailyChallengesCompleted: string[];
  lastChallengeDate: string;
  pendingUnlock: string | null;            // déclenche UnlockNotification
  pendingNarrativeEvent: string | null;    // déclenche EventNotification
  pendingWelcomeBack: { minutes: number; mana: string } | null; // reconnexion
}
```

**Sérialisation Decimal** : `mana` est stocké en string dans localStorage via `partialize`, puis réhydraté en `new Decimal(state.mana)` dans `onRehydrateStorage`.

---

## Boucle de jeu

`GameLoopManager` (singleton) tourne à **100ms/tick** :

```
tick(deltaMs):
  1. computeBonuses()
  2. Pour chaque poisson : calcule revenu × jalons × deepFishBonus
  3. Applique globalIncomeMult × boostMultiplier
  4. addMana(incomeThisTick)
  5. Accumule Gemmes : Poisson Céleste (+1/min) + Mystique/Océanologie
  6. Timer narratif : si accum >= intervalle aléatoire (5-10 min) → pickRandomEvent()
  7. checkAchievements()
  8. updateLastSaveTime()
```

**OfflineManager** s'exécute une fois au montage de `App.tsx` :
- Calcule le revenu passif depuis `lastSaveTime`
- Cap à 24h maximum
- Affiche un toast de bienvenue (`pendingWelcomeBack`) si absent > 1 minute

---

## Calcul des bonus

`computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked)`

Retourne un objet `ComputedBonuses` utilisé par **GameLoopManager**, **OfflineManager**, **Store** (upgradePond, buyFish, prestige) et les composants UI.

```typescript
interface ComputedBonuses {
  globalIncomeMult: number;       // ×(1 + tous les % de revenu)
  pondCostMult: number;           // max(0.05, 1 - réductions%)
  boostDurationMs: number;        // 5min + bonus alchimie
  boostCost: number;              // 10 - réduction alchimie (min 1)
  boostMultiplier: number;        // 2 + bonus alch_4
  fishCostMult: number;           // max(0.05, 1 - réductions%)
  offlineMult: number;            // bonus marché des perles
  milestoneLevelReduction: number;// bio_4 : jalons 1 niveau plus tôt
  gemmeRewardMult: number;        // myst_1/4 : × sur récompenses succès
  passiveGemmesPerMin: number;    // myst_2/3 + ocean_4
  deepFishIncomeMult: number;     // ocean_3 : +50% pour profondeur 4+
  prestigePearlMult: number;      // prest_pearl_bonus_1/2
  celestialCostMult: number;      // prest_celestial_cost : ×0.4
  prestigeStartingMana: number;   // prest_mana / prest_mana_max
  prestigeStartingDepth: number;  // prest_depth_1/2
  prestigeStartingLevel: number;  // prest_level / prest_level_10
  prestigeKeepFishPercent: number;// prest_keep_fish : 10%
  prestigeGlobalIncomePercent: number; // prest_global_mult
}
```

---

## Persistance

Zustand `persist` middleware stocke l'état complet dans `localStorage` sous la clé `etang-des-merveilles-storage`.

**Exceptions non-persistées :**
- `GameLoopManager` (singleton avec `intervalId`)
- `OfflineManager` (calcul one-shot)
- `_sessionManaEarned` dans `utils/session.ts` (module-level variable, pour les défis)
- Les accumulateurs de gemmes dans `GameLoopManager`

---

## Scène Phaser

`PondScene` crée un monde de hauteur `1080 × 8 = 8640px` avec 8 zones empilées verticalement.

- **Scroll** : molette souris + drag tactile, limité aux zones débloquées
- **Couleurs** : chaque biome a sa propre palette (bleu → rouge chaud → noir violet → or mystique)
- **Poissons** : cercles colorés animés par tweens `Sine.easeInOut` avec oscillation aléatoire
- **Nexus (zone 7)** : particules de mana supplémentaires animées en pulsation
- **Communication React ↔ Phaser** : via `game.events.on/emit` (`update-fishes`, `update-depth`, `scene-ready`)

---

*Généré automatiquement — Étang des Merveilles v1.x*
