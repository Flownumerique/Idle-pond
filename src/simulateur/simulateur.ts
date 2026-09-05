/**
 * IdlePond — simulateur.
 *
 * Réutilise `noyau/` tel quel. C'est tout l'intérêt du contrat du §5.1 : le
 * simulateur n'a pas de moteur à lui, il appelle le même `tick` que le jeu avec
 * un `dt` plus grand. Une divergence entre les deux serait une divergence entre
 * ce qui est mesuré et ce qui est joué.
 *
 * Le simulateur porte les POLITIQUES — ce que le joueur fait et quand il
 * revient. Le noyau ne décide jamais à sa place : aucune décision de joueur ne
 * vit dans le reducer.
 *
 * §13.4, à garder en tête en lisant toute sortie d'ici : l'économie est
 * invariante d'échelle. Chaque cycle est le même problème économique à une plus
 * grande échelle, et le réglage de paramètres ne peut donc pas produire de
 * croissance de cycle en temps ACTIF. Seules les politiques de check-in
 * produisent une croissance apparente en temps calendaire. Toute cible
 * exprimée en heures actives par cycle sera rejetée ici.
 */
import Decimal from 'break_infinity.js'
import type { BancId, EtatJeu } from '../noyau/types'
import {
  contenance,
  creuser,
  convaincre,
  eclore,
  estBloque,
  etatInitial,
  monterNiveau,
  productionTotaleParSeconde,
  tick,
} from '../noyau/noyau'
import { coutCreuser, coutDeblocage, coutNiveau, toutEstCreuse } from '../noyau/economie'
import { PALIERS } from '../donnees/paliers'
import { relever, type Releve } from '../adaptateurs/telemetrie'

export interface Politique {
  /**
   * Granularité minimale d'un pas. Le simulateur avance jusqu'au prochain achat
   * plutôt qu'à cadence fixe : c'est exactement ce que le §5.2 rend licite, et
   * la raison pour laquelle le simulateur n'a pas besoin d'un moteur à lui.
   */
  readonly dtMinSecondes: number
  /**
   * Intervalle de check-in : le joueur ne revient jamais plus tard que ça.
   * §13.4 — le temps calendaire est produit par cette politique, pas par les
   * paramètres économiques ; c'est ici, et nulle part ailleurs, que se règle la
   * différence entre ~38 h de jeu actif et ~600 h calendaires.
   */
  readonly dtMaxSecondes: number
  /**
   * Patience avant l'éclosion, une fois le blocage doux atteint. Le joueur
   * reste tant qu'un achat lui coûte moins que ce délai de production — au-delà,
   * il part pour la profondeur plutôt que de rester pour la Foi.
   */
  readonly patienceSecondes: number
  /** Garde-fou : un cycle qui dépasse cette durée est déclaré non convergent. */
  readonly dureeMaxParCycleSecondes: number
}

export const POLITIQUE_PAR_DEFAUT: Politique = {
  dtMinSecondes: 60,
  dtMaxSecondes: 4 * 3600,
  patienceSecondes: 4 * 3600,
  dureeMaxParCycleSecondes: 1000 * 3600,
}

interface Option {
  readonly cout: Decimal
  readonly appliquer: (etat: EtatJeu) => EtatJeu
}

/**
 * Toutes les dépenses ATTEIGNABLES à cet instant, de la moins chère à la plus
 * chère.
 *
 * Une dépense qui coûte plus que la contenance n'est pas « chère » : elle est
 * hors de portée pour toujours, puisque le stock ne peut pas monter jusque-là.
 * C'est la même limite qui produit le blocage doux du §6.4 — et elle est douce
 * parce que le coût de creusement part 7,5 fois au-dessus du premier niveau du
 * palier le plus profond : quand la descente est fermée, il reste une
 * quinzaine de niveaux à monter avant que le stock ne ferme aussi celle-là.
 */
function optionsOuvertes(etat: EtatJeu): readonly Option[] {
  const plafond = contenance(etat)
  const options: Option[] = []

  if (!toutEstCreuse(etat)) {
    const cout = coutCreuser(etat, etat.cycle.paliersOuverts)
    if (cout.lte(plafond)) options.push({ cout, appliquer: creuser })
  }

  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    for (const banc of PALIERS[palier].bancs) {
      const niveau = etat.cycle.bancs[banc.id]?.niveau ?? 0
      const id: BancId = banc.id
      const cout = niveau === 0 ? coutDeblocage(etat, banc) : coutNiveau(etat, banc, niveau)
      if (cout.gt(plafond)) continue
      options.push({
        cout,
        appliquer: niveau === 0 ? (e) => convaincre(e, id) : (e) => monterNiveau(e, id),
      })
    }
  }

  return options
}

