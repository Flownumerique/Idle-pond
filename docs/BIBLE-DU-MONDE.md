# IdlePond — la bible du monde

**Version** : 0.2 — 2026-11-23
**Statut** : espace des **propositions** de monde, avant validation par l'auteur. Ce qui est
validé passe dans `docs/CODEX.md` (fiction) et `docs/da/gabarits.md` (dessin), et sort d'ici.

**Historique.** La v0.1 (magies, tribus, quinze espèces à venir, peuples, panthéon, faune) a été
validée en entier le 2026-11-22 et fait foi au Codex v1.3, §2 ; son texte reste dans
l'historique git (commit `2a74e3b`).

---

## Proposition en cours — la dérivation des six espèces livrées (v0.2)

**Ce qu'on cherche.** Que le joueur dise « c'est une sorte de vairon » et voie aussitôt que ce
n'en est pas un (Codex §2). Les noms ne changent pas ; l'aspect, le comportement à l'écran et
le texte changent.

### Ce que le dessin permet

Un poisson d'espèce mesure **7 à 12 pixels** de long (`docs/da/gabarits.md`), deux images de
nage, trois couleurs (corps, dos, ventre) et un contour. À cette taille, une dérivation se lit
par trois moyens, et la proposition s'y tient :

1. **Une couleur franche** par espèce, au lieu des gris-verts actuels.
2. **Un accent** : un ou deux pixels d'une quatrième couleur (une épine, une rune, une
   nageoire). *Demande au code* : un quatrième champ `Accent` dans `CouleursDePoisson`
   (`RegistreDArt.cs`), et sa couleur dans la palette de la famille.
3. **Un comportement** : ce que la tribu fait à l'écran quand il se passe quelque chose. Le flux
   `Partie.EvenementsDeJeu` existe déjà (espèce convaincue, seuil franchi, eau troublée,
   renaissance…) ; rien ne s'y abonne encore. C'est là que la fantaisie se voit le mieux, sans
   grossir les sprites.

