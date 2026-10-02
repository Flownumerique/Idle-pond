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
 * croissance de cycle en temps ACTIF. Seul l'intervalle entre deux relevés
 * produit une croissance apparente en temps calendaire (RESULTATS.md,
 * finding 2). Toute cible exprimée en heures actives par cycle sera rejetée
 * ici.
 */
import Decimal from 'break_infinity.js'
import type { Espece, EtatJeu, MesureDeCycle, Reglage } from '../noyau/types'
import {
  ameliorer,
  benir,
  creuser,
  debloquer,
  eclore,
  estBloque,
  etatInitial,
  grandir,
  productionTotaleParSeconde,
  tick,
} from '../noyau/noyau'
import {
  coutDeBenediction,
  coutDeCroissance,
  coutDeDescente,
  coutDeDeblocage,
  coutDeNiveau,
  debitBeni,
  multiplicateurDeBenediction,
  multiplicateurDeSeuil,
  multiplicateurDesDrapeaux,
  multiplicateursGlobaux,
} from '../noyau/economie'
import {
  ACQUIS_MAX,
  BONUS_GLOBAL_A_CENT_INDIVIDUS,
  BONUS_PAR_NIVEAU_DU_HEROS,
  DEBIT_HEROS,
  SEUIL_DU_DRAPEAU_PERMANENT,
} from '../noyau/constantes'
import { BENEDICTIONS } from '../donnees/benedictions'
import { ESPECES } from '../donnees/especes'
import { relever, type Releve } from '../adaptateurs/telemetrie'

export interface Politique {
  /**
   * Intervalle entre deux relevés du joueur, en secondes.
   *
   * 0 = achat continu : le joueur optimal du finding 2 (RESULTATS.md), présent
   * à chaque pas. 4 h = un relevé toutes les quatre heures : le joueur relâché,
   * qui ne joue pas plus mal, mais moins souvent.
   *
   * C'est LE réglage de temps calendaire du jeu, et le seul. Entre deux relevés
   * le mana s'accumule, plafonne à la contenance, et le surplus expire vers
   * l'ambiant : revenir moins souvent coûte donc quelque chose.
   */
  readonly secondesEntreReleves: number
  /**
   * Pas d'intégration entre deux décisions, en secondes. En achat continu, c'est
   * l'intervalle entre deux relevés ; sinon, la granularité de l'absence. C'est
   * aussi ce que dure un relevé : le temps que le joueur passe devant l'écran à
   * chaque retour.
   */
  readonly pas: number
  /**
   * Part de `A∞` au-delà de laquelle rester ne rapporte plus de profondeur.
   *
   * C'est la forme opérationnelle de la seule vraie décision du joueur (§6.4).
   * L'acquis de séjour sature ; passé ce point, une heure de plus dans la même
   * vie n'achète que du Souffle, alors qu'une éclosion achète de la profondeur.
   * Le joueur optimal part. Un minuteur de patience, à sa place, ne mesurerait
   * que l'impatience du simulateur.
   */
  readonly fractionDeSaturationPourEclore: number
  /**
   * Garde-fou : un cycle qui dépasse cette durée est déclaré non convergent, et
   * la simulation s'arrête là. Le calibrage balaie des réglages dont certains
   * ne convergent pas : sans lui, il bouclerait.
   */
  readonly dureeMaxParCycleSecondes: number
}

export const POLITIQUE_PAR_DEFAUT: Politique = {
  secondesEntreReleves: 0,
  pas: 60,
  fractionDeSaturationPourEclore: 0.95,
  dureeMaxParCycleSecondes: 4000 * 3600,
}

/** Les quatre achats du noyau v1.0, chacun avec son coût et la production qu'il ajoute. */
export type Achat =
  | { readonly type: 'creuser'; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'grandir'; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'debloquer'; readonly espece: Espece; readonly cout: Decimal; readonly gain: Decimal }
  | { readonly type: 'niveau'; readonly espece: Espece; readonly cout: Decimal; readonly gain: Decimal }

