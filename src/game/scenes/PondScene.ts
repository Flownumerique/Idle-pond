import Phaser from 'phaser';
import type { PoissonInstance } from '../../store/useGameStore';

const ZONE_HEIGHT = 1080;
const MAX_DEPTH = 7;
const WORLD_HEIGHT = ZONE_HEIGHT * (MAX_DEPTH + 1);

// Couleur de fond et de l'eau par profondeur
const DEPTH_COLORS = [
  { bg: 0x0d2a4a, water: 0x1a5a8f },  // 0 – Lac de Surface (bleu clair)
  { bg: 0x0a1f3a, water: 0x134a72 },  // 1 – Rivière Souterraine
  { bg: 0x071428, water: 0x0d3454 },  // 2 – Récif Corallien
  { bg: 0x04090f, water: 0x07182a },  // 3 – Abysses (presque noir)
  { bg: 0x1a0500, water: 0x4a1200 },  // 4 – Zone Hydrothermale (rouge sombre)
  { bg: 0x00040a, water: 0x001428 },  // 5 – Plaine Abyssale (bleu nuit profond)
  { bg: 0x050003, water: 0x10001f },  // 6 – Fosse des Origines (violet très sombre)
  { bg: 0x0d0018, water: 0x1f0040 },  // 7 – Nexus de Mana (violet-or mystique)
];

const DEPTH_LABELS = [
  'Lac de Surface',
  'Rivière Souterraine',
  'Récif Corallien',
  'Abysses',
  'Zone Hydrothermale',
  'Plaine Abyssale',
  'Fosse des Origines',
  'Nexus de Mana',
];

// Profondeur requise par type de poisson
const FISH_DEPTH: Record<string, number> = {
  gold:       0,
  carpe:      0,
  ruby:       1,
  dragonfly:  1,
  diamond:    2,
  crab:       2,
  abyssal:    3,
  octopus:    3,
  salamander: 4,
  eel:        4,
  jellyfish:  5,
  shark:      5,
  dragon:     6,
  leviathan:  6,
  egregore:   7,
  celestial:  7,
};

const FISH_COLORS: Record<string, number> = {
  gold:       0xffd700,
  carpe:      0xff8c00,
  ruby:       0xe0115f,
  dragonfly:  0x4169e1,
  diamond:    0x00d4ff,
  crab:       0x00ffcc,
  abyssal:    0x9b59b6,
  octopus:    0x4b0082,
  salamander: 0xff4500,
  eel:        0xff6600,
  jellyfish:  0x00ff99,
  shark:      0x708090,
  dragon:     0x228b22,
  leviathan:  0xe8e8e8,
  egregore:   0xdaa520,
  celestial:  0xfffacd,
};

export class PondScene extends Phaser.Scene {
  private fishSprites = new Map<string, Phaser.GameObjects.Arc>();
  private currentDepth = 0;
  private isDragging = false;
  private lastPointerY = 0;

  constructor() {
    super('PondScene');
  }

  create() {
    const width = this.scale.width;

    // Monde plus haut que la caméra pour permettre le scroll
    this.cameras.main.setBounds(0, 0, width, WORLD_HEIGHT);

    // Fond par zone de profondeur
    for (let d = 0; d <= MAX_DEPTH; d++) {
      const yStart = d * ZONE_HEIGHT;
      const { bg, water } = DEPTH_COLORS[d];

      this.add.rectangle(width / 2, yStart + ZONE_HEIGHT / 2, width, ZONE_HEIGHT, bg);

      // Bulles décoratives
      const bubbleCount = d >= 5 ? 12 : 18;
      for (let i = 0; i < bubbleCount; i++) {
        this.add.circle(
          Phaser.Math.Between(0, width),
          yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
          Phaser.Math.Between(8, 40),
          water,
          0.2
        );
      }

      // Particules de mana pour le Nexus (depth 7)
      if (d === 7) {
        for (let i = 0; i < 30; i++) {
          const star = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(2, 8),
            0xdaa520,
            0.6
          );
          this.tweens.add({
            targets: star,
            alpha: { from: 0.2, to: 0.8 },
            scaleX: { from: 0.8, to: 1.2 },
            scaleY: { from: 0.8, to: 1.2 },
            duration: Phaser.Math.Between(1500, 3500),
            yoyo: true,
            repeat: -1,
            ease: 'Sine.easeInOut',
          });
        }
      }

      // Étiquette de zone
      this.add.text(width / 2, yStart + 40, DEPTH_LABELS[d], {
        fontSize: '22px',
        color: '#ffffff',
        alpha: 0.3,
      } as Phaser.Types.GameObjects.Text.TextStyle).setOrigin(0.5, 0).setAlpha(0.3);

      // Ligne séparatrice entre zones
      if (d > 0) {
        const line = this.add.rectangle(width / 2, yStart, width, 3, 0xffffff, 0.06);
        line.setDepth(1);
      }
    }

