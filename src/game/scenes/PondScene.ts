import Phaser from 'phaser';
import type { PoissonInstance } from '../../store/useGameStore';

const ZONE_HEIGHT = 1080;
const MAX_DEPTH = 10;
const WORLD_HEIGHT = ZONE_HEIGHT * (MAX_DEPTH + 1);

const DEPTH_COLORS = [
  { bg: 0x0d2a4a, water: 0x1a5a8f },  // 0 – Lac de Surface
  { bg: 0x0a1f3a, water: 0x134a72 },  // 1 – Rivière Souterraine
  { bg: 0x071428, water: 0x0d3454 },  // 2 – Récif Corallien
  { bg: 0x04090f, water: 0x07182a },  // 3 – Abysses
  { bg: 0x1a0500, water: 0x4a1200 },  // 4 – Zone Hydrothermale
  { bg: 0x00040a, water: 0x001428 },  // 5 – Plaine Abyssale
  { bg: 0x050003, water: 0x10001f },  // 6 – Fosse des Origines
  { bg: 0x0d0018, water: 0x1f0040 },  // 7 – Nexus de Mana
  { bg: 0x1f0200, water: 0x6a0800 },  // 8 – Cœur Volcanique
  { bg: 0x04000f, water: 0x120030 },  // 9 – Royaume Céleste
  { bg: 0x000a08, water: 0x001a18 },  // 10 – Dimension Quantique
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
  'Cœur Volcanique',
  'Royaume Céleste',
  'Dimension Quantique',
];

const FISH_DEPTH: Record<string, number> = {
  gold: 0, carpe: 0, frog: 0, duck: 0,
  ruby: 1, dragonfly: 1, cobalt: 1, nymph: 1,
  diamond: 2, crab: 2, snail: 2, shrimp: 2,
  abyssal: 3, octopus: 3, anemone: 3, spectre: 3,
  salamander: 4, eel: 4, scorpion: 4, lava_snake: 4,
  jellyfish: 5, shark: 5, dolphin: 5, whale: 5,
  dragon: 6, leviathan: 6, plesio: 6, basilisk: 6,
  egregore: 7, phoenix_nexus: 7, nexus_spirit: 7, celestial: 7,
  lava_spirit: 8, pyro_ray: 8, lava_titan: 8,
  angel: 9, aurora_fish: 9, sun_fish: 9,
  cyberfish: 10, prism_manta: 10, quantum: 10,
};

const FISH_COLORS: Record<string, number> = {
  gold: 0xffd700,      carpe: 0xff8c00,
  frog: 0x00cc55,      duck: 0xffdd00,
  ruby: 0xe0115f,      dragonfly: 0x4169e1,
  cobalt: 0x0088ff,    nymph: 0xff88ff,
  diamond: 0x00d4ff,   crab: 0x00ffcc,
  snail: 0xb088ff,     shrimp: 0xffb0c8,
  abyssal: 0x9b59b6,   octopus: 0x4b0082,
  anemone: 0xff5577,   spectre: 0x88ffff,
  salamander: 0xff4500, eel: 0xff6600,
  scorpion: 0xff2200,  lava_snake: 0xff7700,
  jellyfish: 0x00ff99, shark: 0x708090,
  dolphin: 0x44ccff,   whale: 0x223355,
  dragon: 0x228b22,    leviathan: 0xe8e8e8,
  plesio: 0x33aa44,    basilisk: 0x886600,
  egregore: 0xdaa520,  phoenix_nexus: 0xff9900,
  nexus_spirit: 0xeeeeff, celestial: 0xfffacd,
  lava_spirit: 0xff3300, pyro_ray: 0xff6622,
  lava_titan: 0xcc1100,
  angel: 0xffffcc,     aurora_fish: 0xcc88ff,
  sun_fish: 0xffee00,
  cyberfish: 0x00ffcc, prism_manta: 0x6688ff,
  quantum: 0x9900ff,
};

export class PondScene extends Phaser.Scene {
  private fishSprites = new Map<string, Phaser.GameObjects.Arc>();
  private currentDepth = 0;
  private isDragging = false;
  private lastPointerY = 0;

  constructor() { super('PondScene'); }

