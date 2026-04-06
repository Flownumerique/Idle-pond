import { useGameStore } from '../store/useGameStore';
import { LORE_ENTRIES } from '../data/lore';

export const Lore = () => {
  const pondDepth = useGameStore(s => s.pondDepth);

  const unlockedEntries = LORE_ENTRIES.filter(e => e.unlockedAtDepth <= pondDepth);
  const lockedCount = LORE_ENTRIES.length - unlockedEntries.length;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Journal de l'Étang</h3>
        <span className="text-xs text-blue-400">{unlockedEntries.length} / {LORE_ENTRIES.length} entrées</span>
      </div>

      <div className="flex flex-col gap-3">
        {unlockedEntries.map(entry => (
          <div
            key={entry.id}
            className="bg-blue-900/10 rounded-lg p-4 border border-blue-800/20"
          >
            <div className="text-xs text-blue-400 uppercase tracking-widest font-bold mb-2">
              ⬛ Profondeur {entry.unlockedAtDepth}
            </div>
            <h4 className="font-bold text-blue-200 mb-2">{entry.title}</h4>
            <p className="text-sm text-gray-300 leading-relaxed italic">{entry.text}</p>
          </div>
        ))}

        {lockedCount > 0 && (
          <div className="bg-black/20 rounded-lg p-4 border border-dashed border-white/10 text-center">
            <div className="text-gray-600 text-sm">
              🔒 {lockedCount} entrée{lockedCount > 1 ? 's' : ''} verrouillée{lockedCount > 1 ? 's' : ''}
            </div>
            <div className="text-xs text-gray-700 mt-1">Creusez plus profond pour en apprendre davantage…</div>
          </div>
        )}
      </div>
    </div>
  );
};
