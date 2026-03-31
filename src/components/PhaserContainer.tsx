import React, { useEffect, useRef } from 'react';
import { Game, AUTO, Scale } from 'phaser';
import { PondScene } from '../game/scenes/PondScene';
import { useGameStore } from '../store/useGameStore';
import { EventBus } from '../game/EventBus';

export const PhaserContainer: React.FC = () => {
  const gameRef = useRef<Game | null>(null);

  // S'abonner aux changements du tableau de poissons
  const ownedFishes = useGameStore(state => state.ownedFishes);
  const previousFishesCount = useRef(
    useGameStore.getState().ownedFishes.reduce((acc, fish) => acc + fish.quantite, 0)
  );

  useEffect(() => {
    // Initialiser Phaser une seule fois
    if (!gameRef.current) {
      gameRef.current = new Game({
        type: AUTO,
        parent: 'phaser-container',
        transparent: true, // Fond transparent selon la demande
        scale: {
          mode: Scale.FIT,
          autoCenter: Scale.CENTER_BOTH,
          width: 800,
          height: 450, // Aspect ratio 16:9
        },
        scene: [PondScene]
      });
    }

    // Nettoyage à la destruction du composant
    return () => {
      if (gameRef.current) {
        gameRef.current.destroy(true);
        gameRef.current = null;
      }
    };
  }, []);

  useEffect(() => {
    // Calculer le nombre total actuel de poissons
    const currentFishesCount = ownedFishes.reduce((acc, fish) => acc + fish.quantite, 0);

    // Si des poissons ont été ajoutés (nouveau total > ancien total)
    if (currentFishesCount > previousFishesCount.current) {
      const difference = currentFishesCount - previousFishesCount.current;

      // Émettre l'événement pour chaque nouveau poisson
      for (let i = 0; i < difference; i++) {
        EventBus.emit('SPAWN_FISH');
      }
    }

    // Mettre à jour la référence pour la prochaine comparaison
    previousFishesCount.current = currentFishesCount;
  }, [ownedFishes]);

  return (
    <div className="absolute inset-0 w-full h-full z-0">
      <div id="phaser-container" className="w-full h-full" style={{ aspectRatio: '16/9' }} />
    </div>
  );
};
