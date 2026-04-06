import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import Decimal from 'break_infinity.js';
import { ACHIEVEMENTS } from '../data/achievements';

export const MAX_FISH_LEVEL = 100;

export interface PoissonInstance {
  id: string;
  type: string;
  baseIncome: number;
  level: number;
}

// Coût pour passer au prochain niveau de profondeur
export const getPondUpgradeCost = (currentDepth: number): Decimal => {
  return new Decimal(500).times(Decimal.pow(10, currentDepth));
};

// Perles de prestige gagnées en fonction de la progression
const calcPrestigeReward = (pondDepth: number, mana: Decimal): number => {
  const depthBonus = pondDepth * 5;
  const manaBonus = mana.gt(0) ? Math.floor(Math.log10(mana.toNumber() + 1)) : 0;
  return Math.max(1, depthBonus + manaBonus);
};

export interface GameState {
  mana: Decimal;
  gemmes: number;
  perles: number;               // Monnaie de prestige
  poissons: PoissonInstance[];
  pondDepth: number;
  boostActiveUntil: number;
  lastSaveTime: number;
  unlockedAchievementIds: string[];
  pendingUnlock: string | null;  // Notification de déblocage en attente

  // Actions
  addMana: (amount: Decimal) => void;
  buyFish: (type: string, baseIncome: number, cost: Decimal) => void;
  buyFishBulk: (type: string, baseIncome: number, baseCost: number, count: number) => void;
  upgradeFish: (id: string, cost: Decimal) => void;
  upgradePond: () => void;
  activateBoost: () => void;
  prestige: () => void;
  checkAchievements: () => void;
  clearPendingUnlock: () => void;
  updateLastSaveTime: () => void;
}

const BOOST_DURATION_MS = 5 * 60 * 1000;
const BOOST_COST_GEMMES = 10;
const MAX_DEPTH = 4;

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
      unlockedAchievementIds: [],
      pendingUnlock: null,

      addMana: (amount: Decimal) => set((state) => ({
        mana: state.mana.plus(amount)
      })),

      buyFish: (type: string, baseIncome: number, cost: Decimal) => set((state) => {
        if (!state.mana.gte(cost)) return state;

        const newFish: PoissonInstance = {
          id: `${type}-${Date.now()}-${Math.random().toString(36).slice(2)}`,
          type,
          baseIncome,
          level: 1,
        };

        return {
          mana: state.mana.minus(cost),
          poissons: [...state.poissons, newFish],
        };
      }),

      buyFishBulk: (type: string, baseIncome: number, baseCost: number, count: number) => set((state) => {
        const ownedCount = state.poissons.filter(f => f.type === type).length;
        let totalCost = new Decimal(0);
        const newFish: PoissonInstance[] = [];

        for (let i = 0; i < count; i++) {
          const cost = new Decimal(baseCost).mul(new Decimal(1.15).pow(ownedCount + i));
          totalCost = totalCost.plus(cost);
          newFish.push({
            id: `${type}-${Date.now()}-${i}-${Math.random().toString(36).slice(2)}`,
            type,
            baseIncome,
            level: 1,
          });
        }

        if (!state.mana.gte(totalCost)) return state;

        return {
          mana: state.mana.minus(totalCost),
          poissons: [...state.poissons, ...newFish],
        };
      }),

      upgradeFish: (id: string, cost: Decimal) => set((state) => {
        if (!state.mana.gte(cost)) return state;

        const index = state.poissons.findIndex(f => f.id === id);
        if (index === -1) return state;
        if (state.poissons[index].level >= MAX_FISH_LEVEL) return state;

        const updatedPoissons = [...state.poissons];
        updatedPoissons[index] = {
          ...updatedPoissons[index],
          level: updatedPoissons[index].level + 1,
        };

        return {
          mana: state.mana.minus(cost),
          poissons: updatedPoissons,
        };
      }),

      upgradePond: () => set((state) => {
        if (state.pondDepth >= MAX_DEPTH) return state;
        const cost = getPondUpgradeCost(state.pondDepth);
        if (!state.mana.gte(cost)) return state;

        const newDepth = state.pondDepth + 1;
        return {
          mana: state.mana.minus(cost),
          pondDepth: newDepth,
          pendingUnlock: `depth_${newDepth}`,
        };
      }),

      activateBoost: () => set((state) => {
        if (state.gemmes < BOOST_COST_GEMMES || state.boostActiveUntil > Date.now()) return state;

        return {
          gemmes: state.gemmes - BOOST_COST_GEMMES,
          boostActiveUntil: Date.now() + BOOST_DURATION_MS,
        };
      }),

      prestige: () => set((state) => {
        if (state.pondDepth < 2) return state; // Minimum depth 2 to prestige

        const earned = calcPrestigeReward(state.pondDepth, state.mana);
        return {
          mana: new Decimal(10),
          poissons: [],
          pondDepth: 0,
          boostActiveUntil: 0,
          perles: state.perles + earned,
          // Garder : gemmes, perles, unlockedAchievementIds
        };
      }),

      checkAchievements: () => {
        const state = get();
        const toUnlock = ACHIEVEMENTS.filter(
          a => !state.unlockedAchievementIds.includes(a.id) && a.check(state)
        );

        if (toUnlock.length === 0) return;

        const newGemmes = toUnlock.reduce((sum, a) => sum + a.gemReward, 0);
        set((s) => ({
          unlockedAchievementIds: [...s.unlockedAchievementIds, ...toUnlock.map(a => a.id)],
          gemmes: s.gemmes + newGemmes,
        }));
      },

      clearPendingUnlock: () => set({ pendingUnlock: null }),

      updateLastSaveTime: () => set({ lastSaveTime: Date.now() }),
    }),
    {
      name: 'etang-des-merveilles-storage',
      partialize: (state) => ({
        ...state,
        mana: state.mana.toString() as unknown as Decimal,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.mana = new Decimal(state.mana);
        }
      }
    }
  )
);
