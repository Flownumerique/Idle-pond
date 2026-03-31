import { Scene, GameObjects, Math as PhaserMath } from 'phaser';
import { EventBus } from '../EventBus';
import { useGameStore } from '../../store/useGameStore';

export class FishSprite extends GameObjects.Arc {
  private speed: number;
  private targetX: number;
  private targetY: number;
  private changeDirectionTimer: number;

  constructor(scene: Scene, x: number, y: number) {
    // Rayon aléatoire entre 10 et 16 pour avoir des tailles de 20x20 à 32x32
    const radius = PhaserMath.Between(10, 16);
    // Couleur aléatoire
    const color = PhaserMath.Between(0x000000, 0xffffff);

    super(scene, x, y, radius, 0, 360, false, color, 1);

    scene.add.existing(this);

    // Vitesse lente et fluide (50 - 100 px/s)
    this.speed = PhaserMath.Between(50, 100);
    this.targetX = x;
    this.targetY = y;
    this.changeDirectionTimer = 0;

    this.setNewTarget();
  }

  setNewTarget() {
    const { width, height } = this.scene.scale;
    // Marge de sécurité pour ne pas aller tout au bord (rebonds)
    const margin = 20;

    this.targetX = PhaserMath.Between(margin, width - margin);
    this.targetY = PhaserMath.Between(margin, height - margin);

    // Prochain changement de direction dans 2 à 5 secondes (2000 - 5000 ms)
    this.changeDirectionTimer = this.scene.time.now + PhaserMath.Between(2000, 5000);

    // Flip horizontal si on va vers la gauche
    if (this.targetX < this.x) {
      this.setFlipX(true);
    } else {
      this.setFlipX(false);
    }
  }

  preUpdate(time: number, delta: number) {
    // Si on a atteint le temps de changer de direction
    if (time > this.changeDirectionTimer) {
      this.setNewTarget();
    }

    // Calcul de la distance au point cible
    const dist = PhaserMath.Distance.Between(this.x, this.y, this.targetX, this.targetY);

    if (dist > 5) {
      // Déplacement progressif vers la cible (interpolation linéaire)
      // On convertit la vitesse en déplacement par rapport au delta (ms)
      const moveDistance = (this.speed * delta) / 1000;
      const t = moveDistance / dist;

      this.x = PhaserMath.Linear(this.x, this.targetX, t);
      this.y = PhaserMath.Linear(this.y, this.targetY, t);
    } else {
      // On est très proche du point cible, on en cherche un nouveau sans attendre
      this.setNewTarget();
    }
  }
}

export class PondScene extends Scene {
  private fishPool!: GameObjects.Group;

  constructor() {
    super({ key: 'PondScene' });
  }

  create() {
    // Initialisation du groupe pour l'Object Pooling
    this.fishPool = this.add.group({
      classType: FishSprite,
      runChildUpdate: true // Exécute preUpdate sur chaque FishSprite
    });

    // Lecture du store Zustand pour instancier les poissons déjà possédés
    const state = useGameStore.getState();
    const totalFishes = state.ownedFishes.reduce((acc, fish) => acc + fish.quantite, 0);

    for (let i = 0; i < totalFishes; i++) {
      this.spawnFish();
    }

    // Écoute de l'EventBus pour les nouveaux achats
    EventBus.on('SPAWN_FISH', () => {
      this.spawnFish();
    });

    // Écoute pour nettoyer l'événement quand la scène est détruite
    this.events.on('shutdown', () => {
      EventBus.off('SPAWN_FISH');
    });
  }

  spawnFish() {
    const { width, height } = this.scale;
    const x = PhaserMath.Between(20, width - 20);
    const y = PhaserMath.Between(20, height - 20);

    // Utilisation du pool au lieu de créer de zéro à chaque fois
    const fish = this.fishPool.get(x, y) as FishSprite;

    if (fish) {
      // Si l'objet existait dans le pool mais était désactivé, on le réactive
      fish.setActive(true);
      fish.setVisible(true);
      // Réinitialiser la position en cas de réutilisation
      fish.setPosition(x, y);
      fish.setNewTarget();
    }
  }
}
