/**
 * Tests de canon — les phrases qui, si elles sont violées, invalident le build.
 *
 * Ils portent sur des registres aujourd'hui vides. C'est délibéré : le §8 note
 * que le typage, la visibilité et le registre figé « ne se rétrofitent pas », et
 * la même chose vaut pour les gardes. Un test de frontière ajouté après le
 * contenu ne fait que constater les dégâts.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import {
  ALPHA_GAIN_DE_DENSITE,
  SEUILS_DE_JALON,
  THETA_PART_COMPENSEE,
  densiteExposant,
  BONUS_PAR_NIVEAU_DU_HEROS,
  BUDGET_DE_VERBES_ARBRE,
  BUDGET_DE_VERBES_TOTAL,
  CROISSANCE_PAR_CYCLE_VISEE,
  DEBIT_RATIO_ESPECE,
  D_PRODUCTION_PAR_PALIER,
  ESPECE_TOUS_LES_N_PALIERS,
  G_COUT_PALIER,
  NOMBRE_DE_PALIERS,
  NOMBRE_D_ESPECES_DE_BASE,
  PALIERS_PAR_CYCLE_VISE,
  RAPPORT_G_SUR_D,
  multiplicateurDePalier,
} from '../src/noyau/constantes'
import { TERMES_DE_CONFORT, TERMES_DE_COUT, TERMES_DE_PRODUCTION } from '../src/noyau/types'
import type { CapaciteId } from '../src/noyau/types'
import { NOEUDS_TECHNIQUE } from '../src/donnees/noeuds-technique'
import { SUCCES } from '../src/donnees/succes/index'
import { PALIERS } from '../src/donnees/paliers'
import { ESPECE_RESERVEE, ESPECES } from '../src/donnees/especes'
import { ASSISES } from '../src/donnees/assises'
import { BENEDICTIONS } from '../src/donnees/benedictions'
import { sansChaines, sansCommentaires } from './outils'
import {
  NOM_DES_ASSISES,
  NOM_DES_ESPECES,
  TEXTE_DE_SUCCES_INCONNU,
  texteDuSucces,
} from '../src/donnees/textes-provisoires'
import { profondeur, sourceDuTerme } from '../src/ui/format'
import type { SourceDeTerme } from '../src/noyau/types'

/** Toutes les formes de source, pour que le test couvre le gabarit entier. */
const SOURCES_A_VERIFIER: readonly SourceDeTerme[] = [
  { quoi: 'niveau', niveau: 0 },
  { quoi: 'niveau', niveau: 12 },
  { quoi: 'palier', palier: 0 },
  { quoi: 'palier', palier: 4 },
  { quoi: 'drapeaux_permanents', especes: 0 },
  { quoi: 'drapeaux_permanents', especes: 3 },
  { quoi: 'profondeur', paliersOuverts: 1 },
  { quoi: 'profondeur', paliersOuverts: 7 },
  { quoi: 'densite', densite: 0 },
  { quoi: 'densite', densite: 12.5 },
  { quoi: 'heros', niveau: 1 },
  { quoi: 'heros', niveau: 7 },
  { quoi: 'benediction', rang: 0 },
  { quoi: 'benediction', rang: 3 },
]

const RACINE = resolve(__dirname, '..')

function fichiersTs(racine: string): string[] {
  return readdirSync(racine).flatMap((entree) => {
    const chemin = join(racine, entree)
    if (statSync(chemin).isDirectory()) return fichiersTs(chemin)
    return chemin.endsWith('.ts') || chemin.endsWith('.tsx') ? [chemin] : []
  })
}

