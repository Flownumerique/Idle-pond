using System;
using System.Collections.Generic;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    /// La palette d'une assise.
    /// `Fond` : la roche derrière l'eau, 0xRRGGBB. `Eau` : l'eau elle-même.
    /// `Lumiere` : de 0 à 1, ce qu'il reste de jour — elle décroît strictement en descendant.
    public sealed record PaletteDAssise(int Fond, int Eau, double Lumiere);

    /// Les marques du corps du héros, une par assise fixée (GDD §15.1).
    public enum MarqueId { Branchies, Membranes, Luminescence, Mineralisation, Epaississement, Halo }

    /// <summary>
    /// IdlePond — palettes et marques, données pures de la scène.
    ///
    /// GDD §15.2 : « la lumière est la variable de progression la plus lisible. Elle
    /// décroît continûment jusqu'à ce que la seule lumière restante soit celle que le
    /// mana produit. » Six palettes, une par assise, la lumière qui baisse.
    ///
    /// GDD §15.1 : « un corps de base sur lequel s'ajoutent des marques, une par assise
    /// fixée : branchies, membranes, luminescence, minéralisation, épaississement. »
    /// Cinq marques nommées pour six assises : la sixième, le halo, est un [P] — à
    /// confirmer par la DA.
    ///
    /// Aucune image : §15.3 bloque le premier sprite définitif sur l'anatomie du corps de
    /// base, et §15.4 met l'imagerie de l'Étang des Merveilles hors registre.
    ///
    /// Ce fichier ne touche pas à UnityEngine : les couleurs sont des entiers 0xRRGGBB,
    /// que `SceneDeLaMare` convertit en `Color`. C'est ce qui le rend testable ici.
    /// </summary>
    public static class Palette
    {
        /* ─── Les jetons de index.css, convertis une fois en sRGB ──────────────────
           Source : `--color-*` de src/index.css, en oklch, converties par la matrice
           OKLab → LMS → sRGB linéaire de Björn Ottosson, puis la courbe sRGB. Les
           mêmes valeurs vivent dans Theme.uss : le décor et l'interface partagent
           une seule eau. */

        /// oklch(0.17 0.03 240) — le fond de la caméra et de la page.
        public const int EAU_ABYSSE = 0x03111B;
        /// oklch(0.22 0.035 235)
        public const int EAU_FOND = 0x081D28;
        /// oklch(0.30 0.04 230)
        public const int EAU_BORD = 0x16323E;
        /// oklch(0.42 0.05 225)
        public const int EAU_CLAIR = 0x2D5362;
        /// oklch(0.93 0.012 220)
        public const int JOUR = 0xE0EAED;
        /// oklch(0.78 0.11 195)
        public const int MANA = 0x4FCDCD;

        /* ─── Les couleurs propres à la scène (portées de SceneDeLaMare.ts) ──────── */

        /// Le voile de l'eau qui se trouble : un limon, pas une alerte (GDD §2.4).
        public const int VOILE_DE_TROUBLE = 0x8A7A3A;
        /// La lumière du jour sur la bande, un voile clair qui s'amincit en descendant.
        public const int LUMIERE_DU_JOUR = 0xDDE9E6;
        public const int CORPS_DU_HEROS = 0xB9C7C2;
        public const int OEIL_DU_HEROS = 0x1B2422;

        static readonly PaletteDAssise[] PALETTES_PAR_RANG =
        {
            new PaletteDAssise(0x24312B, 0x2F5F5A, 1.0),
            new PaletteDAssise(0x1D2A2C, 0x244A52, 0.75),
            new PaletteDAssise(0x17222A, 0x1B3A4B, 0.5),
            new PaletteDAssise(0x121A24, 0x152B3D, 0.32),
            new PaletteDAssise(0x1A1414, 0x2A1C1C, 0.18),
            new PaletteDAssise(0x0B0D14, 0x0F1424, 0.08),
        };

        static readonly MarqueId[] MARQUES_PAR_RANG =
        {
            MarqueId.Branchies, MarqueId.Membranes, MarqueId.Luminescence,
            MarqueId.Mineralisation, MarqueId.Epaississement, MarqueId.Halo,
        };

        static Dictionary<string, T> ParAssise<T>(IReadOnlyList<T> parRang)
        {
            var resultat = new Dictionary<string, T>();
            for (var i = 0; i < Assises.Toutes.Count; i += 1) resultat[Assises.Toutes[i].Id] = parRang[i];
            return resultat;
        }

        public static readonly IReadOnlyDictionary<string, PaletteDAssise> Palettes = ParAssise(PALETTES_PAR_RANG);

        public static readonly IReadOnlyDictionary<string, MarqueId> MarqueParAssise = ParAssise(MARQUES_PAR_RANG);

        /// La palette d'une assise ; une assise inconnue prend la plus profonde, la plus sombre.
        public static PaletteDAssise PaletteDe(string assise) =>
            Palettes.TryGetValue(assise ?? "", out var palette) ? palette : PALETTES_PAR_RANG[PALETTES_PAR_RANG.Length - 1];

        /// <summary>
        /// Couleur des poissons d'une espèce, par rang : une teinte qui tourne, une clarté
        /// qui baisse. C'est le HSV (teinte/360, 0,35, 0,85 − min(0,5 ; rang·0,02)) du
        /// TypeScript, converti ici plutôt que dans Unity pour rester pur.
        /// </summary>
        public static int CouleurDesPoissons(int rang)
        {
            var teinte = ((rang * 47) % 360) / 360.0;
            var saturation = 0.35;
            var valeur = 0.85 - Math.Min(0.5, rang * 0.02);
            var secteur = teinte * 6;
            var i = (int)Math.Floor(secteur) % 6;
            var f = secteur - Math.Floor(secteur);
            var p = valeur * (1 - saturation);
            var q = valeur * (1 - f * saturation);
            var t = valeur * (1 - (1 - f) * saturation);
            double r, g, b;
            switch (i)
            {
                case 0: r = valeur; g = t; b = p; break;
                case 1: r = q; g = valeur; b = p; break;
                case 2: r = p; g = valeur; b = t; break;
                case 3: r = p; g = q; b = valeur; break;
                case 4: r = t; g = p; b = valeur; break;
                default: r = valeur; g = p; b = q; break;
            }
            return (Octet(r) << 16) | (Octet(g) << 8) | Octet(b);
        }

        static int Octet(double composante) => (int)Math.Round(Math.Min(1, Math.Max(0, composante)) * 255);
    }
}
