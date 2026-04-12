import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import Decimal from 'break_infinity.js';
import { ACHIEVEMENTS } from '../data/achievements';
import { CHALLENGE_POOL } from '../data/challenges';
import { RESEARCH } from '../data/research';
import { PEARL_UPGRADES } from '../data/pearlUpgrades';
import { PRESTIGE_UPGRADES } from '../data/prestigeUpgrades';
import { FISH_TYPES } from '../data/fishTypes';
import { GLOBAL_UPGRADES, ZONE_UPGRADES } from '../data/runUpgrades';
import { computeBonuses } from '../utils/bonuses';
import { getSessionManaEarned } from '../utils/session';

export const MAX_FISH_LEVEL = 100;

export interface PoissonInstance {
  id: string;
  type: string;
  baseIncome: number;
  level: number;
}

export const getPondUpgradeCost = (currentDepth: number): Decimal =>
  new Decimal(500).times(Decimal.pow(10, currentDepth));

// Paliers de perles basés sur le pic de mana du run
export const MANA_PEARL_TIERS: { threshold: Decimal; pearls: number; label: string }[] = [
  { threshold: new Decimal('1e2'),  pearls: 1,   label: '100'   },
  { threshold: new Decimal('1e3'),  pearls: 2,   label: '1K'    },
  { threshold: new Decimal('1e4'),  pearls: 3,   label: '10K'   },
  { threshold: new Decimal('1e5'),  pearls: 5,   label: '100K'  },
  { threshold: new Decimal('1e6'),  pearls: 7,   label: '1M'    },
  { threshold: new Decimal('1e7'),  pearls: 10,  label: '10M'   },
  { threshold: new Decimal('1e8'),  pearls: 13,  label: '100M'  },
  { threshold: new Decimal('1e9'),  pearls: 17,  label: '1B'    },
  { threshold: new Decimal('1e10'), pearls: 22,  label: '10B'   },
  { threshold: new Decimal('1e12'), pearls: 28,  label: '1T'    },
  { threshold: new Decimal('1e15'), pearls: 36,  label: '1Qa'   },
  { threshold: new Decimal('1e18'), pearls: 46,  label: '1Qi'   },
  { threshold: new Decimal('1e21'), pearls: 58,  label: '1Sx'   },
  { threshold: new Decimal('1e25'), pearls: 73,  label: '10^25' },
  { threshold: new Decimal('1e30'), pearls: 92,  label: '10^30' },
  { threshold: new Decimal('1e40'), pearls: 120, label: '10^40' },
  { threshold: new Decimal('1e50'), pearls: 160, label: '10^50' },
];

export const calcPrestigeReward = (manaRunHigh: Decimal): number => {
  let pearls = 0;
  for (const tier of MANA_PEARL_TIERS) {
    if (manaRunHigh.gte(tier.threshold)) pearls = tier.pearls;
    else break;
  }
  return Math.max(1, pearls);
};

export const getNextManaTier = (manaRunHigh: Decimal) => {
  return MANA_PEARL_TIERS.find(t => manaRunHigh.lt(t.threshold)) ?? null;
};

export interface GameState {
  mana: Decimal;
  manaRunHigh: Decimal;
  gemmes: number;
  perles: number;
  poissons: PoissonInstance[];
  pondDepth: number;
  boostActiveUntil: number;
  lastSaveTime: number;
  prestiges: number;

  unlockedAchievementIds: string[];
  claimedAchievementIds: string[];
  researchUnlocked: string[];
  pearlUpgradesUnlocked: string[];
  prestigeUpgradesUnlocked: string[];
  runUpgradesOwned: string[];

  dailyChallengesCompleted: string[];
  lastChallengeDate: string;

  pendingUnlock: string | null;
  pendingNarrativeEvent: string | null;
  pendingWelcomeBack: { minutes: number; mana: string } | null;

