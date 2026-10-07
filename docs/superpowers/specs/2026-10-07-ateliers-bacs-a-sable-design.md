# Les ateliers — des bacs à sable pour essayer le jeu à la main — design

Écrit le 2026-10-07. Validé en conversation le même jour.

## Intention

Regarder et essayer le jeu sans jouer des heures pour atteindre un état : le héros à un
stade donné, une espèce débloquée, l'eau troublée, une absence de huit heures, une
renaissance. Des **scènes d'atelier**, ouvertes dans l'éditeur, avec un panneau de commandes.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| Le genre | Des bacs à sable **manuels**, pas des scènes de tests automatiques |
| Les sujets | Héros et stades ; espèces et nage ; économie et progression ; interface |
| La forme | **Quatre scènes, un seul panneau** ; panneau en IMGUI |

## §1. Ce qui ne change pas

- Les scènes restent **générées** (`GenerateurDeScenes`), jamais retouchées à la main.
- La `Partie` reste un miroir du noyau : l'atelier passe par ses actes (`Grandir`,
  `Renaitre`…) quand ils existent, et sinon construit un état d'essai et le pose par
  `Partie.Remplacer` — le chemin que les captures de contrôle prennent déjà.
- Rien de l'atelier n'entre dans un build : assembly `IdlePond.Atelier` sous contrainte
  `UNITY_EDITOR`, scènes hors des Build Settings.

## §2. La sauvegarde du joueur est intouchable

La `Boucle` sauvegarde toutes les 10 s dans `persistentDataPath`. Un atelier qui donne du
mana écraserait la vraie partie. Donc :

- `ServicesDePartie` porte une **redirection du dossier de sauvegarde** (nulle par défaut,
  remise à nulle par `Oublier`). `Boucle` l'emploie quand elle est posée, pour écrire comme
  pour créer la partie.
- L'`Atelier` s'exécute avant la `Boucle` (ordre −2000) : il pose la redirection vers un
  dossier temporaire et installe sa partie avant que quiconque n'en demande une.

## §3. Le temps

- **Horloge décalée** : l'heure système (via `HorlogeSysteme`, seule autorisée à la lire)
  plus un décalage. « Partir 8 h » décale l'horloge puis appelle `Partie.Reprendre` : le
  chemin réel du retour, écran compris.
- **Accélération** ×1, ×10, ×100 : l'atelier ajoute des pas FIXES de
  `PERIODE_DE_TICK_MS` à ceux de la boucle, jamais un pas de la durée de l'image (§11).
- **Avancer d'1 min / 1 h en jeu** : la même chose, d'un bloc, par pas fixes.

## §4. Les quatre scènes

`Assets/IdlePond/Scenes/Ateliers/Atelier-{Heros,Especes,Economie,Interface}.unity` : la
Mare (caméra, `SceneDeLaMare`, `Boucle`, interface) plus un objet `Atelier`, réglé sur la
section ouverte par défaut et un état de départ.

| Scène | État de départ | Section ouverte |
|---|---|---|
| Héros | La Noue ouverte, héros niveau 1 | Héros |
| Espèces | La Noue ouverte, vairon et loche débloqués | Espèces |
| Économie | Partie neuve | Économie |
| Interface | Mi-partie | Interface |

## §5. Le panneau

| Section | Commandes |
|---|---|
| Héros | Niveau −/+ ; stades 0 à 3 (niveaux 1, 4, 16, 256) ; « Mue » : stade suivant |
| Espèces | Débloquer chaque espèce de l'assise I, ou toutes ; niveau −/+ ; eau troublée oui/non ; paliers ouverts −/+ |
| Économie | Mana ×10 ; contenance pleine ; Souffle +100 ; vitesse ×1/×10/×100 ; +1 min, +1 h ; absence de 8 h ; renaissance ; repartir de zéro |
| Interface | États types : début, mi-partie, contenance pleine, après renaissance ; absence de 8 h (écran de retour) |

Le bandeau du panneau rappelle toujours : niveau et stade du héros, mana / contenance,
Souffle, paliers ouverts, vitesse, dossier de sauvegarde de l'atelier.

## §6. Les états d'essai

Des fonctions pures (`EtatsDEssai`), testées en EditMode : chacune prend un `EtatJeu` et en
rend un autre. Ce sont les seules qui fabriquent un état hors des actes du joueur.

## Critère de réussite

1. Les tests EditMode et PlayMode sont verts, dont : la redirection est oubliée avec la
   partie ; la `Boucle` redirigée n'écrit pas dans `persistentDataPath` ; chaque état
   d'essai rend ce qu'il annonce.
2. Les quatre scènes se génèrent par « IdlePond ▸ Générer les scènes » et se jouent seules.
3. Aucune scène d'atelier dans les Build Settings.
