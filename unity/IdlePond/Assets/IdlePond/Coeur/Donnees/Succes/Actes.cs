/*
 * IdlePond — succès de famille ACTE. Port de `src/donnees/succes/actes.ts`.
 *
 * §8.1 : « Une fois chacun. À la main. » Ceux de l'assise I sont les gestes
 * d'ouverture : convaincre, creuser, remplir, répéter. Ils portent l'essentiel
 * du plancher du §8.4 sur les toutes premières minutes.
 */
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Actes
    {
        private const string Assise = "noue";

        private static readonly EffetDeSucces RemiseSurLaPlace =
            EffetDeSucces.ReductionDe(TermeDeFormule.CoutPlace, Constantes.PartRemiseDUnSucces);

        public static readonly IReadOnlyList<Noyau.Succes> Liste = new[]
        {
            new Noyau.Succes("acte-premiere-conviction", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.BancsConvaincus, 1), null),
            // Les toutes premières minutes tiennent sur ces deux-là. §8.4 : « premier
            // succès dans les deux premières minutes », puis un toutes les 3 à 5.
            new Noyau.Succes("acte-deuxieme-niveau", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, Assise,
                DeclencheurDeSucces.DeBanc(QuoiDeclencheur.PlaceDeBanc, Paliers.IdDeBanc("vairon", 0), 2), null),
            new Noyau.Succes("acte-cinquieme-niveau", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, Assise,
                DeclencheurDeSucces.DeBanc(QuoiDeclencheur.PlaceDeBanc, Paliers.IdDeBanc("vairon", 0), 5), RemiseSurLaPlace),
            new Noyau.Succes("acte-premier-banc-de-cinq", FamilleDeSucces.Acte, VisibiliteDeSucces.Ferme, Assise,
                DeclencheurDeSucces.DeBanc(QuoiDeclencheur.EffectifDeBanc, Paliers.IdDeBanc("vairon", 0), 5), null),
            new Noyau.Succes("acte-premier-creusement", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.PaliersOuverts, 2),
                EffetDeSucces.ReductionDe(TermeDeFormule.CoutCreuser, Constantes.PartRemiseDUnSucces)),
            new Noyau.Succes("acte-deux-bancs", FamilleDeSucces.Acte, VisibiliteDeSucces.Ouvert, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.BancsConvaincus, 2),
                EffetDeSucces.ReductionDe(TermeDeFormule.ReductionTechnique, Constantes.PartRemiseDUnSucces)),
            new Noyau.Succes("acte-dixieme-niveau", FamilleDeSucces.Acte, VisibiliteDeSucces.Ferme, Assise,
                DeclencheurDeSucces.DeBanc(QuoiDeclencheur.PlaceDeBanc, Paliers.IdDeBanc("vairon", 0), 10), RemiseSurLaPlace),
            new Noyau.Succes("acte-trois-bancs", FamilleDeSucces.Acte, VisibiliteDeSucces.Ferme, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.BancsConvaincus, 3),
                EffetDeSucces.ReductionDe(TermeDeFormule.ReductionTechnique, Constantes.PartRemiseDUnSucces)),
            // Secret : un emplacement vide, rien d'autre. Le joueur découvrira qu'un
            // palier peut être plein en le remplissant, pas en lisant une consigne.
            new Noyau.Succes("acte-premier-palier-sature", FamilleDeSucces.Acte, VisibiliteDeSucces.Secret, Assise,
                DeclencheurDeSucces.Sature(0), RemiseSurLaPlace),
        };
    }
}
