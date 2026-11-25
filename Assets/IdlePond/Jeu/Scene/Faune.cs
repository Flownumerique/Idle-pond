using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.Scene
{
    /// Une bête à un instant : quelle image, où (le coin bas-gauche de son cadre), vers où.
    public sealed record PoseDeFaune(string Nom, int Image, int X, int Y, bool VersLaDroite = true);

    /// <summary>
    /// IdlePond — la faune d'ambiance (Codex v1.3, §2) : ce qui vit dans la mare sans produire.
    /// Les libellules-lanternes, dont les ailes s'allument ; les gerris-scribes, qui tracent des
    /// runes à la surface ; les grenouilles-chantres, plus nombreuses et plus bavardes à chaque
    /// renaissance ; les écrevisses porte-bannière, en procession quand une tribu est ralliée ;
    /// l'hydre, accrochée aux racines ; les jardins de la Sève, qui fleurissent et grandissent à
    /// chaque renaissance. Les méduses du Gour sont dans son décor.
    ///
    /// Pur et déterministe, comme `Nage` : même instant, même image. Rien ici ne touche
    /// l'économie ; `FauneDAmbiance` dessine. La surface, la berge et les racines sont dans la
    /// première bande (la Noue) : c'est là que vivent les bêtes de surface.
    /// </summary>
    public static class Faune
    {
        public const string LIBELLULE = "libellule";
        public const string GERRIS = "gerris";
        public const string GRENOUILLE = "grenouille";
        public const string ECREVISSE = "ecrevisse";
        public const string ECREVISSE_BANNIERE = "ecrevisse-banniere";
        public const string HYDRE = "hydre";
        public const string FLEUR = "fleur";
        /// La méduse d'eau douce n'a qu'un portrait (la bibliothèque) : dans la mare, elle est
        /// dessinée dans le décor du Gour.
        public const string MEDUSE = "meduse";
        /// Les saisons (`Saisons`) : la glace du gel, les œufs du frai, la feuille de la crue.
        public const string GLACE = "glace";
        public const string OEUFS = "oeufs";
        public const string FEUILLE = "feuille";

        /// Les dessins (`outils/da/faune.py`) : le cadre commun de chaque bête et son nombre
        /// d'images.
        public static readonly IReadOnlyDictionary<string, (Cadre Cadre, int Images)> DESSINS =
            new Dictionary<string, (Cadre, int)>
            {
                [LIBELLULE] = (new Cadre(14, 7), 3),
                [GERRIS] = (new Cadre(11, 5), 2),
                [GRENOUILLE] = (new Cadre(14, 8), 2),
                [ECREVISSE] = (new Cadre(14, 6), 2),
                [ECREVISSE_BANNIERE] = (new Cadre(14, 11), 2),
                [HYDRE] = (new Cadre(7, 12), 2),
                [MEDUSE] = (new Cadre(7, 7), 1),
                [GLACE] = (new Cadre(48, 4), 1),
                [OEUFS] = (new Cadre(7, 5), 1),
                [FEUILLE] = (new Cadre(6, 4), 2),
                [FLEUR] = (new Cadre(7, 9), 3),
            };

        public const int LIBELLULES = 2;

        /* ─── Une mare qui change à chaque renaissance (chantier 6 ; Codex, acte III :
         *     « une faune permanente un peu plus étrange qu'avant ») ───────────────── */

        /// Deux libellules, une de plus à la 3ᵉ et à la 6ᵉ renaissance.
        public static int Libellules(int renaissances) => LIBELLULES + Math.Min(2, Math.Max(0, renaissances) / 3);

        /// L'hydre repousse coupée en deux : à la 4ᵉ renaissance, elles sont deux.
        public static int Hydres(int renaissances) => renaissances >= 4 ? 2 : 1;

        /// Les étincelles de Sève, qui montent du fond des bandes de la Noue : deux de plus par
        /// renaissance, douze au plus par bande. Aucune avant la première.
        public static int Etincelles(int renaissances) => Math.Min(12, 2 * Math.Max(0, renaissances));

        public const double MONTEE_D_UNE_ETINCELLE = 9.0;

        /// Une étincelle de Sève à cet instant : elle monte du fond au haut de sa bande en neuf
        /// secondes, en ondulant, et ne brille qu'une partie du chemin. Null quand elle est
        /// éteinte. Un point de deux pixels.
        public static Pixel Etincelle(int bande, int numero, double secondes)
        {
            var h = (uint)(bande * 7919 + numero * 104729 + 13);
            h ^= h >> 15; h *= 0x2c1b3c6dU; h ^= h >> 12;
            var phase = (h % 1000u) / 1000.0 * MONTEE_D_UNE_ETINCELLE;
            var t = Modulo(secondes + phase, MONTEE_D_UNE_ETINCELLE) / MONTEE_D_UNE_ETINCELLE;
            if (t > 0.8) return null;
            var x0 = 10 + (int)((h >> 10) % (uint)(Gabarits.LARGEUR_VISEE - 20));
            var x = x0 + (int)Math.Round(5 * Math.Sin(secondes * 1.1 + numero));
            var bas = -(bande + 1) * Gabarits.HAUTEUR_DE_BANDE + 6;
            return new Pixel(x, bas + (int)Math.Round(t * (Gabarits.HAUTEUR_DE_BANDE - 12)));
        }
        public const int GERRIS_A_LA_SURFACE = 3;
        public const int ECREVISSES = 4;

        /* ─── Les libellules-lanternes ──────────────────────────────────────────── */

        /// L'image allumée, et combien de temps une lanterne brille : en éclair, jamais en
        /// continu — une fois toutes les sept secondes.
        public const int IMAGE_ALLUMEE = 2;
        public const double PERIODE_DE_LA_LANTERNE = 7.0;
        public const double ECLAT_DE_LA_LANTERNE = 0.4;

        public static PoseDeFaune Libellule(int numero, double secondes)
        {
            var cadre = DESSINS[LIBELLULE].Cadre;
            var phase = numero * 2.1;
            // Quatre au plus, réparties sur la largeur : les deux premières aux deux côtés.
            var centre = new[] { 80, 260, 170, 330 }[numero % 4];
            var x = centre + (int)Math.Round(60 * Math.Sin(secondes * 0.45 + phase));
            var versLaDroite = Math.Cos(secondes * 0.45 + phase) >= 0;
            var y = -18 - (numero % 2) * 7 + (int)Math.Round(4 * Math.Sin(secondes * 1.7 + phase));
            x = Math.Max(0, Math.Min(Gabarits.LARGEUR_VISEE - cadre.Largeur, x));
            var allumee = Modulo(secondes + numero * 3.3, PERIODE_DE_LA_LANTERNE) < ECLAT_DE_LA_LANTERNE;
            var image = allumee ? IMAGE_ALLUMEE : (int)Math.Floor(secondes * 12) % 2;
            return new PoseDeFaune(LIBELLULE, image, x, y, versLaDroite);
        }

        /* ─── Les gerris-scribes ───────────────────────────────────────────────── */

        /// Un gerris file d'un coup, puis se pose : un saut toutes les 1,6 s, qui dure 0,4 s.
        public const double PERIODE_DU_SAUT = 1.6;
        public const double DUREE_DU_SAUT = 0.4;
        /// Là où il s'est posé, il laisse une rune, qui reste un peu plus d'une seconde.
        public const double DUREE_DE_LA_RUNE = 1.2;

        static int PosteDuGerris(int numero, long saut)
        {
            var cadre = DESSINS[GERRIS].Cadre;
            var centre = 50 + numero * 115;
            var x = centre + (int)Math.Round(45 * Math.Sin(saut * 1.37 + numero * 2.0));
            return Math.Max(0, Math.Min(Gabarits.LARGEUR_VISEE - cadre.Largeur, x));
        }

        /// Il marche sur l'eau : son ventre affleure la surface.
        public static int YDeLaSurface(Cadre cadre) => -1 - cadre.Hauteur;

        public static PoseDeFaune Gerris(int numero, double secondes)
        {
            var t = secondes + numero * 0.53;
            var saut = (long)Math.Floor(t / PERIODE_DU_SAUT);
            var dans = t - saut * PERIODE_DU_SAUT;
            var depart = PosteDuGerris(numero, saut);
            var arrivee = PosteDuGerris(numero, saut + 1);
            var avance = Math.Min(1, dans / DUREE_DU_SAUT);
            var x = depart + (int)Math.Round((arrivee - depart) * avance);
            var image = dans < DUREE_DU_SAUT ? (int)Math.Floor(dans * 20) % 2 : 0;
            return new PoseDeFaune(GERRIS, image, x, YDeLaSurface(DESSINS[GERRIS].Cadre), arrivee >= depart);
        }

        /// La rune que le gerris a laissée là où il s'est posé, tant qu'elle se voit : un point
        /// de deux pixels, juste sous la surface. Null quand elle s'est effacée.
        public static Pixel RuneDuGerris(int numero, double secondes)
        {
            var t = secondes + numero * 0.53;
            var saut = (long)Math.Floor(t / PERIODE_DU_SAUT);
            var dans = t - saut * PERIODE_DU_SAUT;
            if (dans >= DUREE_DE_LA_RUNE) return null;
            return new Pixel(PosteDuGerris(numero, saut) + DESSINS[GERRIS].Cadre.Largeur / 2, -4);
        }

        /* ─── Les grenouilles-chantres ─────────────────────────────────────────── */

        /// Une grenouille de plus à chaque renaissance, trois au plus — une par feuille.
        public static int Grenouilles(int renaissances) => Math.Min(3, 1 + Math.Max(0, renaissances));

        /// Elles chantent plus souvent à chaque renaissance : toutes les huit secondes au
        /// début, toutes les deux secondes et demie au plus.
        public static double PeriodeDuChant(int renaissances) => Math.Max(2.5, 8.0 - Math.Max(0, renaissances));

        public const double DUREE_DU_CHANT = 0.6;

        /// Le milieu des feuilles de nénuphar de la berge (le générateur de provisoires).
        public static readonly IReadOnlyList<int> FEUILLES = new[] { 51, 180, 307 };

        public static PoseDeFaune Grenouille(int numero, double secondes, int renaissances)
        {
            var cadre = DESSINS[GRENOUILLE].Cadre;
            var chante = Modulo(secondes + numero * 1.3, PeriodeDuChant(renaissances)) < DUREE_DU_CHANT;
            // Dans l'eau au pied de sa feuille, les yeux à la surface.
            return new PoseDeFaune(GRENOUILLE, chante ? 1 : 0, FEUILLES[numero % FEUILLES.Count] - cadre.Largeur / 2 + 3,
                -cadre.Hauteur, numero % 2 == 0);
        }

        /* ─── L'hydre ──────────────────────────────────────────────────────────── */

        public const int X_DE_L_HYDRE = 132;

        /// Accrochée sous la surface, ses bras pendent et ondulent.
        public static PoseDeFaune Hydre(double secondes, int numero = 0) =>
            new PoseDeFaune(HYDRE, (int)Math.Floor(secondes / 1.5 + numero * 0.5) % 2, X_DE_L_HYDRE + numero * 151,
                -3 - DESSINS[HYDRE].Cadre.Hauteur);

        /* ─── Les jardins de la Sève ───────────────────────────────────────────── */

        /// Une fleur de plus par bande de la Noue à chaque renaissance, six au plus ; elles
        /// grandissent : bourgeon, fleur, puis grande fleur à la troisième et à la sixième.
        public static int FleursParBande(int renaissances) => Math.Min(6, Math.Max(0, renaissances));

        public static int StadeDesFleurs(int renaissances) => renaissances >= 6 ? 2 : renaissances >= 3 ? 1 : 0;

        /// Sur la corniche de la bande (le puits, 2026-11-25), chacune dans sa case : celles qui
        /// poussent à la renaissance suivante ne font pas bouger les autres.
        public static IReadOnlyList<PoseDeFaune> Jardin(int bande, int renaissances)
        {
            var fleurs = new List<PoseDeFaune>();
            var stade = StadeDesFleurs(renaissances);
            var corniche = Puits.CornicheDe(bande);
            for (var i = 0; i < FleursParBande(renaissances); i++)
            {
                var x = Puits.XSurLaCorniche(corniche, i, 6, DESSINS[FLEUR].Cadre.Largeur, 11);
                fleurs.Add(new PoseDeFaune(FLEUR, stade, x, corniche.Sol));
            }
            return fleurs;
        }

        /* ─── Les écrevisses porte-bannière ────────────────────────────────────── */

        public const double DUREE_DE_LA_PROCESSION = 7.0;
        public const int ECART_DANS_LA_PROCESSION = 18;

        /// <summary>
        /// La procession, à cet avancement : quatre écrevisses sortent de la paroi sur la
        /// corniche de la bande (le puits, 2026-11-25), la première porte la bannière jusqu'à la
        /// pointe ; puis elles font demi-tour et rentrent dans la roche. Seules celles qui sont
        /// sorties de la paroi sont rendues.
        /// </summary>
        public static IReadOnlyList<PoseDeFaune> Procession(int bande, double avancement)
        {
            var poses = new List<PoseDeFaune>();
            if (avancement <= 0 || avancement >= 1) return poses;
            var corniche = Puits.CornicheDe(bande);
            var largeur = DESSINS[ECREVISSE].Cadre.Largeur;
            var aller = avancement < 0.5;
            var p = aller ? avancement * 2 : (1 - avancement) * 2;
            // Jusqu'où la tête s'avance depuis la paroi : presque la pointe.
            var portee = corniche.Longueur - largeur - 6;
            var tete = -largeur + (int)Math.Round(p * (portee + largeur));
            for (var i = 0; i < ECREVISSES; i++)
            {
                var d = tete - i * ECART_DANS_LA_PROCESSION;
                if (d < 0 || d > portee) continue;
                var x = corniche.AGauche ? corniche.Debut + d : corniche.Fin - largeur - d;
                var pas = ((d / 3) % 2 + 2) % 2;
                poses.Add(new PoseDeFaune(i == 0 ? ECREVISSE_BANNIERE : ECREVISSE, pas, x, corniche.Sol, corniche.AGauche == aller));
            }
            return poses;
        }

        static double Modulo(double a, double b)
        {
            var r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
