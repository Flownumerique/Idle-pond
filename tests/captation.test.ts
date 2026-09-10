/**
 * Le détail de captation (§8.2, GDD §14.3) et l'invariant qu'il protège :
 * « aucun effet chiffré flottant, un nœud cible toujours un TermeDeFormule
 * nommé » (§7.5 règle 3).
 *
 * `multiplicateurDeProfondeur` multipliait la production sans être nommé dans
 * le registre depuis une tâche antérieure, et aucun test ne l'avait remarqué.
 * La tâche 9 ferme ce trou en même temps qu'elle ajoute un troisième
 * multiplicateur global — la densité — à la production (finding 3,
 * RESULTATS.md) : c'est l'occasion de verrouiller que production et registre
 * ne peuvent plus diverger en silence.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import {
  detailDeCaptation,
  multiplicateurDeProfondeur,
  multiplicateurDesDrapeaux,
  productionDeLEspece,
  productionTotaleParSeconde,
} from '../src/noyau/economie'
import { densiteTotale, multiplicateurDensite } from '../src/noyau/densite'
import { DEBIT_HEROS, ECHELLE_DE_PRODUCTION } from '../src/noyau/constantes'
import { ESPECES } from '../src/donnees/especes'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

describe('la production totale ne double-compte aucun multiplicateur global', () => {
  it('somme des productions par espèce, plus le débit du héros, égale le total', () => {
    // État non trivial : plusieurs paliers ouverts, plusieurs espèces à des
    // niveaux différents, des densités inégales — un état plat ne prouverait
    // rien, tous les multiplicateurs y valant 1.
    const etat = etatDeTravail()

    // L'égalité ci-dessous tient parce que `ECHELLE_DE_PRODUCTION` est neutre :
    // elle ne multiplie que le TOTAL (voir `productionTotaleParSeconde`),
    // jamais chaque espèce prise à part. Si elle cessait de valoir 1, le débit
    // du héros ci-dessous devrait la porter lui aussi pour que l'égalité tienne
    // encore — ce test le dirait.
    expect(ECHELLE_DE_PRODUCTION).toBe(1)

    const globaux = multiplicateurDeProfondeur(etat)
      .mul(multiplicateurDensite(densiteTotale(etat)))
      .mul(multiplicateurDesDrapeaux(etat))
    const termeDuHeros = new Decimal(DEBIT_HEROS).mul(globaux)

    const sommeDesEspeces = ESPECES.reduce(
      (somme, espece) => somme.add(productionDeLEspece(etat, espece)),
      new Decimal(0),
    )

    comparerAToleranceFlottante(sommeDesEspeces.add(termeDuHeros), productionTotaleParSeconde(etat))
  })
})

describe('aucun multiplicateur global ne flotte hors du registre (§7.5 règle 3)', () => {
  it('le produit des termes nommés du détail de captation égale la production réelle', () => {
    // Un test qui se contenterait de LISTER les termes d'aujourd'hui ne dirait
    // rien d'un cinquième multiplicateur ajouté en silence demain : la liste
    // contiendrait toujours les mêmes noms. Celui-ci recalcule la production à
    // partir du SEUL détail publié — le produit de ses lignes — et le compare à
    // la vraie fonction de production : tout multiplicateur ajouté à
    // `productionDeLEspece` sans ligne correspondante ici fait diverger les
    // deux, quel que soit le nom qu'il porterait.
    const etat = etatDeTravail()
    const especesOuvertes = ESPECES.filter((e) => e.palier < etat.cycle.paliersOuverts)
    expect(especesOuvertes.length).toBeGreaterThan(0)

    for (const espece of especesOuvertes) {
      const produit = detailDeCaptation(etat, espece).reduce(
        (acc, ligne) => acc.mul(ligne.valeur),
        new Decimal(1),
      )
      comparerAToleranceFlottante(produit, productionDeLEspece(etat, espece))
    }
  })
})