/**
 * Les achats ouverts, avec leur gain marginal — la production par seconde
 * qu'ils ajoutent à l'instant où ils sont payés.
 *
 * La production est une assiette (espèces et héros) multipliée par des
 * facteurs globaux, et ces facteurs viennent de `multiplicateursGlobaux`, la
 * source que partagent la production, la production par espèce et le terme du
 * héros. Ils ne sont PAS relistés ici : une liste recopiée à la main dans le
 * simulateur avait laissé la densité hors du gain marginal et biaisé toute
 * mesure en silence (revue de qualité de la tâche 9). Un facteur ajouté là-bas
 * vaut ici sans resaisie.
 *
 *   débloquer : l'espèce entre au niveau 1, soit `débit × seuil(1) × globaux` ;
 *   niveau    : `débit × Δ(n × seuil(n)) × globaux`, et si ce niveau pose le
 *               drapeau des cent — mêmes conditions qu'`ameliorer` —, le
 *               multiplicateur des drapeaux passe de `m` à `m + 0,03` : toute
 *               la production, ce niveau compris, gagne `0,03 / m` ;
 *   creuser   : le palier ouvert change les facteurs globaux — la profondeur,
 *               et la densité du séjour si ce palier en porte plus —, donc la
 *               production entière est multipliée par leur rapport. Ce rapport
 *               se lit sur `multiplicateursGlobaux` d'un état où ce palier est
 *               ouvert, et non sur `m_p` seul, qui ignorerait la densité.
 *
 * `budget`, s'il est donné, retire de la liste les achats qu'il ne paie pas —
 * `cout > budget`, le test exact que la politique appliquerait ensuite — et
 * leur gain n'est alors pas calculé. C'est le seul endroit où ce test est
 * écrit : `meilleurAchat` lui passe le mana courant et ne le refait pas. Sans
 * budget, la liste est complète — c'est sous cette forme que le test du gain
 * marginal la confronte au noyau. Un relevé évalue jusqu'à toutes les espèces
 * pour n'en payer qu'une, et le gain vaut à lui seul la moitié du prix d'une
 * évaluation.
 *
 * Creuser n'est proposé que s'il ne bloque pas (`estBloque`, qui lit
 * `contenance`) : au-delà, il est hors de portée pour toujours, puisque le stock
 * ne peut pas monter jusque-là. Ce gain ne compte pas l'accès aux espèces que
 * le palier ouvre ; c'est la règle du harnais de RESULTATS.md, et creuser y
 * entre dans la comparaison sans traitement de faveur.
 */
