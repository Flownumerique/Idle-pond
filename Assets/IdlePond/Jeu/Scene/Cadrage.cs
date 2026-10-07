using System;

namespace IdlePond.Jeu.Scene
{
    public sealed record DimensionsDuRendu(int Facteur, int Largeur, int Hauteur);

    /// Ce que voit la caméra, en pixels du monde (y monte). La bande i occupe
    /// [−(i+1)·56 ; −i·56].
    public sealed record ChampDeCamera(int Gauche, int Haut, int Largeur, int Hauteur)
    {
        public int Bas => Haut - Hauteur;
    }

    /// <summary>
    /// IdlePond — le cadrage du rendu pixel (spec DA §2). Pur : k, la taille de la texture et
    /// le champ de la caméra se calculent sans Unity, et se testent.
    /// </summary>
    public static class Cadrage
    {
        /// k est ENTIER : tous les pixels du dessin ont la même taille à l'écran. La texture
        /// tient dans le cadre ; le reste (moins de k pixels) prend le fond de #scene.
        public static DimensionsDuRendu Dimensions(int largeurPx, int hauteurPx)
        {
            if (largeurPx < 1 || hauteurPx < 1) return null;
            var k = Math.Max(1, (int)Math.Round(largeurPx / (double)Gabarits.LARGEUR_VISEE, MidpointRounding.AwayFromZero));
            return new DimensionsDuRendu(k, Math.Max(1, largeurPx / k), Math.Max(1, hauteurPx / k));
        }

        /// Tout tient : le haut de la coupe en haut du cadre. Sinon, le bas de la coupe en bas
        /// du cadre — c'est là que vit le héros. Le champ est centré sur la largeur visée.
        public static ChampDeCamera Cadrer(DimensionsDuRendu d, int nombreDeBandes) =>
            Cadrer(d, nombreDeBandes, nombreDeBandes);

        /// <summary>
        /// Avec la roche à creuser dessinée sous l'eau : la coupe part de la surface tant que
        /// l'eau CREUSÉE tient dans le cadre — la roche remplit alors le dessous au lieu du
        /// vide. Sinon, le bas de l'eau creusée reste en bas du cadre, comme avant : la roche
        /// ne pousse jamais le héros hors de l'écran.
        /// </summary>
        public static ChampDeCamera Cadrer(DimensionsDuRendu d, int bandesOuvertes, int bandesDessinees)
        {
            var gauche = Gabarits.LARGEUR_VISEE / 2 - d.Largeur / 2;
            var eau = Math.Max(0, bandesOuvertes) * Gabarits.HAUTEUR_DE_BANDE;
            var tout = Math.Max(eau, Math.Max(0, bandesDessinees) * Gabarits.HAUTEUR_DE_BANDE);
            var haut = tout <= d.Hauteur || eau <= d.Hauteur ? 0 : d.Hauteur - eau;
            return new ChampDeCamera(gauche, haut, d.Largeur, d.Hauteur);
        }

        public static (int Premiere, int Derniere) BandesVisibles(ChampDeCamera champ, int nombreDeBandes)
        {
            if (nombreDeBandes <= 0) return (0, -1);
            var h = (double)Gabarits.HAUTEUR_DE_BANDE;
            var premiere = Math.Max(0, (int)Math.Floor(-champ.Haut / h));
            var derniere = Math.Min(nombreDeBandes - 1, (int)Math.Ceiling(-champ.Bas / h) - 1);
            return (premiere, derniere);
        }
    }
}
