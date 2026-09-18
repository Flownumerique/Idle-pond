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
        souffle: serialiserDecimal(etat.permanent.souffle),
        contenanceMana: serialiserDecimal(etat.permanent.contenanceMana),
        manaAmbiant: serialiserDecimal(etat.permanent.manaAmbiant),
      },
      telemetrie: {
        ...etat.telemetrie,
        cycles: etat.telemetrie.cycles.map((c) => ({
          ...c,
          productionPicParSeconde: serialiserDecimal(c.productionPicParSeconde),
          souffleGagne: serialiserDecimal(c.souffleGagne),
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
   * 3 → 4 — les deux canaux de captation (GDD §3), à l'origine.
   *
   * Cette version introduisait `partsMures` dans l'état permanent. La
   * maturation qu'il portait est retirée depuis le noyau v1.0 (2026-09-08) :
   * elle gouverne ce qu'un lieu peut DEVENIR, jamais ce que le héros GAGNE, et
   * le champ a disparu du type en mémoire avec elle.
   *
   * Le contenu n'est donc plus modifié ici. Une save v3 qui traverse cette
   * étape n'a jamais porté `partsMures` ; une save v4 antérieure à ce retrait
   * peut encore le porter, et le garde comme propriété surnuméraire jamais lue
   * — la migration ne supprime pas une clef morte, elle cesse seulement d'en
   * écrire une neuve.
   */
  3: (contenu) => contenu,

  /**
   * 4 → 5 — le noyau v1.0. La population, la maturation et le second canal
   * meurent (§8.3 : on ne SUPPRIME aucun champ). `bancs`, `partsMures`,
   * `acclimatations` et `secondesEnSaturation` restent dans la save telle
   * quelle — cette migration ne les touche pas — mais plus personne ne les
   * lit : `deserialiser` désérialise par spread générique, ils deviennent des
   * propriétés surnuméraires jamais lues.
   *
   * On ouvre `cycle.especes` à `{}` : une save d'avant le modèle à niveau n'a
   * aucun niveau d'espèce à reconstruire depuis `bancs`, qui décrivait des
   * effectifs, pas des niveaux. Le reste de `cycle` (mana courant, paliers
   * ouverts, durée) n'est pas touché. Ce n'est pas une perte de progression :
   * les niveaux d'espèces sont scopés au cycle et se remettent à zéro à
   * chaque éclosion de toute façon (f = 1, reset complet) ; la contenance, la
   * densité, la Foi et la technique vivent dans `permanent`, que cette
   * migration ne modifie pas non plus.
   */
  4: (contenu) => {
    const etat = contenu as Record<string, Record<string, unknown>>
    return {
      ...etat,
      cycle: { ...etat.cycle, especes: {} },
    }
  },

  /**
   * 5 → 6 — la mesure de redescente meurt.
   *
   * `telemetrie.secondesEnRedescente` et sa copie dans chaque cycle clos
   * (`telemetrie.cycles[].secondesEnRedescente`) relevaient le risque « la
   * redescente devient le jeu » du GDD §16.4, que `docs/PRESEANCE.md` déclare
   * dépassé par le noyau v1.0 (`f = 1`, retraverser coûte plein tarif, comme
   * dans n'importe quel idle). Mesurée une dernière fois avant son retrait :
   * ~0,01 % du temps d'un cycle pour le joueur optimal.
   *
   * Les deux champs sortent du type en mémoire ; cette migration ne les touche
   * pas (§8.3 : on ne SUPPRIME aucun champ). `deserialiser` désérialise par
   * spread générique : ils deviennent des propriétés surnuméraires jamais lues.
   * Rien n'est à reconstruire — aucun champ neuf n'entre dans l'état.
   */
  5: (contenu) => contenu,

  /**
   * 6 → 7 — l'axe héros et les bénédictions (spec 2026-09-17).
   *
   * Deux champs NEUFS, aucun retiré : `cycle.niveauDuHeros` part à 1 — une
   * save en cours de vie reprend avec un héros qui n'a pas encore grandi, ce
   * qui est vrai — et `permanent.benedictions` part vide. Le spread de
   * `deserialiser` les comblerait depuis le repli, mais une sémantique nouvelle
   * exige son incrément de version (noyau v1.0 §8.3), et l'écrire ici rend
   * l'intention lisible dans la chaîne.
   */
  6: (contenu) => {
    const brut = (contenu ?? {}) as Record<string, unknown>
    const cycle = (brut.cycle ?? {}) as Record<string, unknown>
    const permanent = (brut.permanent ?? {}) as Record<string, unknown>
    return {
      ...brut,
      cycle: { ...cycle, niveauDuHeros: 1 },
      permanent: { ...permanent, benedictions: {} },
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
    // JAMAIS lu de la save (R41) : un réglage appartient à la version du jeu,
    // pas à la partie. `serialiser` ne l'écrit pas ; s'il traînait dans un
    // vieux fichier, on l'ignorerait quand même.
    reglage: repli.reglage,
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
      souffle: deserialiserDecimal(permanent.souffle, repli.permanent.souffle),
      contenanceMana: deserialiserDecimal(permanent.contenanceMana, repli.permanent.contenanceMana),
      manaAmbiant: deserialiserDecimal(permanent.manaAmbiant, repli.permanent.manaAmbiant),
    },
    telemetrie: {
      ...repli.telemetrie,
      ...telemetrie,
      cycles: cycles.map((c) => ({
        ...(c as unknown as EtatJeu['telemetrie']['cycles'][number]),
        productionPicParSeconde: deserialiserDecimal(c.productionPicParSeconde, new Decimal(0)),
        souffleGagne: deserialiserDecimal(c.souffleGagne, new Decimal(0)),
      })),
    },
  }
}
