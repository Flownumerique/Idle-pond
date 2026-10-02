/**
 * IdlePond — registre des nœuds de technique.
 *
 * VIDE À CE JALON, et c'est délibéré. Le §12 place technique et bénédictions au
 * jalon v0.4, « ajoutées au jeu et au simulateur en même temps, puis
 * recalibrées ensemble » ; le §16 interdit de faire du contenu avant que le
 * jalon précédent soit accepté. Le §7.3 en donne les trente nœuds : ils seront
 * transcrits ici en v0.4, avec la table `verbe → cycle d'ouverture visé` en
 * entrée du calibreur (§7.2).
 *
 * Le registre existe dès maintenant parce que les tests de canon qui le
 * parcourent — frontière technique/bénédiction du §4.3, source unique d'une
 * capacité et budget de verbes du §7.5 — doivent être en place AVANT le
 * contenu, pas ajoutés après.
 */
import type { NoeudTechnique } from '../noyau/types'

export const NOEUDS_TECHNIQUE: readonly NoeudTechnique[] = []
