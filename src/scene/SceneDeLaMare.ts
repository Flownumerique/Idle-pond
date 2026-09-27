/**
 * IdlePond — la scène : une coupe verticale, le héros, ses bancs.
 *
 * Elle ne connaît pas l'état du jeu. Elle reçoit une `VueDeScene` par
 * l'événement `vue` du jeu Phaser (émis par `ui/Scene.tsx`) et redessine ce
 * qui a changé. Tout est dessiné au trait — aucun sprite, spec [D10] : ce que
 * la DA remplacera, c'est ce fichier, jamais `vue.ts`.
 *
 * Ce qu'elle montre, et rien d'autre :
 *   - une bande par palier ouvert, colorée par son assise, la lumière qui baisse ;
 *   - dans chaque bande qui porte une espèce, un banc dont l'effectif dessiné
 *     croît avec le logarithme du niveau ;
 *   - le héros, dans la bande la plus basse, à l'échelle de son niveau, avec
 *     une marque par couche ;
 *   - l'eau qui se trouble quand la jauge dépasse l'alerte — un effet, pas un
 *     texte (GDD §2.4).
 */
import Phaser from 'phaser'
import type { VueDePalier, VueDeScene } from './vue'
import { MARQUE_PAR_ASSISE, paletteDe, type MarqueId } from './palette'

export const EVENEMENT_VUE = 'vue'

const HAUTEUR_DE_BANDE = 72
const MARGE = 12
const POISSONS_MAX_PAR_BANC = 14

/** Couleur d'un banc, par rang d'espèce : une teinte qui tourne, une clarté qui baisse. */
function couleurDuBanc(rang: number): number {
  const teinte = (rang * 47) % 360
  const couleur = Phaser.Display.Color.HSVToRGB(teinte / 360, 0.35, 0.85 - Math.min(0.5, rang * 0.02)) as {r: number, g: number, b: number}
  return Phaser.Display.Color.GetColor(couleur.r, couleur.g, couleur.b)
}

export class SceneDeLaMare extends Phaser.Scene {
  private fond!: Phaser.GameObjects.Graphics
  private bancs!: Phaser.GameObjects.Group
  private heros!: Phaser.GameObjects.Container
  private trouble!: Phaser.GameObjects.Rectangle
  private derniereVue: string | null = null

  constructor() {
    super('SceneDeLaMare')
  }

