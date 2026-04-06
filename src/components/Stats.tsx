import { useEffect, useState } from 'react';
import { useGameStore } from '../store/useGameStore';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

const DEPTH_NAMES = ['Peu profond', 'Standard', 'Profond', 'Abyssal', 'Maximum'];

export const Stats = () => {
  const mana = useGameStore(state => state.mana);
  const gemmes = useGameStore(state => state.gemmes);
  const perles = useGameStore(state => state.perles);
  const poissons = useGameStore(state => state.poissons);
  const pondDepth = useGameStore(state => state.pondDepth);
  const boostActiveUntil = useGameStore(state => state.boostActiveUntil);

  const [isBoostActive, setIsBoostActive] = useState(() => boostActiveUntil > Date.now());

  useEffect(() => {
    const interval = setInterval(() => {
      setIsBoostActive(boostActiveUntil > Date.now());
    }, 1000);
    return () => clearInterval(interval);
  }, [boostActiveUntil]);

  let manaPerSec = new Decimal(0);
  for (const fish of poissons) {
    const multiplier = new Decimal(1.5).pow(fish.level - 1);
    manaPerSec = manaPerSec.add(new Decimal(fish.baseIncome).mul(multiplier));
  }
  if (isBoostActive) manaPerSec = manaPerSec.mul(2);

  return (
    <div className="bg-slate-900/60 backdrop-blur-md p-6 rounded-xl border border-white/10 shadow-xl pointer-events-auto min-w-[300px]">
      <div className="flex flex-col gap-4">
        <div>
          <h2 className="text-sm text-blue-300 font-semibold uppercase tracking-wider">Mana</h2>
          <div className="text-4xl font-bold text-blue-100 font-mono tracking-tight drop-shadow-md">
            {formatNumber(mana)}
          </div>
          <div className="text-sm text-blue-400 mt-1">
            + {formatNumber(manaPerSec)} / sec
            {isBoostActive && <span className="text-yellow-400 font-bold ml-1">(x2 BOOST)</span>}
          </div>
        </div>

        <div className="flex justify-between items-end border-t border-white/10 pt-4">
          <div>
            <h2 className="text-sm text-emerald-300 font-semibold uppercase tracking-wider">Gemmes 💎</h2>
            <div className="text-2xl font-bold text-emerald-100">{gemmes}</div>
          </div>
          {perles > 0 && (
            <div className="text-right">
              <h2 className="text-sm text-purple-300 font-semibold uppercase tracking-wider">Perles 🪸</h2>
              <div className="text-2xl font-bold text-purple-100">{perles}</div>
            </div>
          )}
        </div>

        <div className="flex justify-between items-end border-t border-white/10 pt-3">
          <div>
            <h2 className="text-sm text-cyan-300 font-semibold uppercase tracking-wider">Poissons</h2>
            <div className="text-2xl font-bold text-cyan-100">{poissons.length}</div>
          </div>
          <div className="text-right">
            <h2 className="text-sm text-teal-300 font-semibold uppercase tracking-wider">Profondeur ⛏️</h2>
            <div className="text-base font-bold text-teal-200">
              Niv. {pondDepth} <span className="text-sm text-teal-500 font-normal">— {DEPTH_NAMES[pondDepth] ?? 'Maximum'}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
