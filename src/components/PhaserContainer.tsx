import { useEffect, useRef } from 'react';
import Phaser from 'phaser';
import { PondScene } from '../game/scenes/PondScene';
import { useGameStore } from '../store/useGameStore';

export const PhaserContainer = () => {
  const containerRef = useRef<HTMLDivElement>(null);
  const gameRef = useRef<Phaser.Game | null>(null);

  const poissons = useGameStore(state => state.poissons);
  const pondDepth = useGameStore(state => state.pondDepth);

  // Initialisation Phaser
  useEffect(() => {
    if (containerRef.current && !gameRef.current) {
      const config: Phaser.Types.Core.GameConfig = {
        type: Phaser.AUTO,
        parent: containerRef.current,
        width: 1920,
        height: 1080,
        scale: {
          mode: Phaser.Scale.FIT,
          autoCenter: Phaser.Scale.CENTER_BOTH,
          autoRound: true,
        },
        transparent: true,
        scene: [PondScene],
      };

      gameRef.current = new Phaser.Game(config);

      gameRef.current.events.once('scene-ready', () => {
        const state = useGameStore.getState();
        gameRef.current?.events.emit('update-depth', state.pondDepth);
        gameRef.current?.events.emit('update-fishes', state.poissons);
      });
    }

    return () => {
      if (gameRef.current) {
        gameRef.current.destroy(true);
        gameRef.current = null;
      }
    };
  }, []);

  // Synchroniser les poissons
  useEffect(() => {
    gameRef.current?.events.emit('update-fishes', poissons);
  }, [poissons]);

  // Synchroniser la profondeur
  useEffect(() => {
    gameRef.current?.events.emit('update-depth', pondDepth);
  }, [pondDepth]);

  return (
    <div
      ref={containerRef}
      className="absolute top-0 left-0 w-full h-full z-0 overflow-hidden bg-gray-900"
    />
  );
};
