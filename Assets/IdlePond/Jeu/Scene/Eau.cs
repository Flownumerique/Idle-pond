using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.Scene
{
    /// Un filet du courant : un trait d'un pixel de haut qui file vers la droite.
    public sealed record FiletDEau(int X, int Y, int Longueur);

    /// <summary>
    /// IdlePond — l'eau vivante (2026-11-25) : ce qui bouge dans l'eau elle-même, sans être une
    /// bête. Pur et déterministe ; `EauVivante` le dessine.
    ///
    /// - **Les caustiques** : le réseau de lumière que la surface projette, deux couches
    ///   (`Eau/caustiques.png`) qui glissent en sens contraires et se croisent. C'est de la
    ///   lumière : sans éclairage, et d'autant plus pâle que le palier est sombre.
    /// - **Les bulles**, partout : elles montent du fond de chaque bande.
    /// - **Ce qui est propre à un lieu**, lu dans son décor : le pollen qui dérive dans la Noue
    ///   (`DecorDArt.Grains`), les filets du courant qui filent dans le Gour (`DecorDArt.Courant`).
    /// </summary>
    public static class Eau
    {
        /* ─── Les caustiques ──────────────────────────────────────────────────── */

        /// La largeur du motif : il se raccorde à lui-même, et divise la largeur du décor.
        public const int LARGEUR_DES_CAUSTIQUES = 96;

        /// Deux couches, en pixels par seconde : l'une vers la droite, l'autre plus lente vers la
        /// gauche. Le courant du Gour les emporte deux fois et demie plus vite.
        static readonly double[] VITESSES = { 4.0, -2.5 };
        public const double VITESSE_DANS_LE_COURANT = 2.5;

        /// L'opacité de chaque couche dans une bande en pleine lumière.
        static readonly double[] FORCES = { 0.42, 0.26 };

        public const int COUCHES_DE_CAUSTIQUES = 2;

        /// Le décalage entier d'une couche, de 0 à la largeur du motif exclue : le motif glisse
        /// par pixels entiers, jamais entre deux.
        public static int DecalageDesCaustiques(int couche, double secondes, bool courant)
        {
            var vitesse = VITESSES[couche] * (courant ? VITESSE_DANS_LE_COURANT : 1.0);
            var pixels = (long)Math.Floor(secondes * vitesse);
            var r = (int)(pixels % LARGEUR_DES_CAUSTIQUES);
            return r < 0 ? r + LARGEUR_DES_CAUSTIQUES : r;
        }

        /// L'opacité d'une couche dans un palier : la lumière qui y reste. La lumière baisse en
        /// descendant (GDD §15.2) ; ses reflets aussi.
        public static double ForceDesCaustiques(int couche, int palier) =>
            FORCES[couche] * RegistreDArt.LumiereDuPalier(palier);

        /// La couleur des reflets : celle des rayons du lieu.
        public static int CouleurDesCaustiques(string assise) => RegistreDArt.DecorDe(assise).Rayon;

        /* ─── Les bulles ──────────────────────────────────────────────────────── */

        public const int BULLES = 3;
        public const double MONTEE_D_UNE_BULLE = 6.0;

        /// Une bulle à cet instant : elle monte du fond au haut de sa bande en oscillant, pendant
        /// les deux tiers de son cycle ; le reste du temps, rien (null). Un point de deux pixels.
        public static Pixel Bulle(int bande, int numero, double secondes)
        {
            var h = Hacher(bande, numero, 41);
            var periode = MONTEE_D_UNE_BULLE * (0.8 + (h % 400u) / 1000.0);
            var t = Modulo(secondes + (h >> 8) % 1000u / 1000.0 * periode, periode) / periode;
            const double MONTEE = 2.0 / 3.0;
            if (t > MONTEE) return null;
            t /= MONTEE;
            var x0 = 14 + (int)((h >> 12) % (uint)(Gabarits.LARGEUR_VISEE - 28));
            var x = x0 + (int)Math.Round(1.5 * Math.Sin(secondes * 4.0 + numero * 2.1));
            var bas = -(bande + 1) * Gabarits.HAUTEUR_DE_BANDE + 6;
            // Elle accélère un peu en montant.
            return new Pixel(x, bas + (int)Math.Round(t * (0.6 + 0.4 * t) * (Gabarits.HAUTEUR_DE_BANDE - 10)));
        }

        /// La couleur des bulles : la lumière de la surface, ou l'écume, ou les rayons.
        public static int CouleurDesBulles(string assise)
        {
            var d = RegistreDArt.DecorDe(assise);
            return d.Surface ?? (d.Ecume.Count > 0 ? d.Ecume[0] : d.Rayon);
        }

        /* ─── Le pollen de la Noue ────────────────────────────────────────────── */

        public const int GRAINS = 5;
        public const double CHUTE_D_UN_GRAIN = 16.0;

        /// Un grain de pollen à cet instant : il descend lentement de la surface de sa bande vers
        /// le fond en dérivant de côté, et s'éteint avant de toucher la vase. Un point de deux
        /// pixels ; null quand il s'est déposé.
        public static Pixel Grain(int bande, int numero, double secondes)
        {
            var h = Hacher(bande, numero, 73);
            var t = Modulo(secondes + (h % 1000u) / 1000.0 * CHUTE_D_UN_GRAIN, CHUTE_D_UN_GRAIN) / CHUTE_D_UN_GRAIN;
            if (t > 0.85) return null;
            var x0 = 10 + (int)((h >> 10) % (uint)(Gabarits.LARGEUR_VISEE - 20));
            var x = x0 + (int)Math.Round(8 * t + 3 * Math.Sin(secondes * 0.7 + numero * 1.3));
            var haut = -bande * Gabarits.HAUTEUR_DE_BANDE - 6;
            return new Pixel(x, haut - (int)Math.Round(t * (Gabarits.HAUTEUR_DE_BANDE - 18)));
        }

        /* ─── Les filets du courant ───────────────────────────────────────────── */

        public const int FILETS = 4;

        /// Un filet du courant à cet instant : il traverse le cadre de gauche à droite, à sa
        /// hauteur dans la bande, entre la surface et le lit de galets. Toujours là : le
        /// courant ne s'arrête pas.
        public static FiletDEau Filet(int bande, int numero, double secondes)
        {
            var h = Hacher(bande, numero, 97);
            var vitesse = 55.0 + (h % 50u);
            const int MARGE = 24;
            var parcours = Gabarits.LARGEUR_VISEE + 2 * MARGE;
            var x = -MARGE + (int)Math.Floor(Modulo(secondes * vitesse + (h >> 6) % (uint)parcours, parcours));
            var bas = -(bande + 1) * Gabarits.HAUTEUR_DE_BANDE;
            var y = bas + 14 + (int)((h >> 14) % (uint)(Gabarits.HAUTEUR_DE_BANDE - 26));
            return new FiletDEau(x, y, 4 + (int)((h >> 20) % 7u));
        }

        /// Ce qu'un lieu met dans son eau, lu dans son décor : du pollen s'il a des grains, des
        /// filets s'il a du courant.
        public static bool APollen(string assise) => RegistreDArt.DecorDe(assise).Grains.Count > 0;
        public static bool ACourant(string assise) => RegistreDArt.DecorDe(assise).Courant;

        public static IReadOnlyList<int> CouleursDuPollen(string assise) => RegistreDArt.DecorDe(assise).Grains;
        public static int CouleurDesFilets(string assise)
        {
            var d = RegistreDArt.DecorDe(assise);
            return d.Ecume.Count > 0 ? d.Ecume[0] : d.Rayon;
        }

        static uint Hacher(int bande, int numero, uint sel)
        {
            var h = (uint)(bande * 7919 + numero * 104729) + sel;
            h ^= h >> 15; h *= 0x2c1b3c6dU; h ^= h >> 12; h *= 0x297a2d39U; h ^= h >> 15;
            return h;
        }

        static double Modulo(double a, double b)
        {
            var r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
