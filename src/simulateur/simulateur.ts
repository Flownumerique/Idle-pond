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
  acheterPlace,
  productionTotaleParSeconde,
  tick,
} from '../noyau/noyau'
import {
  coutDeDescente,
  coutDeConviction,
  coutDePlace,
  tauxParIndividu,
  toutEstCreuse,
} from '../noyau/economie'
import { ACQUIS_MAX } from '../noyau/constantes'
import { PALIERS } from '../donnees/paliers'
import { relever, type Releve } from '../adaptateurs/telemetrie'

export interface Politique {
  /**
   * Intervalle entre deux retours du joueur. §5.4 : ~600 h calendaires « sous
   * check-in à 4 h ».
   *
   * C'est LE réglage de temps calendaire du jeu, et le seul. Entre deux
   * sessions le mana s'accumule, plafonne à la contenance, et le surplus expire
   * vers l'ambiant : revenir moins souvent coûte donc quelque chose, et c'est
   * ce coût qui donne un sens à la branche Entretien.
   */
  readonly intervalleDeCheckInSecondes: number
  /** Le joueur reste tant que les achats s'enchaînent sous ce délai. */
  readonly patienceDansLaSessionSecondes: number
  /** Il ne reste jamais plus longtemps que ça d'affilée. */
  readonly dureeMaxDeSessionSecondes: number
  /** Granularité minimale d'un pas. */
  readonly dtMinSecondes: number
  /**
   * Part de `A∞` au-delà de laquelle rester ne rapporte plus de profondeur.
   *
   * C'est la forme opérationnelle de la seule vraie décision du joueur (§6.4).
   * L'acquis de séjour sature ; passé ce point, une heure de plus dans la même
   * vie n'achète que de la Foi, alors qu'une éclosion achète de la profondeur.
   * Le joueur optimal part. Un minuteur de patience, à sa place, ne mesurerait
   * que l'impatience du simulateur.
   */
  readonly fractionDeSaturationPourEclore: number
  /** Garde-fou : un cycle qui dépasse cette durée est déclaré non convergent. */
  readonly dureeMaxParCycleSecondes: number
}

export const POLITIQUE_PAR_DEFAUT: Politique = {
  intervalleDeCheckInSecondes: 4 * 3600,
  patienceDansLaSessionSecondes: 90,
  dureeMaxDeSessionSecondes: 15 * 60,
  dtMinSecondes: 5,
  fractionDeSaturationPourEclore: 0.95,
  dureeMaxParCycleSecondes: 4000 * 3600,
}

interface Option {
  readonly cout: Decimal
  /** Production supplémentaire à pleine charge, une fois la place peuplée. */
  readonly gain: Decimal
  readonly estUnCreusement: boolean
  readonly appliquer: (etat: EtatJeu) => EtatJeu
}

/**
 * Toutes les dépenses ATTEIGNABLES à cet instant.
 *
 * Une dépense qui coûte plus que la contenance n'est pas « chère » : elle est
 * hors de portée pour toujours, puisque le stock ne peut pas monter jusque-là.
 * C'est la même limite qui produit le blocage doux du §6.4.
 *
 * Le `gain` est évalué à PLEINE CHARGE — la place une fois peuplée —, pas à
 * l'effectif courant : c'est ce que l'achat vaudra, et c'est sur cette valeur
 * qu'un joueur décide.
 */
function optionsOuvertes(etat: EtatJeu): readonly Option[] {
  const plafond = contenance(etat)
  const options: Option[] = []

  if (!toutEstCreuse(etat)) {
    const cout = coutDeDescente(etat, etat.cycle.paliersOuverts)
    if (cout.lte(plafond)) {
      options.push({ cout, gain: new Decimal(0), estUnCreusement: true, appliquer: creuser })
    }
  }

  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    for (const banc of PALIERS[palier].bancs) {
      const place = etat.cycle.bancs[banc.id]?.place ?? 0
      const id: BancId = banc.id
      const cout = place === 0 ? coutDeConviction(etat, banc) : coutDePlace(etat, banc, place)
      if (cout.gt(plafond)) continue
      const avant = tauxParIndividu(etat, banc, place).mul(place)
      const apres = tauxParIndividu(etat, banc, place + 1).mul(place + 1)
      options.push({
        cout,
        gain: apres.sub(avant),
        estUnCreusement: false,
        appliquer: place === 0 ? (e) => convaincre(e, id) : (e) => acheterPlace(e, id),
      })
    }
  }

  return options
}

