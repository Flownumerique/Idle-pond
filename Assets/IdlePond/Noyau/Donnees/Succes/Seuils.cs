using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — succès de famille SEUIL.
    ///
    /// §8.1 : « 10/25/50/100 individus, palier saturé, divergence observée.
    /// Continue. GÉNÉRÉS PAR GABARIT. » Ce fichier est donc un gabarit, pas une
    /// liste.
    ///
    /// Les seuils comptaient des INDIVIDUS ; ils comptent maintenant des NIVEAUX —
    /// il n'y a plus de population. Les quatre paliers du §8.1 sont inchangés :
    /// 10 / 25 / 50 / 100, et le centième est celui qui pose le drapeau permanent.
    ///
    /// L'épinoche est passée en tête de l'assise II avec la répartition
    /// 2/4/4/4/4/3 ; ses quatre entrées la suivent plutôt que de disparaître, le
    /// registre étant figé (§8). Elles ne seront listées que lorsque son assise sera
    /// atteinte, donc jamais avant qu'elle soit produite.
    ///
    /// Effet : chiffre, conformément au défaut orientatif du §8.2, et une remise de
    /// coût plutôt qu'une montée de production — voir la lecture du §4.3 retenue
    /// dans `EffetDeSucces`.
    /// </summary>
    public static class Seuils
    {
        const string ASSISE = "noue";

        /// Les seuils du §8.1, à la lettre — lus sur le niveau.
        static readonly IReadOnlyList<int> SEUILS_DE_NIVEAU = new[] { 10, 25, 50, 100 };

        /// Fermé passé le premier : le joueur a compris le motif, on ne le lui répète pas.
        static VisibiliteDeSucces VisibiliteDuRang(int rang) => rang == 0 ? VisibiliteDeSucces.Ouvert : VisibiliteDeSucces.Ferme;

        static IEnumerable<Succes> Gabarit(Espece espece)
        {
            for (var rang = 0; rang < SEUILS_DE_NIVEAU.Count; rang += 1)
            {
                var seuil = SEUILS_DE_NIVEAU[rang];
                yield return new Succes(
                    $"seuil-{espece.Id}-{seuil}", FamilleDeSucces.Seuil, VisibiliteDuRang(rang), espece.Assise,
                    new DeclencheurDeSucces(QuoiDeclencheur.NiveauDEspece, seuil, espece.Id),
                    EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, Constantes.PART_REMISE_D_UN_SUCCES));
            }
        }

        /// <summary>
        /// Le Gour (spec du 2026-10-07) : l'épinoche, nommée au canon (§2.E), y était
        /// déjà passée avec ses seuils ; le chabot, la lamproie et l'ombre la rejoignent
        /// par le même gabarit. Un identifiant entré au registre n'en sort plus.
        /// </summary>
        static readonly IReadOnlyList<Espece> DU_GOUR = Especes.DeLAssise("gour");

        /// <summary>
        /// Ce que la mare porte en tout. Le gabarit par espèce se tait dès que le joueur
        /// change de banc ; celui-ci compte tous les niveaux tenus et ne se tait jamais.
        /// </summary>
        static readonly IReadOnlyList<int> SEUILS_DE_LA_MARE = new[] { 25, 45, 70, 95, 130, 200, 320, 500 };

        /// Profondeur atteinte dans la vie courante. L'axe de la descente.
        static readonly IReadOnlyList<int> SEUILS_DE_PROFONDEUR = new[] { 3, 5 };

        static readonly int PALIERS_DE_L_ASSISE = Assises.Toutes[0].NombreDePaliers;

        static IReadOnlyList<Succes> Construire()
        {
            var liste = new List<Succes>();

            foreach (var espece in Especes.DeLAssise(ASSISE).Concat(DU_GOUR))
                liste.AddRange(Gabarit(espece));

            for (var rang = 0; rang < SEUILS_DE_LA_MARE.Count; rang += 1)
            {
                var seuil = SEUILS_DE_LA_MARE[rang];
                liste.Add(new Succes(
                    $"seuil-mare-{seuil}", FamilleDeSucces.Seuil, VisibiliteDuRang(rang), ASSISE,
                    new DeclencheurDeSucces(QuoiDeclencheur.NiveauxCumules, seuil),
                    EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, Constantes.PART_REMISE_D_UN_SUCCES)));
            }

            for (var rang = 0; rang < SEUILS_DE_PROFONDEUR.Count; rang += 1)
            {
                var seuil = SEUILS_DE_PROFONDEUR[rang];
                liste.Add(new Succes(
                    $"seuil-profondeur-{seuil}", FamilleDeSucces.Seuil, VisibiliteDuRang(rang), ASSISE,
                    new DeclencheurDeSucces(QuoiDeclencheur.PaliersOuverts, seuil),
                    EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, Constantes.PART_REMISE_D_UN_SUCCES)));
            }

            // Creux au complet : le §8.1 le range explicitement dans les seuils. Le
            // premier est un acte — la découverte qu'un creux peut être plein ; les
            // suivants sont la mesure de ce qu'on a rempli, et restent secrets pour ne
            // pas transformer l'écran en liste de courses.
            for (var index = 0; index < PALIERS_DE_L_ASSISE - 1; index += 1)
            {
                var palier = index + 1;
                liste.Add(new Succes(
                    $"seuil-palier-sature-{palier}", FamilleDeSucces.Seuil, VisibiliteDeSucces.Secret, ASSISE,
                    new DeclencheurDeSucces(QuoiDeclencheur.PalierAuComplet, Palier: palier),
                    EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, Constantes.PART_REMISE_D_UN_SUCCES)));
            }

            return liste;
        }

        public static readonly IReadOnlyList<Succes> Tous = Construire();
    }
}
