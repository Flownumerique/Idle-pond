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
    <div className="lg-panel">
      <div className="lg-subtabs">
        {(['journal', 'bestiaire'] as Tab[]).map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            className={`lg-subtab${activeTab === tab ? ' active' : ''}`}
          >
            {tab === 'journal' ? `📖 Journal (${unlockedJournal.length}/${LORE_ENTRIES.length})` : `🦎 Bestiaire (${unlockedBestiaire.length}/${BESTIAIRE_ENTRIES.length})`}
          </button>
        ))}
      </div>

      {activeTab === 'journal' && (
        <>
          {unlockedJournal.map(entry => (
            <div key={entry.id} className="lg-entry">
              <div className="lg-entry-eyebrow" style={{ color: '#2A78B5' }}>
                ◆ Profondeur {entry.unlockedAtDepth}
              </div>
              <div className="lg-entry-title">{entry.title}</div>
              <p className="lg-entry-text">{entry.text}</p>
            </div>
          ))}

          {lockedJournalCount > 0 && (
            <div className="lg-note-card">
              <div className="t">🔒 {lockedJournalCount} entrée{lockedJournalCount > 1 ? 's' : ''} verrouillée{lockedJournalCount > 1 ? 's' : ''}</div>
              <div className="s">Creusez plus profond pour en apprendre davantage…</div>
            </div>
          )}
        </>
      )}

      {activeTab === 'bestiaire' && (
        <>
          {unlockedBestiaire.length === 0 && (
            <div className="lg-note-card">
              <div className="t">Aucune entrée encore débloquée</div>
              <div className="s">Achetez de nouveaux poissons pour découvrir leurs histoires…</div>
            </div>
          )}

          {unlockedBestiaire.map(entry => (
            <div key={entry.id} className="lg-entry">
              <div className="lg-entry-eyebrow" style={{ color: '#1F8E54' }}>🦎 Bestiaire</div>
              <div className="lg-entry-title">{entry.title}</div>
              <p className="lg-entry-text">{entry.text}</p>
            </div>
          ))}

          {lockedBestiaireCount > 0 && (
            <div className="lg-note-card">
              <div className="t">🔒 {lockedBestiaireCount} espèce{lockedBestiaireCount > 1 ? 's' : ''} non découverte{lockedBestiaireCount > 1 ? 's' : ''}</div>
              <div className="s">Achetez de nouvelles espèces pour compléter le bestiaire…</div>
            </div>
          )}
        </>
      )}
    </div>
  );
};
