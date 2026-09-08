/**
 * IdlePond — bénédictions.
 *
 * §4.3, la ligne qui ne se franchit pas : la bénédiction monte la PRODUCTION.
 * Elle ne réduit aucun coût, n'automatise rien, n'ouvre aucun verbe. Le test de
 * canon le vérifie sur le registre ; ce module ne sait appliquer que des termes
 * de production, ce qui rend la faute impossible à écrire ici.
 *
 * Deux formes et deux seulement (§6.6) :
 *   ciblée sur une espèce → multiplicative, elle récompense la spécialisation
 *   globale               → additive, elle crée le croisement début/fin de
 *                           partie sans aucun ratio à régler
 */
import Decimal from 'break_infinity.js'
import type { BenedictionId, EspeceId, EtatJeu } from './types'
import { BENEDICTIONS } from '../donnees/benedictions'

function rang(etat: EtatJeu, id: BenedictionId): number {
  return etat.permanent.benedictions[id] ?? 0
}

/** Produit des bénédictions ciblées sur cette espèce. Neutre = 1. */
export function multiplicateurCible(etat: EtatJeu, espece: EspeceId): number {
  let facteur = 1
  for (const benediction of BENEDICTIONS) {
    if (benediction.effet.forme !== 'multiplicative') continue
    if (benediction.effet.espece !== espece) continue
    const r = rang(etat, benediction.id)
    if (r > 0) facteur *= 1 + r
  }
  return facteur
}

/**
 * Somme des bénédictions globales, ajoutée au taux de base avant tout
 * multiplicateur. Additive : c'est ce qui donne le croisement gratuitement.
 */
export function apportGlobalAdditif(etat: EtatJeu): Decimal {
  let apport = new Decimal(0)
  for (const benediction of BENEDICTIONS) {
    if (benediction.effet.forme !== 'additive') continue
    const r = rang(etat, benediction.id)
    if (r > 0) apport = apport.add(r)
  }
  return apport
}
