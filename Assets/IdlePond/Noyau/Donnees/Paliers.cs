using System;
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — les 62 paliers, et l'espèce que chacun ouvre.
    ///
    /// Un palier porte au plus UNE espèce, et deux paliers sur trois n'en portent
    /// aucune : le noyau v1.0 §1.3 ancre une espèce tous les trois paliers à partir
    /// du premier de l'assise. Ce qu'un palier sans espèce apporte n'est pas rien —
    /// c'est le multiplicateur de profondeur, qui porte `D` là où le bestiaire ne le
    /// porte pas.
    ///
    /// Le banc a disparu avec le modèle à population : il n'y a plus d'unité
    /// intermédiaire entre l'espèce et le palier, donc plus de liste par palier.
    /// </summary>
    public static class Paliers
    {
        static IReadOnlyList<Palier> Construire()
        {
            var paliers = new List<Palier>();
            foreach (var assise in Assises.Toutes)
            {
                for (var local = 0; local < assise.NombreDePaliers; local += 1)
                {
                    var index = assise.IndexPremierPalier + local;
                    var espece = Especes.Toutes.FirstOrDefault(e => e.Palier == index);
                    paliers.Add(new Palier(index, assise.Id, espece?.Id));
                }
            }
            if (paliers.Count != Constantes.NOMBRE_DE_PALIERS)
                throw new InvalidOperationException($"Compte de paliers incohérent : {paliers.Count} au lieu de {Constantes.NOMBRE_DE_PALIERS}");
            return paliers;
        }

        public static readonly IReadOnlyList<Palier> Tous = Construire();

        /// L'espèce qu'ouvre ce palier, s'il en ouvre une.
        public static Espece EspeceDuPalier(int index)
        {
            if (index < 0 || index >= Tous.Count) return null;
            var id = Tous[index].Espece;
            return id == null ? null : Especes.ParId(id);
        }
    }
}
