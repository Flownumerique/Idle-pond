import React, { useState, useEffect } from 'react';
import { useGameStore, Fish } from '../store/useGameStore';
import { FISHES } from '../constants/fishes';
import { formatNumber } from '../utils/formatter';
import Decimal from 'break_infinity.js';
import { EventBus } from '../game/EventBus';

export const Shop: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [isShiftDown, setIsShiftDown] = useState(false);

  const mana = useGameStore(state => state.mana);
  const ownedFishes = useGameStore(state => state.ownedFishes);
  const buyFish = useGameStore(state => state.buyFish);

  // Écouteur pour la touche SHIFT
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Shift') setIsShiftDown(true);
    };
    const handleKeyUp = (e: KeyboardEvent) => {
      if (e.key === 'Shift') setIsShiftDown(false);
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
    };
  }, []);

  const calculateCost = (fish: Fish, currentOwned: number): Decimal => {
    return fish.baseCost.times(Decimal.pow(fish.growthRate, currentOwned));
  };

  const handleBuy = (fish: Fish) => {
    const owned = ownedFishes.find(f => f.id === fish.id)?.quantite || 0;
    const cost = calculateCost(fish, owned);

    // Fonctionnalité "Acheter Max" si SHIFT est pressé
    if (isShiftDown) {
      let currentOwned = owned;
      let totalCost = new Decimal(0);
      let nextCost = calculateCost(fish, currentOwned);

      // On simule les achats un par un pour s'assurer qu'on peut payer le total
      // C'est une boucle naïve pour l'exemple, dans un vrai jeu on utiliserait la formule géométrique pour O(1)
      // On va limiter la boucle à 100 pour éviter de bloquer l'UI
      let countToBuy = 0;
      let tempMana = mana;

      while (tempMana.gte(nextCost) && countToBuy < 100) {
        tempMana = tempMana.minus(nextCost);
        totalCost = totalCost.plus(nextCost);
        countToBuy++;
        currentOwned++;
        nextCost = calculateCost(fish, currentOwned);
      }

      if (countToBuy > 0) {
        // Appliquer les achats dans le store
        // Comme le store s'attend à un appel par achat (ou il faudrait modifier buyFish),
        // on fait plusieurs appels, ce qui déclenchera aussi l'event SPAWN_FISH via PhaserContainer
        for(let i=0; i<countToBuy; i++){
          buyFish(fish);
          EventBus.emit('FISH_PURCHASED', { fishId: fish.id });
        }
      }
    } else {
      // Achat simple
      if (mana.gte(cost)) {
        buyFish(fish);
        EventBus.emit('FISH_PURCHASED', { fishId: fish.id });
      }
    }
  };

  return (
    <>
      {/* Bouton d'ouverture (visible si la boutique est fermée) */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="fixed right-4 top-1/2 -translate-y-1/2 bg-blue-600/80 backdrop-blur-md text-white px-4 py-8 rounded-l-2xl shadow-lg border border-blue-400/30 hover:bg-blue-500/80 transition-colors pointer-events-auto"
        >
          <div className="writing-vertical-rl font-bold tracking-widest uppercase">
            Boutique
          </div>
        </button>
      )}

      {/* Panneau latéral rétractable */}
      <div
        className={`fixed top-0 right-0 h-full w-[300px] bg-slate-900/90 backdrop-blur-xl border-l border-white/10 shadow-2xl transition-transform duration-200 ease-in-out pointer-events-auto flex flex-col ${
          isOpen ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        <div className="p-4 border-b border-white/10 flex justify-between items-center bg-slate-800/50">
          <h2 className="text-xl font-bold text-white uppercase tracking-wider">Boutique</h2>
          <button
            onClick={() => setIsOpen(false)}
            className="text-slate-400 hover:text-white p-2"
          >
            ✕
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-4">
          <div className="text-xs text-slate-400 text-center mb-2">
            Maintenez <kbd className="bg-slate-800 px-1 rounded border border-slate-700">SHIFT</kbd> pour Acheter Max
          </div>

          {FISHES.map(fish => {
            const owned = ownedFishes.find(f => f.id === fish.id)?.quantite || 0;
            const currentCost = calculateCost(fish, owned);
            const canAfford = mana.gte(currentCost);

            return (
              <div key={fish.id} className="bg-slate-800/60 border border-slate-700/50 rounded-xl p-3 flex flex-col gap-2 relative overflow-hidden">
                <div className="flex justify-between items-start">
                  <div>
                    <h3 className="font-bold text-white text-lg">{fish.name}</h3>
                    <p className="text-sm text-emerald-400">+{formatNumber(fish.baseIncome)}/s</p>
                  </div>
                  <div className="bg-slate-900/80 px-2 py-1 rounded text-sm font-mono text-slate-300">
                    x{owned}
                  </div>
                </div>

                <button
                  onClick={() => handleBuy(fish)}
                  disabled={!canAfford}
                  className={`min-h-[44px] w-full rounded-lg font-bold flex justify-between items-center px-4 transition-all ${
                    canAfford
                      ? 'bg-blue-600 hover:bg-blue-500 text-white shadow-[0_0_10px_rgba(37,99,235,0.3)]'
                      : 'bg-slate-700 text-slate-500 cursor-not-allowed'
                  }`}
                >
                  <span>{isShiftDown ? 'Acheter Max' : 'Acheter'}</span>
                  <span className={`font-mono ${canAfford ? 'text-blue-100' : 'text-slate-500'}`}>
                    {formatNumber(currentCost)} Mana
                  </span>
                </button>
              </div>
            );
          })}
        </div>
      </div>
    </>
  );
};
