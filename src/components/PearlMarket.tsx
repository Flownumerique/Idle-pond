import { useGameStore } from '../store/useGameStore';
import { PEARL_UPGRADES } from '../data/pearlUpgrades';

export const PearlMarket = () => {
  const perles = useGameStore(s => s.perles);
  const pearlUpgradesUnlocked = useGameStore(s => s.pearlUpgradesUnlocked);
  const buyPearlUpgrade = useGameStore(s => s.buyPearlUpgrade);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Marché des Perles</h3>
        <span className="text-xs text-purple-300 font-bold">{perles} 🪸 disponibles</span>
      </div>

      <p className="text-xs text-gray-500 -mt-2">
        Les perles sont gagnées en faisant un Prestige. Ces améliorations survivent aux resets.
      </p>

      <div className="flex flex-col gap-2">
        {PEARL_UPGRADES.map(p => {
          const owned = pearlUpgradesUnlocked.includes(p.id);
          const prereqMet = !p.requires || pearlUpgradesUnlocked.includes(p.requires);
          const canBuy = !owned && prereqMet && perles >= p.cost;

          return (
            <div
              key={p.id}
              className={`rounded-lg p-3 border transition-all ${
                owned
                  ? 'bg-purple-900/10 border-purple-800/20 opacity-60'
                  : prereqMet
                    ? 'bg-purple-900/20 border-purple-700/30'
                    : 'bg-white/[0.02] border-white/5 opacity-40'
              }`}
            >
              <div className="flex items-start justify-between gap-2">
                <div>
                  <div className={`font-semibold text-sm ${owned ? 'text-gray-400' : 'text-purple-200'}`}>
                    {owned ? '✓ ' : ''}{p.name}
                  </div>
                  <div className="text-xs text-gray-400">{p.description}</div>
                  {p.requires && !owned && (
                    <div className="text-[10px] text-gray-600 mt-0.5">
                      Prérequis : {PEARL_UPGRADES.find(x => x.id === p.requires)?.name}
                    </div>
                  )}
                </div>
                {!owned && (
                  <button
                    onClick={() => buyPearlUpgrade(p.id)}
                    disabled={!canBuy}
                    className={`shrink-0 px-2.5 py-1 rounded text-xs font-bold transition-all ${
                      canBuy
                        ? 'bg-purple-700 hover:bg-purple-600 text-white'
                        : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-60'
                    }`}
                  >
                    {p.cost} 🪸
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
