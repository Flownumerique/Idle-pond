import { useGameStore } from '../store/useGameStore';
import { computeBonuses } from '../utils/bonuses';
import { computeIncomePerSec } from '../utils/incomeCalc';
import { addSessionMana } from '../utils/session';
import { pickRandomEvent } from '../data/narrativeEvents';

export class GameLoopManager {
  private static instance: GameLoopManager;
  private intervalId: number | null = null;
  private readonly TICK_RATE_MS = 100;

  private celestialGemmeAccum = 0;
  private researchGemmeAccum = 0;
  private narrativeEventAccum = 0;
  private narrativeEventIntervalMs = GameLoopManager.randomEventInterval();

  private static randomEventInterval(): number {
    return (5 + Math.random() * 5) * 60 * 1000;
  }

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
    const {
      poissons, researchUnlocked, pearlUpgradesUnlocked,
      prestigeUpgradesUnlocked, runUpgradesOwned, pondDepth,
    } = state;

    const bonuses = computeBonuses(
      researchUnlocked, pearlUpgradesUnlocked, prestigeUpgradesUnlocked, runUpgradesOwned
    );

    if (poissons.length > 0) {
      const finalIncomePerSec = computeIncomePerSec(poissons, bonuses, state.boostActiveUntil, state.masteredZones);
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

    // Événements narratifs ambiants (intervalle aléatoire 5–10 min)
    this.narrativeEventAccum += deltaMs;
    if (this.narrativeEventAccum >= this.narrativeEventIntervalMs) {
      this.narrativeEventAccum = 0;
      this.narrativeEventIntervalMs = GameLoopManager.randomEventInterval();
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
