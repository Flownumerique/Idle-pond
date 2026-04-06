import { useGameStore } from '../store/useGameStore';
import { RESEARCH, type ResearchDef } from '../data/research';

const BRANCH_META = {
  biologie:  { label: '🧬 Biologie',  color: 'emerald', desc: 'Améliore les revenus' },
  geologie:  { label: '⛏️ Géologie',  color: 'amber',   desc: 'Réduit les coûts de creusage' },
  alchimie:  { label: '⚗️ Alchimie',  color: 'violet',  desc: 'Améliore le boost' },
} as const;

const COLOR = {
  emerald: { bg: 'bg-emerald-900/20', border: 'border-emerald-700/30', text: 'text-emerald-300', btn: 'bg-emerald-700 hover:bg-emerald-600', btnDis: 'bg-gray-800 text-gray-500' },
  amber:   { bg: 'bg-amber-900/20',   border: 'border-amber-700/30',   text: 'text-amber-300',   btn: 'bg-amber-700 hover:bg-amber-600',   btnDis: 'bg-gray-800 text-gray-500' },
  violet:  { bg: 'bg-violet-900/20',  border: 'border-violet-700/30',  text: 'text-violet-300',  btn: 'bg-violet-700 hover:bg-violet-600', btnDis: 'bg-gray-800 text-gray-500' },
};

function ResearchCard({ r, gemmes, unlocked, canUnlock }: {
  r: ResearchDef;
  gemmes: number;
  unlocked: boolean;
  canUnlock: boolean;
}) {
  const unlock = useGameStore(s => s.unlockResearch);
  const meta = BRANCH_META[r.branch];
  const c = COLOR[meta.color];

  return (
    <div className={`rounded-lg p-3 border ${c.bg} ${c.border} ${unlocked ? 'opacity-60' : ''}`}>
      <div className="flex items-start justify-between gap-2 mb-1">
        <div>
          <div className={`font-semibold text-sm ${unlocked ? 'text-gray-400' : 'text-white'}`}>
            {unlocked ? '✓ ' : ''}{r.name}
          </div>
          <div className="text-xs text-gray-400">{r.description}</div>
        </div>
        {!unlocked && (
          <button
            onClick={() => unlock(r.id)}
            disabled={!canUnlock}
            className={`shrink-0 px-2.5 py-1 rounded text-xs font-bold transition-all ${
              canUnlock ? `${c.btn} text-white` : `${c.btnDis} cursor-not-allowed opacity-60`
            }`}
          >
            {r.cost} 💎
          </button>
        )}
      </div>
      {r.requires && (
        <div className="text-[10px] text-gray-600">Prérequis : {RESEARCH.find(x => x.id === r.requires)?.name}</div>
      )}
    </div>
  );
}

export const Research = () => {
  const gemmes = useGameStore(s => s.gemmes);
  const researchUnlocked = useGameStore(s => s.researchUnlocked);

  const branches = (['biologie', 'geologie', 'alchimie'] as const).map(branch => ({
    branch,
    items: RESEARCH.filter(r => r.branch === branch),
  }));

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Arbre de Recherche</h3>
        <span className="text-xs text-emerald-300 font-bold">{gemmes} 💎 disponibles</span>
      </div>

      {branches.map(({ branch, items }) => {
        const meta = BRANCH_META[branch];
        const c = COLOR[meta.color];
        return (
          <div key={branch} className={`rounded-xl p-3 border ${c.bg} ${c.border}`}>
            <div className={`text-sm font-bold ${c.text} mb-0.5`}>{meta.label}</div>
            <div className="text-xs text-gray-500 mb-3">{meta.desc}</div>
            <div className="flex flex-col gap-2">
              {items.map(r => {
                const unlocked = researchUnlocked.includes(r.id);
                const prereqMet = !r.requires || researchUnlocked.includes(r.requires);
                const canUnlock = !unlocked && prereqMet && gemmes >= r.cost;
                return (
                  <ResearchCard
                    key={r.id}
                    r={r}
                    gemmes={gemmes}
                    unlocked={unlocked}
                    canUnlock={canUnlock}
                  />
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
};