describe("noyau v1.0 §4 — la Foi achète des bénédictions, et rien d'autre ne monte la production", () => {
  /**
   * RETOURNÉ une seconde fois, le 2026-09-17. Le 2026-09-08 ce bloc avait
   * supprimé les bénédictions au nom du GDD §4.2 ; le soir même la préséance
   * est passée au noyau v1.0 pour la mécanique (`docs/PRESEANCE.md`), et le
   * noyau §4 fait des bénédictions « l'écran d'améliorations du jeu ». Le code
   * avait gardé la suppression. Spec 2026-09-17 [D8].
   *
   * Ce qui reste vrai, et vérifié : la technique et les succès ne montent
   * jamais une production ; une bénédiction ne fait QUE cela.
   */
  it('aucun nœud de technique ne monte une production', () => {
    for (const noeud of NOEUDS_TECHNIQUE) {
      if (noeud.effet.nature !== 'chiffre') continue
      const admis = [...TERMES_DE_COUT, ...TERMES_DE_CONFORT] as string[]
      expect(admis, `le nœud ${noeud.id} cible ${noeud.effet.terme}`).toContain(noeud.effet.terme)
    }
  })

  it("une bénédiction ne cible qu'un terme de production, jamais un coût ni un plafond", () => {
    for (const benediction of BENEDICTIONS) {
      const terme = benediction.portee === 'ciblee' ? 'multiplicateur_benediction' : 'benediction_globale'
      expect(TERMES_DE_PRODUCTION as string[]).toContain(terme)
      expect(TERMES_DE_COUT as string[]).not.toContain(terme)
      expect(TERMES_DE_CONFORT as string[]).not.toContain(terme)
    }
  })

  it('une bénédiction ciblée par espèce, une globale, et pas une de plus', () => {
    const ciblees = BENEDICTIONS.filter((b) => b.portee === 'ciblee')
    const globales = BENEDICTIONS.filter((b) => b.portee === 'globale')
    expect(ciblees.map((b) => b.espece)).toEqual(ESPECES.map((e) => e.id))
    expect(globales).toHaveLength(1)
    expect(globales[0].espece).toBeNull()
  })

  it('aucun succès ne monte une production (amendement v1.1 §2.D)', () => {
    // « Un succès ne peut porter qu'un effet qui existe déjà comme terme de
    // technique : réduction de coût, relèvement de plafond, ou verbe. »
    // Le typage l'interdit déjà ; ce test le vérifie sur la donnée, parce
    // qu'un registre peut un jour venir d'ailleurs que du compilateur.
    const genresAdmis = ['reduction_cout', 'plafond', 'verbe']
    for (const succes of SUCCES) {
      if (succes.effet === null) continue
      expect(genresAdmis, `le succès ${succes.id}`).toContain(succes.effet.genre)
      if (succes.effet.genre === 'verbe') continue
      expect(
        TERMES_DE_PRODUCTION as string[],
        `le succès ${succes.id} cible le terme de production ${succes.effet.terme}`,
      ).not.toContain(succes.effet.terme)
    }
  })

  /*
   * RETIRÉ le 2026-09-09 : « un puits, un levier — rien ne double la densité
   * sur la conviction ».
   *
   * Il vérifiait qu'aucun nœud ni succès ne visait `cout_reconviction`, parce
   * que la conviction était payée par la DENSITÉ et par elle seule (GDD §7.1) :
   * lui donner un second levier rendait l'ensemble inéquilibrable. Le terme est
   * devenu `cout_deblocage`, et sa formule ne lit plus la densité du tout — le
   * noyau v1.0 §1.3 en fait une fraction du coût de son palier. Il n'y a donc
   * plus de premier levier à protéger, et interdire le second reviendrait à
   * défendre une règle dont l'objet a disparu.
   *
   * `reduction_technique` et le puits d'aménagement qu'il portait sont partis
   * à leur tour le 2026-09-09 (tâche 6, noyau v1.0 §3.1) : `f` = 1, il n'y a
   * plus qu'un seul puits de descente, `cout_creuser`. Ce qui reste vrai et
   * reste vérifié : aucun effet ne monte une production.
   */

  it('aucun effet chiffré ne flotte sans terme nommé', () => {
    for (const noeud of NOEUDS_TECHNIQUE) {
      if (noeud.effet.nature !== 'chiffre') continue
      expect(typeof noeud.effet.terme, `le nœud ${noeud.id}`).toBe('string')
    }
  })
})

