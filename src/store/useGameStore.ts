import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import Decimal from 'break_infinity.js';

export interface Fish {
  id: string;
  name: string;
  baseCost: Decimal;
  growthRate: number;
  baseIncome: Decimal;
}

export interface OwnedFish {
  id: string;
  quantite: number;
}

export interface GameState {
  mana: Decimal;
  gemmes: number;
  ownedFishes: OwnedFish[];
  multiplierGlobal: number;

  addMana: (amount: Decimal) => void;
  buyFish: (fish: Fish) => void;
}

export const useGameStore = create<GameState>()(
  persist(
    (set) => ({
      mana: new Decimal(10),
      gemmes: 0,
      ownedFishes: [],
      multiplierGlobal: 1,

      addMana: (amount: Decimal) => set((state) => ({
        mana: state.mana.plus(amount)
      })),

      buyFish: (fish: Fish) => set((state) => {
        const owned = state.ownedFishes.find(f => f.id === fish.id);
        const quantitePossedee = owned ? owned.quantite : 0;

        // Coût = Base * (Multiplicateur ^ Possédés)
        const cost = fish.baseCost.times(Decimal.pow(fish.growthRate, quantitePossedee));

        if (state.mana.gte(cost)) {
          const newMana = state.mana.minus(cost);
          const newOwnedFishes = [...state.ownedFishes];

          if (owned) {
            const index = newOwnedFishes.findIndex(f => f.id === fish.id);
            newOwnedFishes[index] = {
              ...newOwnedFishes[index],
              quantite: newOwnedFishes[index].quantite + 1
            };
          } else {
            newOwnedFishes.push({ id: fish.id, quantite: 1 });
          }

          return {
            mana: newMana,
            ownedFishes: newOwnedFishes
          };
        }

        return state; // Pas assez de mana, on ne modifie pas l'état
      })
    }),
    {
      name: 'etang-des-merveilles-storage',
      partialize: (state) => ({
        ...state,
        // Conversion explicite de Decimal en string pour la sérialisation
        mana: state.mana.toString() as unknown as Decimal,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          // Reconstruction de l'objet Decimal à la réhydratation
          state.mana = new Decimal(state.mana);
        }
      }
    }
  )
);
