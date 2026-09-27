/*
 * IdlePond — puissances tabulées des ratios géométriques. Port de
 * `src/donnees/echelles.ts`.
 *
 * Tabulées une fois parce que la puissance est appelée des centaines de milliers
 * de fois par simulation. Ce n'est pas un cache au sens du §5.1 : rien ici ne
 * dépend d'un état de jeu — ce sont des constantes dérivées de constantes.
 */
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Echelles
    {
        private static GrandNombre[] Tabuler(double ratio, int longueur)
        {
            var table = new GrandNombre[longueur];
            table[0] = GrandNombre.Un;
            // `mul(nombre)`, comme la version web : la bibliothèque ne convertit pas
            // le ratio en grand nombre, et l'arrondi en dépend.
            for (var i = 1; i < longueur; i += 1) table[i] = table[i - 1].Mul(ratio);
            return table;
        }

        /// <summary>g^p, pour tous les paliers.</summary>
        private static readonly GrandNombre[] PuissancesDeG = Tabuler(Constantes.GCoutPalier, Constantes.NombreDePaliers + 1);

        /// <summary>D^p, pour tous les paliers.</summary>
        private static readonly GrandNombre[] PuissancesDeD = Tabuler(Constantes.DProductionParPalier, Constantes.NombreDePaliers + 1);

        /// <summary>1.15^n. Au-delà de la table, on retombe sur le calcul direct.</summary>
        private const int NiveauxTabules = 2048;

        private static readonly GrandNombre[] PuissancesDuCoutDeNiveau = Tabuler(Constantes.RatioCoutNiveau, NiveauxTabules);

        private static GrandNombre Lire(GrandNombre[] table, int exposant, double ratio)
        {
            if (exposant >= 0 && exposant < table.Length) return table[exposant];
            return GrandNombre.Pow(ratio, exposant);
        }

        public static GrandNombre PuissanceDeG(int exposant) => Lire(PuissancesDeG, exposant, Constantes.GCoutPalier);

        public static GrandNombre PuissanceDeD(int exposant) => Lire(PuissancesDeD, exposant, Constantes.DProductionParPalier);

        public static GrandNombre PuissanceDuCoutDeNiveau(int exposant) =>
            Lire(PuissancesDuCoutDeNiveau, exposant, Constantes.RatioCoutNiveau);
    }
}
