/**
 * Outils de test — comparaison d'états à la tolérance flottante près.
 *
 * Le §12 formule le critère du test d'équivalence de pas ainsi : « 480 appels à
 * dt = 60 s et 1 appel à dt = 8 h donnent le même état, à la tolérance
 * flottante près ». C'est cette tolérance-là, et rien de plus permissif : elle
 * couvre l'écart entre exp(-k·Δ) évalué une fois et (exp(-k·δ))^480, pas une
 * divergence de modèle.
 */
import Decimal from 'break_infinity.js'
import { expect } from 'vitest'

export const TOLERANCE_RELATIVE = 1e-9

function estDecimal(valeur: unknown): valeur is Decimal {
  return valeur instanceof Decimal
}

function proche(a: number, b: number, tolerance: number): boolean {
  if (a === b) return true
  if (!Number.isFinite(a) || !Number.isFinite(b)) return false
  const echelle = Math.max(Math.abs(a), Math.abs(b), 1e-300)
  return Math.abs(a - b) / echelle <= tolerance
}

export function comparerAToleranceFlottante(
  obtenu: unknown,
  attendu: unknown,
  tolerance = TOLERANCE_RELATIVE,
  chemin = '',
): void {
  if (estDecimal(attendu) || estDecimal(obtenu)) {
    const a = new Decimal(obtenu as Decimal)
    const b = new Decimal(attendu as Decimal)
    if (a.eq(b)) return
    const ecart = a.sub(b).abs().div(Decimal.max(a.abs(), b.abs()).add(1e-300))
    expect(ecart.toNumber(), `Decimal divergent en ${chemin} : ${a} vs ${b}`).toBeLessThanOrEqual(tolerance)
    return
  }
  if (typeof attendu === 'number' && typeof obtenu === 'number') {
    expect(proche(obtenu, attendu, tolerance), `nombre divergent en ${chemin} : ${obtenu} vs ${attendu}`).toBe(true)
    return
  }
  if (Array.isArray(attendu)) {
    expect(Array.isArray(obtenu), `tableau attendu en ${chemin}`).toBe(true)
    const tableau = obtenu as unknown[]
    expect(tableau.length, `longueur divergente en ${chemin}`).toBe(attendu.length)
    attendu.forEach((valeur, index) => comparerAToleranceFlottante(tableau[index], valeur, tolerance, `${chemin}[${index}]`))
    return
  }
  if (attendu !== null && typeof attendu === 'object') {
    expect(typeof obtenu, `objet attendu en ${chemin}`).toBe('object')
    const gauche = obtenu as Record<string, unknown>
    const droite = attendu as Record<string, unknown>
    const clefs = new Set([...Object.keys(gauche), ...Object.keys(droite)])
    for (const clef of clefs) {
      comparerAToleranceFlottante(gauche[clef], droite[clef], tolerance, chemin === '' ? clef : `${chemin}.${clef}`)
    }
    return
  }
  expect(obtenu, `valeur divergente en ${chemin}`).toStrictEqual(attendu)
}

/** Retire commentaires de bloc et de ligne : les tests portent sur le code, pas sur la prose. */
export function sansCommentaires(source: string): string {
  return source.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/(^|[^:])\/\/[^\n]*/g, '$1 ')
}

/**
 * Retire le contenu des chaînes et le texte STATIQUE des gabarits, sans en
 * changer la forme.
 *
 * Un balayage lexical qui porte sur `src` entier traverse aussi la fiction —
 * les identifiants de succès, les textes affichés — où des mots par ailleurs
 * morts en code (le champ d'état `bancs`, le verbe `convaincre`) survivent
 * légitimement comme prose ou comme identifiant figé (`acte-deux-bancs`). Ce
 * n'est pas la même chose qu'une survivance du modèle : `sansCommentaires`
 * neutralise la prose en commentaire, celle-ci neutralise la prose en chaîne.
 *
 * Un gabarit n'est PAS que de la prose : `${…}` porte du code (un accès, un
 * appel), et un motif mort qui reviendrait comme USAGE dans une interpolation
 * — `` `${etat.bancs}` ``, `` `${acheterPlace(dt)}` `` — doit rester visible
 * au balayage. Seul le texte statique autour est blanchi ; chaque `${…}` est
 * recopié tel quel, accolades comprises.
 *
 * Implémentation : UN SEUL parcours de gauche à droite, qui reconnaît `"`,
 * `'` et `` ` `` comme des ouvreurs de chaîne au fil du texte. Une première
 * version faisait deux passes — une regex globale sur `"…"`/`'…'` sur TOUTE
 * la source, suivie d'un parcours conscient des gabarits — et c'était le
 * défaut : les deux passes n'avaient pas la même notion de « où commence une
 * chaîne ». Une apostrophe française ordinaire dans le texte statique d'un
 * gabarit (`` `n'a pas de bancs` ``) n'est pas un ouvreur de chaîne — mais la
 * regex de la première passe, elle, la voyait comme tel, et s'appariait avec
 * le premier `'` rencontré plus loin sur la même ligne, où qu'il soit,
 * avalant en silence tout ce qui séparait les deux, code réel compris. Un
 * seul parcours, une seule notion de « dans une chaîne », ferme la classe
 * entière de trou plutôt qu'un correctif de plus sur les regex.
 */
