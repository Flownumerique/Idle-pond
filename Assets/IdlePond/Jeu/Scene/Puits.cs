using System;

namespace IdlePond.Jeu.Scene
{
    /// Une corniche : l'avancée de roche qui sort d'une paroi au bas d'un palier. `Debut` et
    /// `Fin` bornent son dessus en x (Debut &lt; Fin) ; `Sol` est le y où l'on s'y pose.
    public sealed record Corniche(bool AGauche, int Debut, int Fin, int Sol)
    {
        public int Longueur => Fin - Debut;
    }

    /// Comment un lieu se raccorde à celui d'au-dessus (2026-11-25) : l'eau et la lumière
    /// glissent de l'un à l'autre, ou un seuil de roche les sépare net.
    public enum JonctionDeLieu { Fondu, Cassure }

    /// <summary>
    /// IdlePond — le puits (décidé par l'auteur le 2026-11-25) : creuser ouvre un puits, pas les
    /// étages d'une tour. L'eau descend d'un seul tenant, entre deux parois de roche dont le bord
    /// ondule ; à chaque palier, une corniche sort d'une paroi — c'est là que se posent les
    /// jardins, les nids, les œufs et la procession ; au fond du puits seulement, un vrai sol. Et
    /// quand on creuse, le sol s'ouvre : la roche du palier neuf recule vers le bas, le sol
    /// descend avec elle.
    ///
    /// Tout est fonction du y du monde : le bord d'une paroi se raccorde d'un palier à l'autre
    /// sans couture. Pur ; le générateur dessine les parois et les corniches d'après ces mêmes
    /// fonctions, la scène y pose ce qui vit.
    /// </summary>
    public static class Puits
    {
        const int H = Gabarits.HAUTEUR_DE_BANDE;

        /// Le premier x d'eau à gauche à la hauteur `y` : la paroi est en deçà. De 1 à 23.
        public static int BordGauche(int y) =>
            (int)Math.Round(12 + 6 * Math.Sin(y * 0.029 + 0.7) + 3.5 * Math.Sin(y * 0.077 + 2.1) + 1.5 * Math.Sin(y * 0.19 + 0.3));

        /// Le dernier x d'eau à droite : la paroi est au-delà. De 337 à 359.
        public static int BordDroit(int y) =>
            Gabarits.LARGEUR_VISEE - 1 - (int)Math.Round(12 + 6 * Math.Sin(y * 0.033 + 3.1) + 3.5 * Math.Sin(y * 0.069 + 0.4) + 1.5 * Math.Sin(y * 0.17 + 1.0));

        /// La marge que laissent les parois à ce qui nage : elles n'avancent jamais plus loin.
        public const int MARGE_DES_PAROIS = 24;

        /* ─── Les corniches ───────────────────────────────────────────────────── */

        /// L'épaisseur d'une corniche à la paroi : on se pose à ce y au-dessus du bas du palier.
        public const int EPAISSEUR_DE_CORNICHE = 7;
        public const int CORNICHE_LA_PLUS_COURTE = 110;
        public const int CORNICHE_LA_PLUS_LONGUE = 180;

        /// La corniche d'un palier : d'un côté puis de l'autre en descendant, et deux fois du
        /// même côté tous les cinq paliers, pour que le rythme ne soit pas celui d'un escalier ;
        /// de 110 à 179 pixels.
        public static Corniche CornicheDe(int palier)
        {
            var h = Hacher(palier, 5);
            var aGauche = (palier + palier / 5) % 2 == 0;
            var longueur = CORNICHE_LA_PLUS_COURTE + (int)(h % (uint)(CORNICHE_LA_PLUS_LONGUE - CORNICHE_LA_PLUS_COURTE));
            var sol = -(palier + 1) * H + EPAISSEUR_DE_CORNICHE;
            if (aGauche)
            {
                var debut = BordGauche(sol);
                return new Corniche(true, debut, debut + longueur, sol);
            }
            var fin = BordDroit(sol) + 1;
            return new Corniche(false, fin - longueur, fin, sol);
        }

        /// <summary>
        /// Où poser la `numero`-ième de `cases` choses de `largeur` pixels sur une corniche :
        /// chacune dans sa case, un peu au hasard dans la case, jamais à cheval sur la voisine.
        /// Les cases ne dépendent que de `cases` : ce qui s'ajoute ne fait pas bouger le reste.
        /// </summary>
        public static int XSurLaCorniche(Corniche c, int numero, int cases, int largeur, int graine)
        {
            var debut = c.Debut + 3;
            var caseDe = (c.Longueur - 8) / cases;
            var jeu = Math.Max(1, caseDe - largeur - 1);
            return debut + numero * caseDe + (int)(Hacher(c.Sol * 31 + numero, (uint)graine) % (uint)jeu);
        }

        /* ─── Le sol qui s'ouvre ──────────────────────────────────────────────── */

        public const double DUREE_DE_L_OUVERTURE = 1.4;

        /// La hauteur de roche qui reste dans le palier qu'on vient de creuser, à `age` secondes
        /// de l'ouverture : toute la bande au départ, rien à la fin ; vite d'abord, puis le sol
        /// se pose doucement.
        public static int RocheRestante(double age)
        {
            if (age <= 0) return H;
            if (age >= DUREE_DE_L_OUVERTURE) return 0;
            var t = age / DUREE_DE_L_OUVERTURE;
            var avance = 1 - (1 - t) * (1 - t) * (1 - t);
            return (int)Math.Round(H * (1 - avance));
        }

        static uint Hacher(int a, uint sel)
        {
            var h = (uint)(a * 7919) + sel * 104729u;
            h ^= h >> 15; h *= 0x2c1b3c6dU; h ^= h >> 12; h *= 0x297a2d39U; h ^= h >> 15;
            return h;
        }
    }
}
