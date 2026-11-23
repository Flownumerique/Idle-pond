# IdlePond

Jeu idle/incrémental narratif. Le joueur **descend** dans des assises
sous-marines de plus en plus profondes, **convainc** des espèces qui deviennent
ses générateurs, et recueille le **Souffle** qu'exhale le vivant. Quand le
palier suivant coûte plus que ce que sa contenance peut porter, il rentre et
revient plus grand : c'est la **renaissance**.

Unity 6 (`6000.6.3f1`) · C# 9 · UI Toolkit · NUnit (Unity Test Framework) ·
Newtonsoft.Json. Cibles : **mobile (Android, iOS) et PC**, une seule interface
qui s'adapte au portrait et au paysage.

## État

**La Noue et le Gour** (assises I et II, 18 paliers, 6 espèces) sont jouables de bout
en bout dans Unity : creuser, convaincre, monter, grandir, améliorer, renaître, quitter,
revenir et recevoir le crédit hors ligne. La scène est en **pixel art** (URP 2D,
≈240 px de large, lumières 2D) : l'eau des paliers ouverts, la roche obscure de ceux
qu'il reste à creuser, le héros et ses marques. Les sprites sont **provisoires**,
générés par script ; le cahier des charges du vrai dessin est `docs/da/gabarits.md`.

