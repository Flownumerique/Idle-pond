/*
 * IdlePond — succès de famille FRANCHISSEMENT. Port de
 * `src/donnees/succes/franchissements.ts`.
 *
 * §8.4 impose que la PREMIÈRE ÉCLOSION en déclenche un, obligatoirement.
 *
 * Ils sont chiffrés, et c'est une dérogation assumée au défaut orientatif du
 * §8.2 (« verbe pour les franchissements ») : les onze CapaciteId du §7.3
 * appartiennent tous à l'arbre, et une capacité a exactement une source.
 */
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Franchissements
    {
        private const string Assise = "noue";

        public static readonly IReadOnlyList<Noyau.Succes> Liste = new[]
        {
            new Noyau.Succes("franchissement-premiere-eclosion", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ouvert, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.Eclosions, 1),
                EffetDeSucces.ReductionDe(TermeDeFormule.CoutCreuser, Constantes.PartRemiseDUnSucces)),
            new Noyau.Succes("franchissement-deuxieme-eclosion", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ferme, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.Eclosions, 2),
                EffetDeSucces.ReductionDe(TermeDeFormule.CoutCreuser, Constantes.PartRemiseDUnSucces)),
            new Noyau.Succes("franchissement-troisieme-eclosion", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ferme, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.Eclosions, 3),
                EffetDeSucces.ReductionDe(TermeDeFormule.ReductionTechnique, Constantes.PartRemiseDUnSucces)),
            new Noyau.Succes("franchissement-densite", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Secret, Assise,
                DeclencheurDeSucces.DensiteDe(0, 0.5), null),
            // Fin d'assise : tous les paliers de la mare ouverts dans la même vie.
            new Noyau.Succes("franchissement-fond-de-la-mare", FamilleDeSucces.Franchissement, VisibiliteDeSucces.Ferme, Assise,
                DeclencheurDeSucces.De(QuoiDeclencheur.PaliersOuverts, Assises.Liste[0].NombreDePaliers),
                EffetDeSucces.ReductionDe(TermeDeFormule.CoutCreuser, Constantes.PartRemiseDUnSucces)),
        };
    }
}
