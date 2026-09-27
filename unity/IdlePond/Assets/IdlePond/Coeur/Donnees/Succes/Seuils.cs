/*
 * IdlePond — succès de famille SEUIL. Port de `src/donnees/succes/seuils.ts`.
 *
 * §8.1 : « GÉNÉRÉS PAR GABARIT ». Ce fichier est donc un gabarit, pas une liste.
 * Les seuils comptent des INDIVIDUS, tous paliers confondus.
 */
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Seuils
    {
        private const string Assise = "noue";

        /// <summary>Les seuils du §8.1, à la lettre.</summary>
        private static readonly int[] SeuilsDIndividus = { 10, 25, 50, 100 };

        /// <summary>Effectif de la mare entière : celui-ci ne se tait jamais.</summary>
        private static readonly int[] SeuilsDeLaMare = { 25, 45, 70, 95, 130, 200, 320, 500 };

        /// <summary>Profondeur atteinte dans la vie courante. L'axe de la descente.</summary>
        private static readonly int[] SeuilsDeProfondeur = { 3, 5 };

        /// <summary>Fermé passé le premier : le joueur a compris le motif.</summary>
        private static VisibiliteDeSucces VisibiliteDuRang(int rang) => rang == 0 ? VisibiliteDeSucces.Ouvert : VisibiliteDeSucces.Ferme;

        private static readonly EffetDeSucces RemiseSurLaPlace =
            EffetDeSucces.ReductionDe(TermeDeFormule.CoutPlace, Constantes.PartRemiseDUnSucces);

        public static readonly IReadOnlyList<Noyau.Succes> Liste = Construire();

        private static IReadOnlyList<Noyau.Succes> Construire()
        {
            var liste = new List<Noyau.Succes>();

            foreach (var espece in Especes.DeLAssise(Assise))
            {
                for (var rang = 0; rang < SeuilsDIndividus.Length; rang += 1)
                {
                    var seuil = SeuilsDIndividus[rang];
                    liste.Add(new Noyau.Succes("seuil-" + espece.Id + "-" + seuil, FamilleDeSucces.Seuil, VisibiliteDuRang(rang), Assise,
                        DeclencheurDeSucces.DEspece(espece.Id, seuil), RemiseSurLaPlace));
                }
            }

            for (var rang = 0; rang < SeuilsDeLaMare.Length; rang += 1)
            {
                var seuil = SeuilsDeLaMare[rang];
                liste.Add(new Noyau.Succes("seuil-mare-" + seuil, FamilleDeSucces.Seuil, VisibiliteDuRang(rang), Assise,
                    DeclencheurDeSucces.De(QuoiDeclencheur.EffectifTotal, seuil), RemiseSurLaPlace));
            }

            for (var rang = 0; rang < SeuilsDeProfondeur.Length; rang += 1)
            {
                var seuil = SeuilsDeProfondeur[rang];
                liste.Add(new Noyau.Succes("seuil-profondeur-" + seuil, FamilleDeSucces.Seuil, VisibiliteDuRang(rang), Assise,
                    DeclencheurDeSucces.De(QuoiDeclencheur.PaliersOuverts, seuil),
                    EffetDeSucces.ReductionDe(TermeDeFormule.CoutCreuser, Constantes.PartRemiseDUnSucces)));
            }

            // Palier saturé : le premier est un acte — la découverte ; les suivants
            // sont la mesure de ce qu'on a rempli, et restent secrets.
            var paliersDeLAssise = Assises.Liste[0].NombreDePaliers;
            for (var palier = 1; palier < paliersDeLAssise; palier += 1)
            {
                liste.Add(new Noyau.Succes("seuil-palier-sature-" + palier, FamilleDeSucces.Seuil, VisibiliteDeSucces.Secret, Assise,
                    DeclencheurDeSucces.Sature(palier), RemiseSurLaPlace));
            }

            return liste.ToArray();
        }
    }
}
