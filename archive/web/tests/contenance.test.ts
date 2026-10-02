/**
 * La contenance et l'acquis de séjour — amendement v1.1 §2.B.
 *
 * « Un cycle nominal multiplie la contenance par ≈47,1 (tolérance 2 %). »
 *
 * Ce facteur n'est écrit nulle part dans le code de l'éclosion, et c'est tout
 * l'intérêt du test : il doit ÉMERGER de `A∞` et `τ₀`. Tier 0 §8 — le plafond
 * ne monte que par séjour prolongé en mana dense ; une contenance indexée sur
 * le compteur d'éclosions violerait l'invariant, et passerait ce test tout en
 * étant fausse. C'est pourquoi le test regarde aussi la forme de la montée,
 * pas seulement son résultat.
 */
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import {
  ACQUIS_MAX,
  CONTENANCE_PAR_ECLOSION,
  DUREE_DU_CYCLE_1_HEURES,
  REGLAGE_CANONIQUE,
  TAU_SEJOUR_HEURES,
} from '../src/noyau/constantes'
import { contenance, eclore, etatInitial, tauDuSejourSecondes, tick } from '../src/noyau/noyau'
import { multiplicateurDensite } from '../src/noyau/densite'
import { etatDeTravail } from './etat-de-travail'

const H = 3600

