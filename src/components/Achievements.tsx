import { useGameStore } from '../store/useGameStore';
import { ACHIEVEMENTS } from '../data/achievements';
import { computeBonuses } from '../utils/bonuses';

export const Achievements = () => {
  const unlockedIds = useGameStore(s => s.unlockedAchievementIds);
  const claimedIds = useGameStore(s => s.claimedAchievementIds);
  const gemmes = useGameStore(s => s.gemmes);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);
  const pearlUpgradesUnlocked = useGameStore(s => s.pearlUpgradesUnlocked);
  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const runUpgradesOwned = useGameStore(s => s.runUpgradesOwned);
  const claimAchievement = useGameStore(s => s.claimAchievement);

  const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned);

  const claimable = ACHIEVEMENTS.filter(a => unlockedIds.includes(a.id) && !claimedIds.includes(a.id));
  const claimed = ACHIEVEMENTS.filter(a => claimedIds.includes(a.id));
  const locked = ACHIEVEMENTS.filter(a => !unlockedIds.includes(a.id));

  const totalGemsPending = claimable.reduce(
    (sum, a) => sum + Math.ceil(a.gemReward * bonuses.gemmeRewardMult),
    0
  );

  return (
    <div className="pointer-events-auto flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="font-bold text-white text-lg">Succès</h3>
        <div className="flex items-center gap-2">
          <span className="text-xs text-emerald-300 font-semibold">{gemmes} 💎</span>
          <span className="bg-yellow-700/40 text-yellow-200 text-xs px-2 py-0.5 rounded-full border border-yellow-600/30 font-semibold">
            {claimed.length}/{ACHIEVEMENTS.length}
          </span>
        </div>
      </div>

      {/* À récupérer */}
      {claimable.length > 0 && (
        <div className="flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <div className="text-[10px] text-yellow-400 uppercase tracking-widest font-bold">
              À récupérer ({claimable.length})
            </div>
            {totalGemsPending > 0 && (
              <span className="text-[10px] text-emerald-400 font-bold">+{totalGemsPending} 💎 disponibles</span>
            )}
          </div>
          {claimable.map(a => {
            const reward = Math.ceil(a.gemReward * bonuses.gemmeRewardMult);
            return (
              <div
                key={a.id}
                className="flex items-start gap-3 bg-yellow-900/25 rounded-lg p-3 border border-yellow-600/40"
              >
                <span className="text-xl mt-0.5">🏆</span>
                <div className="flex-1 min-w-0">
                  <div className="font-semibold text-sm text-yellow-200">{a.name}</div>
                  <div className="text-xs text-gray-400">{a.description}</div>
                </div>
                <button
                  onClick={() => claimAchievement(a.id)}
                  className="shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold transition-all"
                >
                  +{reward} 💎
                </button>
              </div>
            );
          })}
        </div>
      )}

      {/* Récupérés */}
      {claimed.length > 0 && (
        <div className="flex flex-col gap-2">
          <div className="text-[10px] text-gray-500 uppercase tracking-widest">Récupérés</div>
          {claimed.map(a => (
            <div key={a.id} className="flex items-start gap-3 bg-white/[0.02] rounded-lg p-3 border border-white/5 opacity-60">
              <span className="text-xl mt-0.5">✅</span>
              <div className="flex-1 min-w-0">
                <div className="font-semibold text-sm text-gray-400">{a.name}</div>
                <div className="text-xs text-gray-600">{a.description}</div>
              </div>
              <span className="text-xs text-gray-600 font-bold whitespace-nowrap">+{a.gemReward} 💎</span>
            </div>
          ))}
        </div>
      )}

      {/* Verrouillés */}
      {locked.length > 0 && (
        <div className="flex flex-col gap-2">
          <div className="text-[10px] text-gray-500 uppercase tracking-widest">À débloquer</div>
          {locked.map(a => (
            <div key={a.id} className="flex items-start gap-3 bg-white/[0.02] rounded-lg p-3 border border-white/5 opacity-50">
              <span className="text-xl mt-0.5 grayscale">🔒</span>
              <div className="flex-1 min-w-0">
                <div className="font-semibold text-sm text-gray-500">{a.name}</div>
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