  // Actions
  addMana: (amount: Decimal) => void;
  addGemmes: (amount: number) => void;
  buyRunUpgrade: (id: string) => void;
  buyFish: (type: string, baseIncome: number, cost: Decimal) => void;
  buyFishBulk: (type: string, baseIncome: number, baseCost: number, count: number) => void;
  upgradeFish: (id: string, cost: Decimal) => void;
  upgradeFishN: (id: string, n: number, totalCost: Decimal) => void;
  upgradePond: () => void;
  activateBoost: () => void;
  prestige: () => void;
  unlockResearch: (id: string) => void;
  buyPearlUpgrade: (id: string) => void;
  buyPrestigeUpgrade: (id: string) => void;
  claimAchievement: (id: string) => void;
  claimChallenge: (id: string) => void;
  checkAchievements: () => void;
  checkDailyReset: () => void;
  clearPendingUnlock: () => void;
  setPendingNarrativeEvent: (event: string | null) => void;
  setPendingWelcomeBack: (data: { minutes: number; mana: string } | null) => void;
  updateLastSaveTime: () => void;
}

const MAX_DEPTH = 11;

export const useGameStore = create<GameState>()(
  persist(
    (set, get) => ({
      mana: new Decimal(10),
      manaRunHigh: new Decimal(10),
      gemmes: 0,
      perles: 0,
      poissons: [],
      pondDepth: 0,
      boostActiveUntil: 0,
      lastSaveTime: Date.now(),
      prestiges: 0,
      unlockedAchievementIds: [],
      claimedAchievementIds: [],
      researchUnlocked: [],
      pearlUpgradesUnlocked: [],
      prestigeUpgradesUnlocked: [],
      runUpgradesOwned: [],
      dailyChallengesCompleted: [],
      lastChallengeDate: '',
      pendingUnlock: null,
      pendingNarrativeEvent: null,
      pendingWelcomeBack: null,

      addMana: (amount) => set((s) => {
        const newMana = s.mana.plus(amount);
        const newHigh = newMana.gt(s.manaRunHigh) ? newMana : s.manaRunHigh;
        return { mana: newMana, manaRunHigh: newHigh };
      }),

      addGemmes: (amount) => set((s) => ({ gemmes: s.gemmes + amount })),

      buyRunUpgrade: (id) => set((s) => {
        if (s.runUpgradesOwned.includes(id)) return s;
        const allUpgrades = [...GLOBAL_UPGRADES, ...ZONE_UPGRADES];
        const upg = allUpgrades.find(u => u.id === id);
        if (!upg) return s;
        if (upg.requires && !s.runUpgradesOwned.includes(upg.requires)) return s;
        if (!s.mana.gte(upg.manaCost)) return s;
        return {
          mana: s.mana.minus(upg.manaCost),
          runUpgradesOwned: [...s.runUpgradesOwned, id],
        };
      }),

      buyFish: (type, baseIncome, cost) => set((s) => {
        if (!s.mana.gte(cost)) return s;
        const fishDef = FISH_TYPES.find(f => f.type === type);
        if (!fishDef) return s;
        if (fishDef.requiredPrestiges && s.prestiges < fishDef.requiredPrestiges) return s;
        const maxOwned = fishDef.maxOwned ?? 1;
        if (s.poissons.filter(f => f.type === type).length >= maxOwned) return s;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked, s.runUpgradesOwned);
        return {
          mana: s.mana.minus(cost),
          poissons: [...s.poissons, {
            id: `${type}-${Date.now()}-${Math.random().toString(36).slice(2)}`,
            type, baseIncome, level: bonuses.prestigeStartingLevel,
          }],
        };
      }),

      buyFishBulk: (type, baseIncome, baseCost, count) => set((s) => {
        const fishDef = FISH_TYPES.find(f => f.type === type);
        if (!fishDef) return s;
        if (fishDef.requiredPrestiges && s.prestiges < fishDef.requiredPrestiges) return s;

        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked, s.runUpgradesOwned);
        const ownedCount = s.poissons.filter(f => f.type === type).length;

        const maxOwned = fishDef.maxOwned ?? 1;
        let cap = Math.min(count, maxOwned - ownedCount);
        if (cap <= 0) return s;

        let totalCost = new Decimal(0);
        const newFish: PoissonInstance[] = [];
        for (let i = 0; i < cap; i++) {
          const cost = new Decimal(baseCost)
            .mul(bonuses.fishCostMult)
            .mul(new Decimal(1.15).pow(ownedCount + i));
          totalCost = totalCost.plus(cost);
          newFish.push({
            id: `${type}-${Date.now()}-${i}-${Math.random().toString(36).slice(2)}`,
            type, baseIncome, level: bonuses.prestigeStartingLevel,
          });
        }
        if (!s.mana.gte(totalCost)) return s;
        return { mana: s.mana.minus(totalCost), poissons: [...s.poissons, ...newFish] };
      }),

      upgradeFish: (id, cost) => set((s) => {
        if (!s.mana.gte(cost)) return s;
        const idx = s.poissons.findIndex(f => f.id === id);
        if (idx === -1 || s.poissons[idx].level >= MAX_FISH_LEVEL) return s;
        const updated = [...s.poissons];
        updated[idx] = { ...updated[idx], level: updated[idx].level + 1 };
        return { mana: s.mana.minus(cost), poissons: updated };
      }),

      upgradeFishN: (id, n, totalCost) => set((s) => {
        if (!s.mana.gte(totalCost) || n <= 0) return s;
        const idx = s.poissons.findIndex(f => f.id === id);
        if (idx === -1) return s;
        const currentLevel = s.poissons[idx].level;
        const newLevel = Math.min(MAX_FISH_LEVEL, currentLevel + n);
        if (newLevel <= currentLevel) return s;
        const updated = [...s.poissons];
        updated[idx] = { ...updated[idx], level: newLevel };
        return { mana: s.mana.minus(totalCost), poissons: updated };
      }),

      upgradePond: () => set((s) => {
        if (s.pondDepth >= MAX_DEPTH) return s;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked, s.runUpgradesOwned);
        let costMult = bonuses.pondCostMult;
        if (s.pondDepth === 0 && s.prestigeUpgradesUnlocked.includes('prest_creusage')) {
          costMult *= 0.5;
        }
        const cost = getPondUpgradeCost(s.pondDepth).mul(costMult);
        if (!s.mana.gte(cost)) return s;
        const newDepth = s.pondDepth + 1;
        return { mana: s.mana.minus(cost), pondDepth: newDepth, pendingUnlock: `depth_${newDepth}` };
      }),

      activateBoost: () => set((s) => {
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked, s.runUpgradesOwned);
        if (s.gemmes < bonuses.boostCost || s.boostActiveUntil > Date.now()) return s;
        return {
          gemmes: s.gemmes - bonuses.boostCost,
          boostActiveUntil: Date.now() + bonuses.boostDurationMs,
        };
      }),

      prestige: () => set((s) => {
        if (s.pondDepth < 2) return s;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked, s.runUpgradesOwned);
        const baseReward = calcPrestigeReward(s.manaRunHigh);
        const earned = Math.ceil(baseReward * bonuses.prestigePearlMult);

        let keptFish: PoissonInstance[] = [];
        if (bonuses.prestigeKeepFishPercent > 0) {
          const keepCount = Math.max(1, Math.floor(s.poissons.length * bonuses.prestigeKeepFishPercent / 100));
          keptFish = s.poissons.slice(0, keepCount);
        }

        const startingMana = new Decimal(bonuses.prestigeStartingMana);
        return {
          mana: startingMana,
          manaRunHigh: startingMana,
          poissons: keptFish,
          pondDepth: bonuses.prestigeStartingDepth,
          boostActiveUntil: 0,
          prestiges: s.prestiges + 1,
          perles: s.perles + earned,
          runUpgradesOwned: [],
        };
      }),

      unlockResearch: (id) => set((s) => {
        if (s.researchUnlocked.includes(id)) return s;
        const r = RESEARCH.find(x => x.id === id);
        if (!r) return s;
        if (r.requires && !s.researchUnlocked.includes(r.requires)) return s;
        if (s.gemmes < r.cost) return s;
        return { gemmes: s.gemmes - r.cost, researchUnlocked: [...s.researchUnlocked, id] };
      }),

      buyPearlUpgrade: (id) => set((s) => {
        if (s.pearlUpgradesUnlocked.includes(id)) return s;
        const p = PEARL_UPGRADES.find(x => x.id === id);
        if (!p) return s;
        if (p.requires && !s.pearlUpgradesUnlocked.includes(p.requires)) return s;
        if (s.gemmes < p.cost) return s;
        return { gemmes: s.gemmes - p.cost, pearlUpgradesUnlocked: [...s.pearlUpgradesUnlocked, id] };
      }),

      buyPrestigeUpgrade: (id) => set((s) => {
        if (s.prestigeUpgradesUnlocked.includes(id)) return s;
        const p = PRESTIGE_UPGRADES.find(x => x.id === id);
        if (!p) return s;
        if (p.requires && !s.prestigeUpgradesUnlocked.includes(p.requires)) return s;
        if (s.perles < p.cost) return s;
        return { perles: s.perles - p.cost, prestigeUpgradesUnlocked: [...s.prestigeUpgradesUnlocked, id] };
      }),

      claimChallenge: (id) => set((s) => {
        if (s.dailyChallengesCompleted.includes(id)) return s;
        const challenge = CHALLENGE_POOL.find(c => c.id === id);
        if (!challenge) return s;
        const ok = challenge.check({
          poissons: s.poissons,
          pondDepth: s.pondDepth,
          researchUnlocked: s.researchUnlocked,
          sessionManaEarned: getSessionManaEarned(),
        });
        if (!ok) return s;
        return {
          perles: s.perles + challenge.pearlReward,
          dailyChallengesCompleted: [...s.dailyChallengesCompleted, id],
        };
      }),

      claimAchievement: (id) => set((s) => {
        if (!s.unlockedAchievementIds.includes(id)) return s;
        if (s.claimedAchievementIds.includes(id)) return s;
        const achievement = ACHIEVEMENTS.find(a => a.id === id);
        if (!achievement) return s;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked, s.runUpgradesOwned);
        const gems = Math.ceil(achievement.gemReward * bonuses.gemmeRewardMult);
        return {
          claimedAchievementIds: [...s.claimedAchievementIds, id],
          gemmes: s.gemmes + gems,
        };
      }),

      checkAchievements: () => {
        const s = get();
        const toUnlock = ACHIEVEMENTS.filter(
          a => !s.unlockedAchievementIds.includes(a.id) && a.check(s)
        );
        if (toUnlock.length === 0) return;
        set((state) => ({
          unlockedAchievementIds: [...state.unlockedAchievementIds, ...toUnlock.map(a => a.id)],
        }));
      },

      checkDailyReset: () => set((s) => {
        const today = new Date().toDateString();
        if (s.lastChallengeDate === today) return s;
        return { lastChallengeDate: today, dailyChallengesCompleted: [] };
      }),

      clearPendingUnlock: () => set({ pendingUnlock: null }),
      setPendingNarrativeEvent: (event) => set({ pendingNarrativeEvent: event }),
      setPendingWelcomeBack: (data) => set({ pendingWelcomeBack: data }),
      updateLastSaveTime: () => set({ lastSaveTime: Date.now() }),
    }),
    {
      name: 'etang-des-merveilles-storage',
      partialize: (state) => ({
        ...state,
        mana: state.mana.toString() as unknown as Decimal,
        manaRunHigh: state.manaRunHigh.toString() as unknown as Decimal,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.mana = new Decimal(state.mana);
          state.manaRunHigh = state.manaRunHigh
            ? new Decimal(state.manaRunHigh)
            : new Decimal(state.mana);
        }
      },
    }
  )
);