describe('§7.5 — les trois règles dures de l’arbre', () => {
  it('une capacité a exactement une source', () => {
    const parLArbre = new Set<CapaciteId>()
    for (const noeud of NOEUDS_TECHNIQUE) {
      if (noeud.effet.nature === 'verbe') parLArbre.add(noeud.effet.capacite)
    }
    for (const succes of SUCCES) {
      if (succes.effet?.genre !== 'verbe') continue
      expect(
        parLArbre.has(succes.effet.capacite),
        `${succes.effet.capacite} est atteignable par l’arbre ET par le succès ${succes.id}`,
      ).toBe(false)
    }
  })

  it('le budget de verbes est commun et tenu', () => {
    const verbesDeLArbre = NOEUDS_TECHNIQUE.filter((n) => n.effet.nature === 'verbe').length
    const verbesDesSucces = SUCCES.filter((s) => s.effet?.genre === 'verbe').length
    expect(verbesDeLArbre).toBeLessThanOrEqual(BUDGET_DE_VERBES_ARBRE)
    expect(verbesDeLArbre + verbesDesSucces).toBeLessThanOrEqual(BUDGET_DE_VERBES_TOTAL)
  })
})

describe('§13 — les valeurs fixées et leurs dérivations', () => {
  it('g/D se dérive de la croissance et des paliers par cycle, jamais saisi', () => {
    expect(RAPPORT_G_SUR_D).toBeCloseTo(Math.pow(CROISSANCE_PAR_CYCLE_VISEE, 1 / PALIERS_PAR_CYCLE_VISE), 12)
    // Le §6.3 cite « 1.039 » ; la dérivation exacte donne 1.03833. L'écart est
    // un arrondi du document, pas un désaccord : c'est bien la dérivation qui
    // fait foi, le §13.2 classant g/D en « recalculer, ne pas saisir ».
    expect(Math.abs(RAPPORT_G_SUR_D - 1.039)).toBeLessThan(1e-3)
  })

  it('D vaut g / 1.039, soit 2.31', () => {
    expect(D_PRODUCTION_PAR_PALIER).toBeCloseTo(G_COUT_PALIER / RAPPORT_G_SUR_D, 12)
    expect(D_PRODUCTION_PAR_PALIER).toBeCloseTo(2.31, 2)
  })

  it('D ≠ g, sinon la durée d’un cycle est plate', () => {
    expect(D_PRODUCTION_PAR_PALIER).not.toBeCloseTo(G_COUT_PALIER, 2)
  })

  it('f = 1 : reset complet, et la constante elle-même n’existe plus', () => {
    const source = fichiersTs(join(RACINE, 'src', 'noyau'))
      .map((f) => sansCommentaires(readFileSync(f, 'utf8')))
      .join('\n')
    expect(source).not.toContain('F_TARIF_REDESCENTE')
    expect(source).not.toContain('estUnAmenagement')
  })

  it('les seuils sont CUMULÉS : le centième niveau vaut ×16, pas ×1024', () => {
    // Le §2.C ordonne cette vérification avant toute ligne de code : `D = 2.31`
    // a été ajusté contre cette lecture, et une table multiplicative rendrait
    // tout le calibrage faux.
    expect(SEUILS_DE_JALON.map((s) => s.multiplicateurCumule)).toEqual([2, 4, 8, 16])
    expect(SEUILS_DE_JALON.map((s) => s.seuil)).toEqual([10, 25, 50, 100])
  })

  it('θ borné à [0, 1], et l’exposant de densité dérivé de θ/α', () => {
    expect(THETA_PART_COMPENSEE).toBeGreaterThanOrEqual(0)
    expect(THETA_PART_COMPENSEE).toBeLessThanOrEqual(1)
    expect(densiteExposant()).toBeCloseTo(THETA_PART_COMPENSEE / ALPHA_GAIN_DE_DENSITE, 12)
  })

  it('62 paliers, 6 assises, 21 espèces de base', () => {
    expect(PALIERS.length).toBe(NOMBRE_DE_PALIERS)
    expect(ASSISES.length).toBe(6)
    expect(ESPECES.length).toBe(NOMBRE_D_ESPECES_DE_BASE)
  })

  it('chaque palier appartient à une assise, et porte au plus une espèce', () => {
    for (const palier of PALIERS) {
      expect(ASSISES.some((a) => a.id === palier.assise)).toBe(true)
      if (palier.espece === null) continue
      expect(ESPECES.map((e) => e.id)).toContain(palier.espece)
    }
    expect(PALIERS.filter((p) => p.espece !== null)).toHaveLength(NOMBRE_D_ESPECES_DE_BASE)
  })

  it('une espèce est ancrée à trois fois son rang, sinon son débit décroche de D', () => {
    // Le débit de base d'une espèce croît de `DEBIT_RATIO_ESPECE` par RANG, et
    // le multiplicateur de profondeur porte le reste de `D` par PALIER. Les
    // deux ne se composent en `D^palier` que si l'ancre vaut exactement
    // `3 × rang` — ce que la répartition 6/12/12/12/12/8 garantit, et qu'une
    // autre casserait sans qu'aucun autre test ne le dise.
    for (const espece of ESPECES) {
      expect(espece.palier, `l’espèce ${espece.id}`).toBe(espece.rang * ESPECE_TOUS_LES_N_PALIERS)
    }
  })

  it('le multiplicateur de palier et le héros portent ensemble la part de D que le bestiaire ne porte pas', () => {
    // Spec 2026-09-17 [D3] : un niveau de héros par palier, et sa part sort de
    // `m_p`. Ce qui doit tenir est le produit des deux, pas `m_p` seul.
    const parTroisPaliers =
      Math.pow(multiplicateurDePalier() * (1 + BONUS_PAR_NIVEAU_DU_HEROS), ESPECE_TOUS_LES_N_PALIERS) *
      DEBIT_RATIO_ESPECE
    expect(parTroisPaliers).toBeCloseTo(Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS), 6)
  })
})

