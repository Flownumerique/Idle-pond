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
    <div className={`lg-card${owned ? ' lg-card--done' : canBuy ? ' lg-card--hot' : ''}`}>
      <div className="lg-card-row">
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="lg-card-title"><span style={{ marginRight: 4 }}>{upg.emoji}</span>{owned ? '✓ ' : ''}{upg.name}</div>
          <div className="lg-card-desc">{upg.desc}</div>
          <div style={{ fontFamily: '"Baloo 2"', fontWeight: 800, fontSize: 10.5, marginTop: 3, color: owned ? '#9DBAC0' : '#2FB873' }}>
            {effectDesc}
          </div>
        </div>
        {!owned && (
          <button
            onClick={onBuy}
            disabled={!canBuy}
            className="cbtn cbtn--coral cbtn--xs"
            style={{ flexShrink: 0, whiteSpace: 'nowrap' }}
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
    <div className="lg-panel" style={{ gap: 18 }}>

      {/* Modal Prestige */}
      {showPrestigeModal && (
        <div className="lg-fixed-scrim" onClick={() => setShowPrestigeModal(false)}>
          <div className="lg-confirm" onClick={e => e.stopPropagation()}>
            <div className="lg-confirm-title">Confirmation du Prestige</div>
            <div className="lg-modal-reward">
              <div className="lab">Récompense pour ce prestige</div>
              <div className="big">+{prestigeRewardPreview} 🪸</div>
              <div className="keep">Total après : {perles + prestigeRewardPreview} 🪸</div>
            </div>
            <p className="lg-panel-note" style={{ fontSize: 11 }}>
              Toute progression sera réinitialisée (mana, poissons, profondeur, améliorations de run).
              Les recherches et améliorations permanentes sont conservées.
            </p>
            <div style={{ display: 'flex', gap: 10, marginTop: 4 }}>
              <button className="cbtn cbtn--ghost" style={{ flex: 1 }} onClick={() => setShowPrestigeModal(false)}>Annuler</button>
              <button className="cbtn cbtn--violet" style={{ flex: 1 }} onClick={() => { prestige(); setShowPrestigeModal(false); }}>✨ Confirmer</button>
            </div>
          </div>
        </div>
      )}

      {/* Zone — Améliorer l'étang */}
      <div className="lg-panel" style={{ gap: 9 }}>
        <div className="lg-section-eyebrow" style={{ color: '#0E9CAB' }}>Zone</div>
        <div className="lg-zone-hero">
          <div className="lg-zone-row">
            <div>
              <div className="lab">Profondeur actuelle</div>
              <div className="dep">Niv. {pondDepth} — {DEPTH_NAMES[pondDepth] ?? 'Maximum'}</div>
            </div>
            <div style={{ fontSize: 30 }}>🏊</div>
          </div>
          {!isMaxDepth ? (
            <>
              <div className="lg-inset" style={{ marginBottom: 11 }}>
                <div style={{ fontFamily: '"Baloo 2"', fontWeight: 800, fontSize: 12, color: '#0B5566' }}>Prochaine zone — {DEPTH_NAMES[nextDepth]}</div>
                {bonuses.pondCostMult < 1 && (
                  <div style={{ fontFamily: '"Nunito"', fontWeight: 700, fontSize: 10, color: '#0E9CAB', marginTop: 3 }}>
                    Réduction totale : -{Math.round((1 - bonuses.pondCostMult) * 100)}%
                  </div>
                )}
              </div>
              <button onClick={upgradePond} disabled={!canUpgradePond} className="cbtn cbtn--coral cbtn--block">
                ⛏️ Creuser — {formatNumber(pondUpgradeCost!)} Mana
              </button>
              {DEPTH_PRESTIGE_HINT[nextDepth] !== undefined && (() => {
                const hint = DEPTH_PRESTIGE_HINT[nextDepth]!;
                const met = prestiges >= hint;
                return (
                  <div style={{
                    fontFamily: '"Nunito"', fontWeight: 700, fontSize: 11, textAlign: 'center', marginTop: 8,
                    padding: '6px 10px', borderRadius: 10,
                    ...(met
                      ? { color: '#2FB873' }
                      : { color: '#A87A12', background: '#FFF3D6', border: '1.5px solid #FBE2A8' }),
                  }}>
                    {met
                      ? `✓ Prêt — ${hint} prestige${hint > 1 ? 's' : ''} recommandé${hint > 1 ? 's' : ''}`
                      : `💡 Recommandé : ${hint} prestige${hint > 1 ? 's' : ''} (vous : ${prestiges})`}
                  </div>
                );
              })()}
            </>
          ) : (
            <div className="lg-panel-note" style={{ textAlign: 'center' }}>Profondeur maximale atteinte</div>
          )}
        </div>

        {ZONE_UPGRADES.map(upg => {
          const owned = runUpgradesOwned.includes(upg.id);
          const prereqMet = !upg.requires || runUpgradesOwned.includes(upg.requires);
          const canBuy = !owned && prereqMet && mana.gte(upg.manaCost);
          return (
            <RunUpgradeCard key={upg.id} upg={upg} owned={owned} canBuy={canBuy} onBuy={() => buyRunUpgrade(upg.id)} />
          );
        })}
      </div>

      {/* Améliorations globales */}
      <div className="lg-panel" style={{ gap: 9 }}>
        <div className="lg-section-eyebrow" style={{ color: '#2A78B5' }}>Globales</div>
        {GLOBAL_UPGRADES.map(upg => {
          const owned = runUpgradesOwned.includes(upg.id);
          const prereqMet = !upg.requires || runUpgradesOwned.includes(upg.requires);
          const canBuy = !owned && prereqMet && mana.gte(upg.manaCost);
          return (
            <RunUpgradeCard key={upg.id} upg={upg} owned={owned} canBuy={canBuy} onBuy={() => buyRunUpgrade(upg.id)} />
          );
        })}
      </div>

      {/* Prestige */}
      <div className="lg-panel" style={{ gap: 9 }}>
        <div className="lg-section-eyebrow" style={{ color: '#6B47F0' }}>Prestige</div>
        <div className="lg-card" style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
          <div className="lg-kv">
            <span className="k">Perles accumulées</span>
            <span className="v pink">🪸 {perles}</span>
          </div>

          <div className="lg-inset" style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            <div className="lg-kv">
              <span className="k">Pic de mana (run)</span>
              <span className="v">{formatNumber(manaRunHigh)} Mana</span>
            </div>
            <div className="lg-kv">
              <span className="k">Palier actuel</span>
              <span className="v pink">{currentTierPearls} 🪸 de base</span>
            </div>
            {nextTier ? (
              <div>
                <div className="lg-kv" style={{ marginBottom: 5 }}>
                  <span className="k">Prochain : {nextTier.label}</span>
                  <span className="v pink">→ {nextTier.pearls} 🪸</span>
                </div>
                {(() => {
                  const tierIdx = MANA_PEARL_TIERS.findIndex(t => t === nextTier);
                  const prevThreshold = tierIdx > 0 ? MANA_PEARL_TIERS[tierIdx - 1].threshold : new Decimal(1);
                  const range = nextTier.threshold.minus(prevThreshold);
                  const progress = manaRunHigh.minus(prevThreshold);
                  const pct = Math.min(100, Math.max(0, progress.div(range).toNumber() * 100));
                  return (
                    <div className="lg-prog pearl"><i style={{ width: `${pct}%` }} /></div>
                  );
                })()}
              </div>
            ) : (
              <div style={{ textAlign: 'center', fontFamily: '"Baloo 2"', fontWeight: 800, fontSize: 11, color: '#E59412' }}>✦ Palier maximum atteint</div>
            )}
          </div>

          {canPrestige && (
            <div className="lg-kv lg-inset">
              <span className="k">Gain si prestige maintenant</span>
              <span className="v pink">+{prestigeRewardPreview} 🪸</span>
            </div>
          )}

          {!canPrestige && (
            <p className="lg-panel-note" style={{ textAlign: 'center' }}>Atteignez la profondeur 2 pour débloquer le Prestige.</p>
          )}

          <button
            onClick={() => canPrestige && setShowPrestigeModal(true)}
            disabled={!canPrestige}
            className="cbtn cbtn--violet cbtn--block"
          >
            ✨ Prestige (réinitialiser)
          </button>
        </div>
      </div>

    </div>
  );
};
