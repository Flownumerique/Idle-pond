import { useGameStore } from '../store/useGameStore';
import { ACHIEVEMENTS } from '../data/achievements';
import { computeBonuses } from '../utils/bonuses';

export const Achievements = () => {
  const unlockedIds = useGameStore(s => s.unlockedAchievementIds);
  const claimedIds = useGameStore(s => s.claimedAchievementIds);
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
    <div className="lg-panel">
      <div className="lg-panel-head">
        <h3>Succès</h3>
        <span className="lg-chip gold">{claimed.length}/{ACHIEVEMENTS.length}</span>
      </div>

      {/* À récupérer */}
      {claimable.length > 0 && (
        <>
          <div className="lg-panel-head">
            <span className="lg-section-eyebrow" style={{ color: '#C98A0F' }}>À récupérer ({claimable.length})</span>
            {totalGemsPending > 0 && (
              <span className="lg-section-eyebrow" style={{ color: '#2FB873' }}>+{totalGemsPending} 💎</span>
            )}
          </div>
          {claimable.map(a => {
            const reward = Math.ceil(a.gemReward * bonuses.gemmeRewardMult);
            return (
              <div key={a.id} className="lg-card lg-card--gold">
                <div className="lg-card-row">
                  <span className="lg-card-icon">🏆</span>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div className="lg-card-title">{a.name}</div>
                    <div className="lg-card-desc">{a.description}</div>
                  </div>
                  <button
                    onClick={() => claimAchievement(a.id)}
                    className="cbtn cbtn--emerald cbtn--xs"
                    style={{ flexShrink: 0 }}
                  >
                    +{reward} 💎
                  </button>
                </div>
              </div>
            );
          })}
        </>
      )}

      {/* Récupérés */}
      {claimed.length > 0 && (
        <>
          <div className="lg-section-eyebrow">Récupérés</div>
          {claimed.map(a => (
            <div key={a.id} className="lg-card lg-card--done">
              <div className="lg-card-row">
                <span className="lg-card-icon">✅</span>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div className="lg-card-title">{a.name}</div>
                  <div className="lg-card-desc">{a.description}</div>
                </div>
                <span className="lg-section-eyebrow" style={{ whiteSpace: 'nowrap' }}>+{a.gemReward} 💎</span>
              </div>
            </div>
          ))}
        </>
      )}

      {/* Verrouillés */}
      {locked.length > 0 && (
        <>
          <div className="lg-section-eyebrow">À débloquer</div>
          {locked.map(a => (
            <div key={a.id} className="lg-card lg-card--locked">
              <div className="lg-card-row">
                <span className="lg-card-icon" style={{ filter: 'grayscale(1)' }}>🔒</span>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div className="lg-card-title">{a.name}</div>
                  <div className="lg-card-desc">{a.description}</div>
                </div>
                <span className="lg-section-eyebrow" style={{ whiteSpace: 'nowrap' }}>+{a.gemReward} 💎</span>
              </div>
            </div>
          ))}
        </>
      )}
    </div>
  );
};
