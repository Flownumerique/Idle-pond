/**
 * Critère d'acceptation du jalon v0.1 :
 * « le simulateur tourne 15 cycles sans jouer, et le test d'équivalence de pas
 * passe. »
 *
 * Le simulateur n'a pas de moteur à lui : il appelle le même `tick` que le jeu
 * avec un `dt` plus grand. Ce test vérifie donc deux choses à la fois — que
 * quinze cycles s'enchaînent, et que les invariants du Tier 0 tiennent sur
 * toute la durée, pas seulement à l'arrivée.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { Espece, EtatJeu } from '../src/noyau/types'
import { ACQUIS_MAX, CONTENANCE_INITIALE, NOMBRE_D_ECLOSIONS_VISE } from '../src/noyau/constantes'
import { achatsDisponibles, POLITIQUE_PAR_DEFAUT, simuler, type Achat } from '../src/simulateur/simulateur'
import { ameliorer, creuser, debloquer, estBloque, grandir, productionTotaleParSeconde } from '../src/noyau/noyau'
import { densiteDuSejour } from '../src/noyau/densite'
import { PALIERS_LIVRES } from '../src/donnees/assises'
import { ESPECES } from '../src/donnees/especes'
import { etatDeTravail } from './etat-de-travail'

/**
 * Un état où les TROIS achats sont ouverts en même temps, choisi pour que
 * chaque approximation connue échoue : densités inégales par palier, donc
 * ouvrir un palier change la densité du séjour (0,78 → 0,91) et un gain de
 * creusement en `prod × (m_p − 1)` est faux ; une espèce déjà drapée, donc le
 * drapeau suivant vaut `0,03 / 1,03` et non `0,03` ; une espèce à 99, non
 * drapée, pour que ce drapeau soit en jeu ; et une espèce ouverte non
 * débloquée, sans quoi la branche « débloquer » n'est jamais prise.
 */
function etatAuxTroisAchats(): { etat: EtatJeu; aCent: Espece; aDebloquer: Espece } {
  const base = etatDeTravail()
  const ouvertes = ESPECES.filter((e) => e.palier < base.cycle.paliersOuverts)
  expect(ouvertes.length).toBeGreaterThanOrEqual(3)
  const [, aCent, aDebloquer] = ouvertes
  expect(base.permanent.especesAyantAtteintCent).not.toContain(aCent.id)
  // Le drapeau déjà posé vient d'`etatDeTravail`. S'il disparaissait de cette
  // fixture partagée, `multiplicateurDesDrapeaux` retomberait à 1 et le terme
  // `0,03 / m` cesserait d'être discriminé, sans qu'aucun test ne le dise.
  expect(base.permanent.especesAyantAtteintCent.length).toBeGreaterThan(0)
  const especes = Object.fromEntries(
    Object.entries(base.cycle.especes).filter(([id]) => id !== aDebloquer.id),
  )
  const etat: EtatJeu = {
    ...base,
    cycle: { ...base.cycle, especes: { ...especes, [aCent.id]: { debloquee: true, niveau: 99 } } },
  }
  expect(densiteDuSejour({ ...etat, cycle: { ...etat.cycle, paliersOuverts: etat.cycle.paliersOuverts + 1 } }))
    .toBeGreaterThan(densiteDuSejour(etat))
  return { etat, aCent, aDebloquer }
}

