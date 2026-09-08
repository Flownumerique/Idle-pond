/**
 * IdlePond — production, coûts, seuils.
 *
 * DEUX CANAUX ADDITIFS — GDD §3, « Fixé (canon) » au §16.1 :
 *
 *   captation/s =   débit_natif(population vivante présente)
 *                 + débit_acclimaté(part_mûre(palier) × rendement_acclimatation)
 *
 * Additifs, jamais multiplicatifs — c'est ce qui rend l'arbitrage réel plutôt
 * que cosmétique. Le natif est le débit des bancs, à 100 % d'emblée, et il tombe
 * avec la population. L'acclimaté ne dépend d'aucun vivant : il vient de l'eau
 * elle-même, et il est borné par la part mûre du palier (§3.0), que peupler
 * dilue.
 *
 * Les deux se rejoignent dans `productionTotaleParSeconde`, et nulle part
 * ailleurs : un module qui n'additionnerait qu'un canal serait faux sans qu'un
 * type ne s'en aperçoive.
 *
 * Les multiplicateurs qui s'ajoutent au canal sont des TermeDeFormule nommés,
 * jamais des facteurs anonymes : c'est ce qui rend le détail de captation
 * auditable (§7.5 règle 3, §8.2).
 */
import Decimal from 'break_infinity.js'
import type {
  Banc,
  EtatJeu,
  IndexPalier,
  LigneDeCaptation,
  TermeDeConfort,
  TermeDeCout,
} from './types'
import {
  AFFINITE_PLEINE_JUSQU_EN_V05,
  COUT_CREUSER_AU_PALIER_1,
  COUT_DEBLOCAGE_AU_PALIER_0,
  COUT_DE_PLACE_AU_PALIER_0,
  BONUS_GLOBAL_A_CENT_INDIVIDUS,
  DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES,
  EXPOSANT_RECONVICTION_DENSITE,
  F_FRACTION_D_AMENAGEMENT,
  INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE,
  NOMBRE_DE_PALIERS,
  SEUIL_D_ALERTE_DE_CONTENANCE,
  RENDEMENT_ACCLIMATATION_PLEIN_JUSQU_EN_V05,
  SEUILS_DE_JALON,
  TAUX_BASE_AU_PALIER_0,
} from './constantes'
import { assiseDuPalier } from '../donnees/assises'
import { densiteDuPalier } from './densite'
import { PART_MURE_D_UNE_EAU_INTOUCHEE } from './maturation'
import { puissanceDeD, puissanceDeG, puissanceDuCoutDeNiveau } from '../donnees/echelles'
import { PALIERS, bancsDuPalier } from '../donnees/paliers'
import { facteurDeTechnique } from './technique'
import { SUCCES } from '../donnees/succes/index'

/* ─── Seuils de jalon ───────────────────────────────────────────────────────*/

/**
 * Multiplicateur de seuil d'un banc, d'après son EFFECTIF (§2.C).
 *
 * La table donne le multiplicateur CUMULÉ lu au seuil : on retient celui du
 * seuil le plus haut franchi, on ne multiplie pas les colonnes entre elles.
 * Cent individus valent ×16, jamais ×1024 — et `D = 2.31` a été calibré
 * contre cette lecture-là.
 *
 * Il se lit sur l'effectif courant à chaque tick, donc il se reperd à
 * l'éclosion avec la population. Le seul acquis qui survit est le drapeau
 * permanent, plus bas.
 */
export function multiplicateurDeSeuil(effectif: number): number {
  let multiplicateur = 1
  for (const seuil of SEUILS_DE_JALON) {
    if (effectif >= seuil.seuil) multiplicateur = seuil.multiplicateurCumule
  }
  return multiplicateur
}

/**
 * Bonus global des espèces ayant DÉJÀ atteint cent individus (§2.C).
 * Définitif, conservé à l'éclosion, additif entre espèces.
 */
export function multiplicateurDesDrapeaux(etat: EtatJeu): number {
  return 1 + BONUS_GLOBAL_A_CENT_INDIVIDUS * etat.permanent.especesAyantAtteintCent.length
}

/* ─── Taux de base ──────────────────────────────────────────────────────────*/

/**
 * Production totale d'un palier à pleine puissance, rapportée au précédent :
 * `D`. Mesurée sur le PALIER, jamais par espèce — les bancs d'un même palier se
 * partagent ce taux, de sorte que le nombre d'espèces par palier ne fait pas
 * dériver le ratio (§6.3).
 */
export function tauxBaseDuPalier(palier: IndexPalier): Decimal {
  return puissanceDeD(palier).mul(TAUX_BASE_AU_PALIER_0)
}

