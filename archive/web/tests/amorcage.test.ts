/**
 * RESULTATS.md, finding 3 : le contenu réel de finding 3 est l'état
 * DÉGÉNÉRÉ — si plus rien ne produisait après que la charge de départ
 * (`MANA_A_LA_SORTIE_DE_L_OEUF`) a été dépensée, le mana resterait à zéro
 * pour toujours et aucune espèce ne serait plus jamais débloquée.
 * `DEBIT_HEROS` (§13.3) ferme ce trou-là : le héros capte l'ambiant tout
 * seul, sans attendre aucune espèce.
 *
 * Les deux mécanismes — la charge et le débit — coexistent délibérément
 * (voir le commentaire de `MANA_A_LA_SORTIE_DE_L_OEUF` dans `constantes.ts`
 * pour la mesure qui l'a établi) : la charge tient §8.4 (premier succès
 * quasi immédiat), le débit tient finding 3 (jamais de zéro permanent). Ce
 * fichier teste les deux séparément — le second test ci-dessous ANNULE la
 * charge dans sa propre fixture pour isoler le débit, sans y toucher dans le
 * jeu réel.
 *
 * Le test « trois paliers valent D³ » du brief n'est pas repris ici : c'est un
 * doublon exact de `tests/canon.test.ts` (« le multiplicateur de palier porte
 * la part de D que le bestiaire ne porte pas »), qui verrouille déjà
 * `multiplicateurDePalier`. Ce fichier ne couvre que l'amorçage.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import { ameliorer, debloquer, etatInitial, tick } from '../src/noyau/noyau'
import { coutDeDeblocage, productionTotaleParSeconde } from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'

describe('finding 3 — la partie démarre toute seule', () => {
  it('à l’état initial, sans aucune espèce, la production est strictement positive', () => {
    expect(productionTotaleParSeconde(etatInitial(1)).gt(0)).toBe(true)
  })

  it('le débit du héros, SEUL, finance la première espèce en moins de deux heures', () => {
    // Fixture : la charge de départ est mise à zéro ICI, dans ce seul test, pour
    // isoler le mécanisme que ce test discrimine — `DEBIT_HEROS` — du mécanisme
    // qui tient normalement le premier achat, `MANA_A_LA_SORTIE_DE_L_OEUF`. Le
    // jeu réel garde les deux (`etatInitial` n'est pas touché) : voir
    // `constantes.ts` pour pourquoi ils ne sont pas interchangeables.
    let etat = etatInitial(1)
    etat = { ...etat, cycle: { ...etat.cycle, manaCourant: new Decimal(0) } }
    const premiere = ESPECES[0]

    // À t = 0 le mana est nul dans cette fixture : sans ce plancher,
    // l'assertion suivante serait vraie même si le héros ne produisait rien,
    // et le test ne prouverait rien.
    expect(etat.cycle.manaCourant.lt(coutDeDeblocage(etat, premiere))).toBe(true)

    etat = tick(etat, 2 * 3600)
    expect(etat.cycle.manaCourant.gte(coutDeDeblocage(etat, premiere))).toBe(true)

    etat = debloquer(etat, premiere.id)
    expect(etat.cycle.especes[premiere.id].debloquee).toBe(true)
  })

  it('le débit du héros devient négligeable dès la première espèce montée', () => {
    let etat = etatInitial(1)
    const heros = productionTotaleParSeconde(etat)

    etat = tick(etat, 2 * 3600)
    etat = debloquer(etat, ESPECES[0].id)

    // Le brief ne montait aucun niveau ici, et son ratio > 10 était donc
    // inatteignable à 0.05 mana/s + 0.2 mana/s (ratio 5). « Négligeable dès la
    // première espèce MONTÉE » exige d'acheter des niveaux tant que le mana le
    // permet, pas seulement de laisser filer le temps.
    for (let n = 0; n < 20; n += 1) {
      etat = tick(etat, 600)
      let suivant = ameliorer(etat, ESPECES[0].id)
      while (suivant !== etat) {
        etat = suivant
        suivant = ameliorer(etat, ESPECES[0].id)
      }
    }

    expect(productionTotaleParSeconde(etat).div(heros).gt(10)).toBe(true)
  })
})
