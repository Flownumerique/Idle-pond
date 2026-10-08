# Réglages — menu, son, formats d'affichage

Date : 2026-10-08. Statut : design validé en conversation, spec à relire.

## Intention

Le joueur doit pouvoir régler le jeu comme n'importe quel jeu : le son, l'affichage, quelques
options de jeu et d'accessibilité. Surtout, l'interface doit être **agréable sur téléphone,
tablette et PC** — chacun avec sa mise en page et sa taille — pour jouer et travailler
confortablement sur les trois. Le format est détecté automatiquement et peut être forcé
dans les réglages (utile pour voir le rendu téléphone en travaillant sur PC).

Ce que l'utilisateur a demandé : un menu Réglages, volume du son et de la musique, de
l'affichage, trois formats (téléphone, PC, tablette) qui redimensionnent l'UI. Il a choisi
« détection auto + forçage manuel » et validé toute la liste d'options ci-dessous.

Supposé (non contredit) : chaque format change la **disposition et la taille** ; un curseur
« Taille de l'interface » s'ajoute par-dessus.

## État de départ

- UI Toolkit : `Mare.uxml`, `Mare.uss`, `Theme.uss`, contrôleur `RacineDeLInterface`.
- `Adaptation.EstPaysage` pose déjà `.portrait` / `.paysage` sur `#racine` (seuil 1,2) ; la
  safe area est gérée.
- `PanelSettings` : ScaleWithScreenSize, référence 1080×1920, match 0,5.
- Aucun son dans le projet : ni fichier audio, ni `AudioSource`, ni mixer.
- Aucun stockage de préférences ; `idlepond.json` ne contient que la partie.
- `Format` est statique et pur ; les montants utilisent la culture invariante (« 1.25 M »).

## Contenu du menu

| Onglet | Réglage | Valeurs | Défaut |
|---|---|---|---|
| Son | Volume général | 0–100 % | 80 % |
| Son | Musique | 0–100 % | 70 % |
| Son | Effets | 0–100 % | 80 % |
| Son | Couper le son | oui/non | non |
| Son | Couper en arrière-plan | oui/non | oui |
| Affichage | Format | Auto / Téléphone / Tablette / PC | Auto |
| Affichage | Taille de l'interface | 90–130 % (pas de 5) | 100 % |
| Affichage | Plein écran (PC et éditeur seulement) | oui/non | selon l'état actuel de la fenêtre |
| Affichage | Images par seconde | 30 / 60 / illimitée | 60 sur PC, 30 sur mobile |
| Jeu | Notation des nombres | Suffixes / Scientifique / Ingénieur | Suffixes |
| Jeu | Annonces de succès | affichées / masquées | affichées |
| Jeu | Réinitialiser la partie | bouton à confirmation | — |
| Accessibilité | Réduire les animations | oui/non | non |
| Accessibilité | Contraste renforcé | oui/non | non |
| (pied du menu) | Rétablir les réglages par défaut | bouton à confirmation | — |

Tous les textes à l'écran vont dans `Textes.Ecran` (`Noyau/Donnees/Textes.cs`).

## Architecture

### Répartition

Nouveau dossier `Assets/IdlePond/Jeu/Reglages/` :

- **`Reglages.cs`** — pur, sans UnityEngine. Un record immuable + énumérations
  (`FormatDAffichage { Auto, Telephone, Tablette, Pc }`, `Notation`, `LimiteDImages`,
  `Canal { Musique, Effets }`). Valeurs par défaut, `Borner()` (volumes 0–1, taille
  0,9–1,3 arrondie au pas de 0,05), sérialisation JSON versionnée (`"version": 1`),
  désérialisation tolérante : champ absent → défaut, champ invalide → défaut, version
  inconnue plus récente → défauts complets.
