using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.Scene
{
    /// Les six points d'ancrage du corps (GDD §10.3, §15.3) : un par marque possible.
    public enum AncrageId { Branchies, Dos, Flanc, Ventre, Tete, Queue }

    public sealed record Cadre(int Largeur, int Hauteur);

    /// Un pixel d'un sprite, origine en bas à gauche — celle des textures Unity.
    public sealed record Pixel(int X, int Y);

    /// <summary>
    /// IdlePond — les gabarits du dessin (spec DA §3, §4, §5). Données pures : le générateur
    /// de provisoires, les tests et le cahier des charges du vrai dessin les lisent tous les
    /// trois. Changer une valeur ici, c'est changer ce qu'un dessinateur doit livrer.
    ///
    /// Un poisson regarde à droite. Son cadre fait sa longueur plus un pixel de contour de
    /// chaque côté, et une hauteur impaire : le tronc a une ligne médiane exacte.
    /// </summary>
    public static class Gabarits
    {
        public const int LARGEUR_VISEE = 240;
        public const int HAUTEUR_DE_BANDE = 56;
        /// Le décor déborde de la largeur visée des deux côtés : un écran plus large que 240
        /// (k arrondi vers le bas) en montre davantage, jamais un vide.
        public const int LARGEUR_DU_DECOR = 384;
        public const int GAUCHE_DU_DECOR = -72;
        public const int IMAGES_DE_NAGE = 4;
        public const int IMAGES_D_ESPECE = 2;
        public const int LARGEUR_DE_FOND = 32;
        public const int LARGEUR_DE_BERGE = 256;
        public const int HAUTEUR_DES_RAYONS = 112;
        public const int COTE_DU_VOILE = 8;
        /// Le milieu du héros, en x : à droite du centre, pour laisser nager les autres.
        public const int X_DU_HEROS = 150;

        /// Spec DA §3 : quatre tailles dessinées, pas un agrandissement — le pixel art ne
        /// supporte pas les facteurs non entiers.
        public static readonly IReadOnlyList<int> LONGUEURS_DU_CORPS = new[] { 21, 28, 42, 63 };
        public static readonly IReadOnlyList<int> SEUILS_DE_STADE = new[] { 4, 16, 256 };

        public static int StadeDuNiveau(int niveau)
        {
            var stade = 0;
            foreach (var seuil in SEUILS_DE_STADE) if (niveau >= seuil) stade += 1;
            return stade;
        }

        public static Cadre CadreDUnPoisson(int longueur) =>
            new Cadre(longueur + 2, 2 * (int)Math.Ceiling(longueur * 0.2) + 5);

        public static Cadre CadreDuCorps(int stade) => CadreDUnPoisson(LONGUEURS_DU_CORPS[stade]);

        /// De 7 px (rang 0) à 12 px (rang 20) : une espèce plus profonde est un peu plus grande.
        public static int LongueurDEspece(int rang) => 7 + Math.Min(5, Math.Max(0, rang) / 4);

        public static Cadre CadreDEspece(int rang) => CadreDUnPoisson(LongueurDEspece(rang));

        /// Le tronc : une ellipse. Il porte les ancrages et ne bouge pas d'une image de nage
        /// à l'autre — seule la queue bat (spec DA §3).
        public sealed record Tronc(double Cx, double Cy, double A, double B)
        {
            public bool Contient(double x, double y)
            {
                var u = (x - Cx) / A;
                var v = (y - Cy) / B;
                return u * u + v * v <= 1;
            }
        }

        public static Tronc TroncDUnPoisson(int longueur)
        {
            var cadre = CadreDUnPoisson(longueur);
            return new Tronc(1 + 0.65 * longueur, (cadre.Hauteur - 1) / 2.0, 0.35 * longueur, 0.2 * longueur);
        }

        public static Pixel Ancrage(int stade, AncrageId ancrage)
        {
            var longueur = LONGUEURS_DU_CORPS[stade];
            var t = TroncDUnPoisson(longueur);
            double x = t.Cx, y = t.Cy;
            switch (ancrage)
            {
                case AncrageId.Branchies: x += 0.18 * longueur; break;
                case AncrageId.Tete: x += 0.26 * longueur; break;
                case AncrageId.Dos: y += 0.6 * t.B; break;
                case AncrageId.Ventre: y -= 0.6 * t.B; break;
                case AncrageId.Flanc: x -= 0.1 * longueur; break;
                case AncrageId.Queue: x -= 0.26 * longueur; break;
            }
            return new Pixel((int)Math.Round(x), (int)Math.Round(y));
        }
    }
}