export function achatsDisponibles(etat: EtatJeu, budget?: Decimal): readonly Achat[] {
  const achats: Achat[] = []
  const horsDePortee = (cout: Decimal): boolean => budget !== undefined && cout.gt(budget)

  // La production totale et les multiplicateurs globaux coûtent à eux seuls
  // plus que tout le reste de cette fonction — la première parcourt les
  // espèces, et chacune redemande les seconds. Ils ne sont tirés qu'à la
  // première DEMANDE : sous un budget serré, aucun candidat n'y arrive, et un
  // relevé qui ne peut rien payer ne paie plus pour le savoir.
  let production: Decimal | null = null
  const productionTotale = (): Decimal => (production ??= productionTotaleParSeconde(etat))
  let globaux: Decimal | null = null
  const multiplicateurs = (): Decimal => (globaux ??= multiplicateursGlobaux(etat))

  if (!estBloque(etat)) {
    const cible = etat.cycle.paliersOuverts
    const cout = coutDeDescente(etat, cible)
    if (!horsDePortee(cout)) {
      const ouvert: EtatJeu = { ...etat, cycle: { ...etat.cycle, paliersOuverts: cible + 1 } }
      achats.push({
        type: 'creuser',
        cout,
        gain: productionTotale().mul(multiplicateursGlobaux(ouvert).div(multiplicateurs()).sub(1)),
      })
    }
  }

  // Grandir — spec 2026-09-17 [D2]. Après l'achat, TOUTE la production est
  // multipliée par (1 + b), et le débit propre du héros passe de n à n + 1 :
  //   P' = (S + D·(n+1)) · M · (1 + b)  avec  P = (S + D·n) · M
  //   P' − P = P·b + D·M·(1 + b)
  // où M est `multiplicateursGlobaux` de l'état courant (héros compris).
  {
    const cout = coutDeCroissance(etat, etat.cycle.niveauDuHeros)
    if (!horsDePortee(cout)) {
      const propre = new Decimal(DEBIT_HEROS).mul(multiplicateurs()).mul(1 + BONUS_PAR_NIVEAU_DU_HEROS)
      achats.push({
        type: 'grandir',
        cout,
        gain: productionTotale().mul(BONUS_PAR_NIVEAU_DU_HEROS).add(propre),
      })
    }
  }

  for (const espece of ESPECES) {
    if (espece.palier >= etat.cycle.paliersOuverts) continue
    const vivante = etat.cycle.especes[espece.id]
    if (vivante === undefined || !vivante.debloquee) {
      const cout = coutDeDeblocage(etat, espece)
      if (horsDePortee(cout)) continue
      achats.push({
        type: 'debloquer',
        espece,
        cout,
        gain: debitBeni(etat, espece).mul(multiplicateurs()).mul(multiplicateurDeSeuil(1)).mul(multiplicateurDeBenediction(etat, espece)),
      })
      continue
    }
    const n = vivante.niveau
    const cout = coutDeNiveau(etat, espece, n)
    if (horsDePortee(cout)) continue
    const propre = debitBeni(etat, espece)
      .mul(multiplicateurs())
      .mul((n + 1) * multiplicateurDeSeuil(n + 1) - n * multiplicateurDeSeuil(n))
      .mul(multiplicateurDeBenediction(etat, espece))
    const poseLeDrapeau =
      n + 1 >= SEUIL_DU_DRAPEAU_PERMANENT && !etat.permanent.especesAyantAtteintCent.includes(espece.id)
    const gain = poseLeDrapeau
      ? propre.add(productionTotale().add(propre).mul(BONUS_GLOBAL_A_CENT_INDIVIDUS / multiplicateurDesDrapeaux(etat)))
      : propre
    achats.push({ type: 'niveau', espece, cout, gain })
  }

  return achats
}

/**
 * Parmi les achats payables, celui qui se rembourse le plus vite — `coût / gain`
 * le plus bas. C'est la règle du harnais qui a produit RESULTATS.md : le joueur
 * optimal ne met rien de côté, il achète le meilleur rapport dès qu'il le peut.
 */
function meilleurAchat(etat: EtatJeu): Achat | null {
  let meilleur: Achat | null = null
  let meilleurRetour: Decimal | null = null
  // Le mana EST le budget : `achatsDisponibles` ne rend déjà que ce qu'il paie.
  // Le test de portée n'est écrit qu'une fois, et il est écrit là-bas.
  for (const achat of achatsDisponibles(etat, etat.cycle.manaCourant)) {
    if (achat.gain.lte(0)) continue
    const retour = achat.cout.div(achat.gain)
    if (meilleurRetour === null || retour.lt(meilleurRetour)) {
      meilleur = achat
      meilleurRetour = retour
    }
  }
  return meilleur
}

function appliquer(etat: EtatJeu, achat: Achat): EtatJeu {
  if (achat.type === 'creuser') return creuser(etat)
  if (achat.type === 'grandir') return grandir(etat)
  if (achat.type === 'debloquer') return debloquer(etat, achat.espece.id)
  return ameliorer(etat, achat.espece.id)
}

/**
 * Un relevé : le joueur dépense tant qu'un achat est payable.
 *
 * La boucle termine d'elle-même — chaque achat coûte, et le coût d'un niveau
 * croît de ×1,15. La borne n'est là que contre un défaut du noyau, et elle
 * CRIE : un relevé tronqué en silence fausserait toute mesure sans le dire.
 */