export function sansChaines(source: string): string {
  let resultat = ''
  let i = 0
  while (i < source.length) {
    const car = source[i]
    if (car === '\\') {
      resultat += source.slice(i, i + 2)
      i += 2
      continue
    }
    if (car === '"' || car === "'") {
      const [chaine, fin] = blanchirChaineSimpleDepuis(source, i, car)
      resultat += chaine
      i = fin
      continue
    }
    if (car === '`') {
      const [gabarit, fin] = blanchirGabaritDepuis(source, i)
      resultat += gabarit
      i = fin
      continue
    }
    resultat += car
    i += 1
  }
  return resultat
}

/**
 * Blanchit le contenu d'une chaîne `'…'` ou `"…"` à partir de son guillemet
 * ouvrant (indice `depart`, caractère `guillemet`), caractère par caractère,
 * en préservant la forme. Une échappée (`\'`, `\"`, `\\`, …) est consommée
 * comme une unité de deux caractères, donc un guillemet échappé ne referme
 * jamais la chaîne prématurément. Une chaîne simple ou double ne s'étend
 * jamais sur plusieurs lignes en JS/TS valide : un saut de ligne avant le
 * guillemet fermant referme la chaîne sur place (comme le faisait l'ancienne
 * regex, avec sa classe `[^"\\\n]`), sans consommer ce saut de ligne — il
 * redevient du texte ordinaire pour la suite du parcours. Non refermée avant
 * la fin du fichier : s'arrête proprement, sans boucler ni lever.
 */
function blanchirChaineSimpleDepuis(source: string, depart: number, guillemet: string): readonly [string, number] {
  let i = depart + 1
  let sortie = guillemet
  while (i < source.length) {
    const car = source[i]
    if (car === '\\') {
      sortie += '  '
      i += 2
      continue
    }
    if (car === guillemet) {
      return [sortie + guillemet, i + 1]
    }
    if (car === '\n') {
      return [sortie, i]
    }
    sortie += ' '
    i += 1
  }
  return [sortie, i]
}

/**
 * Blanchit le texte statique d'un gabarit à partir de son backtick ouvrant
 * (indice `depart`) et recopie chaque `${…}` intact, accolades comprises, y
 * compris quand l'expression contient elle-même des chaînes ou un gabarit
 * imbriqué (leurs accolades ne doivent pas compter dans le niveau englobant).
 * Retourne le texte transformé et l'indice juste après le backtick fermant.
 */
function blanchirGabaritDepuis(source: string, depart: number): readonly [string, number] {
  let i = depart + 1
  let sortie = '`'
  while (i < source.length) {
    const car = source[i]
    if (car === '\\') {
      sortie += '  '
      i += 2
      continue
    }
    if (car === '`') {
      return [sortie + '`', i + 1]
    }
    if (car === '$' && source[i + 1] === '{') {
      const fin = indiceApresAccoladeFermante(source, i + 2)
      sortie += source.slice(i, fin)
      i = fin
      continue
    }
    sortie += ' '
    i += 1
  }
  return [sortie, i] // gabarit non refermé : ne devrait pas arriver sur du code valide
}

/**
 * Depuis l'indice juste après un `${`, retourne l'indice juste après le `}`
 * qui la referme, en traversant sans les compter les accolades qui
 * appartiennent à une chaîne ou à un gabarit imbriqué dans l'expression.
 */
function indiceApresAccoladeFermante(source: string, depart: number): number {
  let i = depart
  let profondeur = 1
  while (i < source.length && profondeur > 0) {
    const car = source[i]
    if (car === '{') {
      profondeur += 1
      i += 1
    } else if (car === '}') {
      profondeur -= 1
      i += 1
    } else if (car === '"' || car === "'") {
      i = indiceApresChaine(source, i, car)
    } else if (car === '`') {
      i = indiceApresGabaritImbrique(source, i)
    } else if (car === '\\') {
      i += 2
    } else {
      i += 1
    }
  }
  return i
}

function indiceApresChaine(source: string, depart: number, guillemet: string): number {
  let i = depart + 1
  while (i < source.length && source[i] !== guillemet) {
    i += source[i] === '\\' ? 2 : 1
  }
  return i + 1
}

function indiceApresGabaritImbrique(source: string, depart: number): number {
  let i = depart + 1
  while (i < source.length) {
    const car = source[i]
    if (car === '\\') {
      i += 2
      continue
    }
    if (car === '`') return i + 1
    if (car === '$' && source[i + 1] === '{') {
      i = indiceApresAccoladeFermante(source, i + 2)
      continue
    }
    i += 1
  }
  return i
}
