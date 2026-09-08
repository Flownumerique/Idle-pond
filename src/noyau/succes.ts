/**
 * IdlePond — succès : déclencheurs, effets, visibilité.
 *
 * Système de plein droit, porteur d'effets ET de la distribution narrative
 * continue. La narration est une récompense distribuée à la cadence idle, pas
 * une couche séparée (§4.2) : elle passe par ici et par le journal.
 *
 * Trois choses ne se rétrofitent pas (§8) et sont donc posées avant le
 * contenu : le typage par famille, l'état de visibilité, et le registre figé.
 *
 * Un effet est appliqué SILENCIEUSEMENT au déclenchement. Ses deux
 * contreparties sont obligatoires mais appartiennent à l'UI : une notification
 * discrète et non bloquante — une ligne qui apparaît et s'efface, jamais une
 * fenêtre — et le détail de la captation, consultable, où chaque terme actif
 * est attribuable à sa source. Le noyau rend la liste des déclenchements ;
 * l'adaptateur en fait une ligne.
 */
import type {
  AssiseId,
  CapaciteId,
  DeclencheurDeSucces,
  EntreeDeSucces,
  EtatJeu,
  Succes,
  SuccesId,
  VisibiliteDeSucces,
} from './types'
import { SATURATION_D_UN_PALIER } from './constantes'
import { palierDeVoix } from './voix'
import { SUCCES } from '../donnees/succes/index'
import { ASSISES, assiseDuPalier } from '../donnees/assises'
import { PALIERS, bancParId } from '../donnees/paliers'
import { productionTotaleParSeconde } from './economie'
import { effectifCible } from './population'

export interface ResultatDeSucces {
  readonly etat: EtatJeu
  readonly declenches: readonly SuccesId[]
}

/* ─── Déclencheurs ──────────────────────────────────────────────────────────*/

function effectifTotal(etat: EtatJeu): number {
  let total = 0
  for (const banc of Object.values(etat.cycle.bancs)) total += banc.effectif
  return total
}

/**
 * Effectif d'une espèce, tous paliers confondus.
 *
 * Les seuils du §8.1 comptent des INDIVIDUS — « 10/25/50/100 individus » — et
 * une espèce peut tenir plusieurs paliers. Compter par banc donnerait quatre
 * succès par palier au lieu de quatre par espèce, soit quarante là où le §8.4
 * en prévoit douze.
 */
function effectifDEspece(etat: EtatJeu, espece: string): number {
  let total = 0
  for (const [id, banc] of Object.entries(etat.cycle.bancs)) {
    if (bancParId(id)?.espece === espece) total += banc.effectif
  }
  return total
}

/**
 * Un palier est saturé quand tous ses bancs sont convaincus et que leur
 * effectif a rejoint sa cible. « Rejoint » à une fraction près : l'effectif
 * converge par une exponentielle et n'atteint jamais exactement sa cible — un
 * test d'égalité stricte ne se déclencherait tout simplement jamais.
 */
function palierSature(etat: EtatJeu, palier: number): boolean {
  if (palier >= etat.cycle.paliersOuverts) return false
  for (const banc of PALIERS[palier].bancs) {
    const etatDuBanc = etat.cycle.bancs[banc.id]
    if (etatDuBanc === undefined || etatDuBanc.place <= 0) return false
    if (etatDuBanc.effectif < effectifCible(etatDuBanc.place) * SATURATION_D_UN_PALIER) return false
  }
  return true
}

/** Lecture de seuil sur l'état de fin de tick. Aucun événement consommé au vol. */
export function estAtteint(etat: EtatJeu, declencheur: DeclencheurDeSucces): boolean {
  switch (declencheur.quoi) {
    case 'eclosions':
      return etat.permanent.nombreEclosions >= declencheur.seuil
    case 'paliers_ouverts':
      return etat.cycle.paliersOuverts >= declencheur.seuil
    case 'profondeur_max':
      return etat.permanent.profondeurMaxAtteinte >= declencheur.seuil
    case 'bancs_convaincus':
      return Object.values(etat.cycle.bancs).filter((b) => b.place > 0).length >= declencheur.seuil
    case 'effectif_de_banc':
      return (etat.cycle.bancs[declencheur.banc]?.effectif ?? 0) >= declencheur.seuil
    case 'effectif_d_espece':
      return effectifDEspece(etat, declencheur.espece) >= declencheur.seuil
    case 'place_de_banc':
      return (etat.cycle.bancs[declencheur.banc]?.place ?? 0) >= declencheur.seuil
    case 'effectif_total':
      return effectifTotal(etat) >= declencheur.seuil
    case 'production_par_seconde':
      return productionTotaleParSeconde(etat).gte(declencheur.seuil)
    case 'foi':
      return etat.permanent.foi.gte(declencheur.seuil)
    case 'densite_de_palier':
      return (etat.permanent.densites[declencheur.palier] ?? 0) >= declencheur.seuil
    case 'palier_sature':
      return palierSature(etat, declencheur.palier)
  }
}