describe('simulateur', () => {
  it('une partie sans UI atteint l’éclosion 2 en headless', () => {
    // Critère d'acceptation du §6 de l'amendement v1.1. Il porte sur le monde
    // LIVRÉ — la Noue et ses six paliers —, pas sur les 62 que le simulateur
    // mesure : c'est le jeu qu'un joueur touche qui doit boucler.
    const partie = simuler(2, undefined, 1, undefined, PALIERS_LIVRES)
    expect(partie.cycleNonConvergent).toBeNull()
    expect(partie.etat.permanent.nombreEclosions).toBe(2)
    expect(partie.etat.permanent.contenanceMana.gt(CONTENANCE_INITIALE)).toBe(true)
    expect(partie.etat.permanent.densites[0]).toBeGreaterThan(0)
  })

  it(`enchaîne ${NOMBRE_D_ECLOSIONS_VISE} cycles sans jouer`, () => {
    const resultat = simuler(NOMBRE_D_ECLOSIONS_VISE)
    expect(resultat.cycleNonConvergent, 'un cycle n’a pas convergé').toBeNull()
    expect(resultat.cyclesAcheves).toBe(NOMBRE_D_ECLOSIONS_VISE)
    expect(resultat.releve.cycles.length).toBe(NOMBRE_D_ECLOSIONS_VISE)
    for (const cycle of resultat.releve.cycles) {
      expect(cycle.dureeEcouleeSecondes).toBeGreaterThan(0)
      expect(cycle.paliersOuverts).toBeGreaterThan(0)
    }
  })

  it('sépare le temps ACTIF du temps ÉCOULÉ', () => {
    // §11 : « durée de cycle, active et calendaire », et « intervalle réel
    // entre deux sessions | distingue temps actif et temps calendaire ».
    // Les confondre fait lire les ~600 h calendaires du §5.4 comme si c'étaient
    // les ~38 h actives — l'erreur exacte que les jalons v0.1 et v0.2 ont
    // rapportée deux fois.
    //
    // Un relevé tient le joueur devant l'écran le temps d'un pas. En achat
    // continu les relevés se touchent : il est là tout du long, et l'actif
    // ÉGALE l'écoulé — c'est le joueur optimal du finding 2. Relâché, il n'est
    // là qu'un pas par relevé : l'actif vaut exactement `pas / intervalle` de
    // l'écoulé. Un compteur qui recopierait l'autre tomberait sur la seconde
    // assertion ; un compteur resté à zéro, sur la première.
    const continu = simuler(3)
    expect(continu.secondesActives).toBeGreaterThan(0)
    expect(continu.secondesActives / continu.secondesEcoulees).toBeCloseTo(1, 9)

    const releves = { ...POLITIQUE_PAR_DEFAUT, secondesEntreReleves: 4 * 3600 }
    const relache = simuler(3, releves)
    expect(relache.secondesActives / relache.secondesEcoulees).toBeCloseTo(
      releves.pas / releves.secondesEntreReleves,
      9,
    )
  })

  it('l’intervalle entre deux relevés est le seul réglage de temps calendaire', () => {
    // §5.4 : « aucun réglage de paramètre ne produira de croissance de cycle en
    // temps actif — seules les politiques ». Le vérifier plutôt que d'y croire :
    // doubler l'absence doit à peu près doubler le calendaire, et laisser
    // l'actif tranquille.
    const court = simuler(6, { ...POLITIQUE_PAR_DEFAUT, secondesEntreReleves: 2 * 3600 })
    const long = simuler(6, { ...POLITIQUE_PAR_DEFAUT, secondesEntreReleves: 8 * 3600 })
    expect(long.secondesEcoulees).toBeGreaterThan(court.secondesEcoulees * 2)
    const ecartActif = Math.abs(long.secondesActives / court.secondesActives - 1)
    expect(ecartActif).toBeLessThan(0.5)
  })

  it('le joueur rentre dans l’œuf sur la saturation, pas sur un minuteur', () => {
    // La seule vraie décision du §6.4, rendue réelle par l'acquis saturant du
    // §2.B : à chaque éclosion, l'acquis doit avoir fait son travail.
    let cycles = 0
    simuler(4, undefined, 1, (etat) => {
      if (etat.permanent.nombreEclosions === cycles) return
      cycles = etat.permanent.nombreEclosions
      // Juste après l'éclosion l'acquis est remis à zéro ; ce qui compte est
      // que la contenance ait bien été multipliée par un acquis saturé.
      expect(etat.cycle.acquisDeSejour).toBe(0)
    })
    const partie = simuler(4)
    const attendu = Math.pow(1 + ACQUIS_MAX * POLITIQUE_PAR_DEFAUT.fractionDeSaturationPourEclore, 4)
    expect(partie.etat.permanent.contenanceMana.div(CONTENANCE_INITIALE).toNumber()).toBeGreaterThan(attendu)
  })

  it('la densité ne redescend jamais (Tier 0)', () => {
    // La comparaison est faite à la main et `expect` n'est appelé qu'à
    // l'arrivée : l'observateur passe des dizaines de milliers de fois sur 62
    // paliers, et un `expect` par palier coûtait six des quatorze secondes du
    // test — du temps de harnais, pas de mesure. La discrimination est la même,
    // la première violation est retenue avec son palier et ses deux valeurs.
    let precedentes: readonly number[] | null = null
    let faute: string | null = null
    const verifier = (etat: EtatJeu) => {
      const densites = etat.permanent.densites
      if (precedentes !== null && faute === null) {
        for (let palier = 0; palier < densites.length; palier += 1) {
          if (densites[palier] < precedentes[palier]) {
            faute = `densité du palier ${palier} : ${precedentes[palier]} → ${densites[palier]}`
            break
          }
        }
      }
      precedentes = densites
    }
    simuler(NOMBRE_D_ECLOSIONS_VISE, undefined, 1, verifier)
    expect(faute, 'la densité a reculé').toBeNull()
    expect(precedentes).not.toBeNull()
    expect(precedentes!.some((d) => d > 0)).toBe(true)
  })

  it('les acquis permanents ne se reperdent jamais', () => {
    let eclosions = 0
    let contenance = 0
    let foi = 0
    let compteurs = 0
    simuler(NOMBRE_D_ECLOSIONS_VISE, undefined, 1, (etat) => {
      // Un être surévolué conserve ses acquis à vie.
      expect(etat.permanent.nombreEclosions).toBeGreaterThanOrEqual(eclosions)
      expect(etat.permanent.contenanceMana.toNumber()).toBeGreaterThanOrEqual(contenance)
      expect(etat.permanent.foi.toNumber()).toBeGreaterThanOrEqual(foi)
      const somme = Object.values(etat.permanent.compteursTechnique).reduce((a, b) => a + b, 0)
      expect(somme, 'un compteur de technique a reculé : on ne désapprend pas').toBeGreaterThanOrEqual(compteurs)
      eclosions = etat.permanent.nombreEclosions
      contenance = etat.permanent.contenanceMana.toNumber()
      foi = etat.permanent.foi.toNumber()
      compteurs = somme
    })
    expect(eclosions).toBe(NOMBRE_D_ECLOSIONS_VISE)
  })

  it('l’éclosion emporte le peuplement et la géométrie, et rien d’autre', () => {
    const resultat = simuler(2)
    expect(resultat.etat.cycle.paliersOuverts).toBe(1)
    expect(Object.keys(resultat.etat.cycle.especes)).toEqual([])
    expect(resultat.etat.permanent.profondeurMaxAtteinte).toBeGreaterThan(1)
    // Le mana courant expire vers l'ambiant. Il n'est pas détruit : aucun
    // système d'IdlePond ne se comporte comme un puits.
    expect(resultat.etat.permanent.manaAmbiant.gt(0)).toBe(true)
  })

  it('la progression descend réellement d’un cycle à l’autre', () => {
    const resultat = simuler(NOMBRE_D_ECLOSIONS_VISE)
    const premier = resultat.releve.cycles[0]
    const dernier = resultat.releve.cycles[resultat.releve.cycles.length - 1]
    expect(dernier.paliersOuverts).toBeGreaterThan(premier.paliersOuverts)
  })
})