/** L'option la moins chère. Un balayage, pas un tri : il est refait très souvent. */
function moinsChere(options: readonly Option[]): Option | null {
  let meilleure: Option | null = null
  for (const option of options) {
    if (meilleure === null || option.cout.lt(meilleure.cout)) meilleure = option
  }
  return meilleure
}

/** Dépense tout ce qui est payable, du moins cher au plus cher. */
function depenser(etat: EtatJeu): EtatJeu {
  let courant = etat
  for (let garde = 0; garde < 2000; garde += 1) {
    const payable = moinsChere(optionsOuvertes(courant))
    if (payable === null || payable.cout.gt(courant.cycle.manaCourant)) return courant
    const suivant = payable.appliquer(courant)
    if (suivant === courant) return courant
    courant = suivant
  }
  return courant
}

/** Secondes d'attente avant que l'achat le moins cher devienne payable. */
function attenteAvantLeProchainAchat(etat: EtatJeu, options: readonly Option[]): number | null {
  const cible = moinsChere(options)
  if (cible === null) return null
  const production = productionTotaleParSeconde(etat)
  if (production.lte(0)) return null
  const manquant = cible.cout.sub(etat.cycle.manaCourant)
  if (manquant.lte(0)) return 0
  return manquant.div(production).toNumber()
}

/**
 * Le pas suivant : jusqu'au prochain achat, borné par l'intervalle de check-in.
 * L'attente est estimée à production constante alors que l'effectif monte
 * encore : elle SURESTIME donc légèrement, et le joueur simulé achète un
 * cheveu plus tard qu'un joueur parfait. C'est une politique, pas une
 * approximation du noyau — le noyau, lui, reste exact pour n'importe quel dt.
 */
function pasSuivant(etat: EtatJeu, politique: Politique, options: readonly Option[]): number {
  const attente = attenteAvantLeProchainAchat(etat, options)
  if (attente === null) return politique.dtMaxSecondes
  return Math.min(Math.max(attente, politique.dtMinSecondes), politique.dtMaxSecondes)
}

/** Le joueur part-il maintenant pour la profondeur ? Sa seule vraie décision. */
function doitEclore(etat: EtatJeu, politique: Politique, options: readonly Option[]): boolean {
  // Plus rien n'est atteignable : rester ne rapporte plus une seule Foi.
  if (options.length === 0) return true
  // Il reste de la profondeur à prendre : on ne rentre pas dans l'œuf.
  if (!estBloque(etat)) return false
  const attente = attenteAvantLeProchainAchat(etat, options)
  return attente === null || attente > politique.patienceSecondes
}

export interface ResultatDeSimulation {
  readonly etat: EtatJeu
  readonly releve: Releve
  readonly cyclesDemandes: number
  readonly cyclesAcheves: number
  readonly cycleNonConvergent: number | null
}

/** Appelé après chaque pas et après chaque éclosion : c'est par là que les
 * invariants du Tier 0 se vérifient sur la durée, et non seulement à l'arrivée. */
export type Observateur = (etat: EtatJeu) => void

export function simuler(
  cycles: number,
  politique: Politique = POLITIQUE_PAR_DEFAUT,
  graine = 1,
  observer?: Observateur,
): ResultatDeSimulation {
  let etat = etatInitial(graine)
  let cycleNonConvergent: number | null = null
  let acheves = 0

  for (let cycle = 0; cycle < cycles; cycle += 1) {
    let dureeDuCycle = 0
    etat = depenser(etat)
    for (;;) {
      const options = optionsOuvertes(etat)
      if (doitEclore(etat, politique, options)) break
      if (dureeDuCycle >= politique.dureeMaxParCycleSecondes) {
        cycleNonConvergent = cycle
        break
      }
      const pas = pasSuivant(etat, politique, options)
      etat = tick(etat, pas)
      dureeDuCycle += pas
      etat = depenser(etat)
      observer?.(etat)
    }
    if (cycleNonConvergent !== null) break
    etat = eclore(etat)
    acheves += 1
    observer?.(etat)
  }

  return {
    etat,
    releve: relever(etat),
    cyclesDemandes: cycles,
    cyclesAcheves: acheves,
    cycleNonConvergent,
  }
}