/* ─── Déclenchement ─────────────────────────────────────────────────────────*/

/**
 * Relit tous les seuils et acquiert ceux qui viennent d'être franchis.
 *
 * Le parcours est un balayage du registre à chaque tick, et c'est licite : le
 * filtre du §5.2 interdit d'itérer sur une FILE d'événements, pas de relire un
 * registre figé dont la taille est connue à la compilation. Le coût ne dépend
 * ni de `dt` ni de l'histoire de la partie.
 */
export function verifierSucces(etat: EtatJeu): ResultatDeSucces {
  let declenches: SuccesId[] | null = null
  for (const succes of SUCCES) {
    if (estAcquis(etat, succes.id)) continue
    if (!estAtteint(etat, succes.declencheur)) continue
    ;(declenches ??= []).push(succes.id)
  }
  if (declenches === null) return { etat, declenches: [] }

  // Le registre de l'entrée est figé ICI, au déclenchement, et ne sera jamais
  // réécrit (§14.5). C'est le seul endroit du code où la voix courante est lue
  // pour être conservée : partout ailleurs elle se dérive.
  const entree: EntreeDeSucces = {
    obtenuAuCycle: etat.permanent.nombreEclosions,
    registre: palierDeVoix(etat),
  }

  return {
    etat: {
      ...etat,
      permanent: {
        ...etat.permanent,
        succes: enOrdreDuRegistre(etat.permanent.succes, new Set(declenches), entree),
      },
    },
    declenches,
  }
}

export function estAcquis(etat: EtatJeu, id: SuccesId): boolean {
  return etat.permanent.succes[id] !== undefined
}

/**
 * Reconstruit la table entière dans l'ordre du registre, jamais dans l'ordre
 * d'arrivée.
 *
 * Deux succès franchis pendant le même intervalle arrivent dans un ordre qui
 * dépend de la taille du pas : un pas de 8 h les voit ensemble, 480 pas de 60 s
 * les voient l'un après l'autre. L'ordre des clefs d'un objet JavaScript étant
 * celui de leur insertion, insérer à l'arrivée ferait diverger la chaîne de
 * sauvegarde de deux parties par ailleurs identiques — et le test de
 * déterminisme compare précisément cette chaîne. L'ordre du registre, lui, ne
 * dépend de rien.
 */
function enOrdreDuRegistre(
  acquis: Readonly<Record<SuccesId, EntreeDeSucces>>,
  nouveaux: ReadonlySet<SuccesId>,
  entree: EntreeDeSucces,
): Record<SuccesId, EntreeDeSucces> {
  const table: Record<SuccesId, EntreeDeSucces> = {}
  for (const succes of SUCCES) {
    const deja = acquis[succes.id]
    if (deja !== undefined) table[succes.id] = deja
    else if (nouveaux.has(succes.id)) table[succes.id] = entree
  }
  return table
}

/**
 * Enregistre l'intervalle écoulé depuis le succès précédent (§11).
 *
 * Séparé du tick, et il faut l'être : le tick doit rendre le même état pour un
 * pas de 8 h et pour 480 pas de 60 s, or l'instant précis où un seuil a été
 * franchi À L'INTÉRIEUR d'un intervalle n'est pas connaissable d'un seul pas.
 * C'est une OBSERVATION, pas une mécanique — elle appartient à celui qui
 * observe le déclenchement, à la cadence à laquelle il l'observe.
 *
 * Le jeu l'appelle à 100 ms et mesure donc l'intervalle vrai. Le crédit hors
 * ligne ne l'appelle pas : il ne peut pas prétendre savoir qu'un succès est
 * tombé il y a six heures.
 */
