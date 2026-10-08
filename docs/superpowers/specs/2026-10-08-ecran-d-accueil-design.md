# Écran d'accueil — menu démarrer

Date : 2026-10-08. Statut : design validé en conversation.

## Intention

Un vrai écran de démarrage : le titre, la mare qui vit derrière, et le choix de continuer,
recommencer, régler ou quitter. Demande de l'utilisateur : « un menu démarrer pour le jeu
avec un écran d'accueil » ; contenu proposé et accepté tel quel.

## Parcours

`Demarrage.unity` → `Amorce` : regarde si une sauvegarde existe, ouvre la partie (charger +
créditer l'absence), l'installe, **demande l'accueil** (`ServicesDePartie.DemanderLAccueil(premiereFois)`),
charge `Mare`.

Dans `Mare`, si l'accueil est demandé, une couche `#accueil` couvre toute l'interface. La
mare dessinée tourne derrière, assombrie par un voile. La partie avance pendant ce temps
(un idle ne s'arrête pas). `Mare` lancée seule (éditeur, tests, ateliers) : pas d'accueil.
La demande est consommée à l'affichage et oubliée par `ServicesDePartie.Oublier`.

## Contenu

- Titre « IdlePond », sous-titre court.
- Carte de résumé (partie en cours) : lieu le plus bas ouvert, mana courant, « absent 3 h »
  si une absence a été créditée.
- Boutons :
  - **Continuer** (partie en cours) ou **Commencer** (première partie) — principal.
  - **Nouvelle partie** — seulement s'il y a une partie en cours ; confirmation en deux
    touchers ; `Partie.Reinitialiser` (copie gardée) puis entrée dans la mare.
  - **Réglages** — ouvre le tiroir Réglages par-dessus l'accueil ; le fermer y ramène.
  - **Quitter** — hors mobile ; `Application.Quit`.
- Version (`Application.version`) en pied.

Entrer dans la mare : la couche s'efface (fondu 0,4 s, 0 en mouvement réduit) puis disparaît ;
la carte « pendant ton absence » se voit ensuite comme aujourd'hui.

## Composants

- `ServicesDePartie` : `DemanderLAccueil(bool premiereFois)`, `AccueilDemande`,
  `PremiereFois`, `ConsommerLAccueil()`.
- `Jeu/UI/ResumeDeLaPartie.cs` — pur : `De(EtatJeu, AbsenceCreditee, bool premiereFois)` →
  libellé principal, lieu, mana, absence (ou null), `NouvellePartiePossible`.
- `Jeu/UI/Accueil.cs` — contrôleur de la couche ; branché par `RacineDeLInterface`.
- `Mare.uxml` : `<VisualElement name="accueil">` en dernier enfant de `#racine`.
- Styles dans `Mare.uss`, adaptés par `.telephone/.tablette/.pc` (colonne centrée, largeur
  max sur tablette et PC).
- Textes dans `Textes.Ecran`.

## Tests

- EditMode `ResumeDeLaPartieTests` : première partie (Commencer, pas de Nouvelle partie),
  partie en cours (Continuer, lieu, mana), absence présente ou non.
- PlayMode `AccueilJouableTests` (vrais touchers) : accueil montré si demandé, absent sinon ;
  Continuer le retire ; Nouvelle partie : un toucher ne fait rien, deux remettent un état
  neuf ; Réglages ouvre le tiroir.
- Captures : `ui-<format>-accueil.png`.

## Hors périmètre

Revenir à l'accueil depuis le jeu, musique d'accueil, logo dessiné.
