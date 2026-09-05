/**
 * IdlePond — éclosion.
 *
 * Le héros ENTRE dans l'œuf. Jamais « ponte », jamais « prestige », jamais
 * « rebirth » — ni ici, ni dans un identifiant, ni à l'écran (§3).
 *
 * §6.5, et rien de plus :
 *   f = 1 — reset complet du peuplement et de la géométrie, aucune fraction
 *           conservée.
 *   Conservé : acclimatation, densité, arbre de technique, bénédictions,
 *              succès, couches, contenance.
 *   Perdu    : population, paliers ouverts, mana courant.
 *   Le mana expire vers l'ambiant — il n'est pas détruit (Tier 0 §5).
 */
import Decimal from 'break_infinity.js'
import type { EtatCycle, EtatJeu } from './types'
import {
  CONTENANCE_EN_SECONDES_DE_PRODUCTION_DE_PIC,
  F_TARIF_REDESCENTE,
  FOI_BASE,
  FOI_EXPOSANT,
  MANA_A_LA_SORTIE_DE_L_OEUF,
  PALIERS_OUVERTS_AU_DEPART,
  PRODUCTION_DE_REFERENCE,
} from './constantes'
import { appliquerGainDeDensite } from './densite'
import { creditCompteur } from './technique'

/** Fraction du peuplement conservée. f = 1 ⇒ zéro : il n'y a pas de demi-vie. */
export const FRACTION_CONSERVEE = 1 - F_TARIF_REDESCENTE

/**
 * Gain de Foi prévu, indexé sur la production de pic du cycle.
 *
 * C'est ce que le nœud « Lire l'eau » affichera en permanence, et c'est le
 * versant « rester pour la Foi » de la seule vraie décision du joueur : la Foi
 * ne se gagne pas en attendant, elle se gagne en faisant monter le pic.
 *
 * [P] graine — le barème n'est fixé par aucun document. À réfuter en v0.3.
 */
export function gainDeFoiPrevu(etat: EtatJeu): Decimal {
  const rapport = etat.cycle.productionPicParSeconde.div(PRODUCTION_DE_REFERENCE)
  if (rapport.lte(1)) return new Decimal(0)
  return new Decimal(FOI_BASE).mul(Decimal.pow(rapport, FOI_EXPOSANT)).floor()
}

/** L'état de cycle d'un départ d'œuf. Aucun acquis permanent n'y figure. */
export function cycleInitial(): EtatCycle {
  return {
    manaCourant: new Decimal(MANA_A_LA_SORTIE_DE_L_OEUF),
    paliersOuverts: PALIERS_OUVERTS_AU_DEPART,
    bancs: {},
    productionPicParSeconde: new Decimal(0),
    dureeSecondes: 0,
  }
}

export function eclore(etat: EtatJeu): EtatJeu {
  const pic = etat.cycle.productionPicParSeconde
  const foiGagnee = gainDeFoiPrevu(etat)
  const densites = appliquerGainDeDensite(etat, etat.cycle.paliersOuverts, pic)

  // La contenance est conservée et progresse d'un cycle à l'autre : c'est elle
  // qui déplace le blocage doux vers le bas. [P] loi de croissance à trancher.
  const contenanceVisee = pic.mul(CONTENANCE_EN_SECONDES_DE_PRODUCTION_DE_PIC)
  const contenanceMana = Decimal.max(etat.permanent.contenanceMana, contenanceVisee)

  return {
    ...etat,
    cycle: cycleInitial(),
    permanent: {
      ...etat.permanent,
      densites,
      foi: etat.permanent.foi.add(foiGagnee),
      contenanceMana,
      profondeurMaxAtteinte: Math.max(etat.permanent.profondeurMaxAtteinte, etat.cycle.paliersOuverts),
      nombreEclosions: etat.permanent.nombreEclosions + 1,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'eclosion', 1),
      // Le mana courant expire vers l'ambiant. Aucun système d'IdlePond ne se
      // comporte comme un puits : il n'y a pas de machine non vivante ici.
      manaAmbiant: etat.permanent.manaAmbiant.add(etat.cycle.manaCourant),
    },
    telemetrie: {
      ...etat.telemetrie,
      cycles: [
        ...etat.telemetrie.cycles,
        {
          index: etat.permanent.nombreEclosions,
          dureeActiveSecondes: etat.cycle.dureeSecondes,
          secondesEnRedescente: etat.telemetrie.secondesEnRedescente,
          paliersOuverts: etat.cycle.paliersOuverts,
          productionPicParSeconde: pic,
          foiGagnee,
        },
      ],
      secondesEnRedescente: 0,
    },
  }
}
