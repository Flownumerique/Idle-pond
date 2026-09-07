/**
 * IdlePond — production, coûts, seuils.
 *
 * Canal unique (§6.1) :
 *   production(banc) = effectif × taux_base × rendement_acclimatation
 * Le modèle à deux canaux natif/acclimaté est supprimé, le système de
 * maturation est supprimé : ni `vive`, ni `mûre`, ni `part_mûre` ici.
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
  COUT_CREUSER_AU_PALIER_1,
  COUT_DEBLOCAGE_AU_PALIER_0,
  COUT_DE_PLACE_AU_PALIER_0,
  BONUS_GLOBAL_A_CENT_INDIVIDUS,
  NOMBRE_DE_PALIERS,
  RENDEMENT_ACCLIMATATION_PLEIN_JUSQU_EN_V05,
  SEUILS_DE_JALON,
  TAUX_BASE_AU_PALIER_0,
} from './constantes'
import { assiseDuPalier } from '../donnees/assises'
import { puissanceDeD, puissanceDeG, puissanceDuCoutDeNiveau } from '../donnees/echelles'
import { PALIERS, bancsDuPalier } from '../donnees/paliers'
import { apportGlobalAdditif, multiplicateurCible } from './benedictions'
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
    .add(apportGlobalAdditif(etat))
    .mul(rendementAcclimatation(etat, banc.palier))
    .mul(multiplicateurDesDrapeaux(etat))
    .mul(multiplicateurCible(etat, banc.espece))
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

export function productionTotaleParSeconde(etat: EtatJeu): Decimal {
  let total = new Decimal(0)
  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    for (const banc of PALIERS[palier].bancs) {
      total = total.add(productionDuBanc(etat, banc))
    }
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
      terme: 'benediction_globale',
      valeur: apportGlobalAdditif(etat).toNumber(),
      source: { quoi: 'benedictions_globales' },
    },
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
    {
      terme: 'benediction_ciblee',
      valeur: multiplicateurCible(etat, banc.espece),
      source: { quoi: 'benedictions_ciblees' },
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
    if (!etat.permanent.succesDebloques.includes(succes.id)) continue
    // `part` est la fraction retirée d'un coût, ou ajoutée à un plafond.
    facteur *= effet.genre === 'reduction_cout' ? 1 - effet.part : 1 + effet.part
  }
  return facteur
}

/** Technique et succès se composent sur un même terme, chacun nommé et attribuable. */
function facteurDeCout(etat: EtatJeu, terme: TermeDeCout): number {
  return facteurDeTechnique(etat, terme) * facteurDeSucces(etat, terme)
}

/** Coût du creusement du palier `cible`. Le palier 0 est ouvert au départ. */
export function coutCreuser(etat: EtatJeu, cible: IndexPalier): Decimal {
  return puissanceDeG(Math.max(0, cible - 1))
    .mul(COUT_CREUSER_AU_PALIER_1)
    .mul(facteurDeCout(etat, 'cout_creuser'))
    .mul(facteurDeCout(etat, 'reduction_technique'))
}

/** Coût de conviction d'un banc : le recruter pour la première fois de la vie. */
export function coutDeblocage(etat: EtatJeu, banc: Banc): Decimal {
  return puissanceDeG(banc.palier)
    .mul(COUT_DEBLOCAGE_AU_PALIER_0)
    .mul(facteurDeCout(etat, 'cout_deblocage'))
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
  return coutCreuser(etat, etat.cycle.paliersOuverts).gt(contenance(etat))
}
