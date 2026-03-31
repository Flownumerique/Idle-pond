import React, { useEffect, useState, useRef } from 'react';
import { useGameStore } from '../store/useGameStore';
import { formatNumber } from '../utils/formatter';
import Decimal from 'break_infinity.js';

export const Stats: React.FC = () => {
  const mana = useGameStore((state) => state.mana);
  const gemmes = useGameStore((state) => state.gemmes);

  const [popEffect, setPopEffect] = useState(false);
  const previousMana = useRef<Decimal>(mana);

  useEffect(() => {
    // Détecter un gain important de mana
    if (mana.gt(previousMana.current)) {
      // Pour éviter le warning ESLint sur le setState synchrone dans un effet,
      // on peut l'envelopper dans un setTimeout court, ce qui est souvent suffisant
      // pour sortir du cycle de rendu actuel et faire l'animation correctement.
      const startTimer = setTimeout(() => {
        setPopEffect(true);
      }, 0);

      const endTimer = setTimeout(() => setPopEffect(false), 300); // 300ms correspond à duration-300
      previousMana.current = mana;
      return () => {
        clearTimeout(startTimer);
        clearTimeout(endTimer);
      };
    }
    previousMana.current = mana;
  }, [mana]);

  return (
    <div className="flex gap-4 p-4 pointer-events-auto">
      {/* Bento Box: Mana */}
      <div className="flex flex-col items-center justify-center bg-blue-900/30 backdrop-blur-md rounded-2xl p-4 min-w-[120px] shadow-lg border border-blue-500/20">
        <span className="text-blue-300 text-sm font-semibold uppercase tracking-wider mb-1">Mana</span>
        <span
          className={`text-2xl font-bold text-white transition-transform duration-300 ${
            popEffect ? 'scale-110 text-blue-200 drop-shadow-[0_0_8px_rgba(59,130,246,0.8)]' : 'scale-100'
          }`}
        >
          {formatNumber(mana)}
        </span>
      </div>

      {/* Bento Box: Gemmes */}
      <div className="flex flex-col items-center justify-center bg-purple-900/30 backdrop-blur-md rounded-2xl p-4 min-w-[120px] shadow-lg border border-purple-500/20">
        <span className="text-purple-300 text-sm font-semibold uppercase tracking-wider mb-1">Gemmes</span>
        <span className="text-2xl font-bold text-white">
          {formatNumber(gemmes)}
        </span>
      </div>
    </div>
  );
};
