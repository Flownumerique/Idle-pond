import { useEffect, useState } from 'react';
import { useGameStore } from '../store/useGameStore';
import { FISH_TYPES } from './Shop';

const DEPTH_NAMES = ['Peu profond', 'Standard', 'Profond', 'Abyssal', 'Maximum'];

export const UnlockNotification = () => {
  const pendingUnlock = useGameStore(state => state.pendingUnlock);
  const clearPendingUnlock = useGameStore(state => state.clearPendingUnlock);
  const [visible, setVisible] = useState(false);
  const [content, setContent] = useState<{ depth: number; fish: typeof FISH_TYPES[0] | undefined } | null>(null);

  useEffect(() => {
    if (!pendingUnlock) return;

    // Extraire le niveau depuis l'id "depth_X"
    const match = pendingUnlock.match(/^depth_(\d+)$/);
    if (!match) {
      clearPendingUnlock();
      return;
    }

    const depth = parseInt(match[1], 10);
    const fish = FISH_TYPES.find(f => f.requiredDepth === depth);

    setContent({ depth, fish });
    setVisible(true);
    clearPendingUnlock();

    const timer = setTimeout(() => setVisible(false), 4000);
    return () => clearTimeout(timer);
  }, [pendingUnlock, clearPendingUnlock]);

  if (!visible || !content) return null;

  return (
    <div
      className="fixed top-8 left-1/2 -translate-x-1/2 z-50 pointer-events-none"
      style={{ animation: 'unlockSlideIn 0.4s ease-out, unlockFadeOut 0.5s ease-in 3.5s forwards' }}
    >
      <div className="bg-teal-900/90 backdrop-blur-md border border-teal-400/40 rounded-2xl px-8 py-5 shadow-2xl shadow-teal-900/60 text-center">
        <div className="text-teal-300 text-xs uppercase tracking-widest font-bold mb-1">
          ⛏️ Profondeur {content.depth} débloquée !
        </div>
        <div className="text-white text-lg font-extrabold mb-1">
          {DEPTH_NAMES[content.depth] ?? 'Nouveau niveau'}
        </div>
        {content.fish && (
          <div className="text-yellow-300 font-semibold text-sm mt-2">
            {content.fish.emoji} {content.fish.name} maintenant disponible !
          </div>
        )}
      </div>

      <style>{`
        @keyframes unlockSlideIn {
          from { opacity: 0; transform: translate(-50%, -20px); }
          to   { opacity: 1; transform: translate(-50%, 0); }
        }
        @keyframes unlockFadeOut {
          from { opacity: 1; }
          to   { opacity: 0; }
        }
      `}</style>
    </div>
  );
};
