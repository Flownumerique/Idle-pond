import { useEffect, useRef } from 'react';
import Phaser from 'phaser';
import { PondScene } from '../game/scenes/PondScene';
import { useGameStore } from '../store/useGameStore';

export const PhaserContainer = () => {
  const containerRef = useRef<HTMLDivElement>(null);
  const gameRef = useRef<Phaser.Game | null>(null);

  const poissons = useGameStore(state => state.poissons);

  // Initialize Phaser
  useEffect(() => {
    if (containerRef.current && !gameRef.current) {
      const config: Phaser.Types.Core.GameConfig = {
        type: Phaser.AUTO, // WebGL fallback Canvas
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

      // Send initial data once scene is ready
      gameRef.current.events.once('scene-ready', () => {
        gameRef.current?.events.emit('update-fishes', useGameStore.getState().poissons);
      });
    }

    return () => {
      // Proper cleanup for React Strict Mode
      if (gameRef.current) {
        gameRef.current.destroy(true);
        gameRef.current = null;
      }
    };
  }, []);

  // Sync state to Phaser Scene (PhaserBridge)
  useEffect(() => {
    if (gameRef.current) {
      // Dispatch custom event to Phaser
      gameRef.current.events.emit('update-fishes', poissons);
    }
  }, [poissons]);

  return (
    <div
      ref={containerRef}
      className="absolute top-0 left-0 w-full h-full z-0 overflow-hidden bg-gray-900"
    />
  );
};
