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
import type { EspeceId, EtatJeu, EtatPrng, Reglage, SuccesId } from './types'
import {
  ACQUIS_MAX,
  CONTENANCE_INITIALE,
  NOMBRE_DE_PALIERS,
  REGLAGE_CANONIQUE,
  SEUIL_DU_DRAPEAU_PERMANENT,
  TAU_SEJOUR_HEURES,
  VERSION_SAVE,
} from './constantes'
import { ESPECES, especeParId } from '../donnees/especes'
import {
  contenance,
  coutDeDescente,
  coutDeDeblocage,
  coutDeNiveau,
  coutDeCroissance,
  productionTotaleParSeconde,
  toutEstCreuse,
} from './economie'
import { cycleInitial } from './eclosion'
import { creditCompteur } from './technique'
import { verifierSucces } from './succes'

export { eclore, gainDeFoiPrevu } from './eclosion'
export {
  contenance,
  detailDeCaptation,
  eauTroublee,
  estBloque,
  estSature,
  partDeContenance,
  productionTotaleParSeconde,
} from './economie'
export { palierDeVoix } from './voix'

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
export function etatInitial(
  graine: number,
  limiteDeContenu = NOMBRE_DE_PALIERS,
  reglage: Reglage = REGLAGE_CANONIQUE,
): EtatJeu {
  return {
    versionSave: VERSION_SAVE,
    prng: { graine: graine >>> 0 },
    tempsJeuSecondes: 0,
    limiteDeContenu,
    reglage,
    cycle: cycleInitial(),
    permanent: {
      densites: new Array<number>(NOMBRE_DE_PALIERS).fill(0),
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
      succes: {},
      nombreEclosions: 0,
      especesAyantAtteintCent: [],
      manaAmbiant: new Decimal(0),
      heuresHorsLigneCreditees: 0,
    },
    telemetrie: {
      cycles: [],
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
 * Avance l'état de `dt` secondes. Un seul pas, pour n'importe quel `dt`.
 *
 * Le pas est HOMOGÈNE, et c'est ce que le passage au modèle à niveau a acheté :
 * la production ne dépend que de niveaux, qui ne changent qu'à l'achat, donc
 * elle est constante sur tout l'intervalle. Plus rien ne peut tomber au milieu
 * d'un pas — ni un effectif qui franchit un seuil, ni le drapeau des cent, qui
 * tombe désormais quand on paie le centième niveau.
 *
 * La saturation de la jauge ne coupe pas davantage : à débit constant, le
 * surplus qui expire vers l'ambiant est le même qu'on le calcule en un pas ou
 * en quatre cent quatre-vingts. C'est ce qui rend l'équivalence de pas triviale
 * au lieu de délicate, et pourquoi il n'y a plus de `prochaineCoupure`.
 */
/**
 * `τ` — le temps caractéristique du séjour, en secondes : `τ₀ × c^profondeur`
 * (amendement v1.2, §2.B, amendé le 2026-09-16).
 *
 * La profondeur est celle ATTEINTE, `profondeurMaxAtteinte`, et non celle qui
 * est ouverte dans la vie courante. Deux raisons, et la seconde est un
 * invariant :
 *
 *   - c'est un acquis de l'être, pas de la plongée : on ne redevient pas jeune
 *     en remontant, et la contenance ne doit pas se regagner plus vite parce
 *     qu'on vient d'éclore ;
 *   - `profondeurMaxAtteinte` est monotone (Tier 0), donc `τ` l'est aussi. Un
 *     `τ` qui pourrait redescendre ferait d'une éclosion un moyen d'accélérer
 *     l'acquis, ce qui rendrait la décision du §6.4 dégénérée.
 *
 * Elle ne bouge que sur un ACTE du joueur, jamais pendant un tick : la forme
 * exponentielle de l'acquis reste donc exacte pour n'importe quel `dt`, et le
 * §5.2 tient. Un test d'équivalence de pas le garde.
 *
 * `τ` change le TEMPS, jamais la valeur : l'acquis sature toujours vers `A∞`.
 * Descendre ne réduit pas ce qu'on peut porter, cela rallonge le temps qu'il
 * faut pour le porter.
 */
export function tauDuSejourSecondes(etat: EtatJeu): number {
  const croissance = etat.reglage.croissanceDuSejourParPalier
  return TAU_SEJOUR_HEURES * 3600 * Math.pow(croissance, etat.permanent.profondeurMaxAtteinte)
}

export function tickDetaille(etat: EtatJeu, dt: number): ResultatDeTick {
  if (!(dt > 0)) return { etat, declenches: [] }

  const production = productionTotaleParSeconde(etat)

  // Acquis de séjour (§2.B) : accumulation saturante vers `A∞`, de temps
  // caractéristique `τ₀` CONSTANT. Forme exponentielle, donc exacte pour
  // n'importe quel `dt` — c'est ce qui permet à la contenance de monter
  // correctement au retour d'une absence de 8 h.
  //
  // La densité n'entre PAS ici. Elle vaut `pointe^α` et croît sans borne : un
  // `τ` divisé par elle tombait à 0,12 h de t₉₀ au deuxième cycle, à 0,05 s au
  // troisième, et les cycles suivants à quelques secondes. L'acquis saturait toujours avant
  // l'éclosion, et la contenance dégénérait en forfait. La saturation borne la
  // VALEUR de l'acquis, pas le TEMPS pour l'atteindre. `τ₀` jauge une durée de
  // cycle constante par construction : il doit l'être aussi.
  const acquisDeSejour =
    ACQUIS_MAX + (etat.cycle.acquisDeSejour - ACQUIS_MAX) * Math.exp(-dt / tauDuSejourSecondes(etat))

  // La contenance limite le stock, pas la production, et elle monte PENDANT le
  // cycle avec l'acquis : le plafond se lit donc à la FIN du pas.
  const brut = etat.cycle.manaCourant.add(production.mul(dt))
  const plafond = contenance({ ...etat, cycle: { ...etat.cycle, acquisDeSejour } })
  const manaCourant = Decimal.min(brut, plafond)
  // Le surplus n'est pas détruit : il expire vers l'ambiant (Tier 0 §5).
  const expire = brut.sub(manaCourant)

  const avance: EtatJeu = {
    ...etat,
    tempsJeuSecondes: etat.tempsJeuSecondes + dt,
    cycle: {
      ...etat.cycle,
      manaCourant,
      productionPicParSeconde: Decimal.max(etat.cycle.productionPicParSeconde, production),
      dureeSecondes: etat.cycle.dureeSecondes + dt,
      acquisDeSejour,
    },
    permanent: {
      ...etat.permanent,
      manaAmbiant: expire.gt(0) ? etat.permanent.manaAmbiant.add(expire) : etat.permanent.manaAmbiant,
    },
    telemetrie: {
      ...etat.telemetrie,
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
  const cout = coutDeDescente(etat, cible)
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

/** Débloquer une espèce. Une fois par espèce et par vie ; elle démarre au niveau 1. */
export function debloquer(etat: EtatJeu, especeId: EspeceId): EtatJeu {
  const espece = especeParId(especeId)
  if (espece === undefined) return etat
  if (espece.palier >= etat.cycle.paliersOuverts) return etat
  if (etat.cycle.especes[especeId]?.debloquee === true) return etat
  const cout = coutDeDeblocage(etat, espece)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      especes: { ...etat.cycle.especes, [especeId]: { debloquee: true, niveau: 1 } },
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'recrutement', 1),
    },
  }
}

/**
 * Monter une espèce d'un niveau. L'achat répétable de la boucle, ×1.15.
 *
 * Le drapeau des cent tombe ICI, à l'achat du centième niveau, et plus au
 * milieu d'un pas de tick : il n'y a plus de population qui le franchit toute
 * seule. C'est ce qui rend le pas homogène.
 */
export function ameliorer(etat: EtatJeu, especeId: EspeceId): EtatJeu {
  const espece = especeParId(especeId)
  if (espece === undefined) return etat
  const avant = etat.cycle.especes[especeId]
  if (avant === undefined || !avant.debloquee) return etat
  const cout = coutDeNiveau(etat, espece, avant.niveau)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  const niveau = avant.niveau + 1
  const atteintCent =
    niveau >= SEUIL_DU_DRAPEAU_PERMANENT &&
    !etat.permanent.especesAyantAtteintCent.includes(especeId)
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      especes: { ...etat.cycle.especes, [especeId]: { debloquee: true, niveau } },
    },
    permanent: {
      ...etat.permanent,
      // Reconstruite dans l'ordre du registre, jamais dans l'ordre des achats :
      // deux parties qui achètent les mêmes niveaux dans un ordre différent ne
      // doivent pas se sérialiser différemment.
      especesAyantAtteintCent: atteintCent
        ? ESPECES.filter(
            (e) => e.id === especeId || etat.permanent.especesAyantAtteintCent.includes(e.id),
          ).map((e) => e.id)
        : etat.permanent.especesAyantAtteintCent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'amelioration', cout.toNumber()),
    },
  }
}

/**
 * Faire grandir le héros d'un niveau — le quatrième achat, spec 2026-09-17.
 *
 * Même forme que les trois autres : payable ou rien ne change. Le compteur
 * crédité est celui d'Amélioration : c'est du mana dépensé en niveaux, et
 * l'arbre n'a pas de branche « héros ». Le niveau agit à l'instant où il est
 * payé, jamais pendant un pas.
 */
export function grandir(etat: EtatJeu): EtatJeu {
  const cout = coutDeCroissance(etat, etat.cycle.niveauDuHeros)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      niveauDuHeros: etat.cycle.niveauDuHeros + 1,
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'amelioration', cout.toNumber()),
    },
  }
}
