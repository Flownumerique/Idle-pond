import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import Decimal from 'break_infinity.js';

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

// Capacité de l'étang selon la profondeur
export const getPondSize = (depth: number): number => {
  return 10 + depth * 5;
};

export interface GameState {
  mana: Decimal;
  gemmes: number;
  poissons: PoissonInstance[];
  pondDepth: number;      // Profondeur actuelle de l'étang (commence à 0)
  boostActiveUntil: number;
  lastSaveTime: number;

  // Actions
  addMana: (amount: Decimal) => void;
  buyFish: (type: string, baseIncome: number, cost: Decimal) => void;
  upgradeFish: (id: string, cost: Decimal) => void;
  upgradePond: () => void;
  activateBoost: () => void;
  updateLastSaveTime: () => void;
}

const BOOST_DURATION_MS = 5 * 60 * 1000; // 5 minutes
const BOOST_COST_GEMMES = 10;

export const useGameStore = create<GameState>()(
  persist(
    (set, get) => ({
      mana: new Decimal(10),
      gemmes: 0,
      poissons: [],
      pondDepth: 0,
      boostActiveUntil: 0,
      lastSaveTime: Date.now(),

      addMana: (amount: Decimal) => set((state) => ({
        mana: state.mana.plus(amount)
      })),

      buyFish: (type: string, baseIncome: number, cost: Decimal) => set((state) => {
        const pondSize = getPondSize(state.pondDepth);
        if (!state.mana.gte(cost) || state.poissons.length >= pondSize) return state;

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

      upgradeFish: (id: string, cost: Decimal) => set((state) => {
        if (!state.mana.gte(cost)) return state;

        const index = state.poissons.findIndex(f => f.id === id);
        if (index === -1) return state;

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
        const cost = getPondUpgradeCost(state.pondDepth);
        if (!state.mana.gte(cost)) return state;

        return {
          mana: state.mana.minus(cost),
          pondDepth: state.pondDepth + 1,
        };
      }),

      activateBoost: () => set((state) => {
        if (state.gemmes < BOOST_COST_GEMMES || state.boostActiveUntil > Date.now()) return state;

        return {
          gemmes: state.gemmes - BOOST_COST_GEMMES,
          boostActiveUntil: Date.now() + BOOST_DURATION_MS,
        };
      }),

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
