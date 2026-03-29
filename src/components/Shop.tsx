import { useGameStore } from '../store/useGameStore';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

export const Shop = () => {
  const mana = useGameStore(state => state.mana);
  const poissons = useGameStore(state => state.poissons);
  const pondSize = useGameStore(state => state.pondSize);
  const buyFish = useGameStore(state => state.buyFish);
  const upgradeFish = useGameStore(state => state.upgradeFish);

  const availableFishTypes = [
    { type: 'gold', baseIncome: 1, baseCost: 10, name: 'Poisson Or', desc: 'Produit 1 Mana/s' },
    { type: 'ruby', baseIncome: 10, baseCost: 150, name: 'Poisson Rubis', desc: 'Produit 10 Mana/s' },
    { type: 'diamond', baseIncome: 100, baseCost: 5000, name: 'Poisson Diamant', desc: 'Produit 100 Mana/s' },
  ];

  const handleBuy = (type: string, baseIncome: number, baseCost: number) => {
    // Calculate cost based on owned count of this type
    const ownedCount = poissons.filter(f => f.type === type).length;
    const cost = new Decimal(baseCost).mul(new Decimal(1.15).pow(ownedCount));

    buyFish(type, baseIncome, cost);
  };

  const handleUpgrade = (id: string, currentLevel: number, type: string) => {
    // Find base cost for type
    const baseCost = availableFishTypes.find(t => t.type === type)?.baseCost || 10;

    // Upgrade cost: baseCost * 2 * 1.5 ^ currentLevel
    const upgradeCost = new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(currentLevel));
    upgradeFish(id, upgradeCost);
  };

  return (
    <div className="bg-slate-900/60 backdrop-blur-md p-6 rounded-xl border border-white/10 shadow-xl pointer-events-auto h-full max-h-[calc(100vh-2rem)] overflow-y-auto w-80 lg:w-96 flex flex-col gap-6">
      <h2 className="text-xl font-bold text-white border-b border-white/10 pb-2">Boutique & Améliorations</h2>

      <div className="flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Acheter des poissons</h3>
        {availableFishTypes.map((fish) => {
          const ownedCount = poissons.filter(f => f.type === fish.type).length;
          const currentCost = new Decimal(fish.baseCost).mul(new Decimal(1.15).pow(ownedCount));
          const canAfford = mana.gte(currentCost) && poissons.length < pondSize;

          return (
            <div key={fish.type} className="bg-white/5 rounded-lg p-4 border border-white/5 transition hover:bg-white/10">
              <div className="flex justify-between items-start mb-2">
                <div>
                  <h4 className="font-bold text-blue-200">{fish.name}</h4>
                  <p className="text-xs text-gray-400">{fish.desc}</p>
                </div>
                <div className="text-xs bg-black/30 px-2 py-1 rounded text-gray-300 border border-white/10">
                  {ownedCount} possédés
                </div>
              </div>
              <button
                onClick={() => handleBuy(fish.type, fish.baseIncome, fish.baseCost)}
                disabled={!canAfford}
                className={`w-full py-2 rounded font-semibold text-sm transition-all duration-200 shadow flex items-center justify-center gap-2 ${
                  canAfford
                    ? 'bg-blue-600 hover:bg-blue-500 text-white shadow-blue-900/50 hover:shadow-blue-500/50'
                    : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                }`}
              >
                Acheter ({formatNumber(currentCost)} Mana)
              </button>
            </div>
          );
        })}
      </div>

      <div className="flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-400 uppercase tracking-wider">Vos Poissons</h3>
        {poissons.length === 0 ? (
          <p className="text-sm text-gray-500 italic text-center py-4 bg-black/20 rounded-lg border border-dashed border-white/10">
            Votre bassin est vide.
          </p>
        ) : (
          <div className="space-y-3">
            {poissons.map(fish => {
              const baseTypeInfo = availableFishTypes.find(t => t.type === fish.type);
              const baseCost = baseTypeInfo?.baseCost || 10;
              const upgradeCost = new Decimal(baseCost).mul(2).mul(new Decimal(1.5).pow(fish.level));
              const canAfford = mana.gte(upgradeCost);

              const fishName = baseTypeInfo?.name || fish.type;

              const multiplier = new Decimal(1.5).pow(fish.level - 1);
              const currentIncome = new Decimal(fish.baseIncome).mul(multiplier);

              return (
                <div key={fish.id} className="bg-black/40 rounded-lg p-3 border border-white/5 flex items-center justify-between gap-2">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className="font-bold text-sm text-gray-200">{fishName}</h4>
                      <span className="text-[10px] bg-indigo-900/50 text-indigo-300 px-1.5 py-0.5 rounded border border-indigo-700/50">Lvl {fish.level}</span>
                    </div>
                    <p className="text-xs text-blue-300/70">{formatNumber(currentIncome)} Mana/s</p>
                  </div>
                  <button
                    onClick={() => handleUpgrade(fish.id, fish.level, fish.type)}
                    disabled={!canAfford}
                    className={`px-3 py-1.5 rounded text-xs font-semibold shadow transition-all ${
                      canAfford
                        ? 'bg-indigo-600 hover:bg-indigo-500 text-white shadow-indigo-900/50'
                        : 'bg-gray-800 text-gray-500 cursor-not-allowed'
                    }`}
                  >
                    Améliorer<br/>({formatNumber(upgradeCost)})
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
