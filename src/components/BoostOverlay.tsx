import { useGameStore } from '../store/useGameStore';
import { useEffect, useState } from 'react';

export const BoostOverlay = () => {
  const gemmes = useGameStore(state => state.gemmes);
  const boostActiveUntil = useGameStore(state => state.boostActiveUntil);
  const activateBoost = useGameStore(state => state.activateBoost);

  const [timeLeft, setTimeLeft] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      const remaining = boostActiveUntil - Date.now();
      setTimeLeft(remaining > 0 ? remaining : 0);
    }, 1000);
    return () => clearInterval(interval);
  }, [boostActiveUntil]);

  const isActive = timeLeft > 0;
  const minutes = Math.floor(timeLeft / 60000);
  const seconds = Math.floor((timeLeft % 60000) / 1000);

  return (
    <div className="bg-slate-900/60 backdrop-blur-md p-4 rounded-xl border border-white/10 shadow-xl pointer-events-auto mt-4 w-fit">
      <div className="flex flex-col gap-3">
        <h2 className="text-sm font-semibold text-yellow-400 uppercase tracking-wider flex items-center gap-2">
          <span>⚡ Boost de Mana</span>
        </h2>

        <p className="text-sm text-gray-300">
          Multipliez votre production par 2 pendant 5 minutes.
        </p>

        {isActive ? (
          <div className="bg-yellow-900/30 border border-yellow-500/30 rounded-lg p-3 text-center">
            <span className="text-yellow-400 font-mono font-bold text-lg animate-pulse">
              {minutes}:{seconds.toString().padStart(2, '0')}
            </span>
            <div className="text-xs text-yellow-500/80 mt-1 uppercase tracking-widest font-bold">Actif</div>
          </div>
        ) : (
          <button
            onClick={activateBoost}
            disabled={gemmes < 10}
            className={`w-full py-2.5 rounded-lg font-bold text-sm transition-all shadow-lg flex items-center justify-center gap-2 ${
              gemmes >= 10
                ? 'bg-gradient-to-r from-yellow-600 to-amber-600 hover:from-yellow-500 hover:to-amber-500 text-white shadow-amber-900/50 hover:scale-[1.02]'
                : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
            }`}
          >
            Activer pour 10 Gemmes
          </button>
        )}
      </div>
    </div>
  );
};
