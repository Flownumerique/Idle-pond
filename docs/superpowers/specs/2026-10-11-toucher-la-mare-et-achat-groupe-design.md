# Toucher la mare, et l'achat groupé — design

Écrit le 2026-10-11, validé en conversation le même jour, à partir de la maquette de
l'utilisateur (canevas « IdlePond — Écran principal mobile », version du 2026-10-10 :
« Touche la mare », sélecteur « Acheter ×1 / ×10 / ×25 / MAX »). Des deux, la spec du
2026-10-10 disait : « la mare à toucher serait une mécanique neuve ». L'utilisateur la veut,
et l'achat groupé avec. Le reste de la nouvelle maquette (onglets fusionnés, panneau du bas,
Récits, Bestiaire, popups) est un chantier séparé.

> **Amendé le 2026-11-13 — l'achat groupé de cette spec est remplacé.** Entre-temps, un autre
> achat en lot a été construit et livré : deux puces en tête du tiroir Espèces, « un cran » et
> « jusqu'au seuil » (`Reducteur.AmeliorerJusquAuSeuil`, `Economie.LotDeNiveaux`), plus « Tout
> améliorer » dans le tiroir de l'œuf. L'utilisateur a choisi de **garder ces deux puces** : le
> sélecteur ×1 / ×10 / ×25 / MAX, `AmeliorerPlusieurs`, `GrandirPlusieurs` et le réglage
> `AchatGroupe` décrits plus bas **ne sont pas à construire**. Le reste de la spec — toucher la
> mare, l'ambiant, l'amélioration du toucher — tient, et n'est pas encore implémenté.

## Intention

Le héros devient **actif**. Ce qu'il capte, il le capte quand le joueur touche la mare :
s'il ne touche pas, le héros ne produit rien, et seuls les poissons produisent. Grandir et
les améliorations rendent son toucher plus fort. C'est le Codex (« actif, le héros est un
courant dans l'eau — il prend ») et la consigne du 2026-09-17 : le joueur voit et améliore
SON poisson. Le toucher reste un coup de pouce : un joueur qui ne touche jamais progresse,
le jeu reste un idle.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| À qui revient le toucher | **Au héros** : sans toucher, il ne produit rien ; son niveau et ses améliorations font monter ce qu'un toucher rapporte |
| Le débit passif du héros (`DEBIT_HEROS`, `RESULTATS.md` finding 3) | **Il quitte le héros** et devient un **ambiant fixe**, au même chiffre, indépendant du niveau. Le trou dégénéré du finding 3 reste fermé. Les références de parité sont régénérées une fois |
| Ce qui améliore le toucher, en plus du niveau | **Une amélioration de renaissance**, payée en Souffle, permanente |
| L'achat groupé | **Tout ou rien** pour ×10 et ×25 ; MAX achète autant que le mana le permet (au moins 1) |

## §1. Le noyau

### L'ambiant remplace le débit propre du héros

- `ProductionDuHeros` disparaît. `ProductionTotaleParSeconde` = somme des espèces + **ambiant**.
- `Economie.ProductionAmbiante(etat) = DEBIT_AMBIANT × MultiplicateursGlobaux(etat)`. Pas de
  facteur de niveau. `DEBIT_AMBIANT` reprend la graine `[P]` de `DEBIT_HEROS` (0,05), et son
  commentaire : il ferme le trou dégénéré, il n'a jamais eu besoin d'être grand.
- Le terme `TermeDeFormule.DebitHeros` est renommé `DebitAmbiant` (identifiant
  `debit_ambiant`), toujours de production. `DetailDuHeros` ne porte plus que
  `MultiplicateurHeros` ; le détail de l'ambiant est publié à côté du total.
- `MultiplicateurDuHeros`, le multiplicateur global du niveau, **ne change pas** : Grandir
  continue de monter la production de tous les poissons.