function depenser(etat: EtatJeu): EtatJeu {
  let courant = etat
  for (let achats = 0; ; achats += 1) {
    if (achats > 100_000) throw new Error('Un relevé ne finit pas de dépenser : le noyau refuse-t-il un achat payable ?')
    const achat = meilleurAchat(courant)
    if (achat === null) return courant
    const suivant = appliquer(courant, achat)
    // Même symptôme que la borne ci-dessus, donc même traitement : la politique
    // a cru payable un achat que le noyau refuse. Inatteignable aujourd'hui —
    // les conditions d'`achatsDisponibles` couvrent les trois refus du noyau —,
    // et c'est justement pourquoi le fermer ne coûte rien : c'était le dernier
    // chemin par lequel une divergence politique/noyau passerait sans un mot.
    if (suivant === courant) {
      throw new Error(`Le noyau refuse un achat que la politique croyait payable : ${achat.type}`)
    }
    courant = suivant
  }
}

/**
 * Le joueur rentre-t-il dans l'œuf ?
 *
 * Deux conditions, et aucun minuteur : il n'y a plus de profondeur à prendre
 * dans cette vie, ET l'acquis de séjour a fait son travail. Rester au-delà
 * n'achète plus que du Souffle — c'est exactement l'arbitrage du §6.4, et c'est
 * le §2.B qui le rend réel en faisant saturer l'acquis.
 *
 * Une troisième condition a été RETIRÉE le 2026-09-09 : « plus aucune dépense
 * ouverte ⇒ éclore ». Elle était un terminateur sûr tant qu'une population
 * mettait des heures à rejoindre sa place ; depuis que le niveau agit à
 * l'instant où il est payé, elle tombe au bout de quelques minutes, et faisait
 * partir le joueur avant que la contenance ait rien gagné. Ne plus avoir quoi
 * acheter n'est pas une raison de partir — c'est exactement le moment où
 * rester ne rapporte plus que du Souffle et de la contenance, donc le moment
 * que le §2.B veut voir arriver.
 *
 * Exportée (tâche 11) : c'est la seule vraie décision du jeu, et elle ne doit
 * vivre qu'ICI. Une partie headless qui écrirait sa propre règle d'éclosion —
 * même équivalente en apparence — dériverait en silence le jour où l'une des
 * deux bouge sans l'autre (c'est la leçon de la tâche 9 sur les listes
 * recopiées à la main). Une réécriture ultérieure du simulateur doit
 * préserver cet export.
 */
export function doitEclore(etat: EtatJeu, politique: Politique): boolean {
  if (!estBloque(etat)) return false
  return etat.cycle.acquisDeSejour >= politique.fractionDeSaturationPourEclore * ACQUIS_MAX
}

export interface ResultatDeSimulation {
  readonly etat: EtatJeu
  readonly releve: Releve
  /** Les cycles clos, dans l'ordre : la télémétrie du noyau, telle quelle. */
  readonly cycles: readonly MesureDeCycle[]
  readonly cyclesDemandes: number
  readonly cyclesAcheves: number
  readonly cycleNonConvergent: number | null
  /**
   * Somme des relevés — le temps passé devant l'écran. Un relevé dure un pas ;
   * en achat continu les relevés se touchent et l'actif égale l'écoulé : ce
   * sont les ~38 h du finding 2, la quantité de jeu que le contenu porte.
   */
  readonly secondesActives: number
  /**
   * Temps de jeu total, plafonnements de contenance inclus — ce que vit le
   * joueur au calendrier. Les ~25 jours du finding 2, à deux relevés par jour.
   */
  readonly secondesEcoulees: number
}

/** Appelé après chaque relevé, chaque pas et chaque éclosion : c'est par là que
 * les invariants du Tier 0 se vérifient sur la durée, et non seulement à
 * l'arrivée. */
export type Observateur = (etat: EtatJeu) => void

