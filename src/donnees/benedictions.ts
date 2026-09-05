/**
 * IdlePond — registre des bénédictions.
 *
 * VIDE À CE JALON (v0.4, cf. noeuds-technique.ts).
 *
 * Deux formes et deux seulement (§6.6) : ciblée sur une espèce et
 * multiplicative, ou globale sur toutes les espèces et additive. Le choix de
 * l'additif sur le global est délibéré : il donne le croisement début/fin de
 * partie gratuitement, là où une réduction en pourcentage aurait demandé un
 * réglage fin.
 *
 * [P] P4 — les miracles sont gelés. Aucun code, aucune donnée, aucun écran.
 */
import type { Benediction } from '../noyau/types'

export const BENEDICTIONS: readonly Benediction[] = []
