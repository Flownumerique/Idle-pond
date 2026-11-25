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

        /// Un lieu qui s'ouvre par une cassure (2026-11-25) : c'est là, et là seulement, qu'il y
        /// a un seuil, une brèche et une cascade. Un lieu en fondu n'en a pas : son eau prolonge
        /// celle d'au-dessus.
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
            return marque != null && marque.Couleurs.Count > 0 ? marque.Couleurs[0] : 0xFFE070;
        }

        static double Modulo(double a, double b)
        {
            var r = a % b;
            return r < 0 ? r + b : r;
        }
    }
}
