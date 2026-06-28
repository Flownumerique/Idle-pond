import { useEffect } from 'react';
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

  // L'événement vit dans le store : on l'affiche directement et on le purge
  // après 6 s, sans dupliquer la valeur dans un state local (évite les
  // re-renders en cascade signalés par react-hooks/set-state-in-effect).
  useEffect(() => {
    if (!pendingWelcomeBack) return;
    const timer = setTimeout(() => setPendingWelcomeBack(null), 6000);
    return () => clearTimeout(timer);
  }, [pendingWelcomeBack, setPendingWelcomeBack]);

  if (!pendingWelcomeBack) return null;
  const data = pendingWelcomeBack;

  return (
    <div
      className="lg-toast lg-toast--welcome"
      style={{ animation: 'lg-toast-in .42s cubic-bezier(.3,.85,.25,1.2), lg-toast-out .45s ease 5.55s forwards' }}
    >
      <div className="lg-toast-icon">🌊</div>
      <div className="lg-toast-eyebrow">Bienvenue de retour</div>
      <div className="lg-toast-title">
        Absent{data.minutes >= 60 ? 'e' : ''} pendant {formatDuration(data.minutes)}
      </div>
      <div className="lg-toast-sub" style={{ color: '#1FA971', fontSize: 15 }}>
        +{data.mana} Mana
      </div>
      <div className="lg-toast-note">Les poissons ont travaillé pendant votre absence</div>
    </div>
  );
};
