using System;
using System.Collections.Generic;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le passage d'un lieu à l'autre (décidé par l'auteur le 2026-11-25) : ce qui se
    /// voit à la jonction de deux lieux, et ce qui se voit du lieu suivant avant de l'atteindre.
    ///
    /// - **La jonction** : en haut du premier palier d'un lieu, son seuil — la lèvre de roche
    ///   de la vasque (`Fonds/{lieu}/seuil.png`), et l'eau du lieu d'au-dessus qui y déborde en
    ///   cascade par une brèche. C'est le nom même du Gour : la vasque d'où l'eau déborde vers la
    ///   suivante.
    /// - **Voir venir** : la roche du premier palier du lieu suivant se montre sous celle du
    ///   lieu actuel (`VueDeScene.IndicesDeRoche`), et la magie de ce lieu suinte à travers,
    ///   en lueurs de sa couleur.
    ///
    /// Pur et déterministe ; rien n'y touche l'économie. La règle de la percée est dans le noyau
    /// (`Economie.TribusARallierPourPercer`).
    /// </summary>
    public static class Lieux
    {
        /// La hauteur du seuil dessiné, à cheval sur le haut de la bande du lieu.
        public const int HAUTEUR_DU_SEUIL = 20;

        /// La brèche de la cascade, en fraction de la largeur visée : un peu à gauche du héros.
        public const double BRECHE = 0.36;

        /// Le palier qui ouvre un lieu, hors le tout premier : là où un seuil se dessine.
        public static bool OuvreUnLieu(int palier) => IdlePond.Noyau.Economie.EstUnePercee(palier);

        /// Le pied du seuil d'un lieu qui s'ouvre au `palier` : sa lèvre affleure la jonction.
        public static int YDuSeuil(int palier) => -palier * Gabarits.HAUTEUR_DE_BANDE - HAUTEUR_DU_SEUIL;

        public static int XDeLaBreche => (int)Math.Round(Gabarits.LARGEUR_VISEE * BRECHE);

        /* ─── La cascade ───────────────────────────────────────────────────────── */

        public const int GOUTTES = 10;
        public const double CHUTE_D_UNE_GOUTTE = 1.1;

        /// Les gouttes qui tombent par la brèche dans la vasque du lieu qui s'ouvre au
        /// `palier` : elles chutent du haut de la bande sur un tiers de sa hauteur, en s'écartant
        /// un peu. Un point de deux pixels chacune ; `Couleur` : 0 l'écume, 1 l'eau claire.
        public static IReadOnlyList<PointDeGerbe> Cascade(int palier, double secondes)
        {
            var gouttes = new List<PointDeGerbe>();
            var haut = -palier * Gabarits.HAUTEUR_DE_BANDE - 4;
            for (var i = 0; i < GOUTTES; i++)
            {
                var phase = i * CHUTE_D_UNE_GOUTTE / GOUTTES;
                var t = Modulo(secondes + phase, CHUTE_D_UNE_GOUTTE) / CHUTE_D_UNE_GOUTTE;
                var ecart = (i % 3 - 1) * (1 + (int)Math.Round(4 * t));
                var y = haut - (int)Math.Round(t * t * Gabarits.HAUTEUR_DE_BANDE / 3.0);
                gouttes.Add(new PointDeGerbe(XDeLaBreche + ecart + (i % 2) * 2, y, i % 2));
            }
            return gouttes;
        }

        /* ─── Voir venir le lieu suivant ───────────────────────────────────────── */

        public const int LUEURS = 7;
        public const double PERIODE_D_UNE_LUEUR = 3.0;

        /// Les lueurs de la magie du lieu suivant, à travers la roche de son premier palier :
        /// sept points qui s'allument chacun un tiers du temps, à leur place dans la roche.
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
                var y = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE + 10 + (int)((h >> 20) % (uint)(Gabarits.HAUTEUR_DE_BANDE - 20));
                lueurs.Add(new Pixel(x, y));
            }
            return lueurs;
        }

        /// La couleur de la magie d'un lieu, celle de sa marque sur le héros (bible v0.3) : le
        /// vert de Sève, le bleu de Courant. Un lieu sans marque dessinée prend l'or des étoiles.
        public static int CouleurDeLaMagie(string lieu)
        {
            var marque = RegistreDArt.MarqueDe(lieu);
            return marque != null && marque.Couleurs.Count > 0 ? marque.Couleurs[0] : 0xFFE070;
        }

        static double Modulo(double a, double b)
        {
            var r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
