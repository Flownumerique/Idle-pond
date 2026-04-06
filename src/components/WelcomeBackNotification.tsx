import { useEffect, useState } from 'react';
import { useGameStore } from '../store/useGameStore';

const formatDuration = (minutes: number): string => {
  if (minutes < 60) return `${minutes} min`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return m > 0 ? `${h}h ${m}min` : `${h}h`;
};

export const WelcomeBackNotification = () => {
  const pendingWelcomeBack = useGameStore(s => s.pendingWelcomeBack);
  const setPendingWelcomeBack = useGameStore(s => s.setPendingWelcomeBack);
  const [visible, setVisible] = useState(false);
  const [data, setData] = useState<{ minutes: number; mana: string } | null>(null);

  useEffect(() => {
    if (!pendingWelcomeBack) return;

    setData(pendingWelcomeBack);
    setVisible(true);
    setPendingWelcomeBack(null);

    const timer = setTimeout(() => setVisible(false), 6000);
    return () => clearTimeout(timer);
  }, [pendingWelcomeBack, setPendingWelcomeBack]);

  if (!visible || !data) return null;

  return (
    <div
      className="fixed top-8 left-1/2 -translate-x-1/2 z-50 pointer-events-none"
      style={{ animation: 'wbSlideIn 0.4s ease-out, wbFadeOut 0.5s ease-in 5.5s forwards' }}
    >
      <div className="bg-slate-900/90 backdrop-blur-md border border-teal-400/40 rounded-2xl px-8 py-5 shadow-2xl shadow-teal-900/40 text-center min-w-[280px]">
        <div className="text-teal-300 text-xs uppercase tracking-widest font-bold mb-2">
          🌊 Bienvenue de retour !
        </div>
        <div className="text-white text-base font-bold mb-1">
          Absent{data.minutes >= 60 ? 'e' : ''} pendant {formatDuration(data.minutes)}
        </div>
        <div className="text-emerald-300 font-extrabold text-lg">
          +{data.mana} Mana
        </div>
        <div className="text-xs text-gray-500 mt-2 italic">
          Les poissons ont travaillé pendant votre absence
        </div>
      </div>

      <style>{`
        @keyframes wbSlideIn {
          from { opacity: 0; transform: translate(-50%, -20px); }
          to   { opacity: 1; transform: translate(-50%, 0); }
        }
        @keyframes wbFadeOut {
          from { opacity: 1; }
          to   { opacity: 0; }
        }
      `}</style>
    </div>
  );
};
