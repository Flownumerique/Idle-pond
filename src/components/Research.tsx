import { useGameStore } from '../store/useGameStore';
import { RESEARCH, type ResearchDef } from '../data/research';

const BRANCH_META = {
  biologie:    { label: '🧬 Biologie',     hex: '#2FB873', desc: 'Améliore les revenus de tous les poissons' },
  geologie:    { label: '⛏️ Géologie',     hex: '#E59412', desc: 'Réduit les coûts de creusage' },
  alchimie:    { label: '⚗️ Alchimie',     hex: '#6B47F0', desc: 'Améliore le Boost de Mana' },
  mystique:    { label: '🔮 Mystique',     hex: '#0E9CAB', desc: 'Génère des Gemmes passivement' },
  oceanologie: { label: '🌊 Océanologie',  hex: '#16C8C4', desc: 'Maîtrise des biomes profonds' },
} as const;

function ResearchNode({ r, unlocked, canUnlock, hex }: {
  r: ResearchDef; unlocked: boolean; canUnlock: boolean; hex: string;
}) {
  const unlock = useGameStore(s => s.unlockResearch);

  return (
    <div className={`lg-node${unlocked ? ' done' : canUnlock ? ' buyable' : ''}`} style={{ ['--branch-c' as string]: hex }}>
      <div className="lg-card-row">
        <div style={{ flex: 1, minWidth: 0 }}>
          <div className="lg-card-title">{unlocked ? '✓ ' : ''}{r.name}</div>
          <div className="lg-card-desc">{r.description}</div>
          {r.requires && !unlocked && (
            <div className="lg-card-prereq">
              Prérequis : {RESEARCH.find(x => x.id === r.requires)?.name}
            </div>
          )}
        </div>
        {!unlocked && (
          <button
            onClick={() => unlock(r.id)}
            disabled={!canUnlock}
            className="cbtn cbtn--emerald cbtn--xs"
            style={{ flexShrink: 0 }}
          >
            {r.cost} 💎
          </button>
        )}
      </div>
    </div>
  );
}

export const Research = () => {
  const gemmes = useGameStore(s => s.gemmes);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);

  const branches = (['biologie', 'geologie', 'alchimie', 'mystique', 'oceanologie'] as const).map(branch => ({
    branch,
    items: RESEARCH.filter(r => r.branch === branch),
  }));

  return (
    <div className="lg-panel">
      <div className="lg-panel-head">
        <h3>Corail de Prestige</h3>
        <span className="lg-chip gold">{gemmes} 💎</span>
      </div>

      {branches.map(({ branch, items }) => {
        const meta = BRANCH_META[branch];
        const unlocked = items.filter(r => researchUnlocked.includes(r.id)).length;
        return (
          <div key={branch} className="lg-branch">
            <div className="lg-branch-head">
              <div className="lg-branch-title" style={{ color: meta.hex }}>{meta.label}</div>
              <span className="lg-section-eyebrow">{unlocked}/{items.length}</span>
            </div>
            <div className="lg-branch-desc">{meta.desc}</div>
            <div className="lg-branch-nodes">
              {items.map(r => {
                const isUnlocked = researchUnlocked.includes(r.id);
                const prereqMet = !r.requires || researchUnlocked.includes(r.requires);
                const canUnlock = !isUnlocked && prereqMet && gemmes >= r.cost;
                return (
                  <ResearchNode key={r.id} r={r} unlocked={isUnlocked} canUnlock={canUnlock} hex={meta.hex} />
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
};
