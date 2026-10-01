using System.Collections.Generic;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — succès de famille FRANCHISSEMENT.
    ///
    /// §8.1 : « Éclosion, couche fixée, acclimatation complétée, fin d'assise.
    /// ~1 par cycle. À la main. »
    ///
    /// §8.4 impose que la PREMIÈRE ÉCLOSION en déclenche un, obligatoirement. C'est
    /// le seul succès de tout le registre qui soit exigé nommément par le contrat,
    /// et le test de plancher le vérifie.
    ///
    /// ── Sur l'effet : ils sont chiffrés, et c'est une dérogation assumée ────────
    /// Le §8.2 pose « verbe pour les franchissements » comme défaut ORIENTATIF. Il
    /// n'est pas suivi ici, pour une raison structurelle et non par facilité : les
    /// onze CapaciteId du §7.3 appartiennent tous à l'arbre, et le §7.5 règle 1 veut
    /// qu'une capacité ait exactement une source. Donner un verbe à un
    /// franchissement demanderait donc d'inventer une capacité — c'est-à-dire du
    /// canon — et de puiser dans les ~5 verbes que le §7.5 règle 2 réserve aux
    /// succès pour toute la partie.
    ///
    /// Les cinq verbes de succès restent donc à allouer, avec l'arbitrage du budget
    /// du §7.6, au jalon v0.4. L'assise I livre des chiffres.
    /// </summary>
    public static class Franchissements
    {
        const string ASSISE = "noue";

        public static readonly IReadOnlyList<Succes> Tous = new[]
        {
            new Succes(
                "franchissement-premiere-eclosion", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ouvert, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.Renaissances, 1),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "franchissement-deuxieme-eclosion", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ferme, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.Renaissances, 2),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "franchissement-troisieme-eclosion", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ferme, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.Renaissances, 3),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),

            new Succes(
                "franchissement-densite", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Secret, ASSISE,
                new DeclencheurDeSucces(QuoiDeclencheur.DensiteDePalier, 0.5, Palier: 0),
                null),

            new Succes(
                "franchissement-fond-de-la-mare", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ferme, ASSISE,
                // Fin d'assise : tous les paliers de la mare ouverts dans la même vie.
                new DeclencheurDeSucces(QuoiDeclencheur.PaliersOuverts, Assises.Toutes[0].NombreDePaliers),
                EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)),
        };
    }
}
