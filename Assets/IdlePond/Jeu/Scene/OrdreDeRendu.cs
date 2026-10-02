namespace IdlePond.Jeu.Scene
{
    /// L'ordre d'affichage, de l'arrière vers l'avant (spec DA §4). Des `sortingOrder` sur
    /// une seule couche de tri : le lexique interdit toute API dont le nom porte l'ancien mot.
    public static class OrdreDeRendu
    {
        public const int FOND = 0;
        public const int RAYONS = 10;
        public const int BORD = 20;
        public const int NAGEURS = 30;
        public const int CORPS = 40;
        /// Une marque par couche : MARQUES + son rang dans l'ordre des assises traversées.
        public const int MARQUES = 50;
        public const int PARTICULES = 70;
        public const int VOILE = 80;
    }
}