/**
 * Ce que le joueur fait de son Souffle : il bénit, la moins chère d'abord, tant
 * qu'il peut payer. Une politique, pas une règle du noyau — la globale et les
 * ciblées ont chacune leur échelle de prix, et le simulateur n'a pas à savoir
 * laquelle rapporte le plus dans une vie qui n'a pas encore commencé.
 *
 * Appelée juste après `eclore` : c'est là que le Souffle est crédité. Elle
 * termine d'elle-même — chaque rang multiplie le prix par le ratio.
 */
export function benirAuMieux(etat: EtatJeu): EtatJeu {
  let courant = etat
  for (let garde = 0; garde < 10_000; garde += 1) {
    let choix: { readonly id: string; readonly cout: Decimal } | null = null
    for (const b of BENEDICTIONS) {
      const cout = coutDeBenediction(courant, b)
      if (cout.gt(courant.permanent.souffle)) continue
      if (choix === null || cout.lt(choix.cout)) choix = { id: b.id, cout }
    }
    if (choix === null) return courant
    const suivant = benir(courant, choix.id)
    if (suivant === courant) throw new Error(`Le noyau refuse une bénédiction que la politique croyait payable : ${choix.id}`)
    courant = suivant
  }
  throw new Error('La politique de bénédiction ne termine pas')
}

export function simuler(
  cycles: number,
  politique: Politique = POLITIQUE_PAR_DEFAUT,
  graine = 1,
  observer?: Observateur,
  limiteDeContenu?: number,
  /**
   * Le réglage de la courbe, pour le calibreur — il balaie des valeurs, et le
   * §5.1 lui interdit de muter un module pour le faire. Par défaut : le canon.
   * La tâche 13 y ajoutera `θ` et l'échelle.
   */
  reglage?: Reglage,
): ResultatDeSimulation {
  if (!(politique.pas > 0)) throw new Error(`Le pas de la politique doit être positif (reçu ${politique.pas})`)
  if (!(politique.secondesEntreReleves >= 0))
    throw new Error(`L'intervalle entre relevés ne peut pas être négatif (reçu ${politique.secondesEntreReleves})`)
  const intervalle = politique.secondesEntreReleves > 0 ? politique.secondesEntreReleves : politique.pas

  let etat = etatInitial(graine, limiteDeContenu, reglage)
  let secondesActives = 0
  let cycleNonConvergent: number | null = null
  let acheves = 0

  for (let cycle = 0; cycle < cycles; cycle += 1) {
    for (;;) {
      // ── Un relevé : le joueur est là ────────────────────────────────────────
      etat = depenser(etat)
      observer?.(etat)
      if (doitEclore(etat, politique)) break
      if (etat.cycle.dureeSecondes >= politique.dureeMaxParCycleSecondes) {
        cycleNonConvergent = cycle
        break
      }

      // ── Jusqu'au relevé suivant ─────────────────────────────────────────────
      // Le premier pas se passe devant l'écran ; le reste de l'intervalle, la
      // mare tourne sans lui — le mana plafonne à la contenance et le surplus
      // expire vers l'ambiant. En achat continu, l'intervalle EST ce premier pas.
      // Un relevé qui fait éclore se prolonge dans le premier relevé du cycle
      // suivant, au même instant : c'est une seule présence, comptée une fois.
      // `reste > 1e-9`, et non `> 0` : un intervalle qui n'est pas un multiple
      // du pas laisse un résidu flottant, et un tick de 1e-14 s n'est pas un
      // pas de simulation. La tâche 13 balaiera des politiques.
      for (let reste = intervalle, present = true; reste > 1e-9; present = false) {
        const dt = Math.min(politique.pas, reste)
        etat = tick(etat, dt)
        if (present) secondesActives += dt
        reste -= dt
        observer?.(etat)
      }
    }

    if (cycleNonConvergent !== null) break
    etat = benirAuMieux(eclore(etat))
    acheves += 1
    observer?.(etat)
  }

  return {
    etat,
    releve: relever(etat),
    cycles: etat.telemetrie.cycles,
    cyclesDemandes: cycles,
    cyclesAcheves: acheves,
    cycleNonConvergent,
    secondesActives,
    secondesEcoulees: etat.tempsJeuSecondes,
  }
}
