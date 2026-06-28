import { useEffect } from 'react';
import { useGameStore } from '../store/useGameStore';

export const EventNotification = () => {
  const pendingNarrativeEvent = useGameStore(s => s.pendingNarrativeEvent);
  const setPendingNarrativeEvent = useGameStore(s => s.setPendingNarrativeEvent);

  // L'événement vit dans le store : affiché directement, purgé après 7 s, sans
  // state local miroir (évite react-hooks/set-state-in-effect).
  useEffect(() => {
    if (!pendingNarrativeEvent) return;
    const timer = setTimeout(() => setPendingNarrativeEvent(null), 7000);
    return () => clearTimeout(timer);
  }, [pendingNarrativeEvent, setPendingNarrativeEvent]);

  if (!pendingNarrativeEvent) return null;
  const currentText = pendingNarrativeEvent;

  return (
    <div
      className="lg-toast lg-toast--event"
      style={{ animation: 'lg-toast-in .42s cubic-bezier(.3,.85,.25,1.2), lg-toast-out .45s ease 6.55s forwards' }}
    >
      <div className="lg-toast-eyebrow">✦ Étang des Merveilles</div>
      <p className="lg-toast-quote">{currentText}</p>
    </div>
  );
};
