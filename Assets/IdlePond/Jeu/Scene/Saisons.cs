using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.Scene
{
    /// Les saisons réelles de l'eau douce (décidé le 2026-11-18). L'été n'en a pas : la mare
    /// est comme d'habitude.
    public enum Saison { Aucune, Frai, Crue, Gel }

    /// <summary>
    /// IdlePond — les saisons cosmétiques (décision du 2026-11-18, GDD : « les saisons réelles de
    /// l'eau douce changent l'aspect de la mare, sans rien à gagner ni à rater »). Le frai au
    /// printemps, la crue à l'automne, le gel l'hiver, lus sur le mois du calendrier de
    /// l'appareil — hémisphère nord.
    ///
    /// Pur : le mois est un paramètre. C'est la scène qui lit l'horloge du téléphone. Aucune
    /// règle, aucun état ne dépend d'une saison ; elle ne fait que changer ce qu'on voit.
    /// </summary>
    public static class Saisons
    {
        /// Mars à mai, le frai ; octobre et novembre, la crue ; décembre à février, le gel.
        public static Saison De(int mois) => mois switch
        {
            3 or 4 or 5 => Saison.Frai,
            10 or 11 => Saison.Crue,
            12 or 1 or 2 => Saison.Gel,
            _ => Saison.Aucune,
        };

        /// L'hiver, les libellules et les grenouilles ne sont pas là — elles reviennent au
        /// printemps. Les gerris patinent sur la glace.
        public static bool BetesDeLaBelleSaison(Saison saison) => saison != Saison.Gel;

        /* ─── Le gel : la glace sur la surface ─────────────────────────────────── */

        /// Le pied de la glace : elle couvre les quatre premières rangées de la surface.
        public static int YDeLaGlace => -Faune.DESSINS[Faune.GLACE].Cadre.Hauteur;

        /* ─── Le frai : des grappes d'œufs au fond des bandes de la Noue ───────── */

        public const int GRAPPES_PAR_BANDE = 3;

        public static IReadOnlyList<PoseDeFaune> Frai(int bande)
        {
            // Sur la corniche de la bande (le puits, 2026-11-25).
            var grappes = new List<PoseDeFaune>();
            var corniche = Puits.CornicheDe(bande);
            for (var i = 0; i < GRAPPES_PAR_BANDE; i++)
            {
                var x = Puits.XSurLaCorniche(corniche, i, GRAPPES_PAR_BANDE, Faune.DESSINS[Faune.OEUFS].Cadre.Largeur, 13);
                grappes.Add(new PoseDeFaune(Faune.OEUFS, 0, x, corniche.Sol));
            }
            return grappes;
        }

        /* ─── La crue : des feuilles mortes que le courant emporte ─────────────── */

        public const int FEUILLES = 6;
        public const double TRAVERSEE_D_UNE_FEUILLE = 14.0;

        /// Une feuille à cet instant : elle entre par la gauche, traverse en dérivant vers le
        /// bas sur une bande et demie, et tourne sur elle-même. Elles partent des bandes
        /// ouvertes, de la surface jusqu'en bas.
        public static PoseDeFaune Feuille(int numero, double secondes, int bandesOuvertes)
        {
            var cadre = Faune.DESSINS[Faune.FEUILLE].Cadre;
            var phase = numero * TRAVERSEE_D_UNE_FEUILLE / FEUILLES;
            var t = Modulo(secondes + phase, TRAVERSEE_D_UNE_FEUILLE) / TRAVERSEE_D_UNE_FEUILLE;
            var tour = (long)Math.Floor((secondes + phase) / TRAVERSEE_D_UNE_FEUILLE);
            var bandes = Math.Max(1, bandesOuvertes);
            var bande = (int)((uint)(numero * 31 + tour * 17) % (uint)bandes);
            var x = -cadre.Largeur + (int)Math.Round(t * (Gabarits.LARGEUR_VISEE + cadre.Largeur));
            var haut = -bande * Gabarits.HAUTEUR_DE_BANDE - 10;
            var descente = (int)Math.Round(t * Gabarits.HAUTEUR_DE_BANDE * 0.5);
            var y = haut - cadre.Hauteur - descente + (int)Math.Round(3 * Math.Sin(secondes * 2 + numero));
            var image = (int)Math.Floor(secondes * 2 + numero) % 2;
            return new PoseDeFaune(Faune.FEUILLE, image < 0 ? -image : image, x, y);
        }

        static double Modulo(double a, double b)
        {
            var r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
