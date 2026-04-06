import { useGameStore } from '../store/useGameStore';
import { getSelfMilestoneMultiplier, getGlobalMultiplier } from '../data/fishTypes';
import { computeBonuses } from '../utils/bonuses';
import { addSessionMana } from '../utils/session';
import Decimal from 'break_infinity.js';

export class GameLoopManager {
  private static instance: GameLoopManager;
  private intervalId: number | null = null;
  private readonly TICK_RATE_MS = 100;

  private celestialGemmeAccum = 0;
  private researchGemmeAccum = 0;

  private constructor() {}

  public static getInstance(): GameLoopManager {
    if (!GameLoopManager.instance) {
      GameLoopManager.instance = new GameLoopManager();
    }
    return GameLoopManager.instance;
  }

  public start() {
    if (this.intervalId !== null) return;
    let lastTick = Date.now();
    this.intervalId = window.setInterval(() => {
      const now = Date.now();
      const deltaMs = now - lastTick;
      lastTick = now;
      this.tick(deltaMs);
    }, this.TICK_RATE_MS);
  }

  public stop() {
    if (this.intervalId !== null) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  private tick(deltaMs: number) {
    const state = useGameStore.getState();
    const { poissons, researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked } = state;

    const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked);

    if (poissons.length > 0) {
      let baseIncomePerSec = new Decimal(0);

      for (const fish of poissons) {
        const levelMult = new Decimal(1.5).pow(fish.level - 1);
        const milestoneMult = getSelfMilestoneMultiplier(fish, bonuses.milestoneLevelReduction);
        baseIncomePerSec = baseIncomePerSec.add(
          new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult)
        );
      }

      const milestoneGlobalMult = getGlobalMultiplier(poissons, bonuses.milestoneLevelReduction);
      let finalIncomePerSec = baseIncomePerSec
        .mul(milestoneGlobalMult)
        .mul(bonuses.globalIncomeMult);

      if (state.boostActiveUntil > Date.now()) {
        finalIncomePerSec = finalIncomePerSec.mul(bonuses.boostMultiplier);
      }

      const incomeThisTick = finalIncomePerSec.mul(deltaMs / 1000);
      if (incomeThisTick.gt(0)) {
        state.addMana(incomeThisTick);
        addSessionMana(incomeThisTick);
      }

      // Gemmes passives du Poisson Céleste (+1 💎/min par exemplaire)
      const celestialCount = poissons.filter(f => f.type === 'celestial').length;
      if (celestialCount > 0) {
        this.celestialGemmeAccum += celestialCount * deltaMs / 60_000;
        const whole = Math.floor(this.celestialGemmeAccum);
        if (whole > 0) {
          this.celestialGemmeAccum -= whole;
          state.addGemmes(whole);
        }
      }
    }

    // Gemmes passives du Corail de Prestige (Mystique)
    if (bonuses.passiveGemmesPerMin > 0) {
      this.researchGemmeAccum += bonuses.passiveGemmesPerMin * deltaMs / 60_000;
      const whole = Math.floor(this.researchGemmeAccum);
      if (whole > 0) {
        this.researchGemmeAccum -= whole;
        state.addGemmes(whole);
      }
    }

    state.checkAchievements();
    state.updateLastSaveTime();
  }
}
