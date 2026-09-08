/**
 * IdlePond — persistance : sérialisation Decimal et versionnage de save.
 *
 * §10 : versionnage dès la v0.1, avec une chaîne de migrations même vide.
 * Rétrofiter un versionnage sur des saves existantes coûte un wipe — c'est la
 * raison d'être de ce fichier au tout premier jalon, avant qu'il y ait quoi que
 * ce soit à migrer.
 *
 * Les Decimal sont sérialisés explicitement, champ par champ. Une promenade
 * générique sur l'objet paraîtrait plus courte et retomberait sur un `any` ;
 * le mappage explicite est ce qui fait que l'aller-retour est testable.
 */
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../noyau/types'
import { NOMBRE_DE_PALIERS, VERSION_SAVE } from '../noyau/constantes'
import { PART_MURE_D_UNE_EAU_INTOUCHEE } from '../noyau/maturation'
import { palierDeVoixApres } from '../noyau/voix'
import { SUCCES } from '../donnees/succes/index'

/** Un Decimal persiste en chaîne : `toString()` fait l'aller-retour à l'exact. */
export type DecimalSerialise = string

export function serialiserDecimal(valeur: Decimal): DecimalSerialise {
  return valeur.toString()
}

export function deserialiserDecimal(valeur: unknown, repli: Decimal): Decimal {
  if (typeof valeur !== 'string' && typeof valeur !== 'number') return repli
  try {
    const decimal = new Decimal(valeur)
    return Number.isNaN(decimal.mantissa) ? repli : decimal
  } catch {
    return repli
  }
}

export interface SaveSerialisee {
  readonly versionSave: number
  readonly contenu: unknown
}

export function serialiser(etat: EtatJeu): SaveSerialisee {
  return {
    versionSave: etat.versionSave,
    contenu: {
      prng: etat.prng,
      tempsJeuSecondes: etat.tempsJeuSecondes,
      limiteDeContenu: etat.limiteDeContenu,
      cycle: {
        ...etat.cycle,
        manaCourant: serialiserDecimal(etat.cycle.manaCourant),
        productionPicParSeconde: serialiserDecimal(etat.cycle.productionPicParSeconde),
      },
      permanent: {
        ...etat.permanent,
        foi: serialiserDecimal(etat.permanent.foi),
        contenanceMana: serialiserDecimal(etat.permanent.contenanceMana),
        manaAmbiant: serialiserDecimal(etat.permanent.manaAmbiant),
      },
      telemetrie: {
        ...etat.telemetrie,
        cycles: etat.telemetrie.cycles.map((c) => ({
          ...c,
          productionPicParSeconde: serialiserDecimal(c.productionPicParSeconde),
          foiGagnee: serialiserDecimal(c.foiGagnee),
        })),
      },
    },
  }
}

/**
 * Chaîne de migrations. Une entrée par version franchie : `migrations[n]`
 * transforme un contenu de version `n` en contenu de version `n + 1`.
 * Vide aujourd'hui, et c'est le but : le mécanisme existe avant le besoin.
 */
