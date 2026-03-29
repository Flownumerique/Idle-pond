import Phaser from 'phaser';
import type { Fish } from '../../store/useGameStore';

export class BaseScene extends Phaser.Scene {
  constructor(key: string) {
    super({ key });
  }

  create() {
    // Shared base scene creation logic if needed
  }
}

export class PondScene extends BaseScene {
  private fishSprites: Map<string, Phaser.GameObjects.Arc>;

  constructor() {
    super('PondScene');
    this.fishSprites = new Map();
  }

  create() {
    super.create();

    // Draw the pond background (placeholder)
    const { width, height } = this.scale;

    // Create a simple gradient or colored rectangle for the pond
    this.add.rectangle(width / 2, height / 2, width, height, 0x0f2a4a);

    // Add some water-like elements (circles)
    for (let i = 0; i < 20; i++) {
      this.add.circle(
        Phaser.Math.Between(0, width),
        Phaser.Math.Between(0, height),
        Phaser.Math.Between(10, 50),
        0x1a457b,
        0.3
      );
    }

    // Listen for events from React
    this.game.events.on('update-fishes', this.updateFishes, this);

    // Let React know the scene is ready
    this.game.events.emit('scene-ready');
  }

  updateFishes(poissons: Fish[]) {
    const { width, height } = this.scale;

    // Add new fishes
    poissons.forEach(fish => {
      if (!this.fishSprites.has(fish.id)) {
        // Placeholder for fish: a colored circle depending on type
        const color = fish.type === 'gold' ? 0xffd700 :
                     fish.type === 'ruby' ? 0xe0115f :
                     0x00ffcc;

        const fishSprite = this.add.circle(
          Phaser.Math.Between(50, width - 50),
          Phaser.Math.Between(50, height - 50),
          15 + (fish.level * 2), // Size increases slightly with level
          color
        );

        // Add simple swimming animation
        this.tweens.add({
          targets: fishSprite,
          x: `+=${Phaser.Math.Between(-100, 100)}`,
          y: `+=${Phaser.Math.Between(-100, 100)}`,
          duration: Phaser.Math.Between(3000, 6000),
          yoyo: true,
          repeat: -1,
          ease: 'Sine.easeInOut'
        });

        this.fishSprites.set(fish.id, fishSprite);
      } else {
        // Update existing fish (e.g., if level increased)
        const sprite = this.fishSprites.get(fish.id);
        if (sprite) {
          sprite.setRadius(15 + (fish.level * 2));
        }
      }
    });

    // Remove fishes that are no longer in the state (if any)
    const currentFishIds = new Set(poissons.map(f => f.id));
    for (const [id, sprite] of this.fishSprites.entries()) {
      if (!currentFishIds.has(id)) {
        sprite.destroy();
        this.fishSprites.delete(id);
      }
    }
  }
}
