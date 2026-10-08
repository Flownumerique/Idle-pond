# Écran d'accueil — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.

**Goal:** un écran d'accueil par-dessus la Mare au lancement : titre, résumé, Continuer/Commencer, Nouvelle partie, Réglages, Quitter.

**Architecture:** l'`Amorce` demande l'accueil via `ServicesDePartie` ; `RacineDeLInterface` branche un contrôleur `Accueil` sur la couche `#accueil` ; le résumé est une fonction pure.

**Tech Stack:** Unity 6000.6.3f1, C# 9, UI Toolkit, NUnit.

**Spec:** `docs/superpowers/specs/2026-10-08-ecran-d-accueil-design.md`

## Global Constraints

- Textes dans `Textes.Ecran` ; aucun style en C# ; l'interface ne lit pas l'horloge (`Time` interdit dans `Jeu/UI`).
- `Mare` lancée seule n'affiche jamais l'accueil (tests, ateliers).
- Tests : `outils/unity.sh tests EditMode|PlayMode [filtre]`, captures : `outils/unity.sh captures`.

## Review Focus

1. Accueil affiché pendant qu'une annonce de succès ou la carte de retour arrive → elles restent sous la couche, visibles après.
2. Nouvelle partie armée puis Continuer → rien n'est effacé.
3. Réglages ouverts depuis l'accueil puis fermés → l'accueil est toujours là et utilisable.
4. Rechargement de la Mare dans la même session (tests) → l'accueil ne réapparaît pas.

---

### Task 1: résumé pur et demande d'accueil
**Files:** Create `Jeu/UI/ResumeDeLaPartie.cs`, `Tests/ResumeDeLaPartieTests.cs` ; Modify `Jeu/ServicesDePartie.cs`, `Noyau/Donnees/Textes.cs`.
- [ ] Tests rouges → implémentation → EditMode vert → commit `feat(accueil): le résumé de la partie`.

### Task 2: la couche d'accueil
**Files:** Create `Jeu/UI/Accueil.cs`, `TestsDeJeu/AccueilJouableTests.cs` ; Modify `Jeu/Amorce.cs`, `Jeu/UI/RacineDeLInterface.cs`, `Jeu/UI/Mare.uxml`, `Jeu/UI/Mare.uss`.
- [ ] Tests PlayMode rouges → implémentation → PlayMode + EditMode verts → commit `feat(accueil): l'écran d'accueil`.

### Task 3: captures
**Files:** Modify `TestsDeJeu/CapturesDeControleDesReglages.cs`.
- [ ] Captures produites et relues → commit `test(accueil): captures`.
