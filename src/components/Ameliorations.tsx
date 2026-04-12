import { useState } from 'react';
import { useGameStore, getPondUpgradeCost, calcPrestigeReward, getNextManaTier, MANA_PEARL_TIERS } from '../store/useGameStore';
import { computeBonuses } from '../utils/bonuses';
import { formatNumber } from '../utils/formatNumber';
import { GLOBAL_UPGRADES, ZONE_UPGRADES, type RunUpgrade } from '../data/runUpgrades';
import Decimal from 'break_infinity.js';

const DEPTH_NAMES = [
  'Lac de Surface', 'Rivière Souterraine', 'Récif Corallien', 'Océan des Profondeurs',
  'Abysses', 'Zone Hydrothermale', 'Plaine Abyssale', 'Fosse des Origines',
  'Nexus de Mana', 'Cœur Volcanique', 'Royaume Céleste', 'Dimension Quantique',
];
const MAX_DEPTH = 11;

const DEPTH_PRESTIGE_HINT: Partial<Record<number, number>> = {
  2: 1, 3: 1, 4: 2, 5: 3, 6: 5, 7: 7, 8: 10, 9: 13, 10: 17, 11: 22,
};

function RunUpgradeCard({ upg, owned, canBuy, onBuy }: {
  upg: RunUpgrade; owned: boolean; canBuy: boolean; onBuy: () => void;
}) {
  const effectDesc = (() => {
    const parts: string[] = [];
    if (upg.effect.globalIncomePercent) {
      const pct = upg.effect.globalIncomePercent;
      parts.push(pct >= 100 ? `×${1 + pct / 100} revenus` : `+${pct}% revenus`);
    }
    if (upg.effect.pondCostReductionPercent)
      parts.push(`-${upg.effect.pondCostReductionPercent}% creusage`);
    if (upg.effect.fishCostReductionPercent)
      parts.push(`-${upg.effect.fishCostReductionPercent}% poissons`);
    return parts.join(' · ');
  })();

  return (
    <div className={`rounded-lg p-3 border transition-all ${
      owned
        ? 'bg-white/[0.02] border-white/5 opacity-55'
        : canBuy
          ? 'bg-white/5 border-white/10 hover:bg-white/10'
          : 'bg-white/[0.02] border-white/5'
    }`}>
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <div className={`font-semibold text-sm flex items-center gap-1.5 ${owned ? 'text-gray-400' : 'text-white'}`}>
            <span>{upg.emoji}</span>
            {owned ? '✓ ' : ''}{upg.name}
          </div>
          <div className="text-xs text-gray-400 mt-0.5">{upg.desc}</div>
          <div className={`text-[10px] mt-1 font-bold ${owned ? 'text-gray-600' : 'text-emerald-400'}`}>
            {effectDesc}
          </div>
        </div>
        {!owned && (
          <button
            onClick={onBuy}
            disabled={!canBuy}
            className={`shrink-0 px-2.5 py-1.5 rounded text-xs font-bold transition-all whitespace-nowrap ${
              canBuy
                ? 'bg-blue-600 hover:bg-blue-500 text-white'
                : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-60'
            }`}
          >
            {formatNumber(upg.manaCost)} Mana
          </button>
        )}
      </div>
    </div>
  );
}

