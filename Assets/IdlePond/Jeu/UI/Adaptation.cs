using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Portrait ou paysage, et téléphone, tablette ou PC : les deux décisions de mise en
    /// page que le code prend. Tout le reste est dans le .uss, qui lit les classes posées
    /// sur la racine.
    ///
    /// Le seuil est 1,2 et pas 1 : une fenêtre de PC presque carrée, ou un téléphone qu'on
    /// vient de pencher, ne doit pas faire basculer toute l'interface à chaque pixel. Entre
    /// 1 et 1,2 on reste en portrait, la colonne unique, qui supporte toutes les largeurs.
    /// Fonction pure, donc testée sans moteur.
    /// </summary>
    public static class Adaptation
    {
        public const double RATIO_PAYSAGE = 1.2;

        public const string CLASSE_PAYSAGE = "paysage";
        public const string CLASSE_PORTRAIT = "portrait";

        /// Un écran de taille nulle ou illisible (avant le premier calcul de mise en page) n'est pas du paysage.
        public static bool EstPaysage(double largeur, double hauteur) =>
            largeur > 0 && hauteur > 0 && largeur >= RATIO_PAYSAGE * hauteur;

        /* ─── Le format (spec du 2026-10-08) ────────────────────────────────────────── */

        /// En dessous, un téléphone ; à partir de là, une tablette (diagonale, en pouces).
        public const double DIAGONALE_TABLETTE_POUCES = 7.0;

        /// Quand l'écran ne dit pas son dpi : la plus petite dimension, en pixels.
        public const double COTE_TABLETTE_PX = 1200;

        /// Le PC se lit de plus près et à la souris : son interface est plus dense.
        public const double ECHELLE_PC = 0.85;

        public const string CLASSE_TELEPHONE = "telephone";
        public const string CLASSE_TABLETTE = "tablette";
        public const string CLASSE_PC = "pc";

        public static readonly IReadOnlyList<string> CLASSES_DE_FORMAT = new[] { CLASSE_TELEPHONE, CLASSE_TABLETTE, CLASSE_PC };

        /// <summary>
        /// Le format de l'appareil. Hors mobile, c'est un PC, quelle que soit la fenêtre : on
        /// y joue à la souris. Sur mobile, la taille physique décide, pas les pixels — un
        /// téléphone récent a plus de pixels qu'une vieille tablette.
        /// </summary>
        public static FormatDAffichage Detecter(double largeurPx, double hauteurPx, double dpi, bool mobile)
        {
            if (!mobile) return FormatDAffichage.Pc;
            if (dpi > 0)
            {
                var diagonale = Math.Sqrt(largeurPx * largeurPx + hauteurPx * hauteurPx) / dpi;
                return diagonale < DIAGONALE_TABLETTE_POUCES ? FormatDAffichage.Telephone : FormatDAffichage.Tablette;
            }
            return Math.Min(largeurPx, hauteurPx) < COTE_TABLETTE_PX ? FormatDAffichage.Telephone : FormatDAffichage.Tablette;
        }

        /// Le réglage du joueur l'emporte ; `Auto` prend le format détecté (téléphone, à défaut).
        public static FormatDAffichage Resoudre(FormatDAffichage reglage, FormatDAffichage detecte)
        {
            if (reglage != FormatDAffichage.Auto) return reglage;
            return detecte == FormatDAffichage.Auto ? FormatDAffichage.Telephone : detecte;
        }

        /// Ce par quoi l'interface est agrandie : le facteur du format, fois la taille choisie.
        public static double FacteurDEchelle(FormatDAffichage format, double taille) =>
            (format == FormatDAffichage.Pc ? ECHELLE_PC : 1.0) * taille;

        public static string ClasseDu(FormatDAffichage format)
        {
            switch (format)
            {
                case FormatDAffichage.Tablette: return CLASSE_TABLETTE;
                case FormatDAffichage.Pc: return CLASSE_PC;
                default: return CLASSE_TELEPHONE;
            }
        }
    }
}
