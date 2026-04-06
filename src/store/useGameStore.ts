import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import Decimal from 'break_infinity.js';
import { ACHIEVEMENTS } from '../data/achievements';
import { CHALLENGE_POOL, getDailyChallengeIds } from '../data/challenges';
import { RESEARCH } from '../data/research';
import { PEARL_UPGRADES } from '../data/pearlUpgrades';
import { PRESTIGE_UPGRADES } from '../data/prestigeUpgrades';
import { FISH_TYPES } from '../data/fishTypes';
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

const calcPrestigeReward = (pondDepth: number, mana: Decimal): number => {
  const depthBonus = pondDepth * 5;
  const manaBonus = mana.gt(1) ? Math.floor(Math.log10(mana.toNumber())) : 0;
  return Math.max(1, depthBonus + manaBonus);
};

export interface GameState {
  mana: Decimal;
  gemmes: number;
  perles: number;
  poissons: PoissonInstance[];
  pondDepth: number;
  boostActiveUntil: number;
  lastSaveTime: number;
  prestiges: number;

  unlockedAchievementIds: string[];
  researchUnlocked: string[];
  pearlUpgradesUnlocked: string[];
  prestigeUpgradesUnlocked: string[];

  dailyChallengesCompleted: string[];
  lastChallengeDate: string;

  pendingUnlock: string | null;
  pendingNarrativeEvent: string | null;
  pendingWelcomeBack: { minutes: number; mana: string } | null;

  // Actions
  addMana: (amount: Decimal) => void;
  addGemmes: (amount: number) => void;
  buyFish: (type: string, baseIncome: number, cost: Decimal) => void;
  buyFishBulk: (type: string, baseIncome: number, baseCost: number, count: number) => void;
  upgradeFish: (id: string, cost: Decimal) => void;
  upgradePond: () => void;
  activateBoost: () => void;
  prestige: () => void;
  unlockResearch: (id: string) => void;
  buyPearlUpgrade: (id: string) => void;
  buyPrestigeUpgrade: (id: string) => void;
  claimChallenge: (id: string) => void;
  checkAchievements: () => void;
  checkDailyReset: () => void;
  clearPendingUnlock: () => void;
  setPendingNarrativeEvent: (event: string | null) => void;
  setPendingWelcomeBack: (data: { minutes: number; mana: string } | null) => void;
  updateLastSaveTime: () => void;
}

const MAX_DEPTH = 7;

export const useGameStore = create<GameState>()(
  persist(
    (set, get) => ({
      mana: new Decimal(10),
      gemmes: 0,
      perles: 0,
      poissons: [],
      pondDepth: 0,
      boostActiveUntil: 0,
      lastSaveTime: Date.now(),
      prestiges: 0,
      unlockedAchievementIds: [],
      researchUnlocked: [],
      pearlUpgradesUnlocked: [],
      prestigeUpgradesUnlocked: [],
      dailyChallengesCompleted: [],
      lastChallengeDate: '',
      pendingUnlock: null,
      pendingNarrativeEvent: null,
      pendingWelcomeBack: null,

      addMana: (amount) => set((s) => ({ mana: s.mana.plus(amount) })),

      addGemmes: (amount) => set((s) => ({ gemmes: s.gemmes + amount })),

      buyFish: (type, baseIncome, cost) => set((s) => {
        if (!s.mana.gte(cost)) return s;
        const fishDef = FISH_TYPES.find(f => f.type === type);
        if (!fishDef) return s;
        if (fishDef.requiredPrestiges && s.prestiges < fishDef.requiredPrestiges) return s;
        if (fishDef.maxOwned !== undefined) {
          if (s.poissons.filter(f => f.type === type).length >= fishDef.maxOwned) return s;
        }
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked);
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

        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked);
        const ownedCount = s.poissons.filter(f => f.type === type).length;

        let cap = count;
        if (fishDef.maxOwned !== undefined) {
          cap = Math.min(count, fishDef.maxOwned - ownedCount);
        }
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

      upgradePond: () => set((s) => {
        if (s.pondDepth >= MAX_DEPTH) return s;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked);
        let costMult = bonuses.pondCostMult;
        // Bonus prestige : premier creusage -50%
        if (s.pondDepth === 0 && s.prestigeUpgradesUnlocked.includes('prest_creusage')) {
          costMult *= 0.5;
        }
        const cost = getPondUpgradeCost(s.pondDepth).mul(costMult);
        if (!s.mana.gte(cost)) return s;
        const newDepth = s.pondDepth + 1;
        return { mana: s.mana.minus(cost), pondDepth: newDepth, pendingUnlock: `depth_${newDepth}` };
      }),

      activateBoost: () => set((s) => {
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked);
        if (s.gemmes < bonuses.boostCost || s.boostActiveUntil > Date.now()) return s;
        return {
          gemmes: s.gemmes - bonuses.boostCost,
          boostActiveUntil: Date.now() + bonuses.boostDurationMs,
        };
      }),

      prestige: () => set((s) => {
        if (s.pondDepth < 2) return s;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked);
        const baseReward = calcPrestigeReward(s.pondDepth, s.mana);
        const earned = Math.ceil(baseReward * bonuses.prestigePearlMult);

        // Conserver un % de poissons
        let keptFish: PoissonInstance[] = [];
        if (bonuses.prestigeKeepFishPercent > 0) {
          const keepCount = Math.max(1, Math.floor(s.poissons.length * bonuses.prestigeKeepFishPercent / 100));
          keptFish = s.poissons.slice(0, keepCount);
        }

        return {
          mana: new Decimal(bonuses.prestigeStartingMana),
          poissons: keptFish,
          pondDepth: bonuses.prestigeStartingDepth,
          boostActiveUntil: 0,
          prestiges: s.prestiges + 1,
          perles: s.perles + earned,
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

      // Marché des Perles utilise des GEMMES
      buyPearlUpgrade: (id) => set((s) => {
        if (s.pearlUpgradesUnlocked.includes(id)) return s;
        const p = PEARL_UPGRADES.find(x => x.id === id);
        if (!p) return s;
        if (p.requires && !s.pearlUpgradesUnlocked.includes(p.requires)) return s;
        if (s.gemmes < p.cost) return s;
        return { gemmes: s.gemmes - p.cost, pearlUpgradesUnlocked: [...s.pearlUpgradesUnlocked, id] };
      }),

      // Améliorations de Prestige utilisent des PERLES
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

      checkAchievements: () => {
        const s = get();
        const toUnlock = ACHIEVEMENTS.filter(
          a => !s.unlockedAchievementIds.includes(a.id) && a.check(s)
        );
        if (toUnlock.length === 0) return;
        const bonuses = computeBonuses(s.researchUnlocked, s.pearlUpgradesUnlocked, s.prestigeUpgradesUnlocked);
        const gems = toUnlock.reduce((sum, a) => sum + Math.ceil(a.gemReward * bonuses.gemmeRewardMult), 0);
        set((state) => ({
          unlockedAchievementIds: [...state.unlockedAchievementIds, ...toUnlock.map(a => a.id)],
          gemmes: state.gemmes + gems,
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
      partialize: (state) => ({ ...state, mana: state.mana.toString() as unknown as Decimal }),
      onRehydrateStorage: () => (state) => {
        if (state) state.mana = new Decimal(state.mana);
      },
    }
  )
);