- Le simulateur (`Simulateur.cs`, l'estimation du débit propre) suit : l'ambiant n'y est plus
  multiplié par le niveau. Le simulateur ne touche jamais.

### Le toucher

- `Economie.ValeurDuToucher(etat) = BASE_DU_TOUCHER × NiveauDuHeros × MultiplicateursGlobaux(etat) × MultiplicateurDuToucher(etat)`.
  Même forme que l'ancien débit du héros : il profite de la profondeur et de la densité.
  `BASE_DU_TOUCHER` est une graine `[P]`, ni mesurée ni simulée.
- `Reducteur.Toucher(etat)` rend `(EtatJeu Etat, Decimal Gain)`. Le gain est plafonné par la
  place qui reste sous la contenance : `min(valeur, Contenance − ManaCourant)`. Mare pleine :
  l'état revient inchangé, gain nul.
- Un toucher n'est pas une dépense : il ne crédite aucun compteur de technique, il ne
  déclenche rien dans le tick, il ne joue jamais hors ligne.
- `Economie.DetailDuToucher(etat)` publie les termes nommés (base, niveau, multiplicateurs
  globaux, `MultiplicateurDuToucher`) pour la fiche du héros.

### L'amélioration du toucher

- `PorteeDAmelioration` gagne `Toucher`. Le registre ajoute une entrée,
  `amelioration-toucher` (identifiant figé : il entre dans les saves), après la globale.
- Prix en Souffle, géométrique comme les autres (`base × ratio ^ rang`), par le terme de coût
  `CoutAmelioration`. Effet : `MultiplicateurDuToucher = (1 + PART_PAR_RANG_DU_TOUCHER)^rang`,
  nouveau terme `TermeDeFormule.MultiplicateurDuToucher`, rangé dans `Termes.DE_PRODUCTION`.
  Graines `[P]`.
- Une sauvegarde qui ne connaît pas cette amélioration la lit au rang 0. Pas de nouvelle
  version de save.

### L'achat groupé

- `Reducteur.AmeliorerPlusieurs(etat, especeId, n)` et `Reducteur.GrandirPlusieurs(etat, n)`.
  **Tout ou rien** : si le n-ième niveau n'est pas payable, l'état revient inchangé. Sinon le
  résultat est **le même, au bit près, que n appels successifs** à `Ameliorer` ou `Grandir` :
  mêmes coûts niveau par niveau, drapeau des cent posé si le lot le franchit, compteurs de
  technique crédités niveau par niveau. L'implémentation est cette boucle, pour que
  l'équivalence ne puisse pas dériver.
- Pour l'écran, sans rien changer : `Economie.CoutDeNiveaux(etat, espece, n)` et
  `Economie.CoutDeCroissances(etat, n)` (la somme exacte des n prochains niveaux, calculée sur
  l'état courant comme les achats successifs la paieraient), `Economie.NiveauxAchetables(etat, espece)`
  et `Economie.CroissancesAchetables(etat)` (combien le mana courant en paie, d'affilée).
- MAX = `Plusieurs(achetables)` quand `achetables ≥ 1`.
- `n ≤ 0`, espèce inconnue ou pas convaincue : l'état revient inchangé, comme pour un achat
  refusé.

### Le geste dans le jeu

- `Partie.Toucher(maintenantMs)` applique `Reducteur.Toucher` et rend le gain, pour le chiffre
  qui s'envole. Au plus **10 touchers par seconde**, fenêtre glissante, lue sur l'horloge de
  `Partie` : au-delà, le toucher est ignoré (gain nul, rien n'est émis). La limite vit dans le
  jeu, pas dans le noyau, qui reste sans temps de mur.
- `Partie.Monter(id, lot)` et `Partie.Grandir(lot)` prennent le lot choisi (×1, ×10, ×25,
  MAX) et appellent le réducteur groupé. Un seul `EtatChange` par lot.

## §2. L'interface

### Où l'on touche

- Une couche `#toucher` dans `#centre`, au-dessus de `#scene` et sous tout le reste (lieu,
  retour, annonces, Creuser, voile, tiroir). On touche la mare entière, sauf ses boutons.
  Tiroir ouvert, le voile garde son rôle : toucher referme, ne récolte pas.

### Le chiffre qui s'envole

- `+gain` en couleur mana, au point touché ; il monte d'environ 48 px et s'efface en 0,7 s.
  Un réservoir de 6 libellés réutilisés : aucune allocation par toucher.
- Mouvement réduit : il ne monte pas, il s'efface seulement.
- Gain nul (mare pleine, ou toucher ignoré) : pas de chiffre. La jauge de contenance, éteinte,
  dit déjà pourquoi.
- Pas de son : le jeu n'a pas encore de fichier sonore.

### La fiche du héros (tiroir Toi)

- La ligne du débit propre du héros devient **« par toucher »**, avec `ValeurDuToucher` et son
  détail de termes. `Heros.cs` (la carte Grandir) dit aussi ce que le prochain niveau ajoute
  par toucher, à la place du débit.

### L'amélioration du toucher (tiroir de l'œuf)

- Elle s'affiche d'elle-même : le panneau montre déjà le registre entier. Son nom et son
  effet entrent dans `Textes.cs`, au vocabulaire du Codex, vérifiés par `LexiqueTests`.

### Le sélecteur ×1 / ×10 / ×25 / MAX

- Quatre puces (`filtres`, comme Débloquer), collées en haut du tiroir Espèces et au-dessus de
  la carte Grandir dans le tiroir Toi. Un seul choix pour les deux.
- Enregistré dans les `Reglages` : champ `AchatGroupe` (`Un`, `Dix`, `VingtCinq`, `Max`), lu
  avec repli sur `Un` quand une ancienne sauvegarde de réglages ne le porte pas.
- Monter et Grandir affichent « ×10 · prix », la somme exacte, et s'éteignent si le lot entier
  n'est pas payable. En MAX : « ×N · prix » ; éteints seulement si aucun niveau ne l'est. En
  ×1, l'écran reste celui d'aujourd'hui.
- Les prix groupés ne se calculent que pour le tiroir ouvert, comme le gain de renaissance :
  pas de boucle de coût à chaque pas pour un tiroir fermé.

## §3. Les références de parité

Retirer le niveau du débit ambiant change la production de toute partie dont le héros a
grandi : 0,05 × (niveau − 1) mana/s avant multiplicateurs. C'est voulu. Les références
(`parties.json`, `hors-ligne.json`) sont régénérées par `GenerateurDeReferences`, dans un
commit qui ne fait que cela et le nomme, avec une entrée au journal des références de
`docs/RESULTATS.md`. Les parties de référence ne touchent jamais et n'achètent pas en lot :
le toucher et l'achat groupé n'y entrent pas.

## §4. Tests

EditMode (noyau, hors Unity) :
- `AmeliorerPlusieurs` et `GrandirPlusieurs` égalent n achats successifs, état sérialisé
  compris, y compris quand le lot franchit le 100e niveau ; tout ou rien au refus ;
- `CoutDeNiveaux` et `CoutDeCroissances` égalent la somme payée ; `NiveauxAchetables` est le
  plus grand n payable ;
- `Toucher` ajoute `ValeurDuToucher`, plafonné par la contenance ; mare pleine : état inchangé ;
- l'ambiant ne dépend pas du niveau du héros ; le finding 3 reste fermé (`AmorcageTests`) ;
- `MultiplicateurDuToucher` suit le rang ; une save sans `amelioration-toucher` se lit au rang 0 ;
- la partition des termes est à jour ; `LexiqueTests` passe.

Jeu (EditMode sur `Partie`, PlayMode dans un atelier qui redirige la sauvegarde) :
- le 11e toucher dans la même seconde est ignoré ;
- toucher la couche `#toucher` monte le mana et montre un chiffre ;
- changer le sélecteur réécrit les prix ; MAX achète le bon nombre de niveaux ;
- le sélecteur survit au rechargement des réglages.

## Critère de réussite

1. Tests verts, EditMode et PlayMode, références régénérées et journalisées.
2. Dans l'atelier, en portrait comme en paysage : toucher la mare fait monter le mana et
   s'envoler le chiffre ; ×10, ×25 et MAX achètent et affichent le bon prix. Captures relues
   par l'utilisateur.

## Ce qui reste

- **La mesure** : `BASE_DU_TOUCHER` et les graines de l'amélioration du toucher sont des
  graines. Il faudra une politique de toucher au simulateur (par exemple N touchers par
  minute de jeu actif) pour les régler sans casser le rythme mesuré dans `RESULTATS.md`.
- Le reste de la maquette du 2026-10-10, dans un chantier à part.
