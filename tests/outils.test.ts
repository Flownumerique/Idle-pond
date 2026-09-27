/**
 * Test de `sansChaines` — le balayage de canon.test.ts (§4, tâche 8) ne vaut
 * que ce que vaut cet outil. Un gabarit n'est pas de la simple prose : `${…}`
 * porte du code, et un motif mort qui reviendrait comme USAGE dans une
 * interpolation — un accès (`etat.bancs`), un appel (`acheterPlace(dt)`) —
 * doit rester visible. Sans ce test, une régression de `sansChaines` rendrait
 * le balayage aveugle sans qu'aucun test ne le dise (relecture du 2026-09-09).
 *
 * Round 2 (2026-09-10) : le correctif du round 1 a introduit sa propre
 * régression — une regex globale sur `"…"`/`'…'` appliquée à TOUTE la
 * source, y compris à l'intérieur des gabarits, avant le parcours conscient
 * des backticks. Une apostrophe française ordinaire dans le texte statique
 * d'un gabarit s'appariait alors avec le premier `'` rencontré plus loin sur
 * la même ligne — n'importe où dans le fichier — avalant en silence tout ce
 * qui séparait les deux, code réel compris. Les tests ci-dessous verrouillent
 * le correctif : un seul parcours de gauche à droite, une seule notion de
 * « dans une chaîne ».
 */
import { describe, expect, it } from 'vitest'
import { sansChaines } from './outils'

describe('sansChaines', () => {
  it('blanchit une chaîne simple ou double', () => {
    expect(sansChaines("'bancs'")).not.toContain('bancs')
    expect(sansChaines('"bancs"')).not.toContain('bancs')
  })

  it('blanchit le texte statique d’un gabarit sans interpolation', () => {
    expect(sansChaines('`Un banc te suit`')).not.toContain('banc')
  })

  it('préserve intact le contenu d’une interpolation — c’est du code', () => {
    // Les deux scénarios de réintroduction cités en relecture : un accès et
    // un appel, tous deux invisibles pour l'ancienne version de l'outil.
    expect(sansChaines('`${etat.bancs}`')).toContain('etat.bancs')
    expect(sansChaines('`${acheterPlace(dt)}`')).toContain('acheterPlace')
  })

  it('blanchit le texte statique AUTOUR d’une interpolation, sans toucher à celle-ci', () => {
    const resultat = sansChaines('`${part * 100}%`')
    expect(resultat).toContain('part * 100')
    expect(resultat).not.toContain('%')
  })

  it('ne rouvre pas le faux positif sur un gabarit où le motif mort est dans le texte, pas l’expression', () => {
    // Le cas réel de src/donnees/textes-provisoires.ts : « bancs » est dans
    // le texte statique (fiction légitime), `n` est dans l'expression (code).
    // Le motif doit disparaître, `n` doit rester.
    const resultat = sansChaines('`${n} crans tenus, tous bancs confondus`')
    expect(resultat).not.toContain('bancs')
    expect(resultat).toContain('${n}')
  })

  it('gère un gabarit imbriqué dans une interpolation sans se perdre', () => {
    const resultat = sansChaines('`${a ? `${b}` : c}`')
    expect(resultat).toContain('a')
    expect(resultat).toContain('b')
    expect(resultat).toContain('c')
  })

  it("une apostrophe française dans le texte statique d'un gabarit n'avale plus le code réel qui suit sur la ligne (régression du round 1)", () => {
    // Exactement la forme de la démonstration de la relecture : une
    // apostrophe française dans le texte statique d'un gabarit (« n'a »),
    // puis une chaîne '…' séparée plus loin sur la même ligne ('x'). Entre
    // les deux, du code réel — ici un identifiant mort plausible,
    // `population` — qui n'est lui-même dans aucune chaîne : il ne doit
    // jamais être blanchi. Sur le code d'avant ce round, la regex globale
    // appariait l'apostrophe de « n'a » avec le `'` ouvrant de 'x', avalait
    // tout l'intervalle — « population » compris — puis le parcours de
    // gabarit, ne trouvant plus de backtick fermant, blanchissait le reste
    // du fichier. Vérifié : ce test échoue sur le code d'avant ce round
    // (sortie : "const msg = `     ", sans trace de "population") et passe
    // sur le correctif.
    const entree = "const msg = `n'a pas de bancs`; const population = 'x'"
    const resultat = sansChaines(entree)
    expect(resultat).toContain('population')
  })

  it("un guillemet échappé à l'intérieur d'une chaîne simple ne la referme pas prématurément", () => {
    // Sans la gestion de l'échappement, `\'` referme la chaîne trop tôt ; le
    // `'` qui suit ouvrirait alors une seconde chaîne non refermée jusqu'à la
    // fin du fichier, avalant `population` au passage.
    const entree = "const x = 'a\\'b'; const population = 1"
    const resultat = sansChaines(entree)
    expect(resultat).toContain('population')
  })

  it('un backtick échappé à l’intérieur d’un gabarit ne le referme pas prématurément', () => {
    // Même raisonnement que ci-dessus, côté gabarit : `\`` ne doit pas être
    // pris pour le backtick fermant, sous peine d'ouvrir un second gabarit
    // non refermé qui avale le reste du fichier.
    const entree = '`a\\`b`; const population = 1'
    const resultat = sansChaines(entree)
    expect(resultat).toContain('population')
  })

  it("`\\${` échappé n'ouvre pas d'interpolation — il reste du texte statique", () => {
    const echappe = sansChaines('`\\${dead}`')
    expect(echappe).not.toContain('dead')
    // Contrôle : sans l'échappement, `${dead}` est bien une interpolation et
    // son contenu reste visible — la seule différence est le `\` devant `$`.
    const interpolation = sansChaines('`${dead}`')
    expect(interpolation).toContain('dead')
  })

  it('un backslash échappé (\\\\) ne décale pas la fermeture de la chaîne qui le suit', () => {
    // Source : 'a\\' (un backslash échappé, deux caractères), immédiatement
    // suivi du vrai guillemet fermant. Si l'échappement consommait le
    // guillemet fermant par erreur, la chaîne resterait ouverte jusqu'à la
    // fin du fichier et avalerait `population`.
    const entree = "const x = 'a\\\\'; const population = 1"
    const resultat = sansChaines(entree)
    expect(resultat).toContain('population')
  })

  it('un gabarit qui s’étend sur plusieurs lignes reste reconnu comme une seule chaîne', () => {
    const entree = '`ligne un\nligne deux`; const population = 1'
    const resultat = sansChaines(entree)
    expect(resultat).not.toContain('ligne')
    expect(resultat).toContain('population')
  })

  it('une chaîne ou un gabarit non refermé en fin de fichier ne boucle ni ne lève', () => {
    expect(() => sansChaines("const x = 'jamais refermée")).not.toThrow()
    expect(() => sansChaines('const y = `jamais refermé non plus')).not.toThrow()
  })
})
