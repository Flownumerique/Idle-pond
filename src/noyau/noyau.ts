/**
 * IdlePond — le noyau. tick(state, dt) -> state.
 *
 * Contrat du §5.1, sans exception :
 *   - fonction pure : aucun Date.now, aucun Math.random, aucun accès DOM,
 *     aucun import React ou Phaser ;
 *   - le PRNG est à graine et vit dans l'état ;
 *   - aucun état hors du reducer : pas de variable de module, pas de cache ;
 *   - le jeu appelle tick à 100 ms, le simulateur avec dt = 60 s ou 8 h, et
 *     c'est un seul code.
 *
 * Le PRNG n'est jamais tiré sur le chemin continu. C'est une contrainte du
 * §5.2 et non une commodité : un tirage par tick ferait diverger 480 pas de
 * 60 s d'un pas de 8 h, et emporterait avec lui le hors ligne et le
 * simulateur. Le hasard n'a droit de cité que sur des événements discrets.
 */
import Decimal from 'break_infinity.js'
import type { BancId, EtatJeu, EtatPrng, SuccesId } from './types'
import {
  CONTENANCE_INITIALE,
  NOMBRE_DE_PALIERS,
  VERSION_SAVE,
} from './constantes'
import { PALIERS, bancParId } from '../donnees/paliers'
import { TYPE_MANA_NATAL } from '../donnees/assises'
import {
  contenance,
  coutCreuser,
  coutDeblocage,
  coutNiveau,
  tauxEffectifDuBanc,
  toutEstCreuse,
} from './economie'
import { avancerBanc, effectifCible } from './population'
import { vitesseDeRepeuplement } from './densite'
import { cycleInitial } from './eclosion'
import { creditCompteur } from './technique'
import { verifierSucces } from './succes'

export { eclore, gainDeFoiPrevu } from './eclosion'
export { estBloque, productionTotaleParSeconde, detailDeCaptation, contenance } from './economie'

/* ─── PRNG ──────────────────────────────────────────────────────────────────*/

/** Tirage pur : rend la valeur ET l'état suivant. Rien ne mute. */
export function tirer(prng: EtatPrng): readonly [number, EtatPrng] {
  const graine = (prng.graine + 0x6d2b79f5) >>> 0
  let x = graine
  x = Math.imul(x ^ (x >>> 15), x | 1)
  x ^= x + Math.imul(x ^ (x >>> 7), x | 61)
  return [((x ^ (x >>> 14)) >>> 0) / 4294967296, { graine }]
}

/* ─── État initial ──────────────────────────────────────────────────────────*/

/**
 * `limiteDeContenu` : combien de paliers le monde offre réellement.
 *
 * Le jeu passe ce qui est livré — l'assise I au jalon v0.2 — et le simulateur
 * passe les 62 paliers, parce que c'est l'économie complète qu'il doit mesurer.
 * Un seul reducer, deux mondes : le §12 veut qu'aucune assise ne soit produite
 * avant que la précédente ait été mesurée, et c'est ce paramètre qui le tient.
 */
export function etatInitial(graine: number, limiteDeContenu = NOMBRE_DE_PALIERS): EtatJeu {
  return {
    versionSave: VERSION_SAVE,
    prng: { graine: graine >>> 0 },
    tempsJeuSecondes: 0,
    limiteDeContenu,
    cycle: cycleInitial(),
    permanent: {
      densites: new Array<number>(NOMBRE_DE_PALIERS).fill(0),
      // Le type natal est acquis d'emblée et ne se repaie jamais (Tier 0).
      acclimatations: { [TYPE_MANA_NATAL]: 1 },
      foi: new Decimal(0),
      contenanceMana: new Decimal(CONTENANCE_INITIALE),
      couches: [],
      profondeurMaxAtteinte: 0,
      compteursTechnique: {
        creusement: 0,
        amelioration: 0,
        recrutement: 0,
        entretien: 0,
        construction: 0,
        eclosion: 0,
      },
      noeudsTechnique: [],
      benedictions: {},
      succesDebloques: [],
      nombreEclosions: 0,
      manaAmbiant: new Decimal(0),
      heuresHorsLigneCreditees: 0,
    },
    telemetrie: {
      cycles: [],
      secondesEnRedescente: 0,
      secondesDepuisDernierSucces: 0,
      intervallesEntreSucces: [],
    },
  }
}

/* ─── Le tick ───────────────────────────────────────────────────────────────*/

export interface ResultatDeTick {
  readonly etat: EtatJeu
  readonly declenches: readonly SuccesId[]
}

/**
 * Avance l'état de `dt` secondes.
 *
 * Un seul pas suffit pour n'importe quel `dt` : l'effectif suit une
 * exponentielle dont la primitive est fermée, et tous les termes qui
 * multiplient cet effectif sont constants sur l'intervalle. La contenance est
 * un état permanent, donc constante elle aussi pendant le pas — c'est ce qui
 * rend le plafonnement du stock exactement composable, là où une contenance
 * dérivée de la production courante l'aurait rendu approximatif.
 */
