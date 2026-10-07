using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Les icônes du dock, en pixel art (spec du 2026-10-07, §2). Des grilles de 12 × 12 :
    /// `#` est un pixel plein, `.` un pixel vide. Écrites en données plutôt que livrées en
    /// image : elles se relisent ici, se dessinent dans la couleur du texte (le bouton actif
    /// change de couleur sans seconde image), et ne demandent aucun paquet.
    /// </summary>
    public static class Icones
    {
        public const int COTE = 12;

        public static readonly string[] TOI =
        {
            "............",
            "............",
            "...#####....",
            ".#########.#",
            "##.#######.#",
            "###########.",
            "###########.",
            ".#########.#",
            "...#####...#",
            "............",
            "............",
            "............",
        };

        public static readonly string[] ESPECES =
        {
            "............",
            ".###.#......",
            "#####.......",
            ".###.#......",
            "............",
            "......###.#.",
            ".....#####..",
            "......###.#.",
            "............",
            ".###.#......",
            "#####.......",
            ".###.#......",
        };

        /// Le trophée du Journal.
        public static readonly string[] JOURNAL =
        {
            "............",
            ".##########.",
            "#.########.#",
            "#.########.#",
            ".#.######.#.",
            "...######...",
            "....####....",
            ".....##.....",
            ".....##.....",
            "....####....",
            "...######...",
            "............",
        };

        /// Le cadenas des lieux et des espèces qu'on n'atteint pas encore.
        public static readonly string[] CADENAS =
        {
            "............",
            "....####....",
            "...#....#...",
            "...#....#...",
            "...#....#...",
            "..########..",
            "..########..",
            "..###..###..",
            "..###..###..",
            "..########..",
            "..########..",
            "............",
        };

        public static readonly string[] OEUF =
        {
            "............",
            ".....##.....",
            "....####....",
            "...######...",
            "...######...",
            "..########..",
            "..########..",
            "..########..",
            "..########..",
            "...######...",
            "....####....",
            "............",
        };

        /// La croix du tiroir : hors du dock, donc hors de `Toutes`.
        public static readonly string[] CROIX =
        {
            "............",
            "............",
            "..##....##..",
            "...##..##...",
            "....####....",
            ".....##.....",
            "....####....",
            "...##..##...",
            "..##....##..",
            "............",
            "............",
            "............",
        };

        /// La goutte du mana, devant son chiffre dans la barre du haut.
        public static readonly string[] GOUTTE =
        {
            "............",
            ".....##.....",
            ".....##.....",
            "....####....",
            "....####....",
            "...######...",
            "..###.####..",
            "..##.#####..",
            "..########..",
            "...######...",
            "....####....",
            "............",
        };

        /// L'étincelle du Souffle, dans sa pastille.
        public static readonly string[] ETINCELLE =
        {
            "............",
            ".....##.....",
            ".....##.....",
            "....####....",
            "..########..",
            "############",
            "############",
            "..########..",
            "....####....",
            ".....##.....",
            ".....##.....",
            "............",
        };

        /// Les quatre icônes du dock, dans son ordre.
        public static readonly IReadOnlyList<(string Nom, string[] Grille)> Toutes = new[]
        {
            ("toi", TOI), ("especes", ESPECES), ("oeuf", OEUF), ("journal", JOURNAL),
        };

        /// Les cases pleines, en (colonne, ligne) depuis le coin haut gauche.
        public static IEnumerable<(int X, int Y)> Pleines(string[] grille)
        {
            for (var y = 0; y < grille.Length; y++)
                for (var x = 0; x < grille[y].Length; x++)
                    if (grille[y][x] == '#') yield return (x, y);
        }
    }

    /// <summary>
    /// Une icône du dock. Sa taille vient du .uss ; chaque pixel de la grille y couvre un
    /// carré entier, arrondi au pixel près, pour que le pixel art reste net.
    /// </summary>
    public sealed class Icone : VisualElement
    {
        readonly string[] grille;

        public Icone(string[] grille)
        {
            this.grille = grille;
            pickingMode = PickingMode.Ignore;
            AddToClassList("icone");
            generateVisualContent += Dessiner;
            // La couleur suit le bouton (actif, doré) : on redessine quand le style change.
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
        }

        void Dessiner(MeshGenerationContext contexte)
        {
            var cote = Mathf.Floor(Mathf.Min(contentRect.width, contentRect.height) / Icones.COTE);
            if (cote <= 0) return;
            var origine = new Vector2(
                Mathf.Floor((contentRect.width - cote * Icones.COTE) / 2),
                Mathf.Floor((contentRect.height - cote * Icones.COTE) / 2));
            var pinceau = contexte.painter2D;
            pinceau.fillColor = resolvedStyle.color;
            pinceau.BeginPath();
            foreach (var (x, y) in Icones.Pleines(grille))
            {
                var coin = origine + new Vector2(x * cote, y * cote);
                pinceau.MoveTo(coin);
                pinceau.LineTo(coin + new Vector2(cote, 0));
                pinceau.LineTo(coin + new Vector2(cote, cote));
                pinceau.LineTo(coin + new Vector2(0, cote));
                pinceau.ClosePath();
            }
            pinceau.Fill();
        }
    }
}
