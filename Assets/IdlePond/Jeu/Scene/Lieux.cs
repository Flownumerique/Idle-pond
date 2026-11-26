using System;
using System.Collections.Generic;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le passage d'un lieu à l'autre (décidé par l'auteur le 2026-11-25) : ce qui se
    /// voit à la jonction de deux lieux, et ce qui se voit du lieu suivant avant de l'atteindre.
    ///
    /// - **La jonction** : en haut du premier palier d'un lieu qui s'ouvre par une cassure, la
    ///   membrane de son mana (2026-11-26) — une barrière bombée comme une bulle, qui sépare son
    ///   eau de celle d'au-dessus ; l'eau d'au-dessus, qui appuie sur elle, est dessinée au seuil
    ///   (`Fonds/{lieu}/seuil.png`), la membrane vivante est tracée par la scène.
    /// - **Voir venir** : la roche du premier palier du lieu suivant se montre sous celle du
    ///   lieu actuel (`VueDeScene.IndicesDeRoche`), et la magie de ce lieu suinte à travers,
    ///   en lueurs de sa couleur.
    ///
    /// Pur et déterministe ; rien n'y touche l'économie. La règle de la percée est dans le noyau
    /// (`Economie.TribusARallierPourPercer`).
    /// </summary>
    public static class Lieux
    {
        /// La hauteur du seuil dessiné, sous le haut de la bande du lieu : l'eau d'au-dessus y
        /// appuie sur la membrane jusqu'aux parois (2026-11-26).
        public const int HAUTEUR_DU_SEUIL = 48;

        /// Là où la percée jaillit contre la membrane, en fraction de la largeur visée : un peu à
        /// gauche du héros.
        public const double BRECHE = 0.36;

        /// Le palier qui ouvre un lieu, hors le tout premier : là où un seuil se dessine.
        public static bool OuvreUnLieu(int palier) => IdlePond.Noyau.Economie.EstUnePercee(palier);

        /// Un lieu qui s'ouvre par une cassure (2026-11-25) : c'est là, et là seulement, qu'il y
        /// a un seuil et une membrane. Un lieu en fondu n'en a pas : son eau prolonge celle
        /// d'au-dessus.
        public static bool ACassure(int palier) =>
            OuvreUnLieu(palier) && RegistreDArt.DecorDe(Assises.DuPalier(palier).Id).Jonction == JonctionDeLieu.Cassure;

        /// Le pied du seuil d'un lieu qui s'ouvre au `palier` : sa lèvre affleure la jonction.
        public static int YDuSeuil(int palier) => -palier * Gabarits.HAUTEUR_DE_BANDE - HAUTEUR_DU_SEUIL;

        public static int XDeLaBreche => (int)Math.Round(Gabarits.LARGEUR_VISEE * BRECHE);

        /// <summary>
        /// La bande `palier` touche-t-elle le champ de la caméra ? Eau ou roche, peu importe :
        /// la roche du lieu qui vient est SOUS la dernière bande d'eau, et un test sur les
        /// bandes d'eau visibles ne l'aurait jamais allumée.
        /// </summary>
        public static bool BandeDansLeChamp(int palier, ChampDeCamera champ)
        {
            if (champ == null || palier < 0) return false;
            var haut = -palier * Gabarits.HAUTEUR_DE_BANDE;
            var bas = haut - Gabarits.HAUTEUR_DE_BANDE;
            return bas < champ.Haut && haut > champ.Bas;
        }

        /* ─── La membrane ───────────────────────────────────────────────────────── */

        /// <summary>
        /// La membrane d'un lieu qui s'ouvre par une cassure (2026-11-26) : une barrière tissée
        /// du mana du lieu, qui sépare son eau de celle d'au-dessus et justifie la cassure. Bombée
        /// d'une paroi à l'autre comme le dessus d'une bulle : à `PROFONDEUR_AU_MILIEU` pixels sous
        /// la jonction au milieu du champ, en arc de cercle jusqu'à `CREUX_DE_LA_MEMBRANE` de plus
        /// contre les parois. Le seuil dessiné (`seuil.png`) met au-dessus d'elle l'eau du lieu
        /// d'au-dessus ; la scène la trace, vivante, par-dessus. Elle reste entière : on la
        /// traverse, on ne la déchire pas.
        /// </summary>
        public const int PROFONDEUR_AU_MILIEU = 5;
        public const int CREUX_DE_LA_MEMBRANE = 36;
        public const int RAYON_DE_LA_MEMBRANE = 176;
        public const int PAS_DE_LA_MEMBRANE = 2;
        /// Le temps que met un reflet à traverser la membrane, d'une paroi à l'autre.
        public const double TRAVERSEE_DU_REFLET = 4.0;

        /// La profondeur de la membrane sous la jonction à l'abscisse `x` du champ, au repos.
        public static double CreuxDeLaMembrane(int x)
        {
            var u = (x - Gabarits.LARGEUR_VISEE / 2) / (double)RAYON_DE_LA_MEMBRANE;
            return PROFONDEUR_AU_MILIEU + CREUX_DE_LA_MEMBRANE * (1 - Math.Sqrt(Math.Max(0.0, 1 - u * u)));
        }

        /// L'ondulation de la membrane : d'un pixel au plus, deux ondes qui courent en sens
        /// contraires.
        public static int OndeDeLaMembrane(int x, double secondes) =>
            (int)Math.Round(0.8 * Math.Sin(2 * Math.PI * x / 64.0 - secondes * 1.7) + 0.5 * Math.Sin(2 * Math.PI * x / 23.0 + secondes * 2.3));

        /// <summary>
        /// Ce qu'un nageur du `palier` laisse libre sous le haut de sa bande quand il passe sur
        /// les x de `x0` à `x1` : la marge ordinaire de nage, ou, dans la vasque d'une cassure, la
        /// membrane et sa lueur, plus deux pixels — rien ne nage dans l'eau d'au-dessus.
        /// </summary>
        public static int PlafondDeNage(int palier, int x0, int x1)
        {
            const int MARGE = 9;
            if (!ACassure(palier)) return MARGE;
            var creux = 0.0;
            for (var x = x0; x <= x1; x++) creux = Math.Max(creux, CreuxDeLaMembrane(x));
            return Math.Max(MARGE, (int)Math.Ceiling(creux) + 7);
        }

        /// <summary>
        /// Les points de la membrane du lieu qui s'ouvre au `palier`, à `secondes` : deux rangées
        /// de points de deux pixels tous les `PAS_DE_LA_MEMBRANE` — quatre pixels d'épaisseur —,
        /// sur toute la largeur du champ (les parois passent devant). `Couleur` : 0 le reflet
        /// d'écume qui la parcourt, 1 la membrane, à la couleur du mana du lieu ; 2 la lueur plus
        /// pâle qui la borde de part et d'autre, un point sur deux.
        /// </summary>
        public static IReadOnlyList<PointDeGerbe> Membrane(int palier, double secondes)
        {
            var points = new List<PointDeGerbe>();
            var jonction = -palier * Gabarits.HAUTEUR_DE_BANDE;
            // Le reflet glisse d'une paroi à l'autre, puis repart : sa position dans le champ.
            var reflet = Modulo(secondes, TRAVERSEE_DU_REFLET) / TRAVERSEE_DU_REFLET * (Gabarits.LARGEUR_VISEE + 80) - 40;
            for (var x = 0; x < Gabarits.LARGEUR_VISEE; x += PAS_DE_LA_MEMBRANE)
            {
                var y = jonction - (int)Math.Round(CreuxDeLaMembrane(x)) - OndeDeLaMembrane(x, secondes) - 1;
                var couleur = Math.Abs(x - reflet) < 24 ? 0 : 1;
                points.Add(new PointDeGerbe(x, y, couleur));
                points.Add(new PointDeGerbe(x, y - 2, couleur));
                if (x % (2 * PAS_DE_LA_MEMBRANE) != 0) continue;
                points.Add(new PointDeGerbe(x, y + 2, 2));
                points.Add(new PointDeGerbe(x, y - 4, 2));
            }
            return points;
        }

        /* ─── Voir venir le lieu suivant ───────────────────────────────────────── */

        public const int LUEURS = 12;
        public const double PERIODE_D_UNE_LUEUR = 3.0;

        /// Les lueurs de la magie du lieu suivant, à travers la roche de son premier palier :
        /// douze points qui s'allument chacun un tiers du temps, à leur place dans le HAUT de la
        /// roche — la rive (`Cadrage.RIVE_DE_ROCHE`) n'en montre que la moitié haute quand l'eau
        /// dépasse le cadre. Le dessin leur ajoute un halo de deux pixels.
        public static IReadOnlyList<Pixel> Lueurs(int palier, double secondes)
        {
            var lueurs = new List<Pixel>();
            for (var i = 0; i < LUEURS; i++)
            {
                var h = (uint)(palier * 7919 + i * 104729 + 29);
                h ^= h >> 15; h *= 0x2c1b3c6dU; h ^= h >> 12;
                var phase = (h % 1000u) / 1000.0 * PERIODE_D_UNE_LUEUR;
                if (Modulo(secondes + phase, PERIODE_D_UNE_LUEUR) > PERIODE_D_UNE_LUEUR / 3) continue;
                var x = 12 + (int)((h >> 10) % (uint)(Gabarits.LARGEUR_VISEE - 24));
                // De 6 px sous la jonction jusqu'à 6 px au-dessus du bas de la rive, halo compris.
                var y = -palier * Gabarits.HAUTEUR_DE_BANDE - 6 - (int)((h >> 20) % (uint)(Cadrage.RIVE_DE_ROCHE - 12));
                lueurs.Add(new Pixel(x, y));
            }
            return lueurs;
        }

        /// La couleur de la magie d'un lieu, celle de sa marque sur le héros (bible v0.3) : le
        /// vert de Sève, le bleu de Courant. Un lieu sans marque dessinée prend l'or des étoiles.
        public static int CouleurDeLaMagie(string lieu)
        {
            var marque = RegistreDArt.MarqueDe(lieu);
            if (marque != null && marque.Couleurs.Count > 0) return marque.Couleurs[0];
            // Un lieu préparé en avance porte sa magie dans son décor (2026-11-26).
            return RegistreDArt.DecorDe(lieu).Magie ?? 0xFFE070;
        }

        static double Modulo(double a, double b)
        {
            var r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
