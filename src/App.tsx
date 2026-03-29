import { useEffect } from 'react';
import { GameLoopManager } from './managers/GameLoopManager';
import { OfflineManager } from './managers/OfflineManager';
import { PhaserContainer } from './components/PhaserContainer';
import { Stats } from './components/Stats';
import { Shop } from './components/Shop';
import { BoostOverlay } from './components/BoostOverlay';

function App() {
  useEffect(() => {
    // Check for offline progress
    OfflineManager.getInstance().calculateOfflineGain();

    // Start game loop
    GameLoopManager.getInstance().start();

    return () => {
      GameLoopManager.getInstance().stop();
    };
  }, []);

  return (
    <div className="relative w-full h-screen overflow-hidden text-white font-sans selection:bg-blue-500/30">
      {/* Background Layer: Phaser */}
      <PhaserContainer />

      {/* UI Layer: React Overlays */}
      <div className="absolute inset-0 pointer-events-none z-10 p-4 sm:p-6 lg:p-8 flex flex-col md:flex-row justify-between">

        {/* Left Column: Stats & Boost */}
        <div className="flex flex-col gap-4 max-w-sm pointer-events-none">
          <div className="pointer-events-auto w-fit">
            <h1 className="text-3xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-emerald-400 drop-shadow-sm mb-6 pb-2 border-b-2 border-white/10 w-max">
              Étang des Merveilles
            </h1>
          </div>

          <Stats />
          <BoostOverlay />
        </div>

        {/* Right Column: Shop */}
        <div className="flex-1 flex justify-end items-start h-full pb-4">
          <Shop />
        </div>
      </div>
    </div>
  );
}

export default App;
