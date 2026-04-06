import { useEffect, useState } from 'react';
import { GameLoopManager } from './managers/GameLoopManager';
import { OfflineManager } from './managers/OfflineManager';
import { PhaserContainer } from './components/PhaserContainer';
import { Stats } from './components/Stats';
import { Shop } from './components/Shop';
import { BoostOverlay } from './components/BoostOverlay';
import { Achievements } from './components/Achievements';
import { UnlockNotification } from './components/UnlockNotification';
import { Research } from './components/Research';
import { PearlMarket } from './components/PearlMarket';
import { Lore } from './components/Lore';
import { Challenges } from './components/Challenges';

type Panel = 'research' | 'pearls' | 'journal' | 'challenges';

const TABS: { id: Panel; icon: string; label: string }[] = [
  { id: 'research',   icon: '🧬', label: 'Recherche' },
  { id: 'pearls',     icon: '🪸', label: 'Marché' },
  { id: 'journal',    icon: '📖', label: 'Journal' },
  { id: 'challenges', icon: '⚔️', label: 'Défis' },
];

function App() {
  const [activePanel, setActivePanel] = useState<Panel | null>(null);

  useEffect(() => {
    OfflineManager.getInstance().calculateOfflineGain();
    GameLoopManager.getInstance().start();
    return () => GameLoopManager.getInstance().stop();
  }, []);

  const togglePanel = (id: Panel) => setActivePanel(p => p === id ? null : id);

  return (
    <div className="relative w-full h-screen overflow-hidden text-white font-sans selection:bg-blue-500/30">
      <PhaserContainer />
      <UnlockNotification />

      <div className="absolute inset-0 pointer-events-none z-10 p-4 sm:p-6 lg:p-8 flex flex-col md:flex-row justify-between gap-4">

        {/* Colonne gauche */}
        <div className="flex flex-col gap-3 max-w-sm pointer-events-none">
          <div className="pointer-events-auto w-fit">
            <h1 className="text-3xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-emerald-400 drop-shadow-sm mb-4 pb-2 border-b-2 border-white/10 w-max">
              Étang des Merveilles
            </h1>
          </div>

          <Stats />
          <BoostOverlay />

          {/* Succès */}
          <div className="relative">
            <Achievements />
          </div>

          {/* Onglets des panneaux */}
          <div className="pointer-events-auto flex gap-1.5">
            {TABS.map(tab => (
              <button
                key={tab.id}
                onClick={() => togglePanel(tab.id)}
                className={`flex-1 py-2 rounded-lg text-xs font-semibold transition-all border ${
                  activePanel === tab.id
                    ? 'bg-white/15 border-white/20 text-white'
                    : 'bg-slate-900/60 border-white/10 text-gray-400 hover:bg-white/10 hover:text-white'
                }`}
                title={tab.label}
              >
                <span className="block text-base leading-none mb-0.5">{tab.icon}</span>
                <span>{tab.label}</span>
              </button>
            ))}
          </div>

          {/* Panneau actif */}
          {activePanel && (
            <div className="pointer-events-auto bg-slate-900/70 backdrop-blur-md rounded-xl border border-white/10 shadow-2xl p-4 overflow-y-auto max-h-[45vh]">
              {activePanel === 'research'   && <Research />}
              {activePanel === 'pearls'     && <PearlMarket />}
              {activePanel === 'journal'    && <Lore />}
              {activePanel === 'challenges' && <Challenges />}
            </div>
          )}
        </div>

        {/* Colonne droite : Boutique */}
        <div className="flex-1 flex justify-end items-start h-full pb-4">
          <Shop />
        </div>
      </div>
    </div>
  );
}

export default App;