describe('§3 — le lexique s’applique au code, pas seulement à la prose', () => {
  /**
   * Deux entrées ont été RETIRÉES de cette liste le 2026-09-08, avec la
   * préséance du GDD. Elles interdisaient son propre vocabulaire :
   *
   *   `ponte`     — l'Annexe A du GDD en fait le nom du prestige, et réserve
   *                 « éclosion » à la naissance du héros (§2.1). Le code porte
   *                 encore l'inverse ; l'interdire empêchait de le corriger.
   *   `part_mûre` — seule entrée du canal acclimaté (§3.0), classée « Fixé
   *                 (canon) » au §16.1. Le prompt de lancement l'avait
   *                 supprimée ; le test en défendait la suppression.
   *
   * Retirer une interdiction ne renomme rien. Les deux chantiers restent à
   * faire ; ils ne sont simplement plus bloqués par un test rouge.
   */
  const PERIMES: readonly { readonly motif: RegExp; readonly quoi: string }[] = [
    { motif: /\bprestige\b/i, quoi: 'prestige' },
    { motif: /\brebirth\b/i, quoi: 'rebirth' },
    { motif: /\bgemmes?\b/i, quoi: 'gemme' },
    { motif: /\bperles?\b/i, quoi: 'perle' },
    { motif: /\bcorail\b/i, quoi: 'Corail' },
    { motif: /\bbuyFish\b/, quoi: 'buyFish (achat par exemplaire)' },
    { motif: /\bpondDepth\b|\bzoneId?\b|\bbiomes?\b/i, quoi: 'ancien vocabulaire de zones/biomes' },
    { motif: /\blayers?\b/i, quoi: 'layer (dire assise)' },
    { motif: /\b[ée]tages?\b/i, quoi: 'étage (toléré à l’oral, jamais dans le code)' },
    { motif: /\bstrates?\b/i, quoi: 'strate (réservé au plan des dieux, interdit pour de la roche)' },
  ]

  it('aucun terme périmé ne subsiste dans le code de src/', () => {
    const fautes: string[] = []
    for (const fichier of fichiersTs(join(RACINE, 'src'))) {
      const code = sansCommentaires(readFileSync(fichier, 'utf8'))
      for (const { motif, quoi } of PERIMES) {
        if (motif.test(code)) fautes.push(`${relative(RACINE, fichier)} : ${quoi}`)
      }
    }
    expect(fautes).toEqual([])
  })

  it('la maturation ne survit nulle part dans le noyau', () => {
    // Noyau v1.0 : la maturation gouverne ce qu'un lieu peut DEVENIR dans la
    // fiction, jamais ce que le héros GAGNE. Elle n'a donc plus sa place dans
    // le calcul de revenu du noyau.
    const source = fichiersTs(join(RACINE, 'src', 'noyau'))
      .map((f) => sansCommentaires(readFileSync(f, 'utf8')))
      .join('\n')
    for (const mot of ['partMure', 'partsMures', 'maturation', 'cibleDeMaturation']) {
      expect(source, `« ${mot} » subsiste dans src/noyau/`).not.toContain(mot)
    }
  })

  it('un seul canal de revenu : les espèces (noyau v1.0 §10)', () => {
    const source = fichiersTs(join(RACINE, 'src', 'noyau'))
      .map((f) => sansCommentaires(readFileSync(f, 'utf8')))
      .join('\n')
    for (const mot of ['acclimat', 'debitAcclimate', 'canalAcclimate']) {
      expect(source, `« ${mot} » subsiste dans src/noyau/`).not.toContain(mot)
    }
  })

  it('le modèle à population ne subsiste pas dans le noyau (noyau v1.0 §1.3)', () => {
    // Le banc, la place et l'effectif sont morts ensemble le 2026-09-09 : une
    // espèce est un générateur avec un niveau. La tâche 4 a montré qu'une
    // suppression peut paraître complète dans `economie.ts` et survivre dans
    // `noyau.ts` — le balayage porte donc sur le répertoire entier.
    const source = fichiersTs(join(RACINE, 'src', 'noyau'))
      .map((f) => sansCommentaires(readFileSync(f, 'utf8')))
      .join('\n')
    for (const mot of [
      'effectif',
      'BancId',
      'EtatBanc',
      'convaincre',
      'acheterPlace',
      'coutDePlace',
      'cout_place',
      'cout_reconviction',
      'vitesseDeRepeuplement',
    ]) {
      expect(source, `« ${mot} » subsiste dans src/noyau/`).not.toContain(mot)
    }
  })

  it('le modèle mort ne subsiste nulle part dans src/ (spec §4)', () => {
    // Portail de fin de phase (noyau v1.0, tâche 8) : les tâches 2 à 7 ont
    // retiré la population simulée, la maturation, le second canal de revenu,
    // la capacité de palier et le tarif réduit de redescente. Ce test ne les
    // reretire pas ; il verrouille qu'ils ne reviennent pas, sur `src` ENTIER
    // — pas seulement `src/noyau` comme les gardes ci-dessus, parce que la
    // tâche 4 a déjà montré qu'une suppression peut sembler complète dans un
    // module et survivre dans un autre.
    //
    // `src/adaptateurs/persistance.ts` est exclu : c'est le seul fichier qui a
    // le droit de connaître les anciens noms de champs, parce qu'il lit les
    // vieilles sauvegardes (v4 et antérieures) pour les migrer. Lui interdire
    // ces mots empêcherait la migration d'exister.
    //
    // « banc » a un usage fictionnel légitime — un banc de poissons — dans
    // `src/ui/Mare.tsx`, `src/donnees/succes/actes.ts` et
    // `src/donnees/textes-provisoires.ts`, jusque dans des identifiants de
    // succès figés qu'on n'a pas le droit de renommer (`acte-deux-bancs`,
    // `acte-trois-bancs` : le registre des succès est immuable par canon, §16.1
    // le classe « Fixé »). Le mot n'est pas interdit ; seul le CHAMP D'ÉTAT
    // `bancs` est mort. `sansChaines` neutralise la prose portée par des
    // chaînes (identifiants de succès compris) exactement comme
    // `sansCommentaires` neutralise celle portée par des commentaires, pour
    // que le balayage porte sur le code et non sur la fiction.
    // `src/scene/` est aussi exclu car la scène dessine les bancs de poissons.
    const migrations = join('src', 'adaptateurs', 'persistance.ts')
    const sceneDir = join(RACINE, 'src', 'scene')
    const source = fichiersTs(join(RACINE, 'src'))
      .filter((f) => {
        const chemin = relative(RACINE, f)
        return chemin !== migrations && !f.startsWith(sceneDir)
      })
      .map((f) => sansChaines(sansCommentaires(readFileSync(f, 'utf8'))))
      .join('\n')
    for (const mot of [
      'population', 'maturation', 'acclimat', 'partMure',
      'cout_place', 'convaincre', 'acheterPlace', 'BANCS', 'bancParId',
      'bancs', 'acclimatations', 'secondesEnSaturation',
      // Retirés par la tâche 12 : la mesure de redescente (GDD §16.4, dépassé
      // par le noyau v1.0 selon `docs/PRESEANCE.md`). Le compteur survit dans
      // le format de sauvegarde v5, que seul `persistance.ts` connaît.
      'secondesEnRedescente', 'fractionEnRedescente',
    ]) {
      expect(source, `« ${mot} » subsiste dans src/`).not.toContain(mot)
    }
  })
})

