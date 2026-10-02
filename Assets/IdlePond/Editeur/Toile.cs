using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// Une petite toile de pixels, origine en bas à gauche. Une case vide est transparente ;
    /// une case peinte est opaque — le pixel art n'a pas de demi-teinte d'alpha.
    public sealed class Toile
    {
        public readonly int Largeur, Hauteur;
        readonly int?[] pixels;

        /// Le tramage ordonné 4×4 : il remplace les dégradés, que le pixel art n'a pas.
        static readonly int[,] BAYER = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

        public Toile(int largeur, int hauteur)
        {
            Largeur = largeur;
            Hauteur = hauteur;
            pixels = new int?[largeur * hauteur];
        }

        public static double Seuil(int x, int y) => (BAYER[y & 3, x & 3] + 0.5) / 16.0;

        public bool Dedans(int x, int y) => x >= 0 && y >= 0 && x < Largeur && y < Hauteur;

        public void Poser(int x, int y, int couleur)
        {
            if (Dedans(x, y)) pixels[y * Largeur + x] = couleur;
        }

        public int? Lire(int x, int y) => Dedans(x, y) ? pixels[y * Largeur + x] : null;

        /// Le contour d'un pixel : toute case vide qui touche par un côté une case peinte
        /// d'une autre couleur que le contour.
        public void Contourner(int couleur)
        {
            var aPeindre = new List<(int X, int Y)>();
            var voisins = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            for (var y = 0; y < Hauteur; y++)
                for (var x = 0; x < Largeur; x++)
                {
                    if (Lire(x, y).HasValue) continue;
                    foreach (var (dx, dy) in voisins)
                    {
                        var v = Lire(x + dx, y + dy);
                        if (v.HasValue && v.Value != couleur) { aPeindre.Add((x, y)); break; }
                    }
                }
            foreach (var (x, y) in aPeindre) Poser(x, y, couleur);
        }

        public byte[] EnPng()
        {
            var texture = new Texture2D(Largeur, Hauteur, TextureFormat.RGBA32, false);
            var couleurs = new Color32[Largeur * Hauteur];
            for (var i = 0; i < couleurs.Length; i++)
            {
                var c = pixels[i];
                couleurs[i] = c.HasValue
                    ? new Color32((byte)(c.Value >> 16), (byte)(c.Value >> 8), (byte)c.Value, 255)
                    : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(couleurs);
            texture.Apply();
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}