  create() {
    this.fond = this.add.graphics()
    this.bancs = this.add.group()
    this.heros = this.add.container(0, 0)
    this.trouble = this.add
      .rectangle(0, 0, this.scale.width, this.scale.height, 0x8a7a3a, 0)
      .setOrigin(0, 0)
    this.game.events.on(EVENEMENT_VUE, this.appliquer, this)
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => this.game.events.off(EVENEMENT_VUE, this.appliquer, this))
    this.scale.on(Phaser.Scale.Events.RESIZE, () => {
      this.trouble.setSize(this.scale.width, this.scale.height)
      this.derniereVue = null
    })
  }

  /** Redessine si la vue a changé. La vue est petite : la comparer en chaîne coûte moins qu'un redessin. */
  appliquer(vue: VueDeScene) {
    const clef = JSON.stringify(vue)
    if (clef === this.derniereVue) return
    this.derniereVue = clef

    const largeur = this.scale.width
    const hauteurTotale = vue.paliers.length * HAUTEUR_DE_BANDE
    this.cameras.main.setBounds(0, 0, largeur, Math.max(hauteurTotale, this.scale.height))
    this.cameras.main.scrollY = Math.max(0, hauteurTotale - this.scale.height)

    this.dessinerLesBandes(vue.paliers, largeur)
    this.dessinerLesBancs(vue.paliers, largeur)
    this.dessinerLeHeros(vue, largeur)

    this.tweens.add({ targets: this.trouble, fillAlpha: vue.eauTroublee ? 0.28 : 0, duration: 900 })
  }

  private dessinerLesBandes(paliers: readonly VueDePalier[], largeur: number) {
    this.fond.clear()
    paliers.forEach((palier, i) => {
      const palette = paletteDe(palier.assise)
      const y = i * HAUTEUR_DE_BANDE
      this.fond.fillStyle(palette.fond, 1)
      this.fond.fillRect(0, y, largeur, HAUTEUR_DE_BANDE)
      this.fond.fillStyle(palette.eau, 0.55 + 0.4 * palette.lumiere)
      this.fond.fillRect(MARGE, y + 2, largeur - 2 * MARGE, HAUTEUR_DE_BANDE - 4)
      // La lumière : un voile clair qui s'amincit en descendant.
      this.fond.fillStyle(0xdde9e6, 0.12 * palette.lumiere)
      this.fond.fillRect(MARGE, y + 2, largeur - 2 * MARGE, 10)
    })
  }

  private dessinerLesBancs(paliers: readonly VueDePalier[], largeur: number) {
    this.bancs.getChildren().forEach((p) => this.tweens.killTweensOf(p))
    this.bancs.clear(true, true)
    paliers.forEach((palier, i) => {
      if (palier.espece === null) return
      const effectif = Math.min(POISSONS_MAX_PAR_BANC, 1 + Math.floor(Math.log2(Math.max(1, palier.espece.niveau))))
      const couleur = couleurDuBanc(palier.espece.rang)
      const y0 = i * HAUTEUR_DE_BANDE + HAUTEUR_DE_BANDE / 2
      for (let k = 0; k < effectif; k += 1) {
        const x = MARGE + 30 + ((k * 53) % (largeur - 2 * MARGE - 120))
        const y = y0 + ((k * 17) % 28) - 14
        const poisson = this.add.ellipse(x, y, 14, 7, couleur, 0.9)
        this.bancs.add(poisson)
        this.tweens.add({
          targets: poisson,
          x: x + 18 + (k % 3) * 6,
          duration: 1800 + (k % 5) * 300,
          yoyo: true,
          repeat: -1,
          ease: 'Sine.easeInOut',
        })
      }
    })
  }

  private dessinerLeHeros(vue: VueDeScene, largeur: number) {
    this.tweens.killTweensOf(this.heros)
    this.heros.removeAll(true)
    const bandeDuBas = Math.max(0, vue.paliers.length - 1)
    const x = largeur - MARGE - 60
    const y = bandeDuBas * HAUTEUR_DE_BANDE + HAUTEUR_DE_BANDE / 2
    this.heros.setPosition(x, y)
    this.heros.setScale(vue.heros.echelle)

    const corps = this.add.graphics()
    corps.fillStyle(0xb9c7c2, 1)
    corps.fillEllipse(0, 0, 34, 16)
    corps.fillTriangle(-16, 0, -28, -8, -28, 8)
    corps.fillStyle(0x1b2422, 1)
    corps.fillCircle(9, -2, 1.6)
    this.heros.add(corps)

    for (const assise of vue.heros.couches) {
      this.heros.add(this.marque(MARQUE_PAR_ASSISE[assise]))
    }

    this.tweens.add({ targets: this.heros, y: y - 4, duration: 2200, yoyo: true, repeat: -1, ease: 'Sine.easeInOut' })
  }

  /** Une marque du corps — GDD §15.1. Des primitives, jusqu'à la DA. */
  private marque(id: MarqueId): Phaser.GameObjects.Graphics {
    const g = this.add.graphics()
    switch (id) {
      case 'branchies':
        g.lineStyle(1.2, 0x5d7a74, 1)
        for (let i = 0; i < 3; i += 1) g.lineBetween(2 + i * 2.5, -5, 2 + i * 2.5, 5)
        break
      case 'membranes':
        g.fillStyle(0x7fb3a8, 0.55)
        g.fillTriangle(-4, -7, 6, -12, 10, -6)
        g.fillTriangle(-4, 7, 6, 12, 10, 6)
        break
      case 'luminescence':
        g.fillStyle(0x9ef0e0, 0.5)
        g.fillCircle(-6, 0, 5)
        g.fillStyle(0xd6fff7, 0.9)
        g.fillCircle(-6, 0, 2)
        break
      case 'mineralisation':
        g.fillStyle(0x8c8f7a, 1)
        for (let i = 0; i < 5; i += 1) g.fillRect(-12 + i * 5, -8 + (i % 2) * 2, 2, 2)
        break
      case 'epaississement':
        g.lineStyle(2.5, 0x8a9a95, 0.9)
        g.strokeEllipse(0, 0, 36, 18)
        break
      case 'halo':
        g.lineStyle(1, 0xf1e4a8, 0.7)
        g.strokeCircle(0, 0, 24)
        break
    }
    return g
  }
}
