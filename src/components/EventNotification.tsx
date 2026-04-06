import { useEffect, useState } from 'react';
import { useGameStore } from '../store/useGameStore';

export const EventNotification = () => {
  const pendingNarrativeEvent = useGameStore(s => s.pendingNarrativeEvent);
  const setPendingNarrativeEvent = useGameStore(s => s.setPendingNarrativeEvent);
  const [visible, setVisible] = useState(false);
  const [currentText, setCurrentText] = useState('');

  useEffect(() => {
    if (!pendingNarrativeEvent) return;

    setCurrentText(pendingNarrativeEvent);
    setVisible(true);
    setPendingNarrativeEvent(null);

    const timer = setTimeout(() => {
      setVisible(false);
    }, 7000);

    return () => clearTimeout(timer);
  }, [pendingNarrativeEvent, setPendingNarrativeEvent]);

  if (!visible || !currentText) return null;

  return (
    <div
      className="fixed bottom-24 left-1/2 -translate-x-1/2 z-50 pointer-events-none"
      style={{ animation: 'fadeInUp 0.5s ease-out' }}
    >
      <div className="bg-slate-900/80 backdrop-blur-md border border-blue-400/20 rounded-xl px-5 py-3 shadow-2xl max-w-sm text-center">
        <div className="text-[10px] text-blue-400/70 uppercase tracking-widest font-bold mb-1">
          ✦ Étang des Merveilles
        </div>
        <p className="text-sm text-blue-100/90 leading-relaxed italic">
          {currentText}
        </p>
      </div>
      <style>{`
        @keyframes fadeInUp {
          from { opacity: 0; transform: translate(-50%, 12px); }
          to   { opacity: 1; transform: translate(-50%, 0); }
        }
      `}</style>
    </div>
  );
};