describe('le simulateur tourne sur le noyau v1.0', () => {
  it('quinze éclosions, et le temps actif est distinct du temps écoulé', () => {
    const r = simuler(15, POLITIQUE_PAR_DEFAUT, 1)
    expect(r.cycles).toHaveLength(15)
    expect(r.secondesActives).toBeGreaterThan(0)
    expect(r.secondesEcoulees).toBeGreaterThanOrEqual(r.secondesActives)
  })

  it('le premier cycle dure environ trois heures', () => {
    const r = simuler(15, POLITIQUE_PAR_DEFAUT, 1)
    const h = r.cycles[0].dureeEcouleeSecondes / 3600
    expect(h).toBeGreaterThan(1)
    expect(h).toBeLessThan(8)
  })

  it('la politique optimale et la politique relâchée diffèrent (finding 2)', () => {
    // Le seuil tenait un ×2 sur une absence de 4 h tant qu'un cycle durait
    // 2,62 h : l'absence dépassait le cycle, et chaque cycle payait un
    // intervalle entier. Sous la courbe v1.2 le cycle passe à 8,9 h dès le
    // quinzième, donc une absence de 4 h y tient DEDANS et coûte relativement
    // moins. Mesuré sur 13 cycles, graine 1 — optimale 67,9 h ; relâchée à
    // 4 h 104,0 h (×1,532), à 8 h 184,0 h (×2,711), à 24 h 504,0 h (×7,425).
    //
    // Le contenu du finding 2 n'est pas le ×2, c'est que l'intervalle de relevé
    // est le SEUL réglage qui gonfle le temps calendaire, et qu'il le gonfle
    // d'autant plus qu'il est long. Le test dit maintenant cela, et le dit sur
    // deux points au lieu d'un : un compteur qui ignorerait l'intervalle rend
    // les trois valeurs égales et tombe sur les deux assertions.
    const optimale = simuler(13, POLITIQUE_PAR_DEFAUT, 1)
    const ecoule = (heures: number) =>
      simuler(13, { ...POLITIQUE_PAR_DEFAUT, secondesEntreReleves: heures * 3600 }, 1).secondesEcoulees
    const a4 = ecoule(4)
    const a24 = ecoule(24)
    expect(a4, 'une absence de 4 h coûte du temps calendaire').toBeGreaterThan(
      optimale.secondesEcoulees * 1.3,
    )
    expect(a24, 'et six fois plus d’absence en coûte davantage').toBeGreaterThan(a4 * 2)
  })

  it('le gain de chaque achat est la production qu’il ajoute réellement', () => {
    // L'outil qui mesure doit le moins pouvoir mentir : à la tâche 9, une liste
    // de multiplicateurs recopiée à la main dans le simulateur a oublié la
    // densité et biaisé toute mesure, en silence. Ici le gain analytique est
    // confronté au noyau lui-même — la production APRÈS l'achat, moins la
    // production avant —, pour les quatre achats.
    const { etat, aCent } = etatAuxTroisAchats()

    const appliquer = (achat: Achat): EtatJeu => {
      if (achat.type === 'creuser') return creuser(etat)
      if (achat.type === 'grandir') return grandir(etat)
      if (achat.type === 'debloquer') return debloquer(etat, achat.espece.id)
      return ameliorer(etat, achat.espece.id)
    }
    const avant = productionTotaleParSeconde(etat)
    const achats = achatsDisponibles(etat)
    expect(new Set(achats.map((a) => a.type))).toEqual(new Set(['creuser', 'grandir', 'debloquer', 'niveau']))
    expect(achats.some((a) => a.type === 'niveau' && a.espece.id === aCent.id)).toBe(true)
    for (const achat of achats) {
      const apres = appliquer(achat)
      expect(apres, `${achat.type} n’a pas été payé`).not.toBe(etat)
      const reel = productionTotaleParSeconde(apres).sub(avant)
      const libelle = achat.type === 'creuser' || achat.type === 'grandir' ? achat.type : `${achat.type} ${achat.espece.id}`
      expect(achat.gain.div(reel).toNumber(), libelle).toBeCloseTo(1, 9)
    }
  })

  it('le joueur optimal fait grandir le héros, à peu près une fois par palier', () => {
    // Spec [D3] : le coût suit g comme le palier, donc le rapport coût/gain des
    // deux achats reste comparable tout le long. On ne demande pas l'égalité —
    // le gain d'un palier vaut (m_p − 1), celui d'un niveau vaut b — mais un
    // héros laissé au niveau 1 signifierait que l'achat n'est jamais rentable,
    // et le rebudget de D serait faux.
    //
    // Le niveau du héros se reperd à chaque éclosion, mais `paliersOuverts`
    // croît cycle après cycle (10 → 15 → 19 sur trois cycles, mesuré) : le pic
    // de `niveauMax` sur plusieurs cycles est donc atteint dans le DERNIER
    // cycle simulé, jamais dans le premier. Comparer contre le premier
    // sous-estimait la référence et rendait le test infaisable — corrigé,
    // task A5, sur ruling du contrôleur.
    let niveauMax = 0
    const resultat = simuler(3, undefined, 1, (etat) => {
      niveauMax = Math.max(niveauMax, etat.cycle.niveauDuHeros)
    })
    const paliersDuDernierCycle = resultat.cycles[resultat.cycles.length - 1].paliersOuverts
    expect(niveauMax).toBeGreaterThanOrEqual(Math.floor(paliersDuDernierCycle / 2))
    expect(niveauMax).toBeLessThanOrEqual(paliersDuDernierCycle + 2)
  })

  it('la saturation ne gèle pas la partie (amendement v1.3)', () => {
    // LE test de genre : IdlePond est un idle incremental, pas un minuteur.
    //
    // Avec un plafond de contenance gelé jusqu'à l'éclosion, une partie mesurée
    // donnait ceci — le dernier palier d'un cycle s'ouvrait à la PREMIÈRE
    // MINUTE, puis 99 % du cycle ne voyait plus ni palier, ni espèce, ni même
    // de production (×1,1 sur 8,9 h au cycle 15). Le joueur regardait un
    // minuteur : le palier suivant coûtait plus que ce qu'il pouvait PORTER, et
    // rien dans la vie courante ne pouvait plus changer cela.
    //
    // Deux quantités le disent, et ce sont les deux que le plafond continu
    // rétablit. Mesuré sur les six premiers cycles, graine 1 :
    //
    //   part du cycle au dernier palier ouvert : 0,166 · 0,411 · 0,225 · 0,629 ·
    //     0,304 · 0,184   (plafond gelé : 0,12 puis 0,005 à 0,01)
    //   production gagnée après le premier blocage : ×36 · ×203 · ×449 · ×170 ·
    //     ×254 · ×131      (plafond gelé : ×8,4 · ×20 · ×7 · ×9 · ×1,1)
    //
    // Les bornes sont posées sous le pire cycle mesuré, pas sur la moyenne :
    // un seul cycle gelé est un cycle où le joueur n'a rien à faire.
    interface Suivi {
      duree: number
      dernierPalier: number
      paliers: number
      prodAuBlocage: number
      prodFin: number
    }
    const neuf = (paliers: number): Suivi => ({
      duree: 0,
      dernierPalier: 0,
      paliers,
      prodAuBlocage: 0,
      prodFin: 0,
    })
    const cycles = new Map<number, Suivi>()
    simuler(6, undefined, 1, (etat) => {
      const index = etat.permanent.nombreEclosions
      let suivi = cycles.get(index)
      if (suivi === undefined) {
        suivi = neuf(etat.cycle.paliersOuverts)
        cycles.set(index, suivi)
      }
      if (etat.cycle.paliersOuverts > suivi.paliers) {
        suivi.paliers = etat.cycle.paliersOuverts
        suivi.dernierPalier = etat.cycle.dureeSecondes
      }
      suivi.duree = Math.max(suivi.duree, etat.cycle.dureeSecondes)
      const production = productionTotaleParSeconde(etat).toNumber()
      if (suivi.prodAuBlocage === 0 && estBloque(etat)) suivi.prodAuBlocage = production
      suivi.prodFin = production
    })

    expect(cycles.size).toBeGreaterThanOrEqual(6)
    for (const [index, suivi] of cycles) {
      if (index >= 6) continue
      const part = suivi.dernierPalier / suivi.duree
      expect(part, `cycle ${index + 1} : le dernier palier s’ouvre à ${(part * 100).toFixed(1)} % du cycle`)
        .toBeGreaterThan(0.1)
      const gagnee = suivi.prodFin / suivi.prodAuBlocage
      expect(gagnee, `cycle ${index + 1} : la production ne gagne que ×${gagnee.toFixed(1)} après le blocage`)
        .toBeGreaterThan(25)
    }
  })

  it('la croissance du séjour est le bouton de la FORME de la courbe', () => {
    // La contrepartie mesurable de l'amendement v1.2, et ce qui donne à la
    // tâche 13 une bissection qui a prise : à `τ` constant, tous les cycles
    // durent `τ₀ ln 20` et le rapport `dernier / premier` vaut 1 quoi qu'on
    // règle ailleurs — c'est ce que la tâche 12 a mesuré sur 45 cycles.
    const rapport = (croissance: number) => {
      const r = simuler(6, undefined, 1, undefined, undefined, {
        croissanceDuSejourParPalier: croissance,
      })
      const durees = r.cycles.map((c) => c.dureeEcouleeSecondes)
      expect(durees.length).toBe(6)
      return durees[durees.length - 1] / durees[0]
    }
    expect(rapport(1), 'à croissance 1, la courbe est plate — la loi d’avant').toBeCloseTo(1, 3)
    expect(rapport(1.05), 'à croissance 1,05, les cycles s’allongent').toBeGreaterThan(2)
  })

  it('le budget retire les achats hors de portée, et rien d’autre', () => {
    // Le chemin chaud passe un budget pour ne pas calculer le gain de ce qu'il
    // ne peut pas payer — une décision d'achat évalue toutes les espèces
    // ouvertes et n'en paie qu'une. C'est une optimisation, donc une occasion
    // de diverger en silence de la liste complète : la famille de défaut qui a
    // mordu la tâche 9. Ce test attache l'une à l'autre.
    const { etat } = etatAuxTroisAchats()
    const libelle = (achat: Achat) =>
      achat.type === 'creuser' || achat.type === 'grandir' ? achat.type : `${achat.type} ${achat.espece.id}`
    const complets = achatsDisponibles(etat)
    expect(new Set(complets.map((a) => a.type))).toEqual(new Set(['creuser', 'grandir', 'debloquer', 'niveau']))

    // Chaque coût de la liste sert à son tour de budget : toutes les coupes
    // sont éprouvées, et chacune sur sa propre valeur — un budget qui vaut
    // exactement un coût doit PAYER cet achat, jamais le retirer.
    for (const coupe of complets) {
      const attendus = complets.filter((achat) => !achat.cout.gt(coupe.cout))
      expect(attendus.map(libelle)).toContain(libelle(coupe))
      const restreints = achatsDisponibles(etat, coupe.cout)
      expect(restreints.map(libelle), `budget du ${libelle(coupe)}`).toEqual(attendus.map(libelle))
      restreints.forEach((achat, rang) => {
        expect(achat.cout.eq(attendus[rang].cout), `coût de ${libelle(achat)}`).toBe(true)
        expect(achat.gain.eq(attendus[rang].gain), `gain de ${libelle(achat)}`).toBe(true)
      })
    }

    // Un budget que rien ne paie rend une liste vide — et n'a pas eu besoin de
    // la production totale pour le dire.
    expect(achatsDisponibles(etat, new Decimal(0))).toEqual([])
  })

  it('un cycle qui dépasse le garde-fou est déclaré non convergent, et la simulation s’arrête', () => {
    // La bissection de θ (tâche 13) passera par des réglages où un cycle ne
    // converge pas : sans garde-fou, le calibrage boucle.
    const r = simuler(3, { ...POLITIQUE_PAR_DEFAUT, dureeMaxParCycleSecondes: 3600 })
    expect(r.cycleNonConvergent).toBe(0)
    expect(r.cyclesAcheves).toBe(0)
    expect(r.etat.permanent.nombreEclosions).toBe(0)
  })
})
