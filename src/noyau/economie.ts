/**
 * IdlePond — production, coûts, seuils.
 *
 * UN SEUL CANAL DE REVENU — noyau v1.0 §10 : les espèces. Et une espèce est un
 * générateur avec un NIVEAU (§1.3) : on la débloque une fois, on monte son
 * niveau, l'effet est immédiat. Plus aucune population n'est simulée, donc plus
 * rien ne varie à l'intérieur d'un pas de tick.
 *
 * Les multiplicateurs qui s'ajoutent au canal sont des TermeDeFormule nommés,
 * jamais des facteurs anonymes : c'est ce qui rend le détail de captation
 * auditable (§7.5 règle 3, §8.2).
 */
import Decimal from 'break_infinity.js'
import type {
  Espece,
  EtatJeu,
  IndexPalier,
  LigneDeCaptation,
  TermeDeConfort,
  TermeDeCout,
} from './types'
import {
  COUT_CREUSER_AU_PALIER_1,
  COUT_DEBLOCAGE_RATIO,
  COUT_NIVEAU_PAR_DEBIT,
  BONUS_GLOBAL_A_CENT_INDIVIDUS,
  DEBIT_RATIO_ESPECE,
  NOMBRE_DE_PALIERS,
  SEUIL_D_ALERTE_DE_CONTENANCE,
  SEUILS_DE_JALON,
  TAUX_BASE_AU_PALIER_0,
  multiplicateurDePalier,
} from './constantes'
import { puissanceDeG, puissanceDuCoutDeNiveau } from '../donnees/echelles'
import { ESPECES } from '../donnees/especes'
import { facteurDeTechnique } from './technique'
import { SUCCES } from '../donnees/succes/index'

/* ─── Seuils de jalon ───────────────────────────────────────────────────────*/

/**
 * Multiplicateur de seuil d'une espèce, d'après son NIVEAU (§2.C).
 *
 * La table donne le multiplicateur CUMULÉ lu au seuil : on retient celui du
 * seuil le plus haut franchi, on ne multiplie pas les colonnes entre elles.
 * Le niveau cent vaut ×16, jamais ×1024 — et `D = 2.31` a été calibré contre
 * cette lecture-là.
 *
 * Il se lit sur le niveau courant, donc il tombe à l'ACHAT et se reperd à
 * l'éclosion avec le niveau. Le seul acquis qui survit est le drapeau
 * permanent, plus bas.
 */
export function multiplicateurDeSeuil(niveau: number): number {
  let multiplicateur = 1
  for (const palier of SEUILS_DE_JALON) {
    if (niveau >= palier.seuil) multiplicateur = palier.multiplicateurCumule
  }
  return multiplicateur
}

/**
 * Bonus global des espèces ayant DÉJÀ atteint le niveau cent (§2.C).
 * Définitif, conservé à l'éclosion, additif entre espèces.
 */
export function multiplicateurDesDrapeaux(etat: EtatJeu): number {
  return 1 + BONUS_GLOBAL_A_CENT_INDIVIDUS * etat.permanent.especesAyantAtteintCent.length
}

/* ─── Production ────────────────────────────────────────────────────────────*/

/**
 * Débit de base d'une espèce, mana/s par niveau.
 *
 * « Chaque espèce nouvelle a un débit de base égal à la somme de toutes les
 * précédentes : elle double donc l'assiette additive à niveaux égaux. » Le
 * reste de `D` est porté par le multiplicateur de profondeur, plus bas — le
 * bestiaire seul ne le porte pas, puisque deux paliers sur trois n'apportent
 * aucune espèce.
 */
export function debitBaseDeLEspece(espece: Espece): Decimal {
  return new Decimal(TAUX_BASE_AU_PALIER_0).mul(Math.pow(DEBIT_RATIO_ESPECE, espece.rang))
}

/**
 * Multiplicateur global accordé par la profondeur ouverte.
 *
 * Il porte la part de `D` que le bestiaire ne porte pas : sans lui, la
 * production croîtrait de 26 % par palier là où le coût croît de 140 %, et
 * l'écart se composerait jusqu'à rendre les cycles profonds interminables.
 */
export function multiplicateurDeProfondeur(etat: EtatJeu): Decimal {
  return Decimal.pow(multiplicateurDePalier(), Math.max(0, etat.cycle.paliersOuverts - 1))
}

/** Ce qu'une espèce apporte à l'assiette additive, avant les multiplicateurs globaux. */
function debitDeLEspece(etat: EtatJeu, espece: Espece): Decimal {
  const vivante = etat.cycle.especes[espece.id]
  if (vivante === undefined || !vivante.debloquee || vivante.niveau === 0) return new Decimal(0)
  return debitBaseDeLEspece(espece).mul(vivante.niveau).mul(multiplicateurDeSeuil(vivante.niveau))
}

