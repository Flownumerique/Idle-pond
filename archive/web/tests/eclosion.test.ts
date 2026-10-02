/**
 * L'éclosion — noyau v1.0 §3 et §6.5.
 *
 * Reset complet (f = 1) : ce qui appartient au cycle repart de l'œuf, et
 * quatre choses traversent — contenance, densité, Souffle, technique —, plus le
 * drapeau des cent, l'unique exception au « tout se reperd ».
 *
 * Chaque assertion porte sur un état de départ où la grandeur visée n'est PAS
 * déjà à sa valeur d'arrivée : un reset affirmé sur un champ déjà vide, ou une
 * conservation affirmée sur un champ vide, ne prouve rien.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { etatDeTravail } from './etat-de-travail'
import { eclore, gainDeSoufflePrevu } from '../src/noyau/eclosion'
import {
  ACQUIS_MAX,
  ALPHA_GAIN_DE_DENSITE,
  MANA_A_LA_SORTIE_DE_L_OEUF,
  PALIERS_OUVERTS_AU_DEPART,
  PRODUCTION_DE_REFERENCE,
} from '../src/noyau/constantes'

function avecCycle(etat: EtatJeu, cycle: Partial<EtatJeu['cycle']>): EtatJeu {
  return { ...etat, cycle: { ...etat.cycle, ...cycle } }
}

function avecPermanent(etat: EtatJeu, permanent: Partial<EtatJeu['permanent']>): EtatJeu {
  return { ...etat, permanent: { ...etat.permanent, ...permanent } }
}

describe('§3.1 — l’éclosion remet le cycle à l’œuf', () => {
  it('mana, paliers, espèces, pointe, durée et acquis repartent de l’œuf', () => {
    const avant = avecCycle(etatDeTravail(), {
      productionPicParSeconde: new Decimal(500),
      dureeSecondes: 7200,
      acquisDeSejour: 30,
      niveauDuHeros: 4,
    })
    // Rien de tout cela n'est déjà à sa valeur de départ.
    expect(avant.cycle.manaCourant.eq(MANA_A_LA_SORTIE_DE_L_OEUF)).toBe(false)
    expect(avant.cycle.paliersOuverts).toBeGreaterThan(PALIERS_OUVERTS_AU_DEPART)
    expect(Object.keys(avant.cycle.especes).length).toBeGreaterThan(0)

    const apres = eclore(avant).cycle
    // Le mana repart CHARGÉ, pas de zéro : c'est la charge de l'œuf, qui tient
    // le plancher de cadence du §8.4 et n'est pas interchangeable avec le
    // débit du héros (voir `MANA_A_LA_SORTIE_DE_L_OEUF`).
    expect(apres.manaCourant.eq(MANA_A_LA_SORTIE_DE_L_OEUF)).toBe(true)
    expect(apres.paliersOuverts).toBe(PALIERS_OUVERTS_AU_DEPART)
    expect(apres.especes).toEqual({})
    expect(apres.productionPicParSeconde.eq(0)).toBe(true)
    expect(apres.dureeSecondes).toBe(0)
    expect(apres.acquisDeSejour).toBe(0)
    expect(apres.niveauDuHeros).toBe(1)
  })
})

describe('§3.1 — ce qui traverse l’éclosion', () => {
  it('sans aucun séjour, la contenance traverse telle quelle — et ne monte pas', () => {
    // Tier 0 §8 : le plafond ne monte QUE par séjour. Une éclosion sans séjour
    // ne doit donc rien lui ajouter — un forfait par éclosion échouerait ici —,
    // et ne doit pas non plus la remettre à sa valeur initiale.
    const avant = avecCycle(etatDeTravail(), { acquisDeSejour: 0 })
    expect(avant.permanent.contenanceMana.eq(new Decimal('1e14'))).toBe(true)
    expect(eclore(avant).permanent.contenanceMana.eq(avant.permanent.contenanceMana)).toBe(true)
  })

  it('le Souffle traverse, augmenté du gain que la pointe du cycle a mérité', () => {
    const avant = avecPermanent(avecCycle(etatDeTravail(), { productionPicParSeconde: new Decimal(1e6) }), {
      souffle: new Decimal(7),
    })
    const gain = gainDeSoufflePrevu(avant)
    expect(gain.gt(0)).toBe(true)
    expect(eclore(avant).permanent.souffle.eq(avant.permanent.souffle.add(gain))).toBe(true)
  })

  it('les nœuds de technique traversent', () => {
    const avant = avecPermanent(etatDeTravail(), { noeudsTechnique: ['noeud-temoin-a', 'noeud-temoin-b'] })
    expect(eclore(avant).permanent.noeudsTechnique).toEqual(['noeud-temoin-a', 'noeud-temoin-b'])
  })

  it('les compteurs de technique traversent, et celui de l’éclosion monte d’un', () => {
    const avant = etatDeTravail()
    const compteursAvant = avant.permanent.compteursTechnique
    // La fixture a creusé, débloqué et amélioré : les compteurs ne sont pas nuls.
    expect(compteursAvant.creusement).toBeGreaterThan(0)
    expect(compteursAvant.recrutement).toBeGreaterThan(0)
    expect(compteursAvant.amelioration).toBeGreaterThan(0)

    const compteursApres = eclore(avant).permanent.compteursTechnique
    expect(compteursApres).toEqual({ ...compteursAvant, eclosion: compteursAvant.eclosion + 1 })
  })

  it('le drapeau des cent survit — c’est l’unique exception (§2.1)', () => {
    const avant = avecPermanent(etatDeTravail(), { especesAyantAtteintCent: ['vairon'] })
    expect(eclore(avant).permanent.especesAyantAtteintCent).toEqual(['vairon'])
  })

  it('les bénédictions traversent', () => {
    const avant = avecPermanent(etatDeTravail(), { benedictions: { 'benediction-globale': 2, 'benediction-vairon': 1 } })
    expect(eclore(avant).permanent.benedictions).toEqual(avant.permanent.benedictions)
  })

  it('chaque assise traversée dans cette vie laisse une couche, dans l’ordre des assises, une seule fois', () => {
    // GDD §15.1 : « une marque par assise fixée ». `couches` était déclaré et
    // jamais écrit (audit du 2026-09-08). Spec 2026-09-17 [D11].
    const dansLaNoue = avecCycle(etatDeTravail(), { paliersOuverts: 4 })
    expect(dansLaNoue.permanent.couches).toEqual([])
    const uneFois = eclore(dansLaNoue)
    expect(uneFois.permanent.couches).toEqual(['noue'])

    // Deux assises ouvertes, la première déjà marquée : une seule couche neuve.
    const plusBas = avecCycle(avecPermanent(uneFois, { couches: ['noue'] }), { paliersOuverts: 8 })
    expect(eclore(plusBas).permanent.couches).toEqual(['noue', 'assise-2'])

    // L'ordre est celui des assises, pas celui de l'obtention.
    const desordre = avecCycle(avecPermanent(etatDeTravail(), { couches: ['assise-2'] }), { paliersOuverts: 2 })
    expect(eclore(desordre).permanent.couches).toEqual(['noue', 'assise-2'])
  })
})

describe('§6.5 — la densité se pose par max, et son gain vaut pointe^α', () => {
  it('chaque palier ouvert porte max(ancienne, pointe^α) ; les paliers fermés ne bougent pas', () => {
    const base = etatDeTravail()
    const ouverts = base.cycle.paliersOuverts
    expect(ouverts).toBeGreaterThan(2)
    const pointe = new Decimal(1e6)
    const laissee = Math.pow(pointe.toNumber() / PRODUCTION_DE_REFERENCE, ALPHA_GAIN_DE_DENSITE)
    // Un palier ouvert DÉJÀ plus dense que ce que le cycle laisse, et un palier
    // fermé chargé : le premier doit garder sa valeur (max, pas affectation),
    // le second ne doit pas être touché (paliers ouverts seulement).
    const densites = base.permanent.densites.map((d, palier) => {
      if (palier === 1) return laissee * 10
      if (palier === ouverts + 2) return laissee * 3
      return d
    })
    const avant = avecPermanent(avecCycle(base, { productionPicParSeconde: pointe }), { densites })

    const apres = eclore(avant).permanent.densites
    expect(apres.length).toBe(densites.length)
    apres.forEach((densite, palier) => {
      if (palier < ouverts) {
        expect(densite, `palier ouvert ${palier}`).toBeCloseTo(Math.max(densites[palier], laissee), 6)
      } else {
        expect(densite, `palier fermé ${palier}`).toBe(densites[palier])
      }
    })
    // Sur un palier vierge (densité 0), la valeur posée est `pointe^α`
    // exactement : la loi du gain, et une conservation neutre. Ce point ne
    // distingue PAS `max` d'une addition (0 + x = max(0, x)) — c'est le
    // palier 1, déjà plus dense que la pointe, qui le fait, dans la boucle
    // ci-dessus.
    expect(apres[0]).toBeCloseTo(laissee, 6)
  })

  it('une pointe plus faible que la densité acquise ne fait rien baisser', () => {
    const base = etatDeTravail()
    const densites = base.permanent.densites.map(() => 100)
    // pointe 2 : laisse 2^0,6 ≈ 1,5, bien sous les 100 acquis.
    const avant = avecPermanent(avecCycle(base, { productionPicParSeconde: new Decimal(2) }), { densites })
    expect(eclore(avant).permanent.densites).toEqual(densites)
  })
})

describe('§2.B — la contenance monte par le séjour', () => {
  // La cible `g^PALIERS_PAR_CYCLE_VISE` d'un cycle NOMINAL est affirmée par
  // `contenance.test.ts` (« un cycle nominal la multiplie par ≈47,1 »). Ici, ce
  // qui compte est que l'éclosion LISE l'acquis : une loi qui l'ignorerait —
  // un forfait par éclosion — ferait tomber ce test.
  it('un cycle écourté fixe moins de contenance qu’un cycle plein', () => {
    const avant = etatDeTravail()
    const court = avecCycle(avant, { acquisDeSejour: ACQUIS_MAX / 4 })
    const plein = avecCycle(avant, { acquisDeSejour: ACQUIS_MAX })
    expect(eclore(court).permanent.contenanceMana.lt(eclore(plein).permanent.contenanceMana)).toBe(true)
  })
})
