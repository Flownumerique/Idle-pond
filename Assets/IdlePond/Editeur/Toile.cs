using System.Collections.Generic;

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

        /// Le PNG de la toile, par l'encodeur pur : mêmes octets dans l'éditeur et hors de lui.
        public byte[] EnPng()
        {
            var rgba = new byte[Largeur * Hauteur * 4];
            for (var y = 0; y < Hauteur; y++)
                for (var x = 0; x < Largeur; x++)
                {
                    var c = pixels[y * Largeur + x];
                    if (!c.HasValue) continue;
                    // La toile a son origine en bas ; le PNG commence par la rangée du haut.
                    var o = ((Hauteur - 1 - y) * Largeur + x) * 4;
                    rgba[o] = (byte)(c.Value >> 16);
                    rgba[o + 1] = (byte)(c.Value >> 8);
                    rgba[o + 2] = (byte)c.Value;
                    rgba[o + 3] = 255;
                }
            return Png.Encoder(Largeur, Hauteur, rgba);
        }
    }
}
