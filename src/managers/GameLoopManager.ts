import { useGameStore } from '../store/useGameStore';
import { getSelfMilestoneMultiplier, getGlobalMultiplier, FISH_TYPES } from '../data/fishTypes';
import { computeBonuses } from '../utils/bonuses';
import { addSessionMana } from '../utils/session';
import { pickRandomEvent } from '../data/narrativeEvents';
import Decimal from 'break_infinity.js';

// Profondeurs des poissons "profonds" (depth 4+) pour le bonus Océanologie
const DEEP_FISH_TYPES = new Set(
  FISH_TYPES.filter(f => f.requiredDepth >= 4).map(f => f.type)
);

export class GameLoopManager {
  private static instance: GameLoopManager;
  private intervalId: number | null = null;
  private readonly TICK_RATE_MS = 100;

  private celestialGemmeAccum = 0;
  private researchGemmeAccum = 0;
  private narrativeEventAccum = 0;
  private readonly NARRATIVE_EVENT_INTERVAL_MS = 7 * 60 * 1000; // 7 minutes

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
    const { poissons, researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, pondDepth } = state;

    const bonuses = computeBonuses(researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked);

    if (poissons.length > 0) {
      let baseIncomePerSec = new Decimal(0);

      for (const fish of poissons) {
        const levelMult = new Decimal(1.5).pow(fish.level - 1);
        const milestoneMult = getSelfMilestoneMultiplier(fish, bonuses.milestoneLevelReduction);
        // Bonus Océanologie : multiplicateur supplémentaire pour les poissons profonds
        const deepMult = DEEP_FISH_TYPES.has(fish.type) ? bonuses.deepFishIncomeMult : 1;
        baseIncomePerSec = baseIncomePerSec.add(
          new Decimal(fish.baseIncome).mul(levelMult).mul(milestoneMult).mul(deepMult)
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

    // Gemmes passives du Corail de Prestige (Mystique + Océanologie)
    if (bonuses.passiveGemmesPerMin > 0) {
      this.researchGemmeAccum += bonuses.passiveGemmesPerMin * deltaMs / 60_000;
      const whole = Math.floor(this.researchGemmeAccum);
      if (whole > 0) {
        this.researchGemmeAccum -= whole;
        state.addGemmes(whole);
      }
    }

    // Événements narratifs ambiants (toutes les ~7 minutes)
    this.narrativeEventAccum += deltaMs;
    if (this.narrativeEventAccum >= this.NARRATIVE_EVENT_INTERVAL_MS) {
      this.narrativeEventAccum = 0;
      const fishTypes = [...new Set(poissons.map(f => f.type))];
      const event = pickRandomEvent(pondDepth, fishTypes);
      if (event) {
        state.setPendingNarrativeEvent(event.text);
      }
    }

    state.checkAchievements();
    state.updateLastSaveTime();
  }
}
