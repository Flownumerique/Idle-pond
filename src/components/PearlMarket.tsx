import { useGameStore } from '../store/useGameStore';
import { PEARL_UPGRADES } from '../data/pearlUpgrades';

export const PearlMarket = () => {
  const gemmes = useGameStore(s => s.gemmes);
  const pearlUpgradesUnlocked = useGameStore(s => s.pearlUpgradesUnlocked);
  const buyPearlUpgrade = useGameStore(s => s.buyPearlUpgrade);

  return (
    <div className="lg-panel">
      <div className="lg-panel-head">
        <h3>Marché des Perles</h3>
        <span className="lg-chip gold">{gemmes} 💎</span>
      </div>

      <p className="lg-panel-note">
        Améliorations permanentes achetées avec des Gemmes 💎 (gagnées via les succès).
      </p>

      {PEARL_UPGRADES.map(p => {
        const owned = pearlUpgradesUnlocked.includes(p.id);
        const prereqMet = !p.requires || pearlUpgradesUnlocked.includes(p.requires);
        const canBuy = !owned && prereqMet && gemmes >= p.cost;

        return (
          <div
            key={p.id}
            className={`lg-card${owned ? ' lg-card--done' : !prereqMet ? ' lg-card--locked' : canBuy ? ' lg-card--hot' : ''}`}
          >
            <div className="lg-card-row">
              <div style={{ flex: 1, minWidth: 0 }}>
                <div className="lg-card-title">{owned ? '✓ ' : ''}{p.name}</div>
                <div className="lg-card-desc">{p.description}</div>
                {p.requires && !owned && (
                  <div className="lg-card-prereq">
                    Prérequis : {PEARL_UPGRADES.find(x => x.id === p.requires)?.name}
                  </div>
                )}
              </div>
              {!owned && (
                <button
                  onClick={() => buyPearlUpgrade(p.id)}
                  disabled={!canBuy}
                  className="cbtn cbtn--emerald cbtn--xs"
                  style={{ flexShrink: 0 }}
                >
                  {p.cost} 💎
                </button>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
};
