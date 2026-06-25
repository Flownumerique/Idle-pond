import { useEffect } from 'react';
import { useGameStore } from '../store/useGameStore';
import { CHALLENGE_POOL, getDailyChallengeIds } from '../data/challenges';
import { getSessionManaEarned } from '../utils/session';

export const Challenges = () => {
  const poissons = useGameStore(s => s.poissons);
  const pondDepth = useGameStore(s => s.pondDepth);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);
  const dailyChallengesCompleted = useGameStore(s => s.dailyChallengesCompleted);
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
    <div className="lg-panel">
      <div className="lg-panel-head">
        <h3>Défis du Jour</h3>
        <span className="lg-section-eyebrow">Reset dans {hoursLeft}h {minsLeft}m</span>
      </div>

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
            className={`lg-card${completed ? ' lg-card--done' : met ? ' lg-card--hot' : ''}`}
          >
            <div className="lg-card-row">
              <div style={{ flex: 1, minWidth: 0 }}>
                <div className="lg-card-title">{completed ? '✓ ' : ''}{ch.name}</div>
                <div className="lg-card-desc">{ch.description}</div>
              </div>
              <div style={{ textAlign: 'right', flexShrink: 0 }}>
                <div className="lg-card-reward">+{ch.pearlReward} 🪸</div>
                {!completed && (
                  <button
                    onClick={() => claimChallenge(ch.id)}
                    disabled={!met}
                    className="cbtn cbtn--coral cbtn--xs"
                    style={{ marginTop: 6 }}
                  >
                    {met ? 'Réclamer' : 'En cours'}
                  </button>
                )}
              </div>
            </div>

            {!completed && (
              <div className={`lg-prog${met ? ' met' : ''}`}>
                <i style={{ width: met ? '100%' : '33%' }} />
              </div>
            )}
          </div>
        );
      })}

      <p className="lg-panel-note" style={{ textAlign: 'center', fontSize: 11 }}>
        3 défis renouvelés chaque jour à minuit
      </p>
    </div>
  );
};
