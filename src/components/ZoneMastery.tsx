import { useGameStore, MAX_FISH_LEVEL } from '../store/useGameStore';
import {
  MASTERY_ZONES,
  ZONE_MASTERY_BONUS_PERCENT,
  getZoneRoster,
  getZoneMasteryProgress,
} from '../data/zones';

// Noms d'affichage des profondeurs (copie partagée — voir App.tsx / Stats.tsx).
const DEPTH_NAMES = [
  'Lac de Surface', 'Rivière Souterraine', 'Récif Corallien', 'Océan des Profondeurs',
  'Abysses', 'Zone Hydrothermale', 'Plaine Abyssale', 'Fosse des Origines',
  'Nexus de Mana', 'Cœur Volcanique', 'Royaume Céleste', 'Dimension Quantique',
];

export const ZoneMastery = () => {
  const poissons = useGameStore(s => s.poissons);
  const masteredZones = useGameStore(s => s.masteredZones) ?? [];

  const masteredCount = MASTERY_ZONES.filter(d => masteredZones.includes(d)).length;

  return (
    <div className="lg-panel">
      <div className="lg-panel-head">
        <h3>Maîtrise des zones</h3>
        <span className="lg-chip gold">{masteredCount}/{MASTERY_ZONES.length}</span>
      </div>

      {MASTERY_ZONES.map(depth => {
        const name = DEPTH_NAMES[depth] ?? `Zone ${depth}`;

        if (masteredZones.includes(depth)) {
          return (
            <div key={depth} className="lg-card lg-card--gold">
              <div className="lg-card-row">
                <span className="lg-card-icon">✓</span>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div className="lg-card-title">{name}</div>
                  <div className="lg-card-desc">+{ZONE_MASTERY_BONUS_PERCENT}% revenus de la zone</div>
                </div>
                <span className="lg-section-eyebrow" style={{ color: '#C98A0F', whiteSpace: 'nowrap' }}>
                  Maîtrisée
                </span>
              </div>
            </div>
          );
        }

        const { done, total } = getZoneMasteryProgress(depth, poissons);
        const remaining = getZoneRoster(depth)
          .filter(species => !poissons.some(p => p.type === species.type && p.level >= MAX_FISH_LEVEL))
          .map(species => species.name);

        return (
          <div key={depth} className="lg-card">
            <div className="lg-card-row">
              <span className="lg-card-icon">🌊</span>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div className="lg-card-title">{name}</div>
                <div className="lg-card-desc">
                  +{ZONE_MASTERY_BONUS_PERCENT}% revenus · {done}/{total} espèces au niv. {MAX_FISH_LEVEL}
                </div>
                {remaining.length > 0 && (
                  <div className="lg-card-desc" style={{ marginTop: 4 }}>
                    Reste : {remaining.join(', ')}
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
};
