namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Portrait ou paysage : la seule décision de mise en page que le code prend. Tout le
    /// reste est dans le .uss, qui lit la classe posée sur la racine.
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
    }
}
