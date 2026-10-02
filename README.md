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

La Noue est jouable de bout en bout dans Unity : creuser, convaincre, monter,
grandir, insuffler, renaître, quitter, revenir et recevoir le crédit hors ligne.
La scène est en **pixel art** (URP 2D, ≈240 px de large, lumières 2D) avec des sprites **provisoires** générés
par script ; le cahier des charges du vrai dessin est `docs/da/gabarits.md`.

Le portage depuis le web est fini — spec
[`docs/superpowers/specs/2026-09-27-portage-unity-design.md`](docs/superpowers/specs/2026-09-27-portage-unity-design.md).
Le C# est la seule vérité de la mécanique ; l'ancienne version web (React,
Phaser, Vite) dort dans [`archive/web/`](archive/web/README.md) et ne sert plus
ni à jouer ni à tester.

Ce qui reste à faire, dans l'ordre : [`docs/ROADMAP.md`](docs/ROADMAP.md). Le
lexique et la fiction : [`docs/CODEX.md`](docs/CODEX.md). Qui fait foi sur quoi :
[`docs/PRESEANCE.md`](docs/PRESEANCE.md).

## Commandes

L'éditeur Unity doit être **fermé** sur ce projet pour tout ce qui passe en batch :

```sh
outils/unity.sh tests EditMode                 # noyau, simulateur, jeu, parité, canon
outils/unity.sh tests EditMode PariteTests     # un filtre
outils/unity.sh tests PlayMode                 # la mare se joue
outils/unity.sh methode IdlePond.Editeur.GenerateurDeScenes.Generer
outils/unity.sh methode IdlePond.Editeur.GenerateurDeSprites.Generer   # les sprites provisoires
outils/unity.sh captures                                              # 17 PNG de contrôle dans Logs/captures/ (dont portrait-surface.png)
```

Les scènes (`Assets/IdlePond/Scenes/`) sont **produites par le générateur**,
menu « IdlePond ▸ Générer les scènes », et versionnées telles quelles. On ne les
retouche jamais à la main : on modifie `GenerateurDeScenes.cs` et on relance.

La CI (`.github/workflows/unity.yml`) lance EditMode et PlayMode dans l'éditeur
via game-ci. Elle a besoin des secrets `UNITY_LICENSE`, `UNITY_EMAIL` et
`UNITY_PASSWORD` ; sans eux, elle s'arrête sur un avertissement.

## Découpage

```
Assets/IdlePond/
├── Noyau/        PUR (asmdef sans moteur). Reducteur.Tick(etat, dt) -> etat.
│                 Decimal (break_infinity porté), PRNG, données, constantes.
├── Simulateur/   Éditeur seulement. Réutilise le noyau tel quel.
├── Jeu/          Le monde impur : Partie, Persistance, Horloge, HorsLigne,
│                 Boucle, Amorce ; Scene/ (VueDeScene pure, SceneDeLaMare) ;
│                 UI/ (UI Toolkit : Mare.uxml, un contrôleur par panneau).
├── Editeur/      GenerateurDeScenes.
├── Scenes/       Demarrage.unity, Mare.unity — générées.
├── Tests/        EditMode. Reference/ : les parties figées produites par le
│                 TypeScript, que PariteTests et PariteHorsLigneTests rejouent.
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
3. La technique baisse les **coûts** et automatise ; l'insufflation monte la
   **production**, et c'est la seule chose qu'elle fait. Aucun nœud, **aucun
   succès** ne franchit cette ligne (`CanonTests`).
4. Cent individus d'une espèce valent **×16**, jamais ×1024 — `D = 2.31` est
   calibré contre cette lecture (`SeuilsTests`).
5. Le plafond ne monte **que** par séjour prolongé en mana dense : le ×47,1 par
   renaissance émerge de `A∞` et `τ₀`, il n'est écrit nulle part
   (`ContenanceTests`).
6. Aucun paramètre « à mesurer » n'est inventé : il est une constante nommée,
   commentée `// [P] graine`, dans `Noyau/Constantes.cs` — un seul endroit.
7. **Parité** : les parties de référence du TypeScript se rejouent à 1e-9 près.
   Une parité rouge ne se corrige ni en relâchant la tolérance, ni en
   régénérant les références : on cherche l'écart.

Le lexique du Codex §5 s'applique **au code, aux identifiants et à l'écran** ;
`LexiqueTests` et `CanonTests` balaient le noyau, le jeu, les `.uxml`, les `.uss`
et `Textes.cs` contre les mots morts et les mots interdits à l'écran.

## Documents à la racine

`Game design.pdf` et `ZONES ET BIOMES v4.docx` sont des sources anciennes. Les
notes de l'ancien projet (gemmes, perles, zones) sont dans
`docs/archive/ancien-projet/` et **ne font plus autorité**.