- **`Son.cs`** — pur : `VolumeEffectif(Canal, Reglages) = general × canal × (muet ? 0 : 1)`.
- **`MagasinDeReglages.cs`** — lit/écrit `reglages.json` dans un dossier **injecté**
  (même principe que `Persistance` : `Application.persistentDataPath` n'est lu qu'au point
  d'entrée). Écriture atomique (fichier temporaire puis remplacement), sans BOM. Fichier
  illisible → défauts, journalisé, jamais d'exception. Expose `Courants` et l'événement
  `Change`. `Modifier(Func<Reglages, Reglages>)` borne, notifie tout de suite et enregistre
  après 0,5 s sans autre changement (et à `OnApplicationPause`/`OnApplicationQuit`).
- **`AppliqueurDAffichage.cs`** (MonoBehaviour) — format, échelle, plein écran, fps.
- **`SourceSonore.cs`** (MonoBehaviour) — à poser à côté d'un `AudioSource` ; déclare son
  `Canal` et règle `AudioSource.volume` = volume de base × `VolumeEffectif`.
- **`SonDeFond.cs`** (MonoBehaviour) — `AudioListener.pause` sur perte de focus / pause
  si « couper en arrière-plan ».
- **`UI/MenuDesReglages.cs`** — le contenu du tiroir Réglages (onglets, lignes, contrôles).

Le magasin est créé par `Boucle` (même durée de vie que la partie, survit au rechargement
de scène) et accessible comme la partie l'est aujourd'hui (`Boucle.ObtenirOuCreerLesReglages()`).
Les ateliers et les tests de jeu redirigent son dossier comme ils redirigent la sauvegarde.

`ArchitectureTests` gagne une règle : `Jeu/Reglages/Reglages.cs` et `Son.cs` ne
référencent ni UnityEngine ni UnityEditor.

### Format d'affichage

`Adaptation` gagne une fonction pure :

```
FormatDAffichage Detecter(int largeurPx, int hauteurPx, float dpi, bool plateformeMobile)
```

- non mobile (PC, éditeur, WebGL de bureau) → `Pc` ;
- mobile : diagonale = √(l² + h²) / dpi ; < 7″ → `Telephone`, sinon `Tablette` ;
- mobile, dpi ≤ 0 ou illisible : plus petite dimension < 1200 px → `Telephone`, sinon
  `Tablette`.

`Resoudre(reglage, detecte)` : `Auto` → détecté, sinon la valeur forcée.

`RacineDeLInterface.Adapter` pose **une** classe parmi `.telephone`, `.tablette`, `.pc` en
plus de `.portrait` / `.paysage` (l'orientation reste calculée comme aujourd'hui). Il
réadapte aussi quand les réglages changent.

**Échelle.** `PanelSettings.scale` = facteur du format × taille de l'interface :
téléphone 1,0, tablette 1,0, PC 0,85. Les facteurs sont des constantes d'`Adaptation`,
ajustables après les captures.

**Disposition** (`Mare.uss`) :

- **Téléphone** : la mise en page actuelle, inchangée.
- **Tablette** : marges plus larges ; en portrait, le tiroir est limité à ~640 px et centré ;
  les listes de cartes (espèces, améliorations) passent sur deux colonnes (`flex-wrap`,
  cartes à ~48 %) quand la largeur le permet.
- **PC** : en paysage, le dock devient une colonne verticale à gauche, le tiroir reste à
  droite ; états `:hover` visibles sur les boutons et cartes ; curseur de souris normal.

### Ouverture du menu

Une roue dentée (`Icones.ENGRENAGE`, à dessiner dans `Icones.cs` comme les autres) à droite
de la `Barre`. Elle ouvre `Tiroir.Reglages`, nouveau membre de l'énumération, **sans bouton
dans le dock**. Le tiroir réutilise tout le mécanisme existant (voile, glissement, croix).
Contenu : `<ui:VisualElement name="tiroir-reglages" class="tiroir-contenu">` dans
`Mare.uxml`. Les onglets sont une rangée de quatre boutons en tête du contenu ; l'onglet
ouvert est retenu le temps de la session.

Changements appliqués **immédiatement**, pas de bouton Valider.

### Son

Pas d'`AudioMixer` : Unity n'a pas d'API publique pour en créer un par script, et tout
le projet est généré par code. `SourceSonore` suit les réglages à chaque `Change`. Ajouter
une musique = un `AudioSource` + `SourceSonore` (canal Musique) dans la scène. **On
n'entend rien tant qu'aucun son n'est ajouté** — hors périmètre.

### Affichage

- Plein écran : `Screen.fullScreenMode` (`FullScreenWindow` / `Windowed`). Ligne cachée
  hors PC/éditeur. Le défaut lu au premier lancement est l'état réel de la fenêtre.
- Images par seconde : `Application.targetFrameRate` (30, 60, ou -1). `QualitySettings.vSyncCount = 0`
  quand une limite est posée, sans quoi elle est ignorée sur PC.

### Jeu

- **Notation.** `Format` gagne une propriété statique `Notation` (défaut `Suffixes`), lue
  par `Montant` et ses voisins ; ses 21 appelants ne changent pas. L'appliqueur la pose
  puis demande un rafraîchissement (`SurEtat(partie.Etat)`) pour réécrire tous les
  nombres. Formats : *Suffixes* `1.25 M` (actuel) ; *Scientifique* `1.25e+6` (l'actuel
  `Exponentielle`) ; *Ingénieur* exposant multiple de 3 : `1.25e+6`, `12.5e+6`, `125e+6`.
  Sous 1000, les trois notations écrivent le nombre tel quel. Les tests de `Format` remettent
  `Notation` à `Suffixes` dans leur `SetUp`.
- **Annonces masquées.** `Annonces` ne montre plus le bandeau ; les succès arrivent dans le
  Journal et la pastille du dock compte toujours. Les annonces en attente sont oubliées
  (`OublierAnnonce`) pour ne pas resurgir quand on réactive.
- **Réinitialiser la partie.** Bouton rouge ; premier toucher → le libellé devient
  « Toucher encore pour confirmer » pendant 3 s ; second toucher dans le délai → la
  sauvegarde actuelle est **copiée** en `idlepond.avant-reinitialisation-<horodatage>.json`
  (même nommage que les sauvegardes corrompues), puis `partie.Remplacer(Partie.NouvelEtat(horloge))`
  et sauvegarde immédiate. Le tiroir se ferme. Les réglages ne sont pas touchés.
- **Rétablir les réglages par défaut** : même confirmation en deux temps.

### Accessibilité

- **Réduire les animations** : classe `.mouvement-reduit` sur `#racine` ; le `.uss` y met
  `transition-duration: 0s` sur le tiroir, le voile et les annonces. `RacineDeLInterface`
  utilise un délai de 0 au lieu de `DUREE_DU_TIROIR_MS` dans ce cas.
- **Contraste renforcé** : classe `.contraste` sur `#racine` qui redéfinit des variables de
  `Theme.uss` : `--couleur-jour-doux` et `--couleur-jour-tu` éclaircis, `--couleur-bord`
  et `--bord-60` plus marqués, `--voile` plus opaque. Aucun style dupliqué.

## Erreurs

- Fichier de réglages illisible ou d'une version future → défauts, message dans la console,
  le fichier n'est écrasé qu'à la prochaine modification.
- Échec d'écriture → journalisé, le jeu continue ; nouvel essai au changement suivant.
- Une exception dans un appliqueur est journalisée et n'empêche pas les autres de
  s'appliquer (même règle que les panneaux dans `SurEtat`).

## Tests

**EditMode (`IdlePond.Tests`)**
- `AdaptationTests` : détection — 6,9″ / 7,0″ mobile, dpi 0 avec 1199/1200 px, PC ;
  `Resoudre` avec Auto et chaque forçage ; facteur d'échelle × taille.
- `ReglagesTests` : défauts ; bornage (−1, 2, 1,27 → 1,25) ; aller-retour JSON ; champ
  absent ; champ du mauvais type ; version future ; JSON tronqué.
- `MagasinDeReglagesTests` (dossier temporaire) : premier lancement, écriture puis relecture,
  fichier corrompu → défauts sans exception.
- `SonTests` : `VolumeEffectif` (muet, zéros, produit).
- `FormatTests` : les trois notations, sous et au-dessus de 1000, retenue 999.5 k.
- `ArchitectureTests` : pureté de `Reglages.cs` et `Son.cs`.

**PlayMode (`IdlePond.TestsDeJeu`)**, sauvegarde et réglages redirigés :
- la roue ouvre `Tiroir.Reglages` ;
- forcer chaque format pose la bonne classe sur `#racine` et une seule ;
- la réinitialisation confirmée remet un état neuf et laisse la copie de sauvegarde ;
  un seul toucher ne fait rien.

**Vérification visuelle** : captures (comme `CapturesDeControle`) en 1080×2340 (téléphone),
1640×2360 et 2360×1640 (tablette), 1920×1080 (PC), avec et sans contraste renforcé.
Nécessite l'éditeur Unity avec le pont UnityMCP actif.

## Hors périmètre

Les sons et musiques eux-mêmes ; le choix de langue ; les raccourcis clavier ; la
synchronisation des réglages entre appareils.