  create() {
    const width = this.scale.width;
    this.cameras.main.setBounds(0, 0, width, WORLD_HEIGHT);

    for (let d = 0; d <= MAX_DEPTH; d++) {
      const yStart = d * ZONE_HEIGHT;
      const { bg, water } = DEPTH_COLORS[d];

      this.add.rectangle(width / 2, yStart + ZONE_HEIGHT / 2, width, ZONE_HEIGHT, bg);

      // Particules spéciales par zone
      if (d === 7) {
        // Nexus : mana dorée pulsante
        for (let i = 0; i < 30; i++) {
          const star = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(2, 8), 0xdaa520, 0.6
          );
          this.tweens.add({ targets: star, alpha: { from: 0.2, to: 0.8 }, scaleX: { from: 0.8, to: 1.2 }, scaleY: { from: 0.8, to: 1.2 }, duration: Phaser.Math.Between(1500, 3500), yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });
        }
      } else if (d === 8) {
        // Cœur Volcanique : particules rouges/oranges qui montent
        for (let i = 0; i < 20; i++) {
          const ember = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(200, ZONE_HEIGHT),
            Phaser.Math.Between(3, 10), d === 8 ? 0xff4400 : 0xff6600, 0.7
          );
          this.tweens.add({ targets: ember, y: `-=${Phaser.Math.Between(100, 300)}`, alpha: { from: 0.7, to: 0 }, duration: Phaser.Math.Between(2000, 5000), repeat: -1, ease: 'Quad.easeIn', delay: Phaser.Math.Between(0, 3000) });
        }
      } else if (d === 9) {
        // Royaume Céleste : étoiles scintillantes
        for (let i = 0; i < 40; i++) {
          const glow = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(1, 6), 0xccaaff, 0.8
          );
          this.tweens.add({ targets: glow, alpha: { from: 0.1, to: 0.9 }, duration: Phaser.Math.Between(800, 2500), yoyo: true, repeat: -1, ease: 'Sine.easeInOut', delay: Phaser.Math.Between(0, 2000) });
        }
      } else if (d === 10) {
        // Dimension Quantique : grille de données
        for (let i = 0; i < 25; i++) {
          const data = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(2, 7), i % 2 === 0 ? 0x00ffcc : 0x9900ff, 0.6
          );
          this.tweens.add({ targets: data, x: `+=${Phaser.Math.Between(-50, 50)}`, alpha: { from: 0.2, to: 0.8 }, duration: Phaser.Math.Between(500, 1500), yoyo: true, repeat: -1, ease: 'Bounce.easeOut', delay: Phaser.Math.Between(0, 1500) });
        }
      } else {
        // Bulles standard
        for (let i = 0; i < 18; i++) {
          this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(8, 40), water, 0.2
          );
        }
      }

      this.add.text(width / 2, yStart + 40, DEPTH_LABELS[d], {
        fontSize: '22px', color: '#ffffff',
      } as Phaser.Types.GameObjects.Text.TextStyle).setOrigin(0.5, 0).setAlpha(0.3);

      if (d > 0) {
        this.add.rectangle(width / 2, yStart, width, 3, 0xffffff, 0.06).setDepth(1);
      }
    }

    this.input.on('wheel', (_ptr: unknown, _objs: unknown, _dx: number, dy: number) => {
      const cam = this.cameras.main;
      cam.setScroll(0, Phaser.Math.Clamp(
        cam.scrollY + dy * 0.8, 0,
        (this.currentDepth + 1) * ZONE_HEIGHT - ZONE_HEIGHT
      ));
    });
    this.input.on('pointerdown', (ptr: Phaser.Input.Pointer) => {
      this.isDragging = true; this.lastPointerY = ptr.y;
    });
    this.input.on('pointermove', (ptr: Phaser.Input.Pointer) => {
      if (!this.isDragging) return;
      const delta = this.lastPointerY - ptr.y;
      this.lastPointerY = ptr.y;
      const cam = this.cameras.main;
      cam.setScroll(0, Phaser.Math.Clamp(
        cam.scrollY + delta, 0,
        (this.currentDepth + 1) * ZONE_HEIGHT - ZONE_HEIGHT
      ));
    });
    this.input.on('pointerup', () => { this.isDragging = false; });

    this.game.events.on('update-fishes', this.updateFishes, this);
    this.game.events.on('update-depth', this.onDepthChange, this);
    this.game.events.emit('scene-ready');
  }

  private onDepthChange(depth: number) { this.currentDepth = depth; }

  updateFishes(poissons: PoissonInstance[]) {
    const width = this.scale.width;

    poissons.forEach(fish => {
      if (!this.fishSprites.has(fish.id)) {
        const fishDepthZone = FISH_DEPTH[fish.type] ?? 0;
        const zoneY = fishDepthZone * ZONE_HEIGHT;
        const x = Phaser.Math.Between(60, width - 60);
        const y = zoneY + Phaser.Math.Between(120, ZONE_HEIGHT - 120);
        const color = FISH_COLORS[fish.type] ?? 0xffffff;
        const radius = 12 + Math.min(fish.level, 50);
        const sprite = this.add.circle(x, y, radius, color, 0.85);
        sprite.setDepth(2);
        this.tweens.add({
          targets: sprite,
          x: `+=${Phaser.Math.Between(-140, 140)}`,
          y: `+=${Phaser.Math.Between(-80, 80)}`,
          duration: Phaser.Math.Between(3000, 7000),
          yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
        });
        this.fishSprites.set(fish.id, sprite);
      } else {
        this.fishSprites.get(fish.id)!.setRadius(12 + Math.min(fish.level, 50));
      }
    });

    const currentIds = new Set(poissons.map(f => f.id));
    for (const [id, sprite] of this.fishSprites.entries()) {
      if (!currentIds.has(id)) { sprite.destroy(); this.fishSprites.delete(id); }
    }
  }
}