/** Ce qu'une espèce donne réellement par seconde, tous termes nommés appliqués. */
export function productionDeLEspece(etat: EtatJeu, espece: Espece): Decimal {
  if (espece.palier >= etat.cycle.paliersOuverts) return new Decimal(0)
  return debitDeLEspece(etat, espece)
    .mul(multiplicateurDeProfondeur(etat))
    .mul(multiplicateurDesDrapeaux(etat))
}

/** La somme des espèces débloquées — noyau v1.0 §10, un seul canal. */
export function productionTotaleParSeconde(etat: EtatJeu): Decimal {
  let assiette = new Decimal(0)
  for (const espece of ESPECES) {
    if (espece.palier >= etat.cycle.paliersOuverts) continue
    assiette = assiette.add(debitDeLEspece(etat, espece))
  }
  if (assiette.lte(0)) return new Decimal(0)
  return assiette.mul(multiplicateurDeProfondeur(etat)).mul(multiplicateurDesDrapeaux(etat))
}

/**
 * Détail de la captation (§8.2) : chaque terme actif attribuable à sa source.
 * C'est la contrepartie obligatoire d'un effet appliqué silencieusement.
 */
export function detailDeCaptation(etat: EtatJeu, espece: Espece): readonly LigneDeCaptation[] {
  const niveau = etat.cycle.especes[espece.id]?.niveau ?? 0
  return [
    { terme: 'niveau', valeur: niveau, source: { quoi: 'niveau', niveau } },
    {
      terme: 'taux_base',
      valeur: debitBaseDeLEspece(espece).toNumber(),
      source: { quoi: 'palier', palier: espece.palier },
    },
    {
      terme: 'multiplicateur_jalon',
      valeur: multiplicateurDeSeuil(niveau),
      source: { quoi: 'niveau', niveau },
    },
    {
      terme: 'multiplicateur_drapeau',
      valeur: multiplicateurDesDrapeaux(etat),
      source: { quoi: 'drapeaux_permanents', especes: etat.permanent.especesAyantAtteintCent.length },
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
 * Ce que coûte de descendre d'un palier — un seul puits, noyau v1.0 §3.1.
 *
 *   coût_base(palier) × technique × succès
 *
 * `f` valait 1 depuis toujours : le noyau v1.0 §3.1 ferme [P5] et retire le
 * tarif réduit qu'un palier déjà atteint dans une vie passée payait avant le
 * 2026-09-09. Retraverser coûte exactement ce qu'un creusement neuf coûterait
 * — la profondeur maximale atteinte n'entre plus dans ce calcul.
 */
export function coutDeDescente(etat: EtatJeu, cible: IndexPalier): Decimal {
  return coutBaseDuPalier(cible)
    .mul(facteurDeTechnique(etat, 'cout_creuser'))
    .mul(facteurDeSucces(etat, 'cout_creuser'))
}

/**
 * Ce que coûte de débloquer une espèce — noyau v1.0 §1.3.
 *
 *   coût_base(palier de l'espèce) × COUT_DEBLOCAGE_RATIO
 *
 * Une fois par espèce et par vie. Le déblocage suit le coût de son palier
 * plutôt qu'une échelle à lui : c'est la profondeur où elle vit qui dit ce
 * qu'il en coûte de l'atteindre.
 *
 * La densité n'entre plus ici. Elle payait la reconviction (GDD §7.1) tant
 * qu'il y avait une population à reconvaincre ; depuis V11 elle n'a qu'un seul
 * débouché, l'acquis de séjour, et le coût de déblocage est redevenu un levier
 * ordinaire.
 */
export function coutDeDeblocage(etat: EtatJeu, espece: Espece): Decimal {
  return coutBaseDuPalier(espece.palier)
    .mul(COUT_DEBLOCAGE_RATIO)
    .mul(facteurDeCout(etat, 'cout_deblocage'))
}

/**
 * Coût du niveau suivant. Achat répétable de la boucle, ×1.15.
 *
 * Il suit le débit de base de SON espèce, donc le temps de remboursement d'un
 * niveau est le même pour la première espèce et pour la vingt et unième.
 */
export function coutDeNiveau(etat: EtatJeu, espece: Espece, niveau: number): Decimal {
  return debitBaseDeLEspece(espece)
    .mul(COUT_NIVEAU_PAR_DEBIT)
    .mul(puissanceDuCoutDeNiveau(Math.max(0, niveau)))
    .mul(facteurDeCout(etat, 'cout_niveau'))
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