describe('§3 — la règle d’UI absolue', () => {
  /**
   * « L'interface n'affiche JAMAIS un nom générique de couche. Pas de
   * "Zone 3", pas de "Assise II". Chaque lieu porte un nom propre. `assise` et
   * `palier` sont des termes de code et de GDD, PAS D'ÉCRAN. »
   *
   * Le test porte sur les chaînes réellement produites, pas sur le source :
   * c'est une capture d'écran qui a montré « palier 0 » dans le détail de la
   * captation, écrit non pas dans les textes mais dans le noyau, qui n'avait
   * rien à faire là.
   */
  const INTERDITS_A_L_ECRAN = /\b(paliers?|assises?|[ée]tages?|strates?|couches?|zones?|biomes?)\b/i

  it('aucun terme de couche ne sort dans un texte affiché', () => {
    const fautes: string[] = []
    const verifier = (ou: string, texte: string) => {
      if (INTERDITS_A_L_ECRAN.test(texte)) fautes.push(`${ou} : « ${texte} »`)
    }

    for (const nom of Object.values(NOM_DES_ASSISES)) verifier('nom de lieu', nom)
    for (const nom of Object.values(NOM_DES_ESPECES)) verifier('nom d’espèce', nom)
    for (const succes of SUCCES) {
      const texte = texteDuSucces(succes.id)
      verifier(`${succes.id}.nom`, texte.nom)
      verifier(`${succes.id}.condition`, texte.condition)
      verifier(`${succes.id}.rapport`, texte.rapport)
    }
    for (const source of SOURCES_A_VERIFIER) verifier('source de terme', sourceDuTerme(source))
    for (let palier = 0; palier < 12; palier += 1) verifier('profondeur', profondeur(palier))

    expect(fautes).toEqual([])
  })

  it('`tanche` n’est assignée à aucun générateur', () => {
    // Longévité, faible débit, très forte contenance : c'est le portrait du
    // héros, pas d'une espèce ordinaire (§2.E).
    expect(ESPECES.map((e) => e.id)).not.toContain(ESPECE_RESERVEE)
  })

  it('chaque succès livré porte un texte, jamais le repli', () => {
    const sansTexte = SUCCES.filter((s) => texteDuSucces(s.id) === TEXTE_DE_SUCCES_INCONNU)
    expect(sansTexte.map((s) => s.id)).toEqual([])
  })
})
