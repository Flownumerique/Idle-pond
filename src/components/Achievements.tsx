import { useState } from 'react';
import { useGameStore } from '../store/useGameStore';
import { ACHIEVEMENTS } from '../data/achievements';

export const Achievements = () => {
  const unlockedIds = useGameStore(state => state.unlockedAchievementIds);
  const gemmes = useGameStore(state => state.gemmes);
  const [open, setOpen] = useState(false);

  const unlocked = ACHIEVEMENTS.filter(a => unlockedIds.includes(a.id));
  const locked = ACHIEVEMENTS.filter(a => !unlockedIds.includes(a.id));

  return (
    <div className="pointer-events-auto">
      <button
        onClick={() => setOpen(v => !v)}
        className="bg-slate-900/60 backdrop-blur-md px-4 py-2.5 rounded-xl border border-white/10 shadow-xl text-sm font-semibold text-yellow-300 hover:bg-white/10 transition flex items-center gap-2"
      >
        🏆 Succès
        <span className="bg-yellow-700/40 text-yellow-200 text-xs px-2 py-0.5 rounded-full border border-yellow-600/30">
          {unlocked.length}/{ACHIEVEMENTS.length}
        </span>
      </button>

      {open && (
        <div className="absolute left-0 top-full mt-2 w-80 bg-slate-900/95 backdrop-blur-md rounded-xl border border-white/10 shadow-2xl overflow-y-auto max-h-[70vh] z-20">
          <div className="p-4 border-b border-white/10 flex items-center justify-between">
            <h3 className="font-bold text-white">Succès</h3>
            <span className="text-xs text-emerald-300">Gemmes : {gemmes} 💎</span>
          </div>

          {unlocked.length > 0 && (
            <div className="p-3 flex flex-col gap-2">
              <div className="text-[10px] text-gray-500 uppercase tracking-widest px-1">Débloqués</div>
              {unlocked.map(a => (
                <div key={a.id} className="flex items-start gap-3 bg-yellow-900/20 rounded-lg p-3 border border-yellow-700/20">
                  <span className="text-xl mt-0.5">🏆</span>
                  <div className="flex-1">
                    <div className="font-semibold text-sm text-yellow-200">{a.name}</div>
                    <div className="text-xs text-gray-400">{a.description}</div>
                  </div>
                  <span className="text-xs text-emerald-300 font-bold whitespace-nowrap">+{a.gemReward} 💎</span>
                </div>
              ))}
            </div>
          )}

          {locked.length > 0 && (
            <div className="p-3 flex flex-col gap-2">
              <div className="text-[10px] text-gray-500 uppercase tracking-widest px-1">À débloquer</div>
              {locked.map(a => (
                <div key={a.id} className="flex items-start gap-3 bg-white/[0.03] rounded-lg p-3 border border-white/5 opacity-70">
                  <span className="text-xl mt-0.5 grayscale">🔒</span>
                  <div className="flex-1">
                    <div className="font-semibold text-sm text-gray-400">{a.name}</div>
                    <div className="text-xs text-gray-600">{a.description}</div>
                  </div>
                  <span className="text-xs text-gray-600 font-bold whitespace-nowrap">+{a.gemReward} 💎</span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