export function tickDetaille(etat: EtatJeu, dt: number): ResultatDeTick {
  if (!(dt > 0)) return { etat, declenches: [] }

  const bancsAvances: Record<BancId, { niveau: number; effectif: number }> = {}
  let manaProduit = new Decimal(0)
  let productionFinale = new Decimal(0)

  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    const k = vitesseDeRepeuplement(etat, palier)
    for (const banc of PALIERS[palier].bancs) {
      const avant = etat.cycle.bancs[banc.id]
      if (avant === undefined || avant.niveau <= 0) continue
      const taux = tauxEffectifDuBanc(etat, banc, avant.niveau)
      const avancee = avancerBanc(avant.effectif, effectifCible(avant.niveau), k, dt)
      bancsAvances[banc.id] = { niveau: avant.niveau, effectif: avancee.effectif }
      manaProduit = manaProduit.add(taux.mul(avancee.integraleEffectif))
      productionFinale = productionFinale.add(taux.mul(avancee.effectif))
    }
  }

  // La contenance limite le stock, pas la production. Le surplus n'est pas
  // détruit : il expire vers l'ambiant (Tier 0 §5).
  const brut = etat.cycle.manaCourant.add(manaProduit)
  const plafond = contenance(etat)
  const manaCourant = Decimal.min(brut, plafond)
  const expire = brut.sub(manaCourant)

  const enRedescente = etat.cycle.paliersOuverts < etat.permanent.profondeurMaxAtteinte

  const avance: EtatJeu = {
    ...etat,
    tempsJeuSecondes: etat.tempsJeuSecondes + dt,
    cycle: {
      ...etat.cycle,
      manaCourant,
      bancs: { ...etat.cycle.bancs, ...bancsAvances },
      productionPicParSeconde: Decimal.max(etat.cycle.productionPicParSeconde, productionFinale),
      dureeSecondes: etat.cycle.dureeSecondes + dt,
    },
    permanent: {
      ...etat.permanent,
      manaAmbiant: expire.gt(0) ? etat.permanent.manaAmbiant.add(expire) : etat.permanent.manaAmbiant,
    },
    telemetrie: {
      ...etat.telemetrie,
      secondesEnRedescente: etat.telemetrie.secondesEnRedescente + (enRedescente ? dt : 0),
      secondesDepuisDernierSucces: etat.telemetrie.secondesDepuisDernierSucces + dt,
    },
  }

  return verifierSucces(avance)
}

/** Le contrat du §5.1. `tickDetaille` en rend en plus les succès déclenchés. */
export function tick(etat: EtatJeu, dt: number): EtatJeu {
  return tickDetaille(etat, dt).etat
}

/* ─── Actes du joueur ───────────────────────────────────────────────────────
 * Des réducteurs purs, comme le tick. Un acte qui n'est pas payable rend l'état
 * inchangé : c'est au-dessus du noyau de ne pas le proposer.
 */

/** Creuser le palier suivant. Bloqué doux si son coût dépasse la contenance. */
export function creuser(etat: EtatJeu): EtatJeu {
  if (toutEstCreuse(etat)) return etat
  const cible = etat.cycle.paliersOuverts
  const cout = coutCreuser(etat, cible)
  if (cout.gt(contenance(etat))) return etat
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      paliersOuverts: cible + 1,
    },
    permanent: {
      ...etat.permanent,
      profondeurMaxAtteinte: Math.max(etat.permanent.profondeurMaxAtteinte, cible + 1),
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'creusement', cout.toNumber()),
    },
  }
}

/** Convaincre un banc : le recruter. Jamais « acheter » (§3). */
export function convaincre(etat: EtatJeu, bancId: BancId): EtatJeu {
  const banc = bancParId(bancId)
  if (banc === undefined) return etat
  if (banc.palier >= etat.cycle.paliersOuverts) return etat
  if ((etat.cycle.bancs[bancId]?.niveau ?? 0) > 0) return etat
  const cout = coutDeblocage(etat, banc)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      bancs: { ...etat.cycle.bancs, [bancId]: { niveau: 1, effectif: 0 } },
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'recrutement', 1),
    },
  }
}

/** Monter le niveau d'un banc : l'achat répétable de la boucle, ×1.15. */
export function monterNiveau(etat: EtatJeu, bancId: BancId): EtatJeu {
  const banc = bancParId(bancId)
  if (banc === undefined) return etat
  if (banc.palier >= etat.cycle.paliersOuverts) return etat
  const avant = etat.cycle.bancs[bancId]
  if (avant === undefined || avant.niveau <= 0) return etat
  const cout = coutNiveau(etat, banc, avant.niveau)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      bancs: { ...etat.cycle.bancs, [bancId]: { niveau: avant.niveau + 1, effectif: avant.effectif } },
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'amelioration', cout.toNumber()),
    },
  }
}