L'écran est pensé pour le **téléphone en portrait** : une barre fine (mana, Souffle,
jauge), la mare au centre — on la touche, et le héros prend : le gain s'envole du
doigt —, « Creuser plus bas » posé dessus, un dock de cinq boutons
(Toi, Espèces, Débloquer, L'œuf, Journal) qui ouvrent des tiroirs. Débloquer montre les
lieux et leur maîtrise, les bonus de lieu et les techniques, achetés au mana. Le tiroir Toi
liste ce que le héros a appris pour toujours (l'arbre de technique : trente nœuds, appris par
l'usage, dont les verbes qui automatisent la redescente). En paysage, le tiroir s'ouvre
à droite. Police : Nunito. Specs : `docs/superpowers/specs/2026-10-07-*.md`.

Le portage depuis le web est fini — spec
[`docs/superpowers/specs/2026-09-27-portage-unity-design.md`](docs/superpowers/specs/2026-09-27-portage-unity-design.md).
Le C# est la seule vérité de la mécanique ; l'ancienne version web (React,
Phaser, Vite) dort dans [`archive/web/`](archive/web/README.md) et ne sert plus
ni à jouer ni à tester.

Ce qui reste à faire, dans l'ordre : [`docs/ROADMAP.md`](docs/ROADMAP.md). Le
lexique et la fiction : [`docs/CODEX.md`](docs/CODEX.md). Qui fait foi sur quoi :
[`docs/PRESEANCE.md`](docs/PRESEANCE.md).

## Commandes

**Éditeur ouvert** — le projet embarque MCP for Unity (`com.coplaydev.unity-mcp`,
v10.3.0) : Claude Code compile, lance les tests, génère, joue et capture dans l'éditeur
ouvert. Les **ateliers** (`Assets/IdlePond/Scenes/Ateliers/`) sont des bacs à sable :
on ouvre `Atelier-Heros`, `-Especes`, `-Economie` ou `-Interface`, on appuie sur Play,
et un panneau change l'état (stades, espèces, mana, vitesse, absence de 8 h…). Leur
sauvegarde va dans un dossier temporaire : la vraie partie n'est jamais touchée.

**Éditeur fermé** — tout ce qui passe en batch :

```sh
outils/unity.sh tests EditMode                 # noyau, simulateur, jeu, parité, canon
outils/unity.sh tests EditMode PariteTests     # un filtre
outils/unity.sh tests PlayMode                 # la mare se joue
outils/unity.sh methode IdlePond.Editeur.GenerateurDeScenes.Generer
outils/unity.sh methode IdlePond.Editeur.GenerateurDeSprites.Generer   # les sprites provisoires
outils/unity.sh captures                                              # les PNG de contrôle dans Logs/captures/ (dont portrait-surface.png et portrait-mue.png)
outils/unity.sh build webgl                                           # prototype navigateur → Build/webgl/
outils/unity.sh build windows                                         # prototype PC → Build/windows/IdlePond.exe
outils/unity.sh build android                                         # APK → Build/android/ (module Android requis)
```

**Tester le prototype sur téléphone** (sans module Android) : `outils/unity.sh build webgl`,
puis `python -m http.server 8080 --bind 0.0.0.0` dans `Build/webgl/`, et ouvrir
`http://<IP du PC>:8080` dans le navigateur du téléphone, sur le même Wi-Fi. Le gabarit
`Assets/WebGLTemplates/IdlePond/` met la toile plein écran ; la save vit dans le stockage du
navigateur (IndexedDB) et survit au rechargement. « Ajouter à l'écran d'accueil » l'ouvre
sans barre d'adresse. Un build modifie au passage `DefaultVolumeProfile.asset`,
`PipelineIdlePond.asset` et `UniversalRenderPipelineGlobalSettings.asset` (préfiltrage URP
recalculé) : à ne pas versionner, `git checkout --` les remet.

Menus de l'éditeur : « IdlePond ▸ Générer les scènes », « Générer les sprites
provisoires », « Parité ▸ Régénérer les références » (changement voulu seulement).

Les scènes (`Assets/IdlePond/Scenes/`) sont **produites par le générateur**,
menu « IdlePond ▸ Générer les scènes », et versionnées telles quelles. On ne les
retouche jamais à la main : on modifie `GenerateurDeScenes.cs` et on relance.

La CI (`.github/workflows/unity.yml`) lance EditMode et PlayMode dans l'éditeur
via game-ci. Elle a besoin des secrets `UNITY_LICENSE`, `UNITY_EMAIL` et
`UNITY_PASSWORD` ; sans eux, elle s'arrête sur un avertissement.

**Le prototype en ligne** (`.github/workflows/webgl-vercel.yml`) : à chaque push sur `main` qui
touche au jeu, game-ci construit le WebGL et l'envoie tout prêt à Vercel, qui ne fait que le
servir (Vercel n'a pas Unity). Il faut, en plus des secrets Unity, `VERCEL_TOKEN`,
`VERCEL_ORG_ID` et `VERCEL_PROJECT_ID` ; sans eux, le job s'arrête sur un avertissement.
`vercel.json` coupe les déploiements automatiques de Vercel, qui construisaient l'ancienne
version web.

## Découpage

```
Assets/IdlePond/
├── Noyau/        PUR (asmdef sans moteur). Reducteur.Tick(etat, dt) -> etat.
│                 Decimal (break_infinity porté), PRNG, données, constantes.
├── Simulateur/   Éditeur seulement. Réutilise le noyau tel quel.
├── Jeu/          Le monde impur : Partie, Persistance, Horloge, HorsLigne,
│                 Boucle, Amorce ; Scene/ (VueDeScene pure, SceneDeLaMare) ;
│                 UI/ (UI Toolkit : Mare.uxml, un contrôleur par panneau).
├── Atelier/      Les bacs à sable — éditeur seulement (contrainte UNITY_EDITOR).
├── Editeur/      Générateurs : scènes, sprites provisoires, catalogue d'art.
├── Scenes/       Demarrage.unity, Mare.unity, Ateliers/ — générées.
├── Art/          Sprites provisoires générés, remplaçables fichier par fichier.
├── Tests/        EditMode. Reference/ : les parties figées que PariteTests et
│                 PariteHorsLigneTests rejouent — produites par le C# depuis le
│                 2026-10-07 (GenerateurDeReferences), par le TypeScript avant.
└── TestsDeJeu/   PlayMode.
```

## Le contrat

Le document de référence est le **prompt de lancement v1.0** (Tier 2). Il n'est
pas dans le dépôt ; il est à relire avant chaque session de build. Ses règles se
vérifient par des tests :

1. `Noyau/` est pur : aucune horloge, aucun `System.Random`, aucun `UnityEngine`,
   aucun champ statique modifiable (`ArchitectureTests`, et l'asmdef).
2. Toute mécanique du cœur se calcule en **un seul pas** pour `dt = 8 h`
   (`EquivalenceDePasTests`).
3. La technique baisse les **coûts** et automatise ; l'amélioration de renaissance monte la
   **production**, et c'est la seule chose qu'elle fait. Aucun nœud, **aucun
   succès** ne franchit cette ligne (`CanonTests`). Une exception, voulue le 2026-10-10 :
   un **bonus de lieu** (payé en mana, perdu à la renaissance) monte la production des
   espèces de *son* lieu, et d'aucune autre. Une seconde, voulue le 2026-11-13 :
   l'amélioration **de séjour** ne monte pas la production, elle raccourcit le temps que met
   la contenance à monter, sur dix rangs au plus.
4. Cent individus d'une espèce valent **×16**, jamais ×1024 — `D = 2.31` est
   calibré contre cette lecture (`SeuilsTests`).
5. Le plafond ne monte **que** par séjour prolongé en mana dense : le ×47,1 par
   renaissance émerge de `A∞` et `τ₀`, il n'est écrit nulle part
   (`ContenanceTests`).
6. Aucun paramètre « à mesurer » n'est inventé : il est une constante nommée,
   commentée `// [P] graine`, dans `Noyau/Constantes.cs` — un seul endroit.
7. **Parité** : les parties de référence se rejouent à 1e-9 près. Elles venaient
   du TypeScript ; depuis le 2026-10-07 (le Gour), elles sont régénérées depuis le
   C#. Une parité rouge ne se corrige ni en relâchant la tolérance, ni en
   régénérant par réflexe : on cherche l'écart, et on ne régénère que pour un
   changement voulu, consigné dans `docs/RESULTATS.md`.
8. **Sauvegarde** : rien d'autre que le jeu n'écrit dans `persistentDataPath`. Les
   ateliers et les tests qui font vivre une `Boucle` redirigent leur sauvegarde
   (`ServicesDePartie.RedirigerLaSauvegarde`).

Le lexique du Codex §5 s'applique **au code, aux identifiants et à l'écran** ;
`LexiqueTests` et `CanonTests` balaient le noyau, le jeu, les `.uxml`, les `.uss`
et `Textes.cs` contre les mots morts et les mots interdits à l'écran.

## Documents à la racine

`Game design.pdf` et `ZONES ET BIOMES v4.docx` sont des sources anciennes. Les
notes de l'ancien projet (gemmes, perles, zones) sont dans
`docs/archive/ancien-projet/` et **ne font plus autorité**.