export function tauxBaseDuBanc(banc: Banc): Decimal {
  return tauxBaseDuPalier(banc.palier).div(bancsDuPalier(banc.palier).length)
}

/** Rendement du héros sur le type de mana du palier. Jamais repayé (Tier 0). */
export function rendementAcclimatation(etat: EtatJeu, palier: IndexPalier): number {
  const typeMana = assiseDuPalier(palier).typeMana
  return etat.permanent.acclimatations[typeMana] ?? RENDEMENT_ACCLIMATATION_PLEIN_JUSQU_EN_V05
}

/**
 * Taux par individu SANS le multiplicateur de seuil.
 *
 * Tous les termes qui restent constants pendant un tick sont ici ; le seul qui
 * varie en cours d'intervalle — le multiplicateur de seuil, qui se lit sur
 * l'effectif (§2.C) — en est sorti, pour que le noyau puisse l'intégrer
 * analytiquement plutôt que de le figer au début du pas.
 */
export function tauxParIndividuHorsSeuil(etat: EtatJeu, banc: Banc): Decimal {
  return tauxBaseDuBanc(banc)
    .mul(rendementAcclimatation(etat, banc.palier))
    .mul(multiplicateurDesDrapeaux(etat))
}

/** Taux d'un banc par individu et par seconde, tous termes nommés appliqués. */
export function tauxParIndividu(etat: EtatJeu, banc: Banc, effectif: number): Decimal {
  return tauxParIndividuHorsSeuil(etat, banc).mul(multiplicateurDeSeuil(effectif))
}

export function productionDuBanc(etat: EtatJeu, banc: Banc): Decimal {
  const bancEtat = etat.cycle.bancs[banc.id]
  if (bancEtat === undefined || bancEtat.place <= 0) return new Decimal(0)
  return tauxParIndividu(etat, banc, bancEtat.effectif).mul(bancEtat.effectif)
}

/* ─── Le canal acclimaté — GDD §3 et §3.0 ───────────────────────────────────*/

export function partMureDuPalier(etat: EtatJeu, palier: IndexPalier): number {
  return etat.permanent.partsMures[palier] ?? PART_MURE_D_UNE_EAU_INTOUCHEE
}

/** Place totale installée sur un palier — ce qui dilue son type (§3.0). */
export function placeDuPalier(etat: EtatJeu, palier: IndexPalier): number {
  let place = 0
  for (const banc of PALIERS[palier].bancs) place += etat.cycle.bancs[banc.id]?.place ?? 0
  return place
}

/**
 * Débit acclimaté d'un palier, par seconde. Ne dépend d'AUCUN vivant.
 *
 * Il vient de l'eau : `part_mûre × rendement_acclimatation`, à la force que la
 * graine exprime en individus équivalents. C'est ce qui donne au héros un revenu
 * dès l'instant où il rouvre une galerie, avant d'y avoir ramené qui que ce
 * soit — et c'est pour ça que le §3 tient à ce que les canaux soient ADDITIFS.
 */
export function productionAcclimateeDuPalier(etat: EtatJeu, palier: IndexPalier): Decimal {
  return tauxBaseDuPalier(palier)
    .mul(INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE)
    .mul(partMureDuPalier(etat, palier))
    .mul(rendementAcclimatation(etat, palier))
}

/** La somme des deux canaux, sur tous les paliers ouverts. */
export function productionTotaleParSeconde(etat: EtatJeu): Decimal {
  let total = new Decimal(0)
  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    for (const banc of PALIERS[palier].bancs) {
      total = total.add(productionDuBanc(etat, banc))
    }
    total = total.add(productionAcclimateeDuPalier(etat, palier))
  }
  return total
}

/**
 * Détail de la captation (§8.2) : chaque terme actif attribuable à sa source.
 * C'est la contrepartie obligatoire d'un effet appliqué silencieusement.
 */
export function detailDeCaptation(etat: EtatJeu, banc: Banc): readonly LigneDeCaptation[] {
  const bancEtat = etat.cycle.bancs[banc.id]
  const effectif = bancEtat?.effectif ?? 0
  return [
    { terme: 'effectif', valeur: effectif, source: { quoi: 'population' } },
    { terme: 'taux_base', valeur: tauxBaseDuBanc(banc).toNumber(), source: { quoi: 'palier', palier: banc.palier } },
    {
      terme: 'rendement_acclimatation',
      valeur: rendementAcclimatation(etat, banc.palier),
      source: { quoi: 'acclimatation', typeMana: assiseDuPalier(banc.palier).typeMana },
    },
    {
      terme: 'multiplicateur_jalon',
      valeur: multiplicateurDeSeuil(effectif),
      source: { quoi: 'place', place: bancEtat?.place ?? 0 },
    },
    {
      terme: 'multiplicateur_drapeau',
      valeur: multiplicateurDesDrapeaux(etat),
      source: { quoi: 'drapeaux_permanents', especes: etat.permanent.especesAyantAtteintCent.length },
    },
  ]
}

