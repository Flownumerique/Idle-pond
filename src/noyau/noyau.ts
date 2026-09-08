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
import type { BancId, EspeceId, EtatJeu, EtatPrng, SuccesId } from './types'
import {
  ACQUIS_MAX,
  CONTENANCE_INITIALE,
  INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE,
  SEUILS_DE_JALON,
  NOMBRE_DE_PALIERS,
  SEUIL_DU_DRAPEAU_PERMANENT,
  TAU_SEJOUR_HEURES,
  VERSION_SAVE,
} from './constantes'
import { ESPECES } from '../donnees/especes'
import { PALIERS, bancParId } from '../donnees/paliers'
import { TYPE_MANA_NATAL } from '../donnees/assises'
import {
  contenance,
  coutDeDescente,
  coutDeConviction,
  coutDePlace,
  rendementAcclimatation,
  tauxBaseDuPalier,
  tauxParIndividuHorsSeuil,
  toutEstCreuse,
} from './economie'
import { avancerBanc, effectifCible } from './population'
import { densiteDuPalier, multiplicateurDensite, vitesseDeRepeuplement } from './densite'
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
      succes: {},
      nombreEclosions: 0,
      especesAyantAtteintCent: [],
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

  const coupure = prochaineCoupure(etat, dt)
  if (coupure !== null) {
    const avant = pasEntier(etat, coupure)
    const apres = tickDetaille(avant.etat, dt - coupure)
    return { etat: apres.etat, declenches: [...avant.declenches, ...apres.declenches] }
  }
  return pasEntier(etat, dt)
}

/**
 * Le premier instant de `]0, dt[` où le pas cesse d'être homogène, s'il existe.
 *
 * Deux choses peuvent tomber à l'intérieur d'un intervalle, et aucune ne
 * s'intègre : le drapeau des cent individus change le taux de TOUS les bancs,
 * et la saturation de la jauge change le débit qui alimente le stock. On
 * coupe donc au plus tôt des deux et on reprend derrière — une partition
 * analytique bornée, jamais une file d'événements.
 *
 * Prendre le MINIMUM est ce qui rend l'ensemble correct : chaque instant est
 * calculé sous les taux courants, donc juste tant qu'aucun autre ne l'a
 * précédé. Le premier l'est toujours ; le suivant est recalculé après la
 * coupure.
 */
function prochaineCoupure(etat: EtatJeu, dt: number): number | null {
  let coupure: number | null = null
  const retenir = (instant: number | null) => {
    if (instant === null || !(instant > 0) || !(instant < dt)) return
    if (coupure === null || instant < coupure) coupure = instant
  }
  retenir(instantDuProchainDrapeau(etat, dt))
  retenir(instantDeSaturation(etat, dt))
  return coupure
}

/**
 * Instant où la jauge se remplit, si elle le fait pendant ce pas.
 *
 * Le mana accumulé est une somme d'intégrales d'exponentielles, découpée aux
 * seuils de jalon : elle ne s'inverse pas. Dichotomie, donc, comme pour le
 * drapeau — et elle converge par le HAUT, de sorte que l'instant rendu porte
 * toujours un stock déjà plein.
 */
function instantDeSaturation(etat: EtatJeu, dt: number): number | null {
  const plafond = contenance(etat)
  if (etat.cycle.manaCourant.gte(plafond)) return null
  if (etat.cycle.manaCourant.add(manaProduitSur(etat, dt)).lt(plafond)) return null

  let bas = 0
  let haut = dt
  for (let i = 0; i < 60; i += 1) {
    const milieu = (bas + haut) / 2
    if (etat.cycle.manaCourant.add(manaProduitSur(etat, milieu)).gte(plafond)) haut = milieu
    else bas = milieu
  }
  return haut
}

interface AvanceeDesBancs {
  readonly bancs: Record<BancId, { place: number; effectif: number }>
  /** Mana capté sur l'intervalle, LES DEUX CANAUX. C'est ce qui entre en poche. */
  readonly manaProduit: Decimal
  /**
   * Débit du seul canal NATIF à la fin de l'intervalle — ce qui indexe la
   * pointe du cycle.
   *
   * Le canal acclimaté en est exclu : ce que la pointe indexe est la densité
   * laissée derrière (§6.5), et le canal acclimaté ne PRODUIT rien — il
   * prélève une charge déjà là. Seul le vivant produit (Tier 0 §5).
   *
   * À reposer si le canal acclimaté cesse d'être « très bas » (§3).
   */
  readonly productionNativeFinale: Decimal
}

/**
 * Avance les deux canaux de captation sur `dt` secondes. Pure, sans état.
 *
 * Une seule quantité varie à l'intérieur de l'intervalle, et elle ne sort pas
 * du signe somme : le multiplicateur de seuil, qui se lit sur l'effectif
 * (§2.C). Il a une primitive fermée, et c'est la condition d'existence du hors
 * ligne : un pas de 8 h doit rendre exactement ce que rendent 480 pas de 60 s.
 * Le canal acclimaté est constant sur l'intervalle — noyau v1.0 a retiré la
 * maturation qui le faisait varier — donc son intégrale est un simple produit
 * par `dt`.
 */