/**
 * L'achat qu'un joueur qui vise la profondeur ferait maintenant, ou `null`.
 *
 * La règle est celle du retour sur investissement, et elle a une raison d'être
 * exactement celle-là : le cycle se termine quand on ne peut plus creuser, donc
 * la seule question qui vaille est « est-ce que cet achat me fait creuser plus
 * tôt ? ». Une place ne le fait que si elle se rembourse — `coût / gain` — avant
 * le creusement qu'on attend. Au-delà, elle retarde ce qu'elle prétend hâter.
 *
 * L'ancienne règle, « le moins cher d'abord », achetait des places dans les
 * eaux hautes parce qu'elles ne coûtaient rien, sans regarder ce qu'elles
 * rapportaient.
 */
function meilleurAchat(etat: EtatJeu, options: readonly Option[]): Option | null {
  const payables = options.filter((o) => o.cout.lte(etat.cycle.manaCourant))
  if (payables.length === 0) return null

  const creusement = options.find((o) => o.estUnCreusement)
  const production = productionTotaleParSeconde(etat)

  // Plus rien à creuser : on peuple, du meilleur rapport au moins bon.
  if (creusement === undefined || production.lte(0)) {
    return payables.reduce<Option | null>((meilleure, option) => {
      if (option.gain.lte(0)) return meilleure
      if (meilleure === null) return option
      return option.gain.div(option.cout).gt(meilleure.gain.div(meilleure.cout)) ? option : meilleure
    }, null)
  }

  const creusementPayable = payables.find((o) => o.estUnCreusement)
  const attenteAvantCreusement = Decimal.max(
    0,
    creusement.cout.sub(etat.cycle.manaCourant),
  ).div(production)

  let meilleure: Option | null = null
  let meilleurRemboursement: Decimal | null = null
  for (const option of payables) {
    if (option.estUnCreusement || option.gain.lte(0)) continue
    const remboursement = option.cout.div(option.gain)
    // Se rembourse-t-elle avant le creusement qu'on attend ?
    if (remboursement.gte(attenteAvantCreusement)) continue
    if (meilleurRemboursement === null || remboursement.lt(meilleurRemboursement)) {
      meilleure = option
      meilleurRemboursement = remboursement
    }
  }
  return meilleure ?? creusementPayable ?? null
}

/** Dépense tant qu'un achat fait gagner du temps sur le creusement suivant. */
function depenser(etat: EtatJeu): EtatJeu {
  let courant = etat
  for (let garde = 0; garde < 2000; garde += 1) {
    const achat = meilleurAchat(courant, optionsOuvertes(courant))
    if (achat === null) return courant
    const suivant = achat.appliquer(courant)
    if (suivant === courant) return courant
    courant = suivant
  }
  return courant
}

/** Secondes d'attente avant que l'achat visé devienne payable. */
function attenteAvantLeProchainAchat(etat: EtatJeu, options: readonly Option[]): number | null {
  let cible: Decimal | null = null
  for (const option of options) {
    if (cible === null || option.cout.lt(cible)) cible = option.cout
  }
  if (cible === null) return null
  const production = productionTotaleParSeconde(etat)
  if (production.lte(0)) return null
  const manquant = cible.sub(etat.cycle.manaCourant)
  if (manquant.lte(0)) return 0
  return manquant.div(production).toNumber()
}

/**
 * Le joueur rentre-t-il dans l'œuf ?
 *
 * Deux conditions, et aucun minuteur : il n'y a plus de profondeur à prendre
 * dans cette vie, ET l'acquis de séjour a fait son travail. Rester au-delà
 * n'achète plus que de la Foi — c'est exactement l'arbitrage du §6.4, et c'est
 * le §2.B qui le rend réel en faisant saturer l'acquis.
 */