/**
 * Détail du canal acclimaté d'un palier.
 *
 * Séparé du précédent, et il doit l'être : le natif se lit par banc, l'acclimaté
 * par palier. Les mêler dans une seule liste laisserait croire qu'un banc porte
 * une part du revenu de l'eau, alors que celui-ci tombe même quand il n'y a
 * personne — ce qui est précisément ce que le joueur doit comprendre du §3.
 */
export function detailDuCanalAcclimate(etat: EtatJeu, palier: IndexPalier): readonly LigneDeCaptation[] {
  const part = partMureDuPalier(etat, palier)
  return [
    {
      terme: 'part_mure',
      valeur: part,
      source: { quoi: 'eau_murie', part },
    },
    {
      terme: 'rendement_acclimatation',
      valeur: rendementAcclimatation(etat, palier),
      source: { quoi: 'acclimatation', typeMana: assiseDuPalier(palier).typeMana },
    },
    {
      terme: 'debit_acclimate',
      valeur: productionAcclimateeDuPalier(etat, palier).toNumber(),
      source: { quoi: 'canal_acclimate' },
    },
  ]
}

/* ─── Coûts ─────────────────────────────────────────────────────────────────*/

/**
 * Facteur appliqué à un terme de coût par les succès acquis.
 *
 * Il vit ici plutôt que dans succes.ts pour que les dépendances restent à sens
 * unique : succes.ts lit la production, l'économie lit les effets. Un cycle
 * d'imports entre les deux tiendrait à l'exécution et tomberait au premier
 * changement d'ordre d'initialisation.
 */
export function facteurDeSucces(etat: EtatJeu, terme: TermeDeCout | TermeDeConfort): number {
  let facteur = 1
  for (const succes of SUCCES) {
    const effet = succes.effet
    if (effet === null || effet.genre === 'verbe') continue
    if (effet.terme !== terme) continue
    // Lu directement, jamais via `succes.ts` : ce module y est importé, et un
    // cycle d'imports tiendrait à l'exécution pour tomber au premier
    // changement d'ordre d'initialisation.
    if (etat.permanent.succes[succes.id] === undefined) continue
    // `part` est la fraction retirée d'un coût, ou ajoutée à un plafond.
    facteur *= effet.genre === 'reduction_cout' ? 1 - effet.part : 1 + effet.part
  }
  return facteur
}

/** Technique et succès se composent sur un même terme, chacun nommé et attribuable. */
function facteurDeCout(etat: EtatJeu, terme: TermeDeCout): number {
  return facteurDeTechnique(etat, terme) * facteurDeSucces(etat, terme)
}

/** Coût d'origine d'un palier, avant tout levier. Le palier 0 est ouvert au départ. */
export function coutBaseDuPalier(cible: IndexPalier): Decimal {
  return puissanceDeG(Math.max(0, cible - 1)).mul(COUT_CREUSER_AU_PALIER_1)
}

/**
 * Vrai si ce palier a déjà été atteint dans une vie précédente — GDD §6.4.
 *
 * C'est toute la distinction entre les deux puits du §4.1 : CREUSER ouvre le
 * palier suivant, AMÉNAGER rend habitable un palier que les galeries
 * effondrées ont refermé. « La roche ne se souvient pas des galeries »
 * (§10.1) ; le héros, lui, se souvient de la profondeur.
 */
export function estUnAmenagement(etat: EtatJeu, cible: IndexPalier): boolean {
  return cible < etat.permanent.profondeurMaxAtteinte
}

/**
 * Ce que coûte de descendre d'un palier — les deux puits du GDD §4.1.
 *
 *   creuser   : coût_base(palier)
 *   aménager  : coût_base(palier) × f × réduction_technique      (§6.4)
 *
 * `reduction_technique` ne touche QUE l'aménagement, et c'est voulu : « un
 * puits, un levier ». Jusqu'au 2026-09-08 elle s'appliquait aussi au
 * creusement, ce qui donnait deux leviers au même coût et rendait l'ensemble
 * inéquilibrable.
 */
export function coutDeDescente(etat: EtatJeu, cible: IndexPalier): Decimal {
  const base = coutBaseDuPalier(cible)
  if (!estUnAmenagement(etat, cible)) return base.mul(facteurDeCout(etat, 'cout_creuser'))
  return base.mul(F_FRACTION_D_AMENAGEMENT).mul(facteurDeCout(etat, 'reduction_technique'))
}

