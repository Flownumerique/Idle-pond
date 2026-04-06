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
import { PrestigeUpgrades } from './components/PrestigeUpgrades';
import { Lore } from './components/Lore';
import { Challenges } from './components/Challenges';
import { Guide } from './components/Guide';

type Panel = 'corail' | 'marche' | 'prestige' | 'journal' | 'defis' | 'guide';

const TABS: { id: Panel; icon: string; label: string }[] = [
  { id: 'corail',   icon: '🧬', label: 'Corail'   },
  { id: 'marche',   icon: '💎', label: 'Marché'   },
  { id: 'prestige', icon: '🪸', label: 'Prestige' },
  { id: 'journal',  icon: '📖', label: 'Journal'  },
  { id: 'defis',    icon: '⚔️', label: 'Défis'    },
  { id: 'guide',    icon: '❓', label: 'Guide'    },
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

          <div className="relative">
            <Achievements />
          </div>

          {/* Barre d'onglets */}
          <div className="pointer-events-auto grid grid-cols-6 gap-1">
            {TABS.map(tab => (
              <button
                key={tab.id}
                onClick={() => togglePanel(tab.id)}
                title={tab.label}
                className={`py-1.5 rounded-lg text-[10px] font-semibold transition-all border flex flex-col items-center gap-0.5 ${
                  activePanel === tab.id
                    ? 'bg-white/15 border-white/25 text-white'
                    : 'bg-slate-900/60 border-white/10 text-gray-400 hover:bg-white/10 hover:text-white'
                }`}
              >
                <span className="text-sm leading-none">{tab.icon}</span>
                <span className="leading-none">{tab.label}</span>
              </button>
            ))}
          </div>

          {/* Panneau actif */}
          {activePanel && (
            <div className="pointer-events-auto bg-slate-900/70 backdrop-blur-md rounded-xl border border-white/10 shadow-2xl p-4 overflow-y-auto max-h-[45vh]">
              {activePanel === 'corail'   && <Research />}
              {activePanel === 'marche'   && <PearlMarket />}
              {activePanel === 'prestige' && <PrestigeUpgrades />}
              {activePanel === 'journal'  && <Lore />}
              {activePanel === 'defis'    && <Challenges />}
              {activePanel === 'guide'    && <Guide />}
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