function avancerLesBancs(etat: EtatJeu, dt: number): AvanceeDesBancs {
  const bancs: Record<BancId, { place: number; effectif: number }> = {}
  let manaProduit = new Decimal(0)
  let productionNativeFinale = new Decimal(0)

  // Uniforme depuis V11 : le repeuplement ne dépend plus du palier. Il en
  // dépendra de nouveau le jour où la régénération locale du GDD §7.2 sera
  // écrite — elle est fonction de la biomasse, donc locale par nature.
  const k = vitesseDeRepeuplement()

  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    // ── Canal natif : ce que la population vivante capte ────────────────────
    for (const banc of PALIERS[palier].bancs) {
      const avant = etat.cycle.bancs[banc.id]
      if (avant === undefined || avant.place <= 0) continue
      const avancee = avancerBanc(avant.effectif, effectifCible(avant.place), k, dt, SEUILS_DE_JALON)
      bancs[banc.id] = { place: avant.place, effectif: avancee.effectif }
      const taux = tauxParIndividuHorsSeuil(etat, banc)
      manaProduit = manaProduit.add(taux.mul(avancee.integralePonderee))
      productionNativeFinale = productionNativeFinale.add(
        taux.mul(avancee.multiplicateurFinal).mul(avancee.effectif),
      )
    }

    // ── Canal acclimaté : ce que l'eau capte toute seule ────────────────────
    // Constant sur l'intervalle : rien ne le fait plus varier dans le temps.
    const debitAcclimate = tauxBaseDuPalier(palier)
      .mul(INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE)
      .mul(rendementAcclimatation(etat, palier))
    // Le mana acclimaté entre en poche ; il n'entre PAS dans la pointe. Voir la
    // note de `productionNativeFinale`.
    manaProduit = manaProduit.add(debitAcclimate.mul(dt))
  }
  return { bancs, manaProduit, productionNativeFinale }
}

/** Ce que la mare produirait sur `dt`, sans rien avancer. Pour la dichotomie. */
function manaProduitSur(etat: EtatJeu, dt: number): Decimal {
  return avancerLesBancs(etat, dt).manaProduit
}

