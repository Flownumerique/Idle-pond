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
  detailDuHeros,
  multiplicateursGlobaux,
  productionDeLEspece,
  productionTotaleParSeconde,
} from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

describe('la production totale ne double-compte aucun multiplicateur global', () => {
  it('somme des productions par espèce, plus le débit du héros, égale le total', () => {
    // État non trivial : plusieurs paliers ouverts, plusieurs espèces à des
    // niveaux différents, des densités inégales — un état plat ne prouverait
    // rien, tous les multiplicateurs y valant 1.
    const etat = etatDeTravail()

    // Le terme du héros vient du détail PUBLIÉ, pas d'une valeur refabriquée
    // ici : une copie à la main de `DEBIT_HEROS` et des multiplicateurs
    // globaux est exactement ce qui avait laissé la densité de côté (revue de
    // qualité de la tâche 9) sans que ce test s'en aperçoive. En passant par
    // `detailDuHeros` et par `multiplicateursGlobaux` — la même fonction que
    // `productionTotaleParSeconde` utilise —, un multiplicateur global ajouté
    // demain sans y être répercuté fait diverger ce test, pas seulement la
    // production réelle.
    const ligneDebitHeros = detailDuHeros().find((ligne) => ligne.terme === 'debit_heros')
    if (ligneDebitHeros === undefined) throw new Error('le détail publié ne porte plus debit_heros')
    const termeDuHeros = new Decimal(ligneDebitHeros.valeur).mul(multiplicateursGlobaux(etat))

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