export const MIGRATIONS: Readonly<Record<number, (contenu: unknown) => unknown>> = {
  /**
   * 1 → 2 — amendement v1.1.
   *
   * Trois choses changent sous les pieds d'une save v1 : « niveau » devient
   * « place », les espèces de la Noue prennent leur nom canonique, et l'assise
   * I passe de dix paliers à six. La dernière est une réécriture de la
   * géométrie : les index de palier d'une save v1 ne désignent plus les mêmes
   * lieux, et aucun remaniement honnête ne les y ramènerait.
   *
   * Le cycle est donc rendu, et le permanent conservé. Ce n'est pas une perte
   * arbitraire : l'éclosion fait exactement cela quinze fois par partie, `f`
   * valant 1. Une save v1 se réveille au sortir de l'œuf, avec tous ses acquis.
   */
  1: (contenu) => {
    const brut = (contenu ?? {}) as Record<string, unknown>
    const permanent = (brut.permanent ?? {}) as Record<string, unknown>
    const couches = Array.isArray(permanent.couches) ? (permanent.couches as string[]) : []
    return {
      ...brut,
      cycle: undefined,
      permanent: {
        ...permanent,
        couches: couches.map((c) => (c === 'assise-1' ? 'noue' : c)),
        especesAyantAtteintCent: [],
        // Les identifiants de succès de la Noue ont suivi ceux des espèces.
        succesDebloques: (Array.isArray(permanent.succesDebloques)
          ? (permanent.succesDebloques as string[])
          : []
        ).map((id) =>
          id
            .replace('seuil-espece-1-1-', 'seuil-vairon-')
            .replace('seuil-espece-1-2-', 'seuil-loche-')
            .replace('seuil-espece-1-3-', 'seuil-epinoche-'),
        ),
      },
    }
  },

  /**
   * 2 → 3 — le GDD devient directif.
   *
   * Deux changements sous les pieds d'une save v2 :
   *
   * `benedictions` disparaît. La Foi n'achète que des miracles (GDD §4.2) et
   * « tout arbre d'achats en Foi est une erreur de conception ». Le registre
   * n'a jamais eu de contenu : aucun joueur ne perd un rang acheté.
   *
   * `succesDebloques: string[]` devient `succes: Record<id, {obtenuAuCycle,
   * registre}>`, où `registre` fige la langue de l'entrée (§14.5).
   *
   * C'EST ICI QUE SE PAIE LE RETARD. Le §14.9 range le registre figé parmi les
   * propriétés qui « ne se rétrofitent pas », et il a raison : ni le cycle
   * d'obtention ni le palier de voix d'alors ne sont reconstituables depuis une
   * save v2, qui ne les a jamais portés. Toutes les entrées reçoivent donc le
   * palier que le nombre de franchissements de la save implique aujourd'hui —
   * faux pour les plus anciennes, et sciemment. Aucune autre valeur ne serait
   * plus vraie, et en inventer une plus flatteuse serait inventer une histoire.
   * Les saves postérieures à cette version portent la vraie.
   */
  2: (contenu) => {
    const brut = (contenu ?? {}) as Record<string, unknown>
    const permanent = (brut.permanent ?? {}) as Record<string, unknown>
    const franchissements = typeof permanent.nombreEclosions === 'number' ? permanent.nombreEclosions : 0
    const registre = palierDeVoixApres(franchissements)
    const acquis = Array.isArray(permanent.succesDebloques) ? (permanent.succesDebloques as string[]) : []

    const succes: Record<string, { obtenuAuCycle: number; registre: string }> = {}
    // Dans l'ordre du registre, comme le noyau : une save migrée et une save
    // native de même contenu doivent se sérialiser à l'identique.
    for (const s of SUCCES) {
      if (acquis.includes(s.id)) succes[s.id] = { obtenuAuCycle: franchissements, registre }
    }

    // Les deux clefs disparues sont RETIRÉES, pas mises à `undefined` : une
    // clef morte qui survit à une migration se retrouve dans la save suivante.
    const reste: Record<string, unknown> = { ...permanent, succes }
    delete reste.benedictions
    delete reste.succesDebloques
    return { ...brut, permanent: reste }
  },

  /**
   * 3 → 4 — les deux canaux de captation (GDD §3 et §3.0).
   *
   * `partsMures` entre dans l'état permanent. Une save v3 ne le porte pas, et
   * la valeur d'accueil n'est pas 0 mais 1 : une eau que rien n'a habitée est
   * mûre (§6.5). Le peuplement de la save la rediluera en quelques heures de
   * jeu, à la vitesse que `TAU_MATURATION_HEURES` fixe.
   *
   * Une migration explicite plutôt qu'un repli silencieux du désérialiseur : le
   * champ change l'économie, et un défaut qui n'apparaît nulle part est un
   * défaut que personne ne relira le jour où il faudra le remettre en cause.
   */
  3: (contenu) => {
    const brut = (contenu ?? {}) as Record<string, unknown>
    const permanent = (brut.permanent ?? {}) as Record<string, unknown>
    return {
      ...brut,
      permanent: {
        ...permanent,
        partsMures: new Array<number>(NOMBRE_DE_PALIERS).fill(PART_MURE_D_UNE_EAU_INTOUCHEE),
      },
    }
  },
}

export function migrer(save: SaveSerialisee): unknown {
  let contenu = save.contenu
  for (let version = save.versionSave; version < VERSION_SAVE; version += 1) {
    const migration = MIGRATIONS[version]
    if (migration === undefined) {
      throw new Error(`Migration de save manquante : version ${version} → ${version + 1}`)
    }
    contenu = migration(contenu)
  }
  return contenu
}

export function deserialiser(save: SaveSerialisee, repli: EtatJeu): EtatJeu {
  const brut = migrer(save) as Record<string, never>
  const cycle = (brut.cycle ?? {}) as Record<string, never>
  const permanent = (brut.permanent ?? {}) as Record<string, never>
  const telemetrie = (brut.telemetrie ?? {}) as Record<string, never>
  const cycles = (telemetrie.cycles ?? []) as unknown as Record<string, never>[]

  return {
    versionSave: VERSION_SAVE,
    prng: (brut.prng ?? repli.prng) as unknown as EtatJeu['prng'],
    tempsJeuSecondes: (brut.tempsJeuSecondes ?? repli.tempsJeuSecondes) as unknown as number,
    // Une save d'un jalon antérieur reprend la limite du jalon courant : une
    // assise livrée depuis ne doit pas rester fermée à qui jouait déjà.
    limiteDeContenu: (brut.limiteDeContenu ?? repli.limiteDeContenu) as unknown as number,
    cycle: {
      ...repli.cycle,
      ...cycle,
      manaCourant: deserialiserDecimal(cycle.manaCourant, repli.cycle.manaCourant),
      productionPicParSeconde: deserialiserDecimal(
        cycle.productionPicParSeconde,
        repli.cycle.productionPicParSeconde,
      ),
    },
    permanent: {
      ...repli.permanent,
      ...permanent,
      foi: deserialiserDecimal(permanent.foi, repli.permanent.foi),
      contenanceMana: deserialiserDecimal(permanent.contenanceMana, repli.permanent.contenanceMana),
      manaAmbiant: deserialiserDecimal(permanent.manaAmbiant, repli.permanent.manaAmbiant),
    },
    telemetrie: {
      ...repli.telemetrie,
      ...telemetrie,
      cycles: cycles.map((c) => ({
        ...(c as unknown as EtatJeu['telemetrie']['cycles'][number]),
        productionPicParSeconde: deserialiserDecimal(c.productionPicParSeconde, new Decimal(0)),
        foiGagnee: deserialiserDecimal(c.foiGagnee, new Decimal(0)),
      })),
    },
  }
}
