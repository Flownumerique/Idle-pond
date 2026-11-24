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
        /// La scène fait 360 px de large (retenu le 2026-11-23, Codex v1.4 ; 240 avant) : une
        /// fois et demie plus fine, pour des poissons détaillés. Toutes les longueurs du dessin
        /// ont suivi d'un même facteur 1,5 : à l'écran, la mise en page ne bouge pas.
        public const int LARGEUR_VISEE = 360;
        public const int HAUTEUR_DE_BANDE = 84;
        /// Le décor déborde de la largeur visée des deux côtés : un écran plus large que 360
        /// (k arrondi vers le bas) en montre davantage, jamais un vide.
        public const int LARGEUR_DU_DECOR = 576;
        public const int GAUCHE_DU_DECOR = -108;
        public const int IMAGES_DE_NAGE = 4;
        public const int IMAGES_D_ESPECE = 2;
        public const int LARGEUR_DE_FOND = 48;
        public const int LARGEUR_DE_BERGE = 384;
        public const int HAUTEUR_DES_RAYONS = 168;
        public const int COTE_DU_VOILE = 8;
        /// Le milieu du héros, en x : à droite du centre, pour laisser nager les autres.
        public const int X_DU_HEROS = 225;

        /// Spec DA §3 : quatre tailles dessinées, pas un agrandissement — le pixel art ne
        /// supporte pas les facteurs non entiers.
        public static readonly IReadOnlyList<int> LONGUEURS_DU_CORPS = new[] { 32, 42, 63, 95 };
        public static readonly IReadOnlyList<int> SEUILS_DE_STADE = new[] { 4, 16, 256 };

        public static int StadeDuNiveau(int niveau)
        {
            var stade = 0;
            foreach (var seuil in SEUILS_DE_STADE) if (niveau >= seuil) stade += 1;
            return stade;
        }

        public static Cadre CadreDUnPoisson(int longueur) =>
            new Cadre(longueur + 2, 2 * (int)Math.Ceiling(longueur * 0.2) + 5);

        /// Le corps du héros est dessiné (bible v0.4, `outils/da/heros.py`, d'après
        /// `docs/da/inspirations`) : ses cadres sont ceux du dessin — la largeur d'un poisson de
        /// sa longueur, la hauteur de ses nageoires.
        static readonly Cadre[] CADRES_DU_CORPS = { new Cadre(34, 22), new Cadre(44, 31), new Cadre(65, 45), new Cadre(97, 62) };

        public static Cadre CadreDuCorps(int stade) => CADRES_DU_CORPS[stade];

        /// Les ancrages du dessin, relevés par `outils/da/heros.py` : chacun sur un pixel opaque
        /// du corps, hors de la queue qui bat. Ordre : Branchies, Dos, Flanc, Ventre, Tete, Queue.
        static readonly Pixel[][] ANCRAGES =
        {
            new[] { new Pixel(24, 11), new Pixel(19, 15), new Pixel(19, 11), new Pixel(19, 7), new Pixel(27, 9), new Pixel(13, 11) },
            new[] { new Pixel(33, 16), new Pixel(26, 20), new Pixel(26, 16), new Pixel(26, 12), new Pixel(37, 14), new Pixel(17, 16) },
            new[] { new Pixel(50, 24), new Pixel(38, 30), new Pixel(38, 24), new Pixel(38, 18), new Pixel(56, 23), new Pixel(23, 24) },
            new[] { new Pixel(77, 33), new Pixel(63, 41), new Pixel(63, 33), new Pixel(63, 25), new Pixel(87, 32), new Pixel(42, 33) },
        };

        /* ─── L'éveil (bible v0.4) ─────────────────────────────────────────────────── */

        /// Les renaissances qu'il faut pour chaque éveil : I les runes, II le voile, III les
        /// étoiles, IV l'aura. Provisoires, pour un parcours visé de 14 renaissances.
        public static readonly IReadOnlyList<int> SEUILS_D_EVEIL = new[] { 1, 3, 6, 10 };

        public static int EveilDe(int renaissances)
        {
            var eveil = 0;
            foreach (var seuil in SEUILS_D_EVEIL) if (renaissances >= seuil) eveil += 1;
            return eveil;
        }

        /// Les calques d'éveil débordent du corps d'une marge de chaque côté, par stade.
        public static readonly IReadOnlyList<int> MARGES_D_EVEIL = new[] { 12, 15, 21, 27 };

        public static Cadre CadreDEveil(int stade)
        {
            var c = CadreDuCorps(stade);
            var m = MARGES_D_EVEIL[stade];
            return new Cadre(c.Largeur + 2 * m, c.Hauteur + 2 * m);
        }

        /// Les étoiles de l'éveil III : trois, puis une de plus par renaissance, jusqu'à huit.
        public static int EtoilesDe(int renaissances) =>
            EveilDe(renaissances) < 3 ? 0 : Math.Min(8, 3 + renaissances - SEUILS_D_EVEIL[2]);

        public const int COTE_D_ETOILE = 7;

        /// Un poisson de troupe mesure environ 24 px (Codex v1.4 ; maquette
        /// `docs/da/apercu-poissons-v3.png`). Les six tribus livrées sont dessinées
        /// (`outils/da/especes.py`) dans un cadre COMMUN : le plus petit qui les contient toutes,
        /// la queue-feuille du vairon comme la voile de l'ombre.
        public const int LONGUEUR_DE_TROUPE = 24;
        public static readonly Cadre CADRE_D_ESPECE = new Cadre(39, 27);

        public static int LongueurDEspece(int rang) => LONGUEUR_DE_TROUPE;

        public static Cadre CadreDEspece(int rang) => CADRE_D_ESPECE;

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

        public static Pixel Ancrage(int stade, AncrageId ancrage) => ANCRAGES[stade][(int)ancrage];
    }
}
