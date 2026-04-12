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
import { Ameliorations } from './components/Ameliorations';
import { PearlMarket } from './components/PearlMarket';
import { PrestigeUpgrades } from './components/PrestigeUpgrades';
import { Lore } from './components/Lore';
import { Challenges } from './components/Challenges';
import { Guide } from './components/Guide';
import { EventNotification } from './components/EventNotification';
import { WelcomeBackNotification } from './components/WelcomeBackNotification';
import { useGameStore } from './store/useGameStore';

type PrimaryPanel = 'run' | 'gemmes' | 'succes' | 'parametres';
type SecondaryTab =
  | 'boutique' | 'ameliorations' | 'corail' | 'defis' | 'guide'
  | 'marche' | 'prestige-up' | 'boost'
  | 'succes' | 'journal';

const PRIMARY_NAV: { id: PrimaryPanel; icon: string; label: string }[] = [
  { id: 'run',     icon: '🎮', label: 'Run'    },
  { id: 'gemmes',  icon: '💎', label: 'Gemmes' },
  { id: 'succes',  icon: '🏆', label: 'Succès' },
];

function App() {
  const [activePrimary, setActivePrimary] = useState<PrimaryPanel | null>(null);
  const [activeSecondary, setActiveSecondary] = useState<SecondaryTab | null>(null);
  const prestiges = useGameStore(s => s.prestiges);

  useEffect(() => {
    OfflineManager.getInstance().calculateOfflineGain();
    GameLoopManager.getInstance().start();
    return () => GameLoopManager.getInstance().stop();
  }, []);

  const getSecondaryConfig = (panel: PrimaryPanel): { id: SecondaryTab; icon: string; label: string }[] => {
    switch (panel) {
      case 'run':
        return [
          { id: 'boutique',      icon: '🛒', label: 'Boutique' },
          { id: 'ameliorations', icon: '⚗️', label: 'Améliorations' },
          { id: 'defis',         icon: '⚔️', label: 'Défis'    },
          { id: 'guide',         icon: '❓', label: 'Guide'    },
        ];
      case 'gemmes': {
        const tabs: { id: SecondaryTab; icon: string; label: string }[] = [
          { id: 'marche',      icon: '💎', label: 'Marché'   },
          { id: 'prestige-up', icon: '🪸', label: 'Prestige' },
          { id: 'boost',       icon: '⚡', label: 'Boost'    },
        ];
        if (prestiges >= 1) {
          tabs.splice(0, 0, { id: 'corail', icon: '🧬', label: 'Corail' });
        }
        return tabs;
      }
      case 'succes':
        return [
          { id: 'succes',  icon: '🏆', label: 'Succès'  },
          { id: 'journal', icon: '📖', label: 'Journal' },
        ];
      default:
        return [];
    }
  };

  const handlePrimaryClick = (id: PrimaryPanel) => {
    if (activePrimary === id) {
      setActivePrimary(null);
      setActiveSecondary(null);
    } else {
      setActivePrimary(id);
      const tabs = getSecondaryConfig(id);
      setActiveSecondary(tabs[0]?.id ?? null);
    }
  };

  const tabs = activePrimary ? getSecondaryConfig(activePrimary) : [];
  const sidebarOpen = activePrimary !== null && tabs.length > 0;

  return (
    <div className="relative w-full h-screen overflow-hidden text-white font-sans selection:bg-blue-500/30">
      <PhaserContainer />
      <WelcomeBackNotification />
      <UnlockNotification />
      <EventNotification />

      {/* ── Primary Activity Bar ── */}
      <div className="absolute left-0 top-0 bottom-0 w-12 z-30 flex flex-col items-center bg-slate-950/80 backdrop-blur-sm border-r border-white/10 shadow-lg">
        {/* Top icons */}
        <div className="flex flex-col items-center gap-1 pt-3 flex-1">
          {PRIMARY_NAV.map(item => {
            const isActive = activePrimary === item.id;
            return (
              <button
                key={item.id}
                onClick={() => handlePrimaryClick(item.id)}
                title={item.label}
                className={`relative w-10 h-10 rounded-lg flex flex-col items-center justify-center gap-0.5 transition-all duration-150 ${
                  isActive
                    ? 'bg-white/15 text-white'
                    : 'text-gray-400 hover:bg-white/10 hover:text-white'
                }`}
              >
                {isActive && (
                  <span className="absolute left-0 top-1/2 -translate-y-1/2 w-0.5 h-6 bg-blue-400 rounded-r-full" />
                )}
                <span className="text-lg leading-none">{item.icon}</span>
                <span className="text-[7px] leading-none font-medium">{item.label}</span>
              </button>
            );
          })}
        </div>

        {/* Settings at bottom */}
        <div className="pb-3">
          <button
            onClick={() => handlePrimaryClick('parametres')}
            title="Paramètres"
            className={`relative w-10 h-10 rounded-lg flex flex-col items-center justify-center gap-0.5 transition-all duration-150 ${
              activePrimary === 'parametres'
                ? 'bg-white/15 text-white'
                : 'text-gray-400 hover:bg-white/10 hover:text-white'
            }`}
          >
            <span className="text-lg leading-none">⚙️</span>
            <span className="text-[7px] leading-none font-medium">Config</span>
          </button>
        </div>
      </div>

      {/* ── Secondary Sidebar (overlay) ── */}
      <div
        className={`absolute left-12 top-0 bottom-0 w-80 z-20 flex flex-col bg-slate-900/90 backdrop-blur-md border-r border-white/10 shadow-2xl transition-transform duration-200 ease-in-out ${
          sidebarOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Sub-tabs */}
        {tabs.length > 0 && (
          <div className="flex shrink-0 border-b border-white/10 bg-slate-950/60">
            {tabs.map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveSecondary(tab.id)}
                className={`flex-1 py-2.5 px-1 text-[10px] font-semibold flex flex-col items-center gap-0.5 transition-all border-b-2 ${
                  activeSecondary === tab.id
                    ? 'border-blue-400 text-white bg-white/5'
                    : 'border-transparent text-gray-400 hover:text-white hover:bg-white/5'
                }`}
              >
                <span className="text-sm leading-none">{tab.icon}</span>
                <span className="leading-none">{tab.label}</span>
              </button>
            ))}
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4">
          {activeSecondary === 'boutique'    && <Shop />}
          {activeSecondary === 'ameliorations' && <Ameliorations />}
          {activeSecondary === 'corail'      && <Research />}
          {activeSecondary === 'defis'       && <Challenges />}
          {activeSecondary === 'guide'       && <Guide />}
          {activeSecondary === 'marche'      && <PearlMarket />}
          {activeSecondary === 'prestige-up' && <PrestigeUpgrades />}
          {activeSecondary === 'boost'       && <BoostOverlay />}
          {activeSecondary === 'succes'      && <Achievements />}
          {activeSecondary === 'journal'     && <Lore />}
        </div>
      </div>

      {/* ── Right Info Panel ── */}
      <div className="absolute right-4 top-4 z-20 flex flex-col gap-3 pointer-events-auto max-w-xs">
        <h1 className="text-2xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-emerald-400 drop-shadow-sm pb-1 border-b border-white/10">
          Étang des Merveilles
        </h1>
        <Stats />
      </div>
    </div>
  );
}

export default App;
