using System.Collections.Generic;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — succès de famille ACTE.
    ///
    /// §8.1 : « Premier temple, premier portail, première reconviction. Une fois
    /// chacun. À la main, ~30 sur la partie. » Ceux de l'assise I sont les gestes
    /// d'ouverture : débloquer, monter, creuser, répéter.
    ///
    /// Ils portent l'essentiel du plancher du §8.4 sur les toutes premières minutes,
    /// parce qu'un acte se déclenche à l'instant où le joueur fait quelque chose.
    /// Depuis que l'espèce est un générateur à niveau, c'est vrai de TOUS les
    /// déclencheurs : plus rien n'attend qu'une population monte. La cadence tient
    /// donc à l'échelonnement des seuils, et à rien d'autre.
    ///
    /// Les identifiants ne bougent pas — le registre est figé (§8) — même là où le
    /// mot « banc » y survit à la mécanique qui l'a porté.
    /// </summary>
    public static class Actes
    {
        const string ASSISE = "noue";

        public static readonly IReadOnlyList<Succes> Tous = new[]
        {
            new Succes(
                "acte-premiere-conviction", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.EspecesDebloquees, 1),
                null),

            // Les toutes premières minutes tiennent sur ces deux-là. §8.4 : « premier
            // succès dans les deux premières minutes », puis un toutes les 3 à 5.
            new Succes(
                "acte-deuxieme-niveau", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.NiveauDEspece, 2, "vairon"),
                null),

            new Succes(
                "acte-cinquieme-niveau", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.NiveauDEspece, 5, "vairon"),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "acte-premier-banc-de-cinq", FamilleDeSucces.Acte, VisibiliteDeSucces.Ferme, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.NiveauxCumules, 3),
                null),

            new Succes(
                "acte-premier-creusement", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.PaliersOuverts, 2),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "acte-deux-bancs", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.EspecesDebloquees, 2),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "acte-dixieme-niveau", FamilleDeSucces.Acte, VisibiliteDeSucces.Ferme, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.NiveauDEspece, 10, "vairon"),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, Constantes.PART_REMISE_D_UN_SUCCES)),

            // La Noue ne porte plus que deux espèces : « trois bancs » n'y existe plus.
            // Le geste qu'il marquait — avoir deux bancs et les tenir tous les deux —
            // se lit désormais sur la seconde espèce.
            new Succes(
                "acte-trois-bancs", FamilleDeSucces.Acte, VisibiliteDeSucces.Ferme, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.NiveauDEspece, 10, "loche"),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "acte-premier-palier-sature", FamilleDeSucces.Acte,
                // Secret : un emplacement vide, rien d'autre. Le joueur découvrira qu'un
                // creux peut être plein en le remplissant, pas en lisant une consigne.
                VisibiliteDeSucces.Secret, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.PalierAuComplet, Palier: 0),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, Constantes.PART_REMISE_D_UN_SUCCES)),
        };
    }
}