function pasEntier(etat: EtatJeu, dt: number): ResultatDeTick {
  const { bancs: bancsAvances, manaProduit, productionNativeFinale } = avancerLesBancs(etat, dt)

  // La contenance limite le stock, pas la production. Le surplus n'est pas
  // détruit : il expire vers l'ambiant (Tier 0 §5).
  const brut = etat.cycle.manaCourant.add(manaProduit)
  const plafond = contenance(etat)
  const manaCourant = Decimal.min(brut, plafond)
  const expire = brut.sub(manaCourant)

  const enRedescente = etat.cycle.paliersOuverts < etat.permanent.profondeurMaxAtteinte

  // Acquis de séjour (§2.B) : accumulation saturante vers `A∞`, dont le temps
  // caractéristique décroît quand la densité monte. Même forme exponentielle
  // que l'effectif, donc exacte pour n'importe quel `dt` — c'est ce qui permet
  // à la contenance de monter correctement au retour d'une absence de 8 h.
  const tauEffSecondes =
    (TAU_SEJOUR_HEURES * 3600) / multiplicateurDensite(densiteDuSejour(etat))
  const acquisDeSejour =
    ACQUIS_MAX + (etat.cycle.acquisDeSejour - ACQUIS_MAX) * Math.exp(-dt / tauEffSecondes)

  const avance: EtatJeu = {
    ...etat,
    tempsJeuSecondes: etat.tempsJeuSecondes + dt,
    cycle: {
      ...etat.cycle,
      manaCourant,
      bancs: { ...etat.cycle.bancs, ...bancsAvances },
      productionPicParSeconde: Decimal.max(etat.cycle.productionPicParSeconde, productionNativeFinale),
      dureeSecondes: etat.cycle.dureeSecondes + dt,
      acquisDeSejour,
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

  return verifierSucces(poserLesDrapeauxPermanents(avance))
}

/**
 * Densité du séjour : la plus dense des eaux où le héros se tient.
 *
 * [P] — le §2.B écrit `multiplicateurDensite(s)` pour l'état entier, alors que
 * la densité est portée par palier. Le maximum sur les paliers ouverts est
 * retenu : c'est celle qu'il peut effectivement habiter. En pratique la
 * question est peu sensible — l'éclosion porte tous les paliers occupés à la
 * même valeur —, mais elle le deviendrait si une assise cessait d'être
 * revisitée à chaque vie.
 */
function densiteDuSejour(etat: EtatJeu): number {
  let densite = 0
  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    densite = Math.max(densite, densiteDuPalier(etat, palier))
  }
  return densite
}

/**
 * Pose le drapeau permanent des espèces ayant atteint cent individus (§2.C).
 *
 * L'unique acquis de seuil qui survive à l'éclosion. Comme tout le reste, c'est
 * une lecture de seuil sur l'état de fin de tick, et la liste est reconstruite
 * dans l'ordre du registre pour ne pas dépendre de la taille du pas.
 */
function poserLesDrapeauxPermanents(etat: EtatJeu): EtatJeu {
  const effectifs = new Map<EspeceId, number>()
  for (const [id, banc] of Object.entries(etat.cycle.bancs)) {
    const espece = bancParId(id)?.espece
    if (espece === undefined) continue
    effectifs.set(espece, (effectifs.get(espece) ?? 0) + banc.effectif)
  }

  const acquis = new Set(etat.permanent.especesAyantAtteintCent)
  let nouveau = false
  for (const espece of ESPECES) {
    if (acquis.has(espece.id)) continue
    if ((effectifs.get(espece.id) ?? 0) < SEUIL_DU_DRAPEAU_PERMANENT) continue
    acquis.add(espece.id)
    nouveau = true
  }
  if (!nouveau) return etat

  return {
    ...etat,
    permanent: {
      ...etat.permanent,
      especesAyantAtteintCent: ESPECES.filter((e) => acquis.has(e.id)).map((e) => e.id),
    },
  }
}

/**
 * Instant, dans `]0, dt[`, où une espèce atteindra cent individus pour la
 * première fois de la partie. `null` si aucune ne le fait sur cet intervalle.
 *
 * Résolu par dichotomie plutôt qu'à la main : l'effectif d'une espèce est une
 * SOMME d'exponentielles, une par banc, chacune avec sa propre vitesse de
 * repeuplement, et une somme d'exponentielles ne s'inverse pas. Elle est
 * monotone, ce qui suffit à la dichotomie, et le calcul n'a lieu que lorsqu'un
 * franchissement est effectivement en vue — au plus une fois par espèce et par
 * partie.
 */
function instantDuProchainDrapeau(etat: EtatJeu, dt: number): number | null {
  const acquis = new Set(etat.permanent.especesAyantAtteintCent)
  let coupure: number | null = null

  for (const espece of ESPECES) {
    if (acquis.has(espece.id)) continue
    if (effectifDEspeceA(etat, espece.id, 0) >= SEUIL_DU_DRAPEAU_PERMANENT) continue
    if (effectifDEspeceA(etat, espece.id, dt) < SEUIL_DU_DRAPEAU_PERMANENT) continue

    let bas = 0
    let haut = dt
    for (let i = 0; i < 60; i += 1) {
      const milieu = (bas + haut) / 2
      if (effectifDEspeceA(etat, espece.id, milieu) >= SEUIL_DU_DRAPEAU_PERMANENT) haut = milieu
      else bas = milieu
    }
    if (haut > 0 && haut < dt && (coupure === null || haut < coupure)) coupure = haut
  }
  return coupure
}

/** Effectif d'une espèce à `t` secondes, tous ses bancs sommés. */
function effectifDEspeceA(etat: EtatJeu, espece: EspeceId, t: number): number {
  let total = 0
  const k = vitesseDeRepeuplement()
  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    for (const banc of PALIERS[palier].bancs) {
      if (banc.espece !== espece) continue
      const avant = etat.cycle.bancs[banc.id]
      if (avant === undefined || avant.place <= 0) continue
      const cible = effectifCible(avant.place)
      total += cible + (avant.effectif - cible) * Math.exp(-k * t)
    }
  }
  return total
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

/** Convaincre un banc : le recruter. Jamais « acheter » (§3). */
export function convaincre(etat: EtatJeu, bancId: BancId): EtatJeu {
  const banc = bancParId(bancId)
  if (banc === undefined) return etat
  if (banc.palier >= etat.cycle.paliersOuverts) return etat
  if ((etat.cycle.bancs[bancId]?.place ?? 0) > 0) return etat
  const cout = coutDeConviction(etat, banc)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      bancs: { ...etat.cycle.bancs, [bancId]: { place: 1, effectif: 0 } },
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'recrutement', 1),
    },
  }
}

/**
 * Acheter une place de plus : l'achat répétable de la boucle, ×1.15.
 *
 * De la PLACE, pas des individus. La population monte seule vers le plafond
 * ainsi ouvert, et c'est pour ça que les seuils tombent avec le temps.
 */
export function acheterPlace(etat: EtatJeu, bancId: BancId): EtatJeu {
  const banc = bancParId(bancId)
  if (banc === undefined) return etat
  if (banc.palier >= etat.cycle.paliersOuverts) return etat
  const avant = etat.cycle.bancs[bancId]
  if (avant === undefined || avant.place <= 0) return etat
  const cout = coutDePlace(etat, banc, avant.place)
  if (etat.cycle.manaCourant.lt(cout)) return etat
  return {
    ...etat,
    cycle: {
      ...etat.cycle,
      manaCourant: etat.cycle.manaCourant.sub(cout),
      bancs: { ...etat.cycle.bancs, [bancId]: { place: avant.place + 1, effectif: avant.effectif } },
    },
    permanent: {
      ...etat.permanent,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'amelioration', cout.toNumber()),
    },
  }
}
