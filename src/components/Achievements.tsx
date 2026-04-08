import { useGameStore } from '../store/useGameStore';
import { ACHIEVEMENTS } from '../data/achievements';

export const Achievements = () => {
  const unlockedIds = useGameStore(state => state.unlockedAchievementIds);
  const gemmes = useGameStore(state => state.gemmes);

  const unlocked = ACHIEVEMENTS.filter(a => unlockedIds.includes(a.id));
  const locked = ACHIEVEMENTS.filter(a => !unlockedIds.includes(a.id));

  return (
    <div className="pointer-events-auto flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="font-bold text-white text-lg">Succès</h3>
        <div className="flex items-center gap-2">
          <span className="text-xs text-emerald-300 font-semibold">Gemmes : {gemmes} 💎</span>
          <span className="bg-yellow-700/40 text-yellow-200 text-xs px-2 py-0.5 rounded-full border border-yellow-600/30 font-semibold">
            {unlocked.length}/{ACHIEVEMENTS.length}
          </span>
        </div>
      </div>

      {unlocked.length > 0 && (
        <div className="flex flex-col gap-2">
          <div className="text-[10px] text-gray-500 uppercase tracking-widest">Débloqués</div>
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
        <div className="flex flex-col gap-2">
          <div className="text-[10px] text-gray-500 uppercase tracking-widest">À débloquer</div>
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
  );
};
