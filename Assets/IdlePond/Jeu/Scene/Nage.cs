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

        public static Trajet TrajetDe(int palier, int numero, int longueur)
        {
            var h = Melanger((uint)(palier * 7919 + numero * 104729 + 1));
            var cadre = Gabarits.CadreDUnPoisson(longueur);
            var amplitude = 8 + (int)(h % 24u);
            // Aller et retour compris, le nageur tient dans [0 ; 240 − largeur de son cadre].
            var marge = Gabarits.LARGEUR_VISEE - 16 - amplitude - cadre.Largeur;
            var x0 = 8 + (int)((h >> 5) % (uint)marge);
            var hauteurLibre = Gabarits.HAUTEUR_DE_BANDE - 12 - cadre.Hauteur;
            var y = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE + 6 + (int)((h >> 11) % (uint)hauteurLibre);
            var periode = 2.0 + ((h >> 17) % 20u) / 10.0;
            var phase = ((h >> 23) % 100u) / 100.0 * periode * 2;
            return new Trajet(x0, y, amplitude, periode, phase);
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