export function enregistrerIntervalleDeSucces(etat: EtatJeu, declenches: readonly SuccesId[]): EtatJeu {
  if (declenches.length === 0) return etat
  return {
    ...etat,
    telemetrie: {
      ...etat.telemetrie,
      intervallesEntreSucces: [
        ...etat.telemetrie.intervallesEntreSucces,
        etat.telemetrie.secondesDepuisDernierSucces,
      ],
      secondesDepuisDernierSucces: 0,
    },
  }
}

/* ─── Effets ────────────────────────────────────────────────────────────────*/

/** Capacités ouvertes par les succès. Jamais les mêmes que celles de l'arbre. */
export function capacitesDesSucces(etat: EtatJeu): ReadonlySet<CapaciteId> {
  const ouvertes = new Set<CapaciteId>()
  for (const succes of SUCCES) {
    if (succes.effet?.genre !== 'verbe') continue
    if (!estAcquis(etat, succes.id)) continue
    ouvertes.add(succes.effet.capacite)
  }
  return ouvertes
}

/* ─── Visibilité (§8.3) ─────────────────────────────────────────────────────*/

export interface SuccesAffichable {
  readonly succes: Succes
  readonly acquis: boolean
  /** Ce que le joueur a le droit de voir, une fois l'assise atteinte. */
  readonly visibilite: VisibiliteDeSucces
  /** Ce qu'on en a retenu, s'il est acquis. Porte le registre figé (§14.5). */
  readonly entree: EntreeDeSucces | null
}

/** Une assise est atteinte dès qu'un de ses paliers a été ouvert, une fois. */
export function assisesAtteintes(etat: EtatJeu): ReadonlySet<AssiseId> {
  const atteintes = new Set<AssiseId>()
  const profondeur = Math.max(etat.permanent.profondeurMaxAtteinte, etat.cycle.paliersOuverts)
  for (let palier = 0; palier < profondeur; palier += 1) {
    atteintes.add(assiseDuPalier(palier).id)
  }
  if (atteintes.size === 0) atteintes.add(ASSISES[0].id)
  return atteintes
}

/**
 * Ce que l'écran des succès a le droit de lister.
 *
 * Verrouillage par assise : un succès n'est listé, quel que soit son état, que
 * lorsque son assise est atteinte. Pas de compteur global de secrets — des
 * emplacements vides par assise, ce qui dit qu'il y a quelque chose là sans
 * dire combien il en reste ailleurs.
 *
 * Un succès acquis se montre toujours en clair : on ne cache pas au joueur ce
 * qu'il vient d'obtenir.
 */
export function succesListables(etat: EtatJeu, assise: AssiseId): readonly SuccesAffichable[] {
  if (!assisesAtteintes(etat).has(assise)) return []
  return SUCCES.filter((s) => s.assise === assise).map((succes) => {
    const entree = etat.permanent.succes[succes.id] ?? null
    const acquis = entree !== null
    return { succes, acquis, visibilite: acquis ? 'ouvert' : succes.visibilite, entree }
  })
}

/** Progression vers un seuil, pour la barre des succès ouverts. 0 à 1. */
export function progressionVersLeSucces(etat: EtatJeu, succes: Succes): number | null {
  const declencheur = succes.declencheur
  if (declencheur.quoi === 'palier_sature') return null
  const seuil = declencheur.seuil
  if (!(seuil > 0)) return null
  const courant = valeurCourante(etat, declencheur)
  if (courant === null) return null
  return Math.max(0, Math.min(1, courant / seuil))
}

function valeurCourante(etat: EtatJeu, declencheur: DeclencheurDeSucces): number | null {
  switch (declencheur.quoi) {
    case 'eclosions':
      return etat.permanent.nombreEclosions
    case 'paliers_ouverts':
      return etat.cycle.paliersOuverts
    case 'profondeur_max':
      return etat.permanent.profondeurMaxAtteinte
    case 'bancs_convaincus':
      return Object.values(etat.cycle.bancs).filter((b) => b.place > 0).length
    case 'effectif_de_banc':
      return etat.cycle.bancs[declencheur.banc]?.effectif ?? 0
    case 'effectif_d_espece':
      return effectifDEspece(etat, declencheur.espece)
    case 'place_de_banc':
      return etat.cycle.bancs[declencheur.banc]?.place ?? 0
    case 'effectif_total':
      return effectifTotal(etat)
    case 'production_par_seconde':
      return productionTotaleParSeconde(etat).toNumber()
    case 'foi':
      return etat.permanent.foi.toNumber()
    case 'densite_de_palier':
      return etat.permanent.densites[declencheur.palier] ?? 0
    case 'palier_sature':
      return null
  }
}
