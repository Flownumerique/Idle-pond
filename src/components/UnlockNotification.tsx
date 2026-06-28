import { useEffect } from 'react';
import { useGameStore } from '../store/useGameStore';
import { FISH_TYPES } from '../data/fishTypes';

const DEPTH_NAMES = [
  'Lac de Surface',
  'Rivière Souterraine',
  'Récif Corallien',
  'Abysses',
  'Zone Hydrothermale',
  'Plaine Abyssale',
  'Fosse des Origines',
  'Nexus de Mana',
  'Cœur Volcanique',
  'Royaume Céleste',
  'Dimension Quantique',
];

export const UnlockNotification = () => {
  const pendingUnlock = useGameStore(state => state.pendingUnlock);
  const clearPendingUnlock = useGameStore(state => state.clearPendingUnlock);

  // L'événement vit dans le store : on le purge 4 s après son apparition, sans
  // recopier la valeur en state local (évite react-hooks/set-state-in-effect).
  useEffect(() => {
    if (!pendingUnlock) return;
    const timer = setTimeout(() => clearPendingUnlock(), 4000);
    return () => clearTimeout(timer);
  }, [pendingUnlock, clearPendingUnlock]);

  // Le contenu est dérivé du store pendant le rendu (id de la forme "depth_X").
  const match = pendingUnlock?.match(/^depth_(\d+)$/);
  if (!match) return null;
  const depth = parseInt(match[1], 10);
  const fish = FISH_TYPES.find(f => f.requiredDepth === depth);
  const content = { depth, fish };

  return (
    <div
      className="lg-toast lg-toast--unlock"
      style={{ animation: 'lg-toast-in .42s cubic-bezier(.3,.85,.25,1.2), lg-toast-out .45s ease 3.55s forwards' }}
    >
      <div className="lg-toast-icon">⛏️</div>
      <div className="lg-toast-eyebrow">Profondeur {content.depth} débloquée</div>
      <div className="lg-toast-title">{DEPTH_NAMES[content.depth] ?? 'Nouveau niveau'}</div>
      {content.fish && (
        <div className="lg-toast-sub" style={{ color: '#C98A0F' }}>
          {content.fish.emoji} {content.fish.name} maintenant disponible !
        </div>
      )}
    </div>
  );
};
