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
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { etatInitial, tick } from '../src/noyau/noyau'
import { multiplicateurDeSeuil, productionTotaleParSeconde } from '../src/noyau/economie'
import { PALIERS } from '../src/donnees/paliers'
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

  it('un intervalle qui FRANCHIT des seuils compose encore', () => {
    // Le cas qui a cassé à l'amendement v1.1 §2.C, et le seul qui prouve la
    // partition analytique : sans elle, les petits pas franchissent 10, 25 et
    // 50 tôt et produisent nettement plus que le grand pas.
    const depart = etatInitial(1)
    const banc = PALIERS[0].bancs[0]
    const vide: EtatJeu = {
      ...depart,
      cycle: {
        ...depart.cycle,
        manaCourant: new Decimal(0),
        bancs: { [banc.id]: { place: 120, effectif: 0 } },
      },
      permanent: { ...depart.permanent, contenanceMana: new Decimal('1e30') },
    }

    let parPetitsPas = vide
    for (let i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = tick(parPetitsPas, PAS)
    const enUnPas = tick(vide, HUIT_HEURES)

    // Les seuils ont bien été traversés : sinon le test ne prouve rien.
    expect(parPetitsPas.cycle.bancs[banc.id].effectif).toBeGreaterThan(100)
    expect(multiplicateurDeSeuil(vide.cycle.bancs[banc.id].effectif)).toBe(1)
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })

  it('un drapeau permanent franchi en cours d’intervalle compose aussi', () => {
    // Le drapeau des cent individus est GLOBAL : il ne s'intègre pas banc par
    // banc, il coupe le pas. Sans la coupure, les petits pas gagnent +3 % sur
    // presque tout l'intervalle et le grand pas sur rien.
    const depart = etatInitial(1)
    const banc = PALIERS[0].bancs[0]
    const proche: EtatJeu = {
      ...depart,
      cycle: {
        ...depart.cycle,
        manaCourant: new Decimal(0),
        bancs: { [banc.id]: { place: 300, effectif: 90 } },
      },
      permanent: { ...depart.permanent, contenanceMana: new Decimal('1e30') },
    }

    let parPetitsPas = proche
    for (let i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = tick(parPetitsPas, PAS)
    const enUnPas = tick(proche, HUIT_HEURES)

    expect(enUnPas.permanent.especesAyantAtteintCent).toContain(banc.espece)
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })
})
