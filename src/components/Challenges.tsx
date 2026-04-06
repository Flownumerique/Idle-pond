import { useEffect } from 'react';
import { useGameStore } from '../store/useGameStore';
import { CHALLENGE_POOL, getDailyChallengeIds } from '../data/challenges';
import { getSessionManaEarned } from '../utils/session';

export const Challenges = () => {
  const poissons = useGameStore(s => s.poissons);
  const pondDepth = useGameStore(s => s.pondDepth);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);
  const dailyChallengesCompleted = useGameStore(s => s.dailyChallengesCompleted);
  const lastChallengeDate = useGameStore(s => s.lastChallengeDate);
  const claimChallenge = useGameStore(s => s.claimChallenge);
  const checkDailyReset = useGameStore(s => s.checkDailyReset);

  // Vérifie le reset quotidien à l'ouverture
  useEffect(() => {
    checkDailyReset();
  }, [checkDailyReset]);

  const todayIds = getDailyChallengeIds();
  const challenges = todayIds
    .map(id => CHALLENGE_POOL.find(c => c.id === id))
    .filter(Boolean) as typeof CHALLENGE_POOL;

  // Calcul du reset : minuit prochain
  const now = new Date();
  const midnight = new Date(now);
  midnight.setHours(24, 0, 0, 0);
  const msLeft = midnight.getTime() - now.getTime();
  const hoursLeft = Math.floor(msLeft / 3_600_000);
  const minsLeft = Math.floor((msLeft % 3_600_000) / 60_000);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Défis du Jour</h3>
        <span className="text-[10px] text-gray-500">
          Reset dans {hoursLeft}h {minsLeft}m
        </span>
      </div>

      <div className="flex flex-col gap-3">
        {challenges.map(ch => {
          const completed = dailyChallengesCompleted.includes(ch.id);
          const met = ch.check({
            poissons,
            pondDepth,
            researchUnlocked,
            sessionManaEarned: getSessionManaEarned(),
          });

          return (
            <div
              key={ch.id}
              className={`rounded-lg p-4 border transition-all ${
                completed
                  ? 'bg-green-900/15 border-green-700/20 opacity-70'
                  : met
                    ? 'bg-orange-900/20 border-orange-600/30'
                    : 'bg-white/[0.03] border-white/5'
              }`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex-1">
                  <div className={`font-semibold text-sm ${completed ? 'text-gray-400' : 'text-white'}`}>
                    {completed ? '✓ ' : ''}{ch.name}
                  </div>
                  <div className="text-xs text-gray-400 mt-0.5">{ch.description}</div>
                </div>
                <div className="text-right shrink-0">
                  <div className="text-xs font-bold text-purple-300">+{ch.pearlReward} 🪸</div>
                  {!completed && (
                    <button
                      onClick={() => claimChallenge(ch.id)}
                      disabled={!met}
                      className={`mt-1.5 px-2.5 py-1 rounded text-xs font-bold transition-all ${
                        met
                          ? 'bg-orange-600 hover:bg-orange-500 text-white'
                          : 'bg-gray-800 text-gray-600 cursor-not-allowed opacity-60'
                      }`}
                    >
                      {met ? 'Réclamer' : 'En cours'}
                    </button>
                  )}
                </div>
              </div>

              {/* Barre de progression visuelle */}
              {!completed && (
                <div className="mt-2 h-1 rounded-full bg-white/5 overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all ${met ? 'bg-orange-500 w-full' : 'bg-indigo-700 w-1/3'}`}
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>

      <p className="text-[10px] text-gray-600 text-center">
        3 défis renouvelés chaque jour à minuit
      </p>
    </div>
  );
};
