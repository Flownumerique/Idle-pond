/**
 * IdlePond — éclosion.
 *
 * Le héros ENTRE dans l'œuf. Jamais « ponte », jamais « prestige », jamais
 * « rebirth » — ni ici, ni dans un identifiant, ni à l'écran (§3).
 *
 * §6.5, et rien de plus :
 *   f = 1 — reset complet du peuplement et de la géométrie, aucune fraction
 *           conservée.
 *   Conservé : densité, arbre de technique, succès, couches, contenance, et le
 *              drapeau des cent — l'unique exception.
 *   Perdu    : espèces débloquées et leurs niveaux, paliers ouverts, mana
 *              courant.
 *   Le mana expire vers l'ambiant — il n'est pas détruit (Tier 0 §5).
 */
import Decimal from 'break_infinity.js'
import type { EtatCycle, EtatJeu } from './types'
import {
  FOI_BASE,
  FOI_EXPOSANT,
  MANA_A_LA_SORTIE_DE_L_OEUF,
  NIVEAU_DU_HEROS_AU_DEPART,
  PALIERS_OUVERTS_AU_DEPART,
  PRODUCTION_DE_REFERENCE,
} from './constantes'
import { appliquerGainDeDensite } from './densite'
import { contenance } from './economie'
import { creditCompteur } from './technique'

/**
 * Gain de Foi prévu, indexé sur la production de pic du cycle.
 *
 * C'est ce que le nœud « Lire l'eau » affichera en permanence, et c'est le
 * versant « rester pour la Foi » de la seule vraie décision du joueur : la Foi
 * ne se gagne pas en attendant, elle se gagne en faisant monter le pic.
 *
 * [P] graine — le barème n'est fixé par aucun document. À réfuter en v0.3.
 */
export function gainDeFoiPrevu(etat: EtatJeu): Decimal {
  const rapport = etat.cycle.productionPicParSeconde.div(PRODUCTION_DE_REFERENCE)
  if (rapport.lte(1)) return new Decimal(0)
  return new Decimal(FOI_BASE).mul(Decimal.pow(rapport, FOI_EXPOSANT)).floor()
}

/**
 * L'état de cycle d'un départ d'œuf. Aucun acquis permanent n'y figure.
 *
 * Le mana courant part chargé — `MANA_A_LA_SORTIE_DE_L_OEUF` — et pas de
 * zéro : la tâche 9 a mesuré que `DEBIT_HEROS` seul ne peut pas tenir cette
 * place (voir son commentaire dans `constantes.ts`). Les deux mécanismes
 * coexistent délibérément.
 */
export function cycleInitial(): EtatCycle {
  return {
    manaCourant: new Decimal(MANA_A_LA_SORTIE_DE_L_OEUF),
    paliersOuverts: PALIERS_OUVERTS_AU_DEPART,
    especes: {},
    productionPicParSeconde: new Decimal(0),
    dureeSecondes: 0,
    acquisDeSejour: 0,
    niveauDuHeros: NIVEAU_DU_HEROS_AU_DEPART,
  }
}

/**
 * L'éclosion.
 *
 * Le seul geste volontaire du jeu (§10.1) : toute éclosion est choisie, et
 * tout l'acquis du cycle est fixé. Le noyau v1.0 §2.2 le pose sans détour :
 * « le blocage est doux, il peut continuer à jouer indéfiniment ». Rester
 * jauge pleine ne fait plus pondre à sa place.
 */
export function eclore(etat: EtatJeu): EtatJeu {
  const pic = etat.cycle.productionPicParSeconde
  const foiGagnee = gainDeFoiPrevu(etat)
  const densites = appliquerGainDeDensite(etat, etat.cycle.paliersOuverts, pic)

  // Le plafond ne monte QUE par séjour prolongé en mana dense (Tier 0 §8) :
  // l'acquis accumulé pendant le cycle se dépense ici, et nulle part ailleurs.
  // « Dense » n'agit plus sur l'acquis, dont le temps vaut `τ₀` constant : il
  // agit sur la production, par le multiplicateur de densité.
  // Aucun facteur n'est écrit en dur — le ×47,1 visé est un RÉSULTAT de
  // `A∞` et `τ₀`, pas une ligne de code (§2.B).
  const contenanceMana = contenance(etat)

  return {
    ...etat,
    cycle: cycleInitial(),
    permanent: {
      ...etat.permanent,
      densites,
      foi: etat.permanent.foi.add(foiGagnee),
      contenanceMana,
      profondeurMaxAtteinte: Math.max(etat.permanent.profondeurMaxAtteinte, etat.cycle.paliersOuverts),
      nombreEclosions: etat.permanent.nombreEclosions + 1,
      compteursTechnique: creditCompteur(etat.permanent.compteursTechnique, 'eclosion', 1),
      // Le mana courant expire vers l'ambiant. Aucun système d'IdlePond ne se
      // comporte comme un puits : il n'y a pas de machine non vivante ici.
      manaAmbiant: etat.permanent.manaAmbiant.add(etat.cycle.manaCourant),
    },
    telemetrie: {
      ...etat.telemetrie,
      cycles: [
        ...etat.telemetrie.cycles,
        {
          index: etat.permanent.nombreEclosions,
          dureeEcouleeSecondes: etat.cycle.dureeSecondes,
          paliersOuverts: etat.cycle.paliersOuverts,
          productionPicParSeconde: pic,
          foiGagnee,
        },
      ],
    },
  }
}