/**
 * Coût de conviction d'un banc — GDD §7.1.
 *
 *   coût_base(espèce) ÷ affinité(type, espèce) ÷ densité_locale_du_type
 *
 * « Le second dénominateur est la réponse au problème du prestige : une espèce
 * se laisse reconvaincre d'autant plus facilement que l'eau est déjà chargée du
 * type qu'elle supporte. Ce n'est pas un bonus, c'est une conséquence — la
 * densité conservée est la mémoire du monde, et c'est elle qui paie le retour. »
 *
 * Aucun facteur de technique ni de succès n'entre ici : la conviction est payée
 * par la densité, et par elle seule (§6.4, « un puits, un levier »).
 *
 * [P] La densité est portée par palier, là où le §7.1 l'indexe sur le TYPE de
 * mana. Les deux coïncident tant qu'une assise entière porte un type unique ;
 * elles cesseront de coïncider avec les temples, qui chargent un palier d'un
 * type qui n'est pas celui de son assise (§9).
 */
export function coutDeConviction(etat: EtatJeu, banc: Banc): Decimal {
  const memoireDuMonde = Math.pow(
    1 + densiteDuPalier(etat, banc.palier),
    EXPOSANT_RECONVICTION_DENSITE,
  )
  return puissanceDeG(banc.palier)
    .mul(COUT_DEBLOCAGE_AU_PALIER_0)
    .div(AFFINITE_PLEINE_JUSQU_EN_V05)
    .div(memoireDuMonde)
}

/**
 * Coût d'une place de plus. Achat répétable, ×1.15.
 * Le joueur achète de la place, jamais des individus (§2.C).
 */
export function coutDePlace(etat: EtatJeu, banc: Banc, place: number): Decimal {
  return puissanceDeG(banc.palier)
    .mul(COUT_DE_PLACE_AU_PALIER_0)
    .mul(puissanceDuCoutDeNiveau(Math.max(0, place - 1)))
    .mul(facteurDeCout(etat, 'cout_place'))
}

/* ─── Contenance et blocage doux (§6.4) ─────────────────────────────────────*/

/** La contenance limite le stock, pas la production. */
export function contenance(etat: EtatJeu): Decimal {
  return etat.permanent.contenanceMana
}

/* ─── La jauge et sa saturation — GDD §2.4 ──────────────────────────────────
 *
 * « Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa
 * partie. C'est la seule pénalité du jeu, et elle est douce. »
 */

/** Part du plafond effectivement portée, de 0 à 1. */
export function partDeContenance(etat: EtatJeu): number {
  const plafond = contenance(etat)
  if (plafond.lte(0)) return 0
  return Math.min(1, etat.cycle.manaCourant.div(plafond).toNumber())
}

/**
 * L'alerte : « l'eau se trouble, la faune s'écarte. Un effet, pas un texte. »
 *
 * Le noyau rend l'état, jamais l'effet : c'est à l'écran de le montrer sans
 * l'écrire.
 */
export function eauTroublee(etat: EtatJeu): boolean {
  return partDeContenance(etat) >= SEUIL_D_ALERTE_DE_CONTENANCE
}

/** Saturation : « la captation s'arrête. Il dépense encore, il ne gagne plus. » */
export function estSature(etat: EtatJeu): boolean {
  return etat.cycle.manaCourant.gte(contenance(etat))
}

/**
 * La divergence non choisie est due : la jauge est restée pleine trop longtemps.
 *
 * Jamais « forcée » — le mot est pris au Tier 0 §2 par le cas inverse, celui
 * de la tentative délibérée qui tue. Ici le joueur n'a rien tenté, il a laissé
 * monter.
 */
export function divergenceNonChoisieEstDue(etat: EtatJeu): boolean {
  return etat.cycle.secondesEnSaturation >= DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES * 3600
}

/** Plus rien à creuser : soit la roche est finie, soit le contenu l'est. */
export function toutEstCreuse(etat: EtatJeu): boolean {
  return etat.cycle.paliersOuverts >= Math.min(etat.limiteDeContenu, NOMBRE_DE_PALIERS)
}

/**
 * Le blocage doux : le palier suivant coûte plus que ce que la contenance peut
 * porter. Le joueur peut continuer à monter des niveaux et à faire grossir sa
 * Foi ; il ne peut simplement plus descendre. C'est la raison diégétique de
 * l'éclosion, et sa seule vraie décision : partir maintenant pour la
 * profondeur, ou rester pour la Foi.
 */
export function estBloque(etat: EtatJeu): boolean {
  if (toutEstCreuse(etat)) return true
  return coutDeDescente(etat, etat.cycle.paliersOuverts).gt(contenance(etat))
}
