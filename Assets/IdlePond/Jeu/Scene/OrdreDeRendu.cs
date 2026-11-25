namespace IdlePond.Jeu.Scene
{
    /// L'ordre d'affichage, de l'arrière vers l'avant (spec DA §4). Des `sortingOrder` sur
    /// une seule couche de tri : le lexique interdit toute API dont le nom porte l'ancien mot.
    public static class OrdreDeRendu
    {
        public const int FOND = 0;
        /// Ce qui flotte dans chaque bande (le pollen, les méduses, les grosses pierres) :
        /// devant l'eau, derrière les rayons.
        public const int AMBIANCE = 5;
        /// Les reflets de la surface sur l'eau de chaque bande : devant ce qui flotte, derrière
        /// les rayons et la roche.
        public const int CAUSTIQUES = 7;
        public const int RAYONS = 10;
        /// La roche à creuser passe devant les rayons, qui débordent sur deux bandes : le jour
        /// n'entre pas dans la pierre.
        public const int ROCHE = 15;
        /// La lèvre de la vasque d'un lieu, à sa jonction avec celui d'au-dessus (2026-11-25) :
        /// devant sa roche comme devant son eau ; les lueurs du lieu suivant juste devant.
        public const int SEUIL = 16;
        public const int LUEURS = 17;
        public const int BORD = 20;
        /// L'hydre, accrochée sous la surface parmi les racines.
        public const int HYDRE = 21;
        /// Les nids-forteresses de l'épinoche, sur le fond de sa bande : derrière les nageurs.
        public const int NIDS = 22;
        /// Les jardins de la Sève et la procession des écrevisses, au fond des bandes.
        public const int JARDINS = 23;
        public const int PROCESSION = 24;
        /// Les bulles, le pollen et les filets du courant : devant le fond, derrière les nageurs.
        public const int EAU_VIVANTE = 25;
        /// La glace du gel, sur la surface : devant la berge, derrière ce qui y patine.
        public const int GLACE = 26;
        /// Le mirage qu'une ombre laisse du nouveau venu : juste derrière les nageurs.
        public const int MIRAGES = 29;
        public const int NAGEURS = 30;
        /// Le représentant de chaque tribu, en tête de sa troupe : devant elle.
        public const int REPRESENTANTS = 31;
        /// Le fil de mana qui va jusqu'aux lamproies : devant les nageurs, derrière le héros.
        public const int FILS = 32;
        /// Les bêtes de surface (libellules, gerris et leurs runes, grenouilles) : devant tout
        /// ce qui nage, derrière le héros et son éveil.
        public const int SURFACE = 33;
        /// L'éveil du héros (bible v0.4) : l'aura, l'anneau et les rubans du voile derrière lui ;
        /// les runes sur lui, par-dessus ses marques ; les étoiles devant.
        public const int AURA = 35;
        public const int ANNEAU = 36;
        public const int RUBANS = 38;
        public const int CORPS = 40;
        public const int RUNES = 60;
        public const int ETOILES = 65;
        /// Une marque par couche : MARQUES + son rang dans l'ordre des assises traversées.
        public const int MARQUES = 50;
        public const int PARTICULES = 70;
        public const int VOILE = 80;
    }
}
