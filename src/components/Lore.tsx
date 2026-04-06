import { useGameStore } from '../store/useGameStore';
import { LORE_ENTRIES, BESTIAIRE_ENTRIES } from '../data/lore';
import { useState } from 'react';

type Tab = 'journal' | 'bestiaire';

export const Lore = () => {
  const pondDepth = useGameStore(s => s.pondDepth);
  const poissons = useGameStore(s => s.poissons);
  const [activeTab, setActiveTab] = useState<Tab>('journal');

  const ownedFishTypes = new Set(poissons.map(f => f.type));

  const unlockedJournal = LORE_ENTRIES.filter(e => (e.unlockedAtDepth ?? 0) <= pondDepth);
  const lockedJournalCount = LORE_ENTRIES.length - unlockedJournal.length;

  const unlockedBestiaire = BESTIAIRE_ENTRIES.filter(e => e.unlockedByFishType && ownedFishTypes.has(e.unlockedByFishType));
  const lockedBestiaireCount = BESTIAIRE_ENTRIES.length - unlockedBestiaire.length;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white uppercase tracking-wider">Journal de l'Étang</h3>
      </div>

      {/* Onglets */}
      <div className="grid grid-cols-2 gap-1">
        {(['journal', 'bestiaire'] as Tab[]).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`py-1.5 rounded-lg text-xs font-semibold transition-all border ${
              activeTab === tab
                ? 'bg-white/15 border-white/25 text-white'
                : 'bg-slate-900/60 border-white/10 text-gray-400 hover:bg-white/10 hover:text-white'
            }`}
          >
            {tab === 'journal' ? `📖 Journal (${unlockedJournal.length}/${LORE_ENTRIES.length})` : `🦎 Bestiaire (${unlockedBestiaire.length}/${BESTIAIRE_ENTRIES.length})`}
          </button>
        ))}
      </div>

      {activeTab === 'journal' && (
        <div className="flex flex-col gap-3">
          {unlockedJournal.map(entry => (
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

          {lockedJournalCount > 0 && (
            <div className="bg-black/20 rounded-lg p-4 border border-dashed border-white/10 text-center">
              <div className="text-gray-600 text-sm">
                🔒 {lockedJournalCount} entrée{lockedJournalCount > 1 ? 's' : ''} verrouillée{lockedJournalCount > 1 ? 's' : ''}
              </div>
              <div className="text-xs text-gray-700 mt-1">Creusez plus profond pour en apprendre davantage…</div>
            </div>
          )}
        </div>
      )}

      {activeTab === 'bestiaire' && (
        <div className="flex flex-col gap-3">
          {unlockedBestiaire.length === 0 && (
            <div className="bg-black/20 rounded-lg p-4 border border-dashed border-white/10 text-center">
              <div className="text-gray-600 text-sm">Aucune entrée encore débloquée</div>
              <div className="text-xs text-gray-700 mt-1">Achetez de nouveaux poissons pour découvrir leurs histoires…</div>
            </div>
          )}

          {unlockedBestiaire.map(entry => (
            <div
              key={entry.id}
              className="bg-emerald-900/10 rounded-lg p-4 border border-emerald-800/20"
            >
              <h4 className="font-bold text-emerald-200 mb-2">{entry.title}</h4>
              <p className="text-sm text-gray-300 leading-relaxed italic">{entry.text}</p>
            </div>
          ))}

          {lockedBestiaireCount > 0 && (
            <div className="bg-black/20 rounded-lg p-4 border border-dashed border-white/10 text-center">
              <div className="text-gray-600 text-sm">
                🔒 {lockedBestiaireCount} espèce{lockedBestiaireCount > 1 ? 's' : ''} non découverte{lockedBestiaireCount > 1 ? 's' : ''}
              </div>
              <div className="text-xs text-gray-700 mt-1">Achetez de nouvelles espèces pour compléter le bestiaire…</div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
