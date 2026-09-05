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
import type { Banc, EtatJeu, IndexPalier, LigneDeCaptation } from './types'
import {
  COUT_CREUSER_AU_PALIER_1,
  COUT_DEBLOCAGE_AU_PALIER_0,
  COUT_NIVEAU_AU_PALIER_0,
  LECTURE_DES_SEUILS_DE_JALON,
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

/* ─── Seuils de jalon ───────────────────────────────────────────────────────*/

/** Multiplicateur de jalon d'un banc, d'après son niveau (§6.2). */
export function multiplicateurJalon(niveau: number): number {
  let multiplicateur = 1
  for (const seuil of SEUILS_DE_JALON) {
    if (niveau < seuil.seuil) continue
    multiplicateur =
      LECTURE_DES_SEUILS_DE_JALON === 'cumule' ? multiplicateur * seuil.multiplicateur : seuil.multiplicateur
  }
  return multiplicateur
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

/** Taux d'un banc par individu et par seconde, tous termes nommés appliqués. */
export function tauxEffectifDuBanc(etat: EtatJeu, banc: Banc, niveau: number): Decimal {
  return tauxBaseDuBanc(banc)
    .add(apportGlobalAdditif(etat))
    .mul(rendementAcclimatation(etat, banc.palier))
    .mul(multiplicateurJalon(niveau))
    .mul(multiplicateurCible(etat, banc.espece))
}

export function productionDuBanc(etat: EtatJeu, banc: Banc): Decimal {
  const bancEtat = etat.cycle.bancs[banc.id]
  if (bancEtat === undefined || bancEtat.niveau <= 0) return new Decimal(0)
  return tauxEffectifDuBanc(etat, banc, bancEtat.niveau).mul(bancEtat.effectif)
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
  const niveau = bancEtat?.niveau ?? 0
  return [
    { terme: 'effectif', valeur: bancEtat?.effectif ?? 0, source: 'population' },
    { terme: 'taux_base', valeur: tauxBaseDuBanc(banc).toNumber(), source: `palier ${banc.palier}` },
    { terme: 'benediction_globale', valeur: apportGlobalAdditif(etat).toNumber(), source: 'bénédictions globales' },
    { terme: 'rendement_acclimatation', valeur: rendementAcclimatation(etat, banc.palier), source: 'acclimatation' },
    { terme: 'multiplicateur_jalon', valeur: multiplicateurJalon(niveau), source: `niveau ${niveau}` },
    { terme: 'benediction_ciblee', valeur: multiplicateurCible(etat, banc.espece), source: 'bénédictions ciblées' },
  ]
}

/* ─── Coûts ─────────────────────────────────────────────────────────────────*/

/** Coût du creusement du palier `cible`. Le palier 0 est ouvert au départ. */
export function coutCreuser(etat: EtatJeu, cible: IndexPalier): Decimal {
  return puissanceDeG(Math.max(0, cible - 1))
    .mul(COUT_CREUSER_AU_PALIER_1)
    .mul(facteurDeTechnique(etat, 'cout_creuser'))
}

/** Coût de conviction d'un banc : le recruter pour la première fois de la vie. */
export function coutDeblocage(etat: EtatJeu, banc: Banc): Decimal {
  return puissanceDeG(banc.palier)
    .mul(COUT_DEBLOCAGE_AU_PALIER_0)
    .mul(facteurDeTechnique(etat, 'cout_deblocage'))
}

/** Coût du passage de `niveau` à `niveau + 1`. Achat répétable, ×1.15. */
export function coutNiveau(etat: EtatJeu, banc: Banc, niveau: number): Decimal {
  return puissanceDeG(banc.palier)
    .mul(COUT_NIVEAU_AU_PALIER_0)
    .mul(puissanceDuCoutDeNiveau(Math.max(0, niveau - 1)))
    .mul(facteurDeTechnique(etat, 'cout_niveau'))
}

/* ─── Contenance et blocage doux (§6.4) ─────────────────────────────────────*/

/** La contenance limite le stock, pas la production. */
export function contenance(etat: EtatJeu): Decimal {
  return etat.permanent.contenanceMana
}

export function toutEstCreuse(etat: EtatJeu): boolean {
  return etat.cycle.paliersOuverts >= NOMBRE_DE_PALIERS
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