    // Scroll à la molette
    this.input.on('wheel', (_ptr: unknown, _objs: unknown, _dx: number, dy: number) => {
      const cam = this.cameras.main;
      const newY = Phaser.Math.Clamp(
        cam.scrollY + dy * 0.8,
        0,
        (this.currentDepth + 1) * ZONE_HEIGHT - ZONE_HEIGHT
      );
      cam.setScroll(0, newY);
    });

    // Drag scroll (tactile / souris)
    this.input.on('pointerdown', (ptr: Phaser.Input.Pointer) => {
      this.isDragging = true;
      this.lastPointerY = ptr.y;
    });
    this.input.on('pointermove', (ptr: Phaser.Input.Pointer) => {
      if (!this.isDragging) return;
      const delta = this.lastPointerY - ptr.y;
      this.lastPointerY = ptr.y;
      const cam = this.cameras.main;
      const newY = Phaser.Math.Clamp(
        cam.scrollY + delta,
        0,
        (this.currentDepth + 1) * ZONE_HEIGHT - ZONE_HEIGHT
      );
      cam.setScroll(0, newY);
    });
    this.input.on('pointerup', () => { this.isDragging = false; });

    // Écoute les events depuis React
    this.game.events.on('update-fishes', this.updateFishes, this);
    this.game.events.on('update-depth', this.onDepthChange, this);
    this.game.events.emit('scene-ready');
  }

  private onDepthChange(depth: number) {
    this.currentDepth = depth;
  }

  updateFishes(poissons: PoissonInstance[]) {
    const width = this.scale.width;

    poissons.forEach(fish => {
      if (!this.fishSprites.has(fish.id)) {
        const fishDepthZone = FISH_DEPTH[fish.type] ?? 0;
        const zoneY = fishDepthZone * ZONE_HEIGHT;

        const x = Phaser.Math.Between(60, width - 60);
        const y = zoneY + Phaser.Math.Between(120, ZONE_HEIGHT - 120);

        const color = FISH_COLORS[fish.type] ?? 0xffffff;
        const radius = 12 + Math.min(fish.level, 50);  // cap visual size at level 50

        const sprite = this.add.circle(x, y, radius, color, 0.85);
        sprite.setDepth(2);

        this.tweens.add({
          targets: sprite,
          x: `+=${Phaser.Math.Between(-140, 140)}`,
          y: `+=${Phaser.Math.Between(-80, 80)}`,
          duration: Phaser.Math.Between(3000, 7000),
          yoyo: true,
          repeat: -1,
          ease: 'Sine.easeInOut',
        });

        this.fishSprites.set(fish.id, sprite);
      } else {
        const sprite = this.fishSprites.get(fish.id)!;
        sprite.setRadius(12 + Math.min(fish.level, 50));
      }
    });

    // Supprimer les sprites de poissons qui n'existent plus
    const currentIds = new Set(poissons.map(f => f.id));
    for (const [id, sprite] of this.fishSprites.entries()) {
      if (!currentIds.has(id)) {
        sprite.destroy();
        this.fishSprites.delete(id);
      }
    }
  }
}
