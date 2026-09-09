/**
 * Test d'équivalence de pas (§12, jalon v0.1).
 *
 * « 480 appels à dt = 60 s et 1 appel à dt = 8 h donnent le même état, à la
 * tolérance flottante près. C'est le test qui garantit le hors ligne et le
 * simulateur d'un seul coup. »
 *
 * Il est ici l'expression exécutable du filtre du §5.2 : toute mécanique du
 * cœur doit se calculer en un seul pas pour dt = 8 heures. Une mécanique qui
 * suivrait un individu, itérerait sur une file d'événements ou vérifierait à
 * chaque tick une contrainte qui change une fois par heure ferait tomber ce
 * test — c'est précisément à ça qu'il sert.
 *
 * Il est devenu facile à tenir le 2026-09-09, et c'est le bénéfice principal du
 * modèle à niveau : la production ne dépend plus que de quantités qui ne
 * changent qu'à l'achat, donc le pas est homogène par construction. Le noyau
 * n'a plus de coupure de pas du tout.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { etatInitial, tick } from '../src/noyau/noyau'
import { multiplicateurDeSeuil, productionTotaleParSeconde } from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

const HUIT_HEURES = 8 * 3600
const PAS = 60
const NOMBRE_DE_PAS = HUIT_HEURES / PAS

describe('équivalence de pas', () => {
  it('480 pas de 60 s valent un pas de 8 h', () => {
    const depart = etatDeTravail()
    let parPetitsPas = depart
    for (let i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = tick(parPetitsPas, PAS)
    const enUnPas = tick(depart, HUIT_HEURES)
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })

  it('la cadence de jeu à 100 ms vaut elle aussi un seul pas', () => {
    const depart = etatDeTravail()
    const duree = 600
    let parTicks = depart
    for (let i = 0; i < duree * 10; i += 1) parTicks = tick(parTicks, 0.1)
    comparerAToleranceFlottante(parTicks, tick(depart, duree))
  })

  it('le plafonnement du stock par la contenance compose lui aussi', () => {
    // Contenance basse : le mana sature en cours d'intervalle. C'est le cas où
    // une contenance dérivée de la production courante ferait diverger les deux
    // chemins ; elle est un état permanent, donc constante pendant le pas.
    const depart = etatDeTravail(999, '1e5')
    let parPetitsPas = depart
    for (let i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = tick(parPetitsPas, PAS)
    const enUnPas = tick(depart, HUIT_HEURES)
    expect(enUnPas.cycle.manaCourant.eq(enUnPas.permanent.contenanceMana)).toBe(true)
    expect(enUnPas.permanent.manaAmbiant.gt(0)).toBe(true)
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })

  it('le hors ligne à 6 h se crédite en un seul appel', () => {
    const depart = etatDeTravail()
    const parPetitsPas = Array.from({ length: 6 * 60 }).reduce<typeof depart>(
      (etat) => tick(etat, 60),
      depart,
    )
    comparerAToleranceFlottante(parPetitsPas, tick(depart, 6 * 3600))
  })

  it("l'état de départ produit bien quelque chose, sinon le test ne prouve rien", () => {
    expect(productionTotaleParSeconde(etatDeTravail()).gt(0)).toBe(true)
  })

  it('plus aucun seuil ne peut tomber À L’INTÉRIEUR d’un pas', () => {
    // Le cas qui a cassé à l'amendement v1.1 §2.C — les petits pas
    // franchissaient 10, 25 et 50 tôt et produisaient plus que le grand pas —
    // n'existe plus : le multiplicateur de seuil lit un NIVEAU, et un niveau ne
    // change qu'à l'achat. Ce qui se vérifie ici est donc l'inverse de ce qui
    // se vérifiait avant : le pas est homogène, et il le reste.
    const depart = etatInitial(1)
    const espece = ESPECES[0]
    const proche: EtatJeu = {
      ...depart,
      cycle: {
        ...depart.cycle,
        manaCourant: new Decimal(0),
        especes: { [espece.id]: { debloquee: true, niveau: 99 } },
      },
      permanent: { ...depart.permanent, contenanceMana: new Decimal('1e30') },
    }

    let parPetitsPas = proche
    for (let i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = tick(parPetitsPas, PAS)
    const enUnPas = tick(proche, HUIT_HEURES)

    // Ni le seuil de cent, ni le drapeau qu'il pose, ne sont tombés tout seuls.
    expect(multiplicateurDeSeuil(enUnPas.cycle.especes[espece.id].niveau)).toBe(8)
    expect(enUnPas.permanent.especesAyantAtteintCent).toEqual([])
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })
})
