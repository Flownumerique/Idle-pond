using System;

namespace IdlePond.Jeu.Scene
{
    public sealed record Trajet(int X0, int Y, int Amplitude, double Periode, double Phase);

    /// <summary>
    /// IdlePond — la nage des espèces (spec DA §4). Pur et déterministe : un hachage
    /// d'entiers, jamais un tirage — même état, même image (et `ArchitectureTests` interdit
    /// tout hasard hors de l'état). Un nageur fait l'aller-retour dans sa bande.
    /// </summary>
    public static class Nage
    {
        static uint Melanger(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352dU;
            x ^= x >> 15;
            x *= 0x846ca68bU;
            x ^= x >> 16;
            return x;
        }

        public static Trajet TrajetDe(int palier, int numero, int longueur) =>
            TrajetDe(palier, numero, Gabarits.CadreDUnPoisson(longueur));

        /// Les marges et l'amplitude sont celles de la scène de 360 px : une fois et demie
        /// celles de 240, la nage garde son allure à l'écran.
        public static Trajet TrajetDe(int palier, int numero, Cadre cadre)
        {
            var h = Melanger((uint)(palier * 7919 + numero * 104729 + 1));
            var amplitude = 12 + (int)(h % 36u);
            // Aller et retour compris, le nageur tient entre les parois du puits (2026-11-25), qui
            // n'avancent jamais au-delà de `Puits.MARGE_DES_PAROIS`.
            var marge = Gabarits.LARGEUR_VISEE - 2 * Puits.MARGE_DES_PAROIS - amplitude - cadre.Largeur;
            var x0 = Puits.MARGE_DES_PAROIS + (int)((h >> 5) % (uint)marge);
            var hauteurLibre = Gabarits.HAUTEUR_DE_BANDE - 18 - cadre.Hauteur;
            var y = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE + 9 + (int)((h >> 11) % (uint)hauteurLibre);
            var periode = 2.0 + ((h >> 17) % 20u) / 10.0;
            var phase = ((h >> 23) % 100u) / 100.0 * periode * 2;
            return new Trajet(x0, y, amplitude, periode, phase);
        }

        public static Trajet TrajetAccroche(int palier, int numero, int longueur) =>
            TrajetAccroche(palier, numero, Gabarits.CadreDUnPoisson(longueur));

        /// Une lamproie accrochée à la paroi (Codex v1.4, les Lamproies pèlerines) : contre le
        /// bord gauche ou droit de la bande, sans aller-retour. `Amplitude` 0 ; l'animation la
        /// fait respirer d'un pixel (`Tribus.Respiration`).
        public static Trajet TrajetAccroche(int palier, int numero, Cadre cadre)
        {
            var h = Melanger((uint)(palier * 7919 + numero * 104729 + 7));
            var hauteurLibre = Gabarits.HAUTEUR_DE_BANDE - 18 - cadre.Hauteur;
            var y = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE + 9 + (int)((h >> 11) % (uint)hauteurLibre);
            // Contre le bord de la paroi du puits à sa hauteur, mordant de deux pixels dans la roche.
            var milieu = y + cadre.Hauteur / 2;
            var x0 = Tribus.AccrocheeAGauche(numero)
                ? Math.Max(0, Puits.BordGauche(milieu) - 2)
                : Math.Min(Gabarits.LARGEUR_VISEE - cadre.Largeur, Puits.BordDroit(milieu) + 3 - cadre.Largeur);
            var phase = ((h >> 23) % 100u) / 100.0 * 2.5;
            return new Trajet(x0, y, 0, 2.5, phase);
        }

        /// Aller-retour en cosinus : de X0 à X0 + amplitude en une période, retour en une
        /// autre. Arrondi au pixel : rien ne se pose entre deux pixels.
        public static (int X, bool VersLaDroite) Position(Trajet t, double secondes)
        {
            var phase = ((secondes + t.Phase) / t.Periode) % 2.0;
            if (phase < 0) phase += 2.0;
            var p = phase <= 1 ? phase : 2 - phase;
            var x = t.X0 + (int)Math.Round(t.Amplitude * 0.5 * (1 - Math.Cos(Math.PI * p)));
            return (x, phase < 1);
        }
    }
}
