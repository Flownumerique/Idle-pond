import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import Decimal from 'break_infinity.js';

export interface Fish {
  id: string;
  type: string;
  level: number;
  baseIncome: number;
}

export interface GameState {
  mana: Decimal;
  gemmes: number;
  poissons: Fish[];
  pondSize: number;
  lastSaveTime: number;
  boostActiveUntil: number;

  // Actions
  addMana: (amount: Decimal | string | number) => void;
  buyFish: (type: string, baseIncome: number, cost: Decimal | string | number) => boolean;
  upgradeFish: (id: string, cost: Decimal | string | number) => boolean;
  spendGemmes: (amount: number) => boolean;
  activateBoost: () => void;
  updateLastSaveTime: () => void;
}

export const useGameStore = create<GameState>()(
  persist(
    (set, get) => ({
      mana: new Decimal(0),
      gemmes: 50, // Starting gems
      poissons: [],
      pondSize: 10,
      lastSaveTime: Date.now(),
      boostActiveUntil: 0,

      addMana: (amount) => set((state) => ({
        mana: state.mana.add(new Decimal(amount))
      })),

      buyFish: (type, baseIncome, cost) => {
        const state = get();
        const costDecimal = new Decimal(cost);

        if (state.mana.gte(costDecimal) && state.poissons.length < state.pondSize) {
          const newFish: Fish = {
            id: crypto.randomUUID(),
            type,
            level: 1,
            baseIncome,
          };

          set({
            mana: state.mana.sub(costDecimal),
            poissons: [...state.poissons, newFish]
          });
          return true;
        }
        return false;
      },

      upgradeFish: (id, cost) => {
        const state = get();
        const costDecimal = new Decimal(cost);

        if (state.mana.gte(costDecimal)) {
          set((state) => ({
            mana: state.mana.sub(costDecimal),
            poissons: state.poissons.map(fish =>
              fish.id === id ? { ...fish, level: fish.level + 1 } : fish
            )
          }));
          return true;
        }
        return false;
      },

      spendGemmes: (amount) => {
        const state = get();
        if (state.gemmes >= amount) {
          set({ gemmes: state.gemmes - amount });
          return true;
        }
        return false;
      },

      activateBoost: () => {
        const BOOST_COST = 10;
        const BOOST_DURATION_MS = 5 * 60 * 1000; // 5 minutes

        const state = get();
        if (state.gemmes >= BOOST_COST) {
          set({
            gemmes: state.gemmes - BOOST_COST,
            boostActiveUntil: Date.now() + BOOST_DURATION_MS
          });
        }
      },

      updateLastSaveTime: () => set({ lastSaveTime: Date.now() }),
    }),
    {
      name: 'etang-des-merveilles-storage',
      storage: {
        getItem: (name) => {
          const str = localStorage.getItem(name);
          if (!str) return null;
          try {
            const parsed = JSON.parse(str);
            if (parsed && parsed.state && parsed.state.mana) {
              parsed.state.mana = new Decimal(parsed.state.mana);
            }
            return parsed;
          } catch (e) {
            console.error(e);
            return null;
          }
        },
        setItem: (name, value) => {
          // Break infinity JS Decimal is stringified appropriately by JSON.stringify
          localStorage.setItem(name, JSON.stringify(value));
        },
        removeItem: (name) => localStorage.removeItem(name),
      },
    }
  )
);
