/**
 * L'instantané d'un état, sous les noms du Codex — partagé par les générateurs de
 * références (`generer-references.ts`, `generer-hors-ligne.ts`). Le C# le relit
 * par `Instantane.De` ; les deux doivent rester champ pour champ identiques.
 */
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../../src/noyau/types'
import {
  contenance,
  eauTroublee,
  estBloque,
  gainDeSoufflePrevu,
  partDeContenance,
  productionTotaleParSeconde,
} from '../../src/noyau/noyau'
import { ESPECES } from '../../src/donnees/especes'
import { BENEDICTIONS } from '../../src/donnees/benedictions'
import { SUCCES } from '../../src/donnees/succes/index'

export const insufflation = (id: string) => id.replace(/^benediction-/, 'insufflation-')

/** Exact : la mantisse et l'exposant tels quels, jamais arrondis. */
export const d = (x: Decimal) => `${x.mantissa}e${x.exponent}`

export function instantane(etat: EtatJeu) {
  const c = etat.cycle
  const p = etat.permanent
  const t = etat.telemetrie
  return {
    tempsJeuSecondes: etat.tempsJeuSecondes,
    limiteDeContenu: etat.limiteDeContenu,
    prng: { graine: etat.prng.graine },
    cycle: {
      manaCourant: d(c.manaCourant),
      paliersOuverts: c.paliersOuverts,
      especes: Object.fromEntries(
        ESPECES.filter((e) => c.especes[e.id] !== undefined).map((e) => [
          e.id,
          { debloquee: c.especes[e.id].debloquee, niveau: c.especes[e.id].niveau },
        ]),
      ),
      productionPicParSeconde: d(c.productionPicParSeconde),
      dureeSecondes: c.dureeSecondes,
      acquisDeSejour: c.acquisDeSejour,
      niveauDuHeros: c.niveauDuHeros,
    },
    permanent: {
      densites: [...p.densites],
      souffle: d(p.souffle),
      contenanceMana: d(p.contenanceMana),
      couches: [...p.couches],
      profondeurMaxAtteinte: p.profondeurMaxAtteinte,
      compteursTechnique: {
        creusement: p.compteursTechnique.creusement,
        amelioration: p.compteursTechnique.amelioration,
        recrutement: p.compteursTechnique.recrutement,
        entretien: p.compteursTechnique.entretien,
        construction: p.compteursTechnique.construction,
        renaissance: p.compteursTechnique.eclosion,
      },
      noeudsTechnique: [...p.noeudsTechnique],
      succes: Object.fromEntries(
        SUCCES.filter((s) => p.succes[s.id] !== undefined).map((s) => [
          s.id,
          { obtenuAuCycle: p.succes[s.id].obtenuAuCycle, registre: p.succes[s.id].registre },
        ]),
      ),
      nombreDeRenaissances: p.nombreEclosions,
      especesAyantAtteintCent: [...p.especesAyantAtteintCent],
      manaAmbiant: d(p.manaAmbiant),
      heuresHorsLigneCreditees: p.heuresHorsLigneCreditees,
      insufflations: Object.fromEntries(
        BENEDICTIONS.filter((b) => p.benedictions[b.id] !== undefined).map((b) => [
          insufflation(b.id),
          p.benedictions[b.id],
        ]),
      ),
    },
    telemetrie: {
      cycles: t.cycles.map((m) => ({
        index: m.index,
        dureeEcouleeSecondes: m.dureeEcouleeSecondes,
        paliersOuverts: m.paliersOuverts,
        productionPicParSeconde: d(m.productionPicParSeconde),
        souffleGagne: d(m.souffleGagne),
      })),
      secondesDepuisDernierSucces: t.secondesDepuisDernierSucces,
      intervallesEntreSucces: [...t.intervallesEntreSucces],
    },
    derives: {
      production: d(productionTotaleParSeconde(etat)),
      contenance: d(contenance(etat)),
      partDeContenance: partDeContenance(etat),
      eauTroublee: eauTroublee(etat),
      estBloque: estBloque(etat),
      gainDeSoufflePrevu: d(gainDeSoufflePrevu(etat)),
    },
  }
}
