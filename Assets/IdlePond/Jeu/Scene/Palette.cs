using System.Collections.Generic;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    /// La palette d'une assise.
    /// `Fond` : la roche derrière l'eau, 0xRRGGBB. `Eau` : l'eau elle-même.
    /// `Lumiere` : de 0 à 1, ce qu'il reste de jour — elle décroît strictement en descendant.
    public sealed record PaletteDAssise(int Fond, int Eau, double Lumiere);

    /// <summary>
    /// IdlePond — palettes, données pures de la scène.
    ///
    /// GDD §15.2 : « la lumière est la variable de progression la plus lisible. Elle
    /// décroît continûment jusqu'à ce que la seule lumière restante soit celle que le
    /// mana produit. » Six palettes, une par assise, la lumière qui baisse.
    ///
    /// Les marques et les couleurs des poissons vivent désormais dans `RegistreDArt` (spec DA).
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

        static readonly PaletteDAssise[] PALETTES_PAR_RANG =
        {
            new PaletteDAssise(0x24312B, 0x2F5F5A, 1.0),
            new PaletteDAssise(0x1D2A2C, 0x244A52, 0.75),
            new PaletteDAssise(0x17222A, 0x1B3A4B, 0.5),
            new PaletteDAssise(0x121A24, 0x152B3D, 0.32),
            new PaletteDAssise(0x1A1414, 0x2A1C1C, 0.18),
            new PaletteDAssise(0x0B0D14, 0x0F1424, 0.08),
        };

        static Dictionary<string, T> ParAssise<T>(IReadOnlyList<T> parRang)
        {
            var resultat = new Dictionary<string, T>();
            for (var i = 0; i < Assises.Toutes.Count; i += 1) resultat[Assises.Toutes[i].Id] = parRang[i];
            return resultat;
        }

        public static readonly IReadOnlyDictionary<string, PaletteDAssise> Palettes = ParAssise(PALETTES_PAR_RANG);

        /// La palette d'une assise ; une assise inconnue prend la plus profonde, la plus sombre.
        public static PaletteDAssise PaletteDe(string assise) =>
            Palettes.TryGetValue(assise ?? "", out var palette) ? palette : PALETTES_PAR_RANG[PALETTES_PAR_RANG.Length - 1];
    }
}