function doitEclore(etat: EtatJeu, politique: Politique, options: readonly Option[]): boolean {
  if (options.length === 0) return true
  if (!estBloque(etat)) return false
  return etat.cycle.acquisDeSejour >= politique.fractionDeSaturationPourEclore * ACQUIS_MAX
}

export interface MesureDeSession {
  readonly cycle: number
  readonly secondesActives: number
}

export interface ResultatDeSimulation {
  readonly etat: EtatJeu
  readonly releve: Releve
  readonly cyclesDemandes: number
  readonly cyclesAcheves: number
  readonly cycleNonConvergent: number | null
  /** Temps où le joueur était devant l'écran. La cible du §5.4 : ~38 h. */
  readonly tempsActifSecondes: number
  /** Temps de jeu écoulé, sessions et absences confondues. Cible : ~600 h. */
  readonly tempsEcouleSecondes: number
  readonly sessions: readonly MesureDeSession[]
}

/** Appelé après chaque pas et après chaque éclosion : c'est par là que les
 * invariants du Tier 0 se vérifient sur la durée, et non seulement à l'arrivée. */
export type Observateur = (etat: EtatJeu) => void

export function simuler(
  cycles: number,
  politique: Politique = POLITIQUE_PAR_DEFAUT,
  graine = 1,
  observer?: Observateur,
  limiteDeContenu?: number,
): ResultatDeSimulation {
  let etat = etatInitial(graine, limiteDeContenu)
  let cycleNonConvergent: number | null = null
  let acheves = 0

  const sessions: MesureDeSession[] = []
  let tempsActif = 0

  for (let cycle = 0; cycle < cycles; cycle += 1) {
    let dureeDuCycle = 0

    // Le tick peut clore le cycle tout seul : la divergence non choisie du
    // §2.4 est une règle du monde, pas une décision de joueur, et elle vit donc
    // dans le noyau. Le simulateur doit s'en apercevoir — sinon il éclôt une
    // seconde fois derrière elle et compte deux cycles pour une vie.
    const eclosionsAuDebut = etat.permanent.nombreEclosions
    const aDivergeSeul = () => etat.permanent.nombreEclosions > eclosionsAuDebut

    for (;;) {
      // ── Le joueur est là ──────────────────────────────────────────────────
      let secondesDeSession = 0
      etat = depenser(etat)
      for (;;) {
        const options = optionsOuvertes(etat)
        if (doitEclore(etat, politique, options)) break
        const attente = attenteAvantLeProchainAchat(etat, options)
        if (attente === null || attente > politique.patienceDansLaSessionSecondes) break
        const reste = politique.dureeMaxDeSessionSecondes - secondesDeSession
        if (reste <= 0) break
        const pas = Math.min(Math.max(attente, politique.dtMinSecondes), reste)
        etat = depenser(tick(etat, pas))
        secondesDeSession += pas
        dureeDuCycle += pas
        observer?.(etat)
      }
      tempsActif += secondesDeSession
      sessions.push({ cycle, secondesActives: secondesDeSession })

      if (aDivergeSeul()) break
      if (doitEclore(etat, politique, optionsOuvertes(etat))) break
      if (dureeDuCycle >= politique.dureeMaxParCycleSecondes) {
        cycleNonConvergent = cycle
        break
      }

      // ── Il s'en va, et la mare tourne sans lui ────────────────────────────
      // Le mana plafonne à la contenance pendant l'absence et le surplus expire
      // vers l'ambiant : l'absence n'est pas punie, elle est simplement bornée.
      const absence = Math.max(politique.intervalleDeCheckInSecondes - secondesDeSession, politique.dtMinSecondes)
      etat = tick(etat, absence)
      dureeDuCycle += absence
      observer?.(etat)
      if (aDivergeSeul()) break
    }

    if (cycleNonConvergent !== null) break
    if (!aDivergeSeul()) etat = eclore(etat)
    acheves += 1
    observer?.(etat)
  }

  return {
    etat,
    releve: relever(etat),
    cyclesDemandes: cycles,
    cyclesAcheves: acheves,
    cycleNonConvergent,
    tempsActifSecondes: tempsActif,
    tempsEcouleSecondes: etat.tempsJeuSecondes,
    sessions,
  }
}