**Le budget de lumières** (quatre à l'écran, spec DA) interdit qu'un poisson sur deux brille en
permanence. Règle proposée : un accent est **une couleur**, pas une lumière ; une espèce
n'émet de lumière qu'**en éclair**, sur un événement (un seuil franchi, une renaissance).

**La magie du lieu se voit dans le sillage** : dans la Noue (la Sève), de petits éclats or et
vert tendre quand une tribu produit ; dans le Gour (le Courant), de fins traits bleu clair.

### Les six espèces

**Aperçu des couleurs** : `docs/da/apercu-derivation-especes.png` — aujourd'hui à gauche, proposé à droite, sur l'eau de chaque lieu. La silhouette y est un gabarit commun, pas le dessin de chaque espèce : seules les couleurs et la place des accents sont à juger.

Couleurs en `#RRGGBB`, à régler à l'œil sur le fond de chaque lieu. Le contour reste `#121C1A`.

#### Le vairon — le Chœur (la Noue)

| | Aujourd'hui | Proposé |
|---|---|---|
| Corps / dos / ventre | `#96AAA0` `#566C64` `#C8D4CC` (gris-vert) | `#7FC8A0` vert d'eau irisé · `#2E7D5B` vert profond · **`#E8483A` ventre rouge** |
| Accent | — | `#B8E86A` : la caudale en forme de feuille (2 px) |

- **À l'œil** : un petit poisson vert vif au ventre rouge, la queue comme une feuille. Le rouge
  du frai, poussé jusqu'à la braise.
- **Le Chœur** : les vairons d'un même palier nagent **à l'unisson** (même rythme d'images, même
  cap) et virent ensemble. À chaque **seuil franchi**, une vague de rouge parcourt la troupe
  d'un bout à l'autre, et le ventre de chacun s'allume un instant.
- **Le texte** (bibliothèque) : « Cent corps, une seule voix. Quand ils chantent, ils changent de
  couleur. »

#### La loche — les Devineresses (la Noue)

| | Aujourd'hui | Proposé |
|---|---|---|
| Corps / dos / ventre | `#A08A5E` `#6A5A3A` `#CCB88A` | `#C08A3E` bronze cuivré · `#6E4A22` brun sombre · `#F0D9A0` crème |
| Accent | — | `#4FE0D0` : deux points de rune turquoise sur le flanc |

- **À l'œil** : une loche de bronze, couchée sur la vase, tachée de deux runes turquoise ; ses
  barbillons (1 px) crépitent parfois d'un éclat blanc.
- **Les devineresses** : elles **sentent venir** ce qui arrive. Un peu avant que l'eau se trouble
  (vers 75 % de la contenance, avant le voile de 85 %), les loches s'agitent et leurs runes
  clignotent : le joueur apprend à les lire comme un baromètre. Même chose juste avant qu'une
  descente se rouvre.
- **Le texte** : « Elles savent avant l'eau. On vient les voir quand on a peur. »

#### L'épinoche — la Garde (le Gour)

| | Aujourd'hui | Proposé |
|---|---|---|
| Corps / dos / ventre | `#7E9A6A` `#4A6040` `#C8D0A0` | `#6FA05A` vert-bronze · `#2F5A35` vert sombre · **`#D8283A` gorge carmin** |
| Accent | — | `#FF8FA0` : trois épines dorsales de verre rouge (1 px chacune, la pointe éclaircie) |

- **À l'œil** : un soldat rouge et vert, épines dressées ; l'œil d'un pixel bleu vif (`#4AA8FF`),
  comme le mâle réel au frai.
- **La Garde** : à chaque **seuil franchi** (10, 25, 50, 100), un **nid-forteresse** d'herbes
  tressées pousse sur le fond de leur palier — un petit rempart de plus. À cent épinoches, le
  palier a sa citadelle.
- **Le texte** : « Elles bâtissent avant de se battre, et se battent pour ce qu'elles ont bâti. »

#### Le chabot — les Gardiens (le Gour)

| | Aujourd'hui | Proposé |
|---|---|---|
| Corps / dos / ventre | `#7A6A4E` `#4E4232` `#A8987A` | `#7C8790` ardoise marbrée · `#4B545C` gris sombre · `#C9CFC9` pierre claire |
| Accent | — | `#C8A040` : une tache de lichen doré sur la tête (1–2 px) |

- **À l'œil** : une grosse tête de pierre, un corps qui s'efface sur le fond ; on le voit parce
  que le lichen d'or bouge.
- **Les gardiens** : chaque chabot se tient sur une pierre du fond. **Quand l'eau se trouble**, ils
  **se pétrifient** : ils deviennent entièrement gris, cessent de nager, et ne redeviennent
  vivants que quand l'eau s'éclaircit. C'est un signal lisible, et c'est leur légende vue à
  l'écran.
- **Le texte** : « Chaque pierre a le sien. Menacé, il devient la pierre. »

#### La lamproie — les Pèlerines (le Gour)

| | Aujourd'hui | Proposé |
|---|---|---|
| Corps / dos / ventre | `#5E5E50` `#3A3A30` `#8A8A78` | `#9B7FC0` lilas · `#5A3F7A` violet sombre · `#D9C8F0` lavande |
| Accent | — | `#9FF0FF` : l'anneau de la bouche, gravé de runes argent-cyan (1 px) |

- **À l'œil** : une longue bête lilas, sans mâchoire, la bouche cerclée d'une lueur froide.
- **Les pèlerines** : elles nagent peu et **s'accrochent aux parois** des galeries, à la verticale.
  De temps en temps, un **fil de mana** très fin part d'une autre tribu et vient jusqu'à leur
  bouche : elles boivent un peu de ce que produisent les autres. Pure image, aucun effet sur
  l'économie.
- **Le texte** : « Elles remontent tout ce qui descend, et boivent un peu de chacun. On les
  craint, on les écoute. »

#### L'ombre — les Hérauts (le Gour)

| | Aujourd'hui | Proposé |
|---|---|---|
| Corps / dos / ventre | `#8A8AA0` `#585870` `#C0C0D0` | `#A8B8D8` argent bleuté · `#4A5A8A` bleu nuit · `#E8EEF8` nacre |
| Accent | — | la dorsale-étendard, haute de 2 px : `#C060F0` violet sur une image, `#40D0C0` turquoise sur l'autre |

- **À l'œil** : un poisson d'argent qui porte une grande voile ; d'une image de nage à l'autre,
  la voile passe du violet au turquoise : elle **chatoie** comme un vitrail.
- **Les hérauts** : ils traversent les paliers d'un bord à l'autre, sans s'arrêter. Quand une
  **autre tribu est convaincue**, une ombre la croise et laisse derrière elle un **mirage** :
  l'image tramée du nouveau venu, qui s'efface. Ils portent la nouvelle.
- **Le texte** : « On les voit passer, jamais arriver. Leur voile dit ce qui vient d'avoir lieu. »

### Ce que la proposition demande au code (sans changer l'économie)

| Demande | Où | Taille |
|---|---|---|
| Les nouvelles couleurs | `RegistreDArt.ESPECES_PAR_RANG`, `docs/da/gabarits.md` | S |
| Un quatrième champ `Accent`, dessiné par le générateur de provisoires | `CouleursDePoisson`, `GenerateurDeSprites` | S |
| Les comportements : unisson, vague, pétrification, agitation, fil de mana, mirage, nids | la scène, abonnée à `Partie.EvenementsDeJeu` et à `PartDeContenance` | M |
| Les sillages de la Sève et du Courant | la scène | S |
| Les textes de bibliothèque | `Textes.cs`, quand la bibliothèque existera (chantier 6) | S |

Aucune constante, aucun test de parité, aucune sauvegarde n'est touché. `LexiqueTests` doit
rester vert sur les nouveaux textes.

### Ce que l'auteur doit trancher

Pour chaque espèce : garder, modifier ou rejeter — la couleur, l'accent, le comportement, le
texte. Puis : la règle « un accent est une couleur, la lumière seulement en éclair ».