describe('contenance', () => {
  it('un cycle nominal la multiplie par ≈47,1', () => {
    const depart = etatInitial(1)
    const apresSejour = tick(depart, DUREE_DU_CYCLE_1_HEURES * H)
    const apres = eclore(apresSejour)
    const rapport = apres.permanent.contenanceMana.div(depart.permanent.contenanceMana).toNumber()
    expect(rapport).toBeCloseTo(CONTENANCE_PAR_ECLOSION, 0)
    expect(Math.abs(rapport / CONTENANCE_PAR_ECLOSION - 1)).toBeLessThan(0.02)
  })

  it('la cible dérivée vaut bien g^4.4', () => {
    expect(CONTENANCE_PAR_ECLOSION).toBeCloseTo(47.1, 1)
  })

  it('l’acquis sature vers A∞, et son t₉₀ tombe à ≈2 h', () => {
    const depart = etatInitial(1)
    const a90 = tick(depart, 2 * H).cycle.acquisDeSejour
    expect(a90 / ACQUIS_MAX).toBeCloseTo(0.9, 2)
    const aLInfini = tick(depart, 200 * H).cycle.acquisDeSejour
    expect(aLInfini).toBeCloseTo(ACQUIS_MAX, 3)
  })

  it('rester au-delà de la saturation ne rapporte plus de profondeur', () => {
    // L'effet secondaire recherché du §2.B, et il ne doit pas se casser : passé
    // la saturation, rester ne rapporte plus que de la Foi. C'est ce qui rend
    // réelle la seule vraie décision du joueur.
    //
    // Le blocage est doux (noyau v1.0 §2.2) : rien ne borne plus la comparaison,
    // le joueur peut rester indéfiniment sans qu'aucune éclosion ne se
    // déclenche à sa place. `longSejour` n'a donc qu'à être largement plus long
    // qu'un cycle nominal — sa valeur exacte n'a plus de portée canonique.
    const longSejour = 200 * H
    expect(longSejour / H).toBeGreaterThan(10 * DUREE_DU_CYCLE_1_HEURES)

    const depart = etatInitial(1)
    const nominal = eclore(tick(depart, DUREE_DU_CYCLE_1_HEURES * H)).permanent.contenanceMana
    const bienPlusLong = eclore(tick(depart, longSejour)).permanent.contenanceMana

    expect(tick(depart, longSejour).permanent.nombreEclosions, 'aucune éclosion ne se déclenche seule').toBe(0)
    expect(bienPlusLong.div(nominal).toNumber()).toBeLessThan(1.04)
  })

  it('l’acquis se dépense entièrement à l’éclosion', () => {
    const apres = eclore(tick(etatInitial(1), 3 * H))
    expect(apres.cycle.acquisDeSejour).toBe(0)
  })

  it('le multiplicateur de densité vaut 1 en eau neutre, jamais moins, et croît', () => {
    // Le point `multiplicateurDensite(1) === 1` verrouillait le PLANCHER de
    // l'ancienne forme (`Math.max(1, densité)^e`), remplacée par celle des
    // contraintes globales, `(1 + densité/d₀)^e` — les deux coïncident à
    // densité 0, pas à densité 1. Ce que ce test protège n'est pas ce point-là :
    // c'est `≥ 1` partout, et croissant. Le multiplicateur ne raccourcit plus
    // le séjour (voir les deux tests suivants) ; il multiplie la production,
    // et une eau plus dense ne doit jamais la faire baisser.
    let precedent = multiplicateurDensite(0)
    expect(precedent).toBe(1)
    for (const densite of [0.5, 1, 2, 5, 10, 50]) {
      const valeur = multiplicateurDensite(densite)
      expect(valeur).toBeGreaterThanOrEqual(1)
      expect(valeur).toBeGreaterThanOrEqual(precedent)
      precedent = valeur
    }
    expect(multiplicateurDensite(10)).toBeGreaterThan(1)
  })

  // Ces deux tests remplacent l'affirmation « une eau dense sature plus vite ».
  // Elle verrouillait une dégénérescence : la densité vaut `pointe^α` et croît
  // sans borne, donc un temps caractéristique divisé par elle s'effondre — t₉₀
  // de 2 h à densité nulle, 0,082 h à densité 10, quasi nul à 10⁶. L'acquis
  // saturait alors en quelques minutes au deuxième cycle, en une fraction de
  // seconde à partir du troisième, et
  // la contenance ne lisait plus que `A∞` : un forfait plat, sous le nom de
  // séjour. Le temps du séjour est désormais `τ₀`, constant.
  it('le temps du séjour ne dépend plus de la densité', () => {
    const depart = etatInitial(1)
    const dense = {
      ...depart,
      permanent: { ...depart.permanent, densites: depart.permanent.densites.map(() => 1e6) },
    }
    const neutre = tick(depart, H).cycle.acquisDeSejour
    expect(tick(dense, H).cycle.acquisDeSejour).toBeCloseTo(neutre, 9)
  })

  it('en eau dense, une heure de séjour ne sature toujours pas l’acquis', () => {
    // Densité 10 : du même ordre que celle que laisse le premier cycle (7,3,
    // mesuré) ; le deuxième en laisse déjà 7 100.
    // Après une heure, l'acquis vaut `1 − e^(−1 h / τ₀)` ≈ 0,683 de `A∞`, et non
    // ≈ 1 : la loi de contenance lit encore la durée du séjour.
    const depart = etatInitial(1)
    const dense = {
      ...depart,
      permanent: { ...depart.permanent, densites: depart.permanent.densites.map(() => 10) },
    }
    const rapport = tick(dense, H).cycle.acquisDeSejour / ACQUIS_MAX
    expect(rapport).toBeCloseTo(1 - Math.exp(-1 / TAU_SEJOUR_HEURES), 6)
    expect(rapport).toBeLessThan(0.7)
  })

  it('la contenance monte PENDANT le cycle, et l’éclosion ne fait que la fixer', () => {
    // Amendement v1.3. Le plafond ne montait qu'à l'éclosion, donc il était
    // GELÉ pendant toute la vie : le palier suivant coûtait plus que ce que le
    // héros pouvait porter, et rien ne pouvait plus changer cela avant la vie
    // d'après. Mesuré sur le cycle 15 : le dernier palier s'ouvrait à la
    // première minute, et les 99,8 % restants ne voyaient ni palier, ni espèce,
    // ni même de production (×1,1 en 8,9 h). Ce n'est pas un jeu incrémental,
    // c'est un minuteur.
    //
    // Le plafond monte maintenant avec l'acquis, donc en continu. Tier 0 §8
    // tient toujours — il ne monte QUE par séjour prolongé —, il monte
    // simplement au fil du séjour au lieu d'être versé en bloc à la sortie.
    const depart = etatInitial(1)
    expect(contenance(depart).eq(depart.permanent.contenanceMana)).toBe(true)

    const apres = tick(depart, H)
    expect(contenance(apres).gt(contenance(depart))).toBe(true)
    expect(contenance(apres).eq(apres.permanent.contenanceMana.mul(1 + apres.cycle.acquisDeSejour))).toBe(true)
    // Le plafond BANQUÉ, lui, n'a pas bougé : l'acquis n'est pas encore dépensé.
    expect(apres.permanent.contenanceMana.eq(depart.permanent.contenanceMana)).toBe(true)

    // L'éclosion ne crée rien : elle fixe ce que le cycle portait déjà.
    const eclos = eclore(apres)
    expect(eclos.permanent.contenanceMana.eq(contenance(apres))).toBe(true)
    expect(contenance(eclos).eq(eclos.permanent.contenanceMana)).toBe(true)
  })

  it('le temps du séjour croît avec la profondeur ATTEINTE (amendement v1.2)', () => {
    // La tâche 12 a mesuré ce que `τ` constant produit : les 45 cycles durent
    // exactement 2,617 h — `τ₀ ln 20` — et `dernier / premier` vaut 1,000 quel
    // que soit le réglage d'économie. La durée d'un cycle est plafonnée par le
    // séjour, pas par l'économie ; aucun bouton d'économie n'a donc prise sur
    // la forme de la courbe. C'est `τ` qui devient ce bouton.
    //
    // Ce n'est PAS le retour de la loi que R39 a révoquée : celle-là DIVISAIT
    // `τ` par la densité, une grandeur sans borne, et `t₉₀` s'effondrait de 2 h
    // à 0,05 s en trois cycles. Ici `τ` croît, et il croît par PALIER — une
    // quantité entière et bornée par les 62 paliers du monde.
    const depart = etatInitial(1)
    expect(depart.reglage).toEqual(REGLAGE_CANONIQUE)
    expect(tauDuSejourSecondes(depart)).toBeCloseTo(TAU_SEJOUR_HEURES * H, 9)

    const profond = (p: number, croissance: number): EtatJeu => ({
      ...depart,
      reglage: { croissanceDuSejourParPalier: croissance },
      permanent: { ...depart.permanent, profondeurMaxAtteinte: p },
    })
    // La profondeur zéro vaut toujours `τ₀` : la loi ne déplace pas son origine.
    expect(tauDuSejourSecondes(profond(0, 1.05))).toBeCloseTo(TAU_SEJOUR_HEURES * H, 9)
    expect(tauDuSejourSecondes(profond(10, 1.05))).toBeCloseTo(TAU_SEJOUR_HEURES * H * Math.pow(1.05, 10), 9)
    expect(tauDuSejourSecondes(profond(10, 1.05))).toBeGreaterThan(tauDuSejourSecondes(profond(9, 1.05)))
    // À croissance 1, la loi d'avant, à l'identique.
    expect(tauDuSejourSecondes(profond(20, 1))).toBeCloseTo(TAU_SEJOUR_HEURES * H, 9)
  })

  it('en profondeur, la même heure de séjour rapporte moins d’acquis', () => {
    // L'effet, et non la formule : c'est par là que les cycles s'allongent.
    const depart = etatInitial(1)
    const profond = (p: number): EtatJeu => ({
      ...depart,
      reglage: { croissanceDuSejourParPalier: 1.05 },
      permanent: { ...depart.permanent, profondeurMaxAtteinte: p },
    })
    const surface = tick(profond(0), H).cycle.acquisDeSejour
    const fond = tick(profond(20), H).cycle.acquisDeSejour
    expect(fond).toBeLessThan(surface)
    // Et l'acquis sature toujours vers le MÊME plafond : `τ` change le temps,
    // jamais la valeur. Cent heures au fond y arrivent encore.
    expect(tick(profond(20), 100 * H).cycle.acquisDeSejour / ACQUIS_MAX).toBeGreaterThan(0.95)
  })

  it('τ₀ est bien le temps caractéristique à densité neutre', () => {
    const apres = tick(etatInitial(1), TAU_SEJOUR_HEURES * H)
    expect(apres.cycle.acquisDeSejour / ACQUIS_MAX).toBeCloseTo(1 - Math.exp(-1), 6)
  })
})

describe('le blocage est doux (noyau v1.0 §2.2)', () => {
  it('une jauge pleine pendant une semaine ne déclenche aucune éclosion', () => {
    let etat = etatDeTravail()
    const eclosionsAvant = etat.permanent.nombreEclosions
    // 7 jours en un seul pas, jauge saturée du début à la fin
    const pleine = contenance(etat)
    etat = tick({ ...etat, cycle: { ...etat.cycle, manaCourant: pleine } }, 7 * 24 * 3600)
    expect(etat.permanent.nombreEclosions).toBe(eclosionsAvant)
    // « Pleine » est devenue une cible MOBILE : le plafond monte avec l'acquis
    // pendant que le joueur est absent, et peut s'éloigner plus vite que la
    // production ne remplit. Ce qui doit tenir n'a pas changé : la jauge ne
    // déborde jamais, elle ne redescend jamais, et rien n'éclôt à sa place.
    expect(etat.cycle.manaCourant.lte(contenance(etat))).toBe(true)
    expect(etat.cycle.manaCourant.gte(pleine)).toBe(true)
  })
})