export const Ameliorations = () => {
  const mana = useGameStore(s => s.mana);
  const manaRunHigh = useGameStore(s => s.manaRunHigh);
  const pondDepth = useGameStore(s => s.pondDepth);
  const perles = useGameStore(s => s.perles);
  const prestiges = useGameStore(s => s.prestiges);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);
  const pearlUpgradesUnlocked = useGameStore(s => s.pearlUpgradesUnlocked);
  const prestigeUpgradesUnlocked = useGameStore(s => s.prestigeUpgradesUnlocked);
  const runUpgradesOwned = useGameStore(s => s.runUpgradesOwned);
  const upgradePond = useGameStore(s => s.upgradePond);
  const prestige = useGameStore(s => s.prestige);
  const buyRunUpgrade = useGameStore(s => s.buyRunUpgrade);

  const [showPrestigeModal, setShowPrestigeModal] = useState(false);

  const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned);
  const isMaxDepth = pondDepth >= MAX_DEPTH;
  const pondUpgradeCost = !isMaxDepth ? getPondUpgradeCost(pondDepth).mul(bonuses.pondCostMult) : null;
  const canUpgradePond = pondUpgradeCost ? mana.gte(pondUpgradeCost) : false;
  const nextDepth = pondDepth + 1;

  const canPrestige = pondDepth >= 2;
  const prestigeRewardPreview = Math.ceil(calcPrestigeReward(manaRunHigh) * bonuses.prestigePearlMult);
  const currentTierPearls = calcPrestigeReward(manaRunHigh);
  const nextTier = getNextManaTier(manaRunHigh);

  return (
    <div className="flex flex-col gap-6 pointer-events-auto">

      {/* Modal Prestige */}
      {showPrestigeModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm">
          <div className="bg-slate-900 border border-purple-700/50 rounded-2xl p-6 w-80 shadow-2xl flex flex-col gap-4">
            <h2 className="text-lg font-bold text-white text-center">Confirmation du Prestige</h2>
            <div className="bg-purple-900/30 border border-purple-700/30 rounded-xl p-4 flex flex-col items-center gap-2">
              <p className="text-xs text-gray-400 text-center">Récompense pour ce prestige</p>
              <div className="flex items-center gap-2">
                <span className="text-3xl">🪸</span>
                <span className="text-3xl font-extrabold text-purple-200">+{prestigeRewardPreview}</span>
              </div>
              <p className="text-xs text-purple-300 text-center mt-1">
                Total après : <span className="font-bold text-white">{perles + prestigeRewardPreview} 🪸</span>
              </p>
            </div>
            <p className="text-xs text-gray-500 text-center leading-relaxed">
              Toute progression sera réinitialisée (mana, poissons, profondeur, améliorations de run).<br/>
              Les recherches et améliorations permanentes sont conservées.
            </p>
            <div className="flex gap-3">
              <button
                onClick={() => setShowPrestigeModal(false)}
                className="flex-1 py-2 rounded-lg text-sm font-semibold bg-gray-800 text-gray-300 hover:bg-gray-700 transition-all"
              >
                Annuler
              </button>
              <button
                onClick={() => { prestige(); setShowPrestigeModal(false); }}
                className="flex-1 py-2 rounded-lg text-sm font-bold bg-gradient-to-r from-purple-700 to-pink-700 hover:from-purple-600 hover:to-pink-600 text-white transition-all"
              >
                ✨ Confirmer
              </button>
            </div>
          </div>
        </div>
      )}

      <h2 className="text-xl font-bold text-white border-b border-white/10 pb-2">Améliorations</h2>

      {/* Zone — Améliorer l'étang */}
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-teal-400 uppercase tracking-wider">Zone</h3>
        <div className="bg-teal-900/20 rounded-lg p-4 border border-teal-700/30">
          <div className="flex items-center justify-between mb-3">
            <div>
              <span className="text-xs text-teal-400 uppercase tracking-wider">Profondeur actuelle</span>
              <div className="font-bold text-teal-200 text-lg">Niv. {pondDepth} — {DEPTH_NAMES[pondDepth] ?? 'Maximum'}</div>
            </div>
            <div className="text-3xl">🏊</div>
          </div>
          {!isMaxDepth ? (
            <>
              <div className="text-xs text-gray-300 mb-3 bg-black/20 rounded p-2 border border-white/5">
                <div className="font-semibold text-white mb-1">Prochaine zone — {DEPTH_NAMES[nextDepth]}</div>
                {bonuses.pondCostMult < 1 && (
                  <div className="text-teal-400 text-[10px] mt-1">
                    Réduction totale : -{Math.round((1 - bonuses.pondCostMult) * 100)}%
                  </div>
                )}
              </div>
              <button
                onClick={upgradePond}
                disabled={!canUpgradePond}
                className={`w-full py-2.5 rounded-lg font-bold text-sm transition-all shadow flex items-center justify-center gap-2 ${
                  canUpgradePond
                    ? 'bg-gradient-to-r from-teal-600 to-cyan-600 hover:from-teal-500 hover:to-cyan-500 text-white hover:scale-[1.02]'
                    : 'bg-gray-800 text-gray-500 cursor-not-allowed opacity-70'
                }`}
              >
                ⛏️ Creuser — {formatNumber(pondUpgradeCost!)} Mana
              </button>
              {DEPTH_PRESTIGE_HINT[nextDepth] !== undefined && (() => {
                const hint = DEPTH_PRESTIGE_HINT[nextDepth]!;
                const met = prestiges >= hint;
                return (
                  <div className={`text-xs mt-2 text-center py-1 px-2 rounded border ${
                    met ? 'text-emerald-400/80 border-transparent' : 'text-amber-300/90 bg-amber-950/30 border-amber-700/30'
                  }`}>
                    {met
                      ? `✓ Prêt — ${hint} prestige${hint > 1 ? 's' : ''} recommandé${hint > 1 ? 's' : ''}`
                      : `💡 Recommandé : ${hint} prestige${hint > 1 ? 's' : ''} (vous : ${prestiges})`}
                  </div>
                );
              })()}
            </>
          ) : (
            <div className="text-center text-xs text-gray-500 italic py-2">Profondeur maximale atteinte</div>
          )}
        </div>

        {/* Améliorations de zone (creusage) */}
        <div className="flex flex-col gap-2">
          {ZONE_UPGRADES.map(upg => {
            const owned = runUpgradesOwned.includes(upg.id);
            const prereqMet = !upg.requires || runUpgradesOwned.includes(upg.requires);
            const canBuy = !owned && prereqMet && mana.gte(upg.manaCost);
            return (
              <RunUpgradeCard
                key={upg.id}
                upg={upg}
                owned={owned}
                canBuy={canBuy}
                onBuy={() => buyRunUpgrade(upg.id)}
              />
            );
          })}
        </div>
      </div>

      {/* Améliorations globales */}
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-blue-400 uppercase tracking-wider">Globales</h3>
        <div className="flex flex-col gap-2">
          {GLOBAL_UPGRADES.map(upg => {
            const owned = runUpgradesOwned.includes(upg.id);
            const prereqMet = !upg.requires || runUpgradesOwned.includes(upg.requires);
            const canBuy = !owned && prereqMet && mana.gte(upg.manaCost);
            return (
              <RunUpgradeCard
                key={upg.id}
                upg={upg}
                owned={owned}
                canBuy={canBuy}
                onBuy={() => buyRunUpgrade(upg.id)}
              />
            );
          })}
        </div>
      </div>

      {/* Prestige */}
      <div className="flex flex-col gap-3">
        <h3 className="text-sm font-semibold text-purple-400 uppercase tracking-wider">Prestige</h3>
        <div className="bg-purple-900/20 rounded-lg p-4 border border-purple-700/30 flex flex-col gap-3">
          <div className="flex justify-between items-center">
            <span className="text-sm text-gray-300">Perles accumulées</span>
            <span className="font-bold text-purple-200">🪸 {perles}</span>
          </div>

          <div className="bg-black/30 rounded-lg p-3 border border-purple-800/30 flex flex-col gap-2">
            <div className="flex justify-between items-center">
              <span className="text-xs text-gray-400">Pic de mana (run)</span>
              <span className="text-xs font-bold text-white">{formatNumber(manaRunHigh)} Mana</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-xs text-gray-400">Palier actuel</span>
              <span className="text-sm font-bold text-purple-200">{currentTierPearls} 🪸 de base</span>
            </div>
            {nextTier ? (
              <div className="mt-1">
                <div className="flex justify-between items-center text-[10px] text-gray-500 mb-1">
                  <span>Prochain palier : {nextTier.label}</span>
                  <span className="text-purple-400">→ {nextTier.pearls} 🪸</span>
                </div>
                {(() => {
                  const tierIdx = MANA_PEARL_TIERS.findIndex(t => t === nextTier);
                  const prevThreshold = tierIdx > 0 ? MANA_PEARL_TIERS[tierIdx - 1].threshold : new Decimal(1);
                  const range = nextTier.threshold.minus(prevThreshold);
                  const progress = manaRunHigh.minus(prevThreshold);
                  const pct = Math.min(100, Math.max(0, progress.div(range).toNumber() * 100));
                  return (
                    <div className="w-full h-1.5 bg-gray-800 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-gradient-to-r from-purple-600 to-pink-500 rounded-full transition-all"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                  );
                })()}
              </div>
            ) : (
              <div className="text-[10px] text-yellow-400 text-center mt-1">✦ Palier maximum atteint</div>
            )}
          </div>

          {canPrestige && (
            <div className="flex items-center justify-between bg-purple-950/40 rounded-lg px-3 py-2 border border-purple-800/30">
              <span className="text-xs text-gray-400">Gain si prestige maintenant</span>
              <span className="text-sm font-bold text-purple-200">+{prestigeRewardPreview} 🪸</span>
            </div>
          )}

          {!canPrestige && (
            <p className="text-xs text-gray-500 text-center">Atteignez la profondeur 2 pour débloquer le Prestige.</p>
          )}

          <button
            onClick={() => canPrestige && setShowPrestigeModal(true)}
            disabled={!canPrestige}
            className={`w-full py-2 rounded-lg font-bold text-sm transition-all ${
              canPrestige
                ? 'bg-gradient-to-r from-purple-700 to-pink-700 hover:from-purple-600 hover:to-pink-600 text-white'
                : 'bg-gray-800 text-gray-600 cursor-not-allowed opacity-60'
            }`}
          >
            ✨ Prestige (réinitialiser)
          </button>
        </div>
      </div>

    </div>
  );
};
