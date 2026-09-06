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
import { VERSION_SAVE } from '../noyau/constantes'

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
export const MIGRATIONS: Readonly<Record<number, (contenu: unknown) => unknown>> = {}

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
