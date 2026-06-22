import Phaser from 'phaser';
import type { PoissonInstance } from '../../store/useGameStore';
import { FISH_TYPES } from '../../data/fishTypes';

const ZONE_HEIGHT = 1080;
const MAX_DEPTH = 11;
const WORLD_HEIGHT = ZONE_HEIGHT * (MAX_DEPTH + 1);
const ANIM_FRAMES = 8;          // frames par spritesheet d'animation
const ANIM_FRAME_SIZE = 128;    // taille d'une frame (px)

const DEPTH_COLORS = [
  { bg: 0x0d2a4a, water: 0x1a5a8f },  // 0  – Lac de Surface
  { bg: 0x0a1f3a, water: 0x134a72 },  // 1  – Rivière Souterraine
  { bg: 0x071428, water: 0x0d3454 },  // 2  – Récif Corallien
  { bg: 0x052a3a, water: 0x0e6b82 },  // 3  – Océan des Profondeurs
  { bg: 0x04090f, water: 0x07182a },  // 4  – Abysses
  { bg: 0x1a0500, water: 0x4a1200 },  // 5  – Zone Hydrothermale
  { bg: 0x00040a, water: 0x001428 },  // 6  – Plaine Abyssale
  { bg: 0x050003, water: 0x10001f },  // 7  – Fosse des Origines
  { bg: 0x0d0018, water: 0x1f0040 },  // 8  – Nexus de Mana
  { bg: 0x1f0200, water: 0x6a0800 },  // 9  – Cœur Volcanique
  { bg: 0x04000f, water: 0x120030 },  // 10 – Royaume Céleste
  { bg: 0x000a08, water: 0x001a18 },  // 11 – Dimension Quantique
];

const DEPTH_LABELS = [
  'Lac de Surface',
  'Rivière Souterraine',
  'Récif Corallien',
  'Océan des Profondeurs',
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
  clownfish: 3,
  abyssal: 4, octopus: 4, anemone: 4, spectre: 4,
  salamander: 5, eel: 5, scorpion: 5, lava_snake: 5,
  jellyfish: 6, shark: 6, dolphin: 6, whale: 6,
  dragon: 7, leviathan: 7, plesio: 7, basilisk: 7,
  egregore: 8, phoenix_nexus: 8, nexus_spirit: 8, celestial: 8,
  lava_spirit: 9, pyro_ray: 9, lava_titan: 9,
  angel: 10, aurora_fish: 10, sun_fish: 10,
  cyberfish: 11, prism_manta: 11, quantum: 11,
};

