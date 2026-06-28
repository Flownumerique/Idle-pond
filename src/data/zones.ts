import { FISH_TYPES, type FishType } from './fishTypes';
import { MAX_FISH_LEVEL, type PoissonInstance } from '../store/useGameStore';

/** Bonus de revenu accordé aux poissons d'une zone maîtrisée (en %). */
export const ZONE_MASTERY_BONUS_PERCENT = 5;

/**
 * Profondeurs (`requiredDepth`) triées et uniques qui possèdent au moins une
 * espèce *normale* (hors légendaires `requiredPrestiges`) — les seules zones
 * éligibles à la maîtrise.
 */
export const MASTERY_ZONES: number[] = [
  ...new Set(
    FISH_TYPES.filter(f => !f.requiredPrestiges).map(f => f.requiredDepth)
  ),
].sort((a, b) => a - b);

/** Espèces normales d'une profondeur (les légendaires sont exclues du roster). */
export const getZoneRoster = (depth: number): FishType[] =>
  FISH_TYPES.filter(f => f.requiredDepth === depth && !f.requiredPrestiges);

/**
 * Progression de maîtrise d'une zone : `done` = espèces du roster dont le joueur
 * possède au moins une instance au niveau maximum ; `total` = taille du roster.
 */
export const getZoneMasteryProgress = (
  depth: number,
  poissons: PoissonInstance[]
): { done: number; total: number } => {
  const roster = getZoneRoster(depth);
  const done = roster.filter(species =>
    poissons.some(p => p.type === species.type && p.level >= MAX_FISH_LEVEL)
  ).length;
  return { done, total: roster.length };
};

/** Une zone est maîtrisée quand toutes ses espèces normales sont au niveau max. */
export const isZoneMastered = (depth: number, poissons: PoissonInstance[]): boolean => {
  const { done, total } = getZoneMasteryProgress(depth, poissons);
  return total > 0 && done === total;
};

/**
 * Renvoie `mastered` augmenté de toute zone fraîchement maîtrisée (ajout seul,
 * permanent). Renvoie la référence d'origine si rien de nouveau n'est maîtrisé.
 */
export const computeNewlyMasteredZones = (
  poissons: PoissonInstance[],
  mastered: number[]
): number[] => {
  const newly = MASTERY_ZONES.filter(
    depth => !mastered.includes(depth) && isZoneMastered(depth, poissons)
  );
  return newly.length === 0 ? mastered : [...mastered, ...newly];
};
