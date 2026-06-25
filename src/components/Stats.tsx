import { useEffect, useState } from 'react';
import { useGameStore } from '../store/useGameStore';
import { formatNumber } from '../utils/formatNumber';
import Decimal from 'break_infinity.js';

const DEPTH_NAMES = ['Peu profond', 'Standard', 'Profond', 'Abyssal', 'Maximum'];

export const Stats = () => {
  const mana = useGameStore(state => state.mana);
  const gemmes = useGameStore(state => state.gemmes);
  const perles = useGameStore(state => state.perles);
  const poissons = useGameStore(state => state.poissons);
  const pondDepth = useGameStore(state => state.pondDepth);
  const boostActiveUntil = useGameStore(state => state.boostActiveUntil);

  const [isBoostActive, setIsBoostActive] = useState(() => boostActiveUntil > Date.now());

  useEffect(() => {
    const interval = setInterval(() => {
      setIsBoostActive(boostActiveUntil > Date.now());
    }, 1000);
    return () => clearInterval(interval);
  }, [boostActiveUntil]);

  let manaPerSec = new Decimal(0);
  for (const fish of poissons) {
    const multiplier = new Decimal(1.5).pow(fish.level - 1);
    manaPerSec = manaPerSec.add(new Decimal(fish.baseIncome).mul(multiplier));
  }
  if (isBoostActive) manaPerSec = manaPerSec.mul(2);

  return (
    <div className="lg-panel">
      <div className="lg-stat-hero">
        <div className="lab">MANA</div>
        <div className="big">{formatNumber(mana)}</div>
        <div className="rate">
          + {formatNumber(manaPerSec)} / sec
          {isBoostActive && <span className="boost"> · ×2 BOOST</span>}
        </div>
      </div>

      <div className="lg-stat-grid">
        <div className="lg-stat-cell gemme">
          <div className="lab">Gemmes 💎</div>
          <div className="val">{gemmes}</div>
        </div>
        <div className="lg-stat-cell perle">
          <div className="lab">Perles 🪸</div>
          <div className="val">{perles}</div>
        </div>
        <div className="lg-stat-cell">
          <div className="lab">Poissons</div>
          <div className="val">{poissons.length}</div>
        </div>
        <div className="lg-stat-cell">
          <div className="lab">Profondeur ⛏️</div>
          <div className="val">Niv. {pondDepth}</div>
          <div className="sub">{DEPTH_NAMES[pondDepth] ?? 'Maximum'}</div>
        </div>
      </div>
    </div>
  );
};