const FISH_COLORS: Record<string, number> = {
  gold: 0xffd700,      carpe: 0xff8c00,
  frog: 0x00cc55,      duck: 0xffdd00,
  ruby: 0xe0115f,      dragonfly: 0x4169e1,
  cobalt: 0x0088ff,    nymph: 0xff88ff,
  diamond: 0x00d4ff,   crab: 0x00ffcc,
  snail: 0xb088ff,     shrimp: 0xffb0c8,
  clownfish: 0xff6600,
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

// Fish types that use sprite images (keyed by fish.type → texture key)
const SPRITE_FISH = new Map<string, string>(
  FISH_TYPES
    .filter(f => f.sprite)
    .map(f => [f.type, f.type])
);

type FishGameObject = Phaser.GameObjects.Arc | Phaser.GameObjects.Sprite;

export class PondScene extends Phaser.Scene {
  private fishSprites = new Map<string, FishGameObject>();
  private currentDepth = 0;
  private isDragging = false;
  private lastPointerY = 0;

  constructor() { super('PondScene'); }

  preload() {
    // Chaque sprite est chargé comme spritesheet d'animation (8 frames de 128px).
    for (const fishDef of FISH_TYPES) {
      if (fishDef.sprite) {
        this.load.spritesheet(fishDef.type, `/anim${fishDef.sprite}`, {
          frameWidth: ANIM_FRAME_SIZE,
          frameHeight: ANIM_FRAME_SIZE,
        });
      }
    }
  }

  private registerAnims() {
    for (const fishDef of FISH_TYPES) {
      if (!fishDef.sprite) continue;
      const key = `${fishDef.type}-swim`;
      if (this.anims.exists(key) || !this.textures.exists(fishDef.type)) continue;
      this.anims.create({
        key,
        frames: this.anims.generateFrameNumbers(fishDef.type, { start: 0, end: ANIM_FRAMES - 1 }),
        frameRate: 5,
        repeat: -1,
      });
    }
  }

  create() {
    const width = this.scale.width;
    this.cameras.main.setBounds(0, 0, width, WORLD_HEIGHT);
    this.registerAnims();

    for (let d = 0; d <= MAX_DEPTH; d++) {
      const yStart = d * ZONE_HEIGHT;
      const { bg, water } = DEPTH_COLORS[d];

      this.add.rectangle(width / 2, yStart + ZONE_HEIGHT / 2, width, ZONE_HEIGHT, bg);

      if (d === 3) {
        // Océan des Profondeurs : bioluminescence et courants
        for (let i = 0; i < 25; i++) {
          const orb = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(4, 16), 0x00aacc, 0.4
          );
          this.tweens.add({ targets: orb, alpha: { from: 0.1, to: 0.6 }, x: `+=${Phaser.Math.Between(-60, 60)}`, duration: Phaser.Math.Between(2000, 5000), yoyo: true, repeat: -1, ease: 'Sine.easeInOut', delay: Phaser.Math.Between(0, 2000) });
        }
      } else if (d === 8) {
        // Nexus de Mana : mana dorée pulsante
        for (let i = 0; i < 30; i++) {
          const star = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(2, 8), 0xdaa520, 0.6
          );
          this.tweens.add({ targets: star, alpha: { from: 0.2, to: 0.8 }, scaleX: { from: 0.8, to: 1.2 }, scaleY: { from: 0.8, to: 1.2 }, duration: Phaser.Math.Between(1500, 3500), yoyo: true, repeat: -1, ease: 'Sine.easeInOut' });
        }
      } else if (d === 9) {
        // Cœur Volcanique : braises qui montent
        for (let i = 0; i < 20; i++) {
          const ember = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(200, ZONE_HEIGHT),
            Phaser.Math.Between(3, 10), 0xff4400, 0.7
          );
          this.tweens.add({ targets: ember, y: `-=${Phaser.Math.Between(100, 300)}`, alpha: { from: 0.7, to: 0 }, duration: Phaser.Math.Between(2000, 5000), repeat: -1, ease: 'Quad.easeIn', delay: Phaser.Math.Between(0, 3000) });
        }
      } else if (d === 10) {
        // Royaume Céleste : étoiles scintillantes
        for (let i = 0; i < 40; i++) {
          const glow = this.add.circle(
            Phaser.Math.Between(0, width),
            yStart + Phaser.Math.Between(0, ZONE_HEIGHT),
            Phaser.Math.Between(1, 6), 0xccaaff, 0.8
          );
          this.tweens.add({ targets: glow, alpha: { from: 0.1, to: 0.9 }, duration: Phaser.Math.Between(800, 2500), yoyo: true, repeat: -1, ease: 'Sine.easeInOut', delay: Phaser.Math.Between(0, 2000) });
        }
      } else if (d === 11) {
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

  // L'ondulation/respiration est jouée par l'animation spritesheet ; ici on
  // ajoute seulement un léger tangage pour finir de donner vie au mouvement.
  update(time: number) {
    const t = time / 1000;
    for (const obj of this.fishSprites.values()) {
      if (!(obj instanceof Phaser.GameObjects.Sprite)) continue;
      const tilt = obj.getData('tilt') as number | undefined;
      if (tilt === undefined) continue;
      const phase = obj.getData('phase') as number;
      const spd = obj.getData('spd') as number;
      obj.rotation = tilt * Math.sin(t * spd + phase);
    }
  }

  updateFishes(poissons: PoissonInstance[]) {
    const width = this.scale.width;

    poissons.forEach(fish => {
      if (!this.fishSprites.has(fish.id)) {
        const fishDepthZone = FISH_DEPTH[fish.type] ?? 0;
        const zoneY = fishDepthZone * ZONE_HEIGHT;
        const x = Phaser.Math.Between(80, width - 80);
        const y = zoneY + Phaser.Math.Between(140, ZONE_HEIGHT - 140);

        let gameObj: FishGameObject;

        if (SPRITE_FISH.has(fish.type) && this.textures.exists(fish.type)) {
          // Poisson animé (spritesheet)
          const size = 48 + Math.min(fish.level, 50);
          const spr = this.add.sprite(x, y, fish.type);
          spr.setDisplaySize(size, size);
          spr.setDepth(2);

          // ondulation/respiration en boucle, désynchronisée d'un poisson à l'autre
          const key = `${fish.type}-swim`;
          if (this.anims.exists(key)) {
            spr.play({ key, startFrame: Phaser.Math.Between(0, ANIM_FRAMES - 1) });
            spr.anims.timeScale = Phaser.Math.FloatBetween(0.55, 0.85);
          }

          // tangage léger via update()
          spr.setData('phase', Phaser.Math.FloatBetween(0, Math.PI * 2));
          spr.setData('spd', Phaser.Math.FloatBetween(0.8, 1.4));
          spr.setData('tilt', Phaser.Math.FloatBetween(0.03, 0.06));

          // Nage : déplacement sinusoïdal + flip horizontal selon la direction
          const dx = Phaser.Math.Between(-100, 100);
          spr.setFlipX(dx < 0);
          this.tweens.add({
            targets: spr,
            x: `+=${dx}`,
            y: `+=${Phaser.Math.Between(-45, 45)}`,
            duration: Phaser.Math.Between(6000, 12000),
            yoyo: true,
            repeat: -1,
            ease: 'Sine.easeInOut',
            onYoyo: () => spr.setFlipX(!spr.flipX),
            onRepeat: () => spr.setFlipX(!spr.flipX),
          });

          gameObj = spr;
        } else {
          // Poisson cercle (défaut, si pas de sprite)
          const color = FISH_COLORS[fish.type] ?? 0xffffff;
          const radius = 12 + Math.min(fish.level, 50);
          const circle = this.add.circle(x, y, radius, color, 0.85);
          circle.setDepth(2);
          this.tweens.add({
            targets: circle,
            x: `+=${Phaser.Math.Between(-140, 140)}`,
            y: `+=${Phaser.Math.Between(-80, 80)}`,
            duration: Phaser.Math.Between(6000, 12000),
            yoyo: true, repeat: -1, ease: 'Sine.easeInOut',
          });
          gameObj = circle;
        }

        this.fishSprites.set(fish.id, gameObj);
      } else {
        const obj = this.fishSprites.get(fish.id)!;
        if (obj instanceof Phaser.GameObjects.Arc) {
          obj.setRadius(12 + Math.min(fish.level, 50));
        } else if (obj instanceof Phaser.GameObjects.Sprite) {
          const size = 48 + Math.min(fish.level, 50);
          obj.setDisplaySize(size, size);
        }
      }
    });

    const currentIds = new Set(poissons.map(f => f.id));
    for (const [id, obj] of this.fishSprites.entries()) {
      if (!currentIds.has(id)) { obj.destroy(); this.fishSprites.delete(id); }
    }
  }
}
