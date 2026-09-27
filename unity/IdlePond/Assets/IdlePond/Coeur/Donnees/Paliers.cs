/*
 * IdlePond — les 62 paliers et les bancs qui les occupent. Port de
 * `src/donnees/paliers.ts`.
 *
 * [P] §4.2 — le placement des espèces est autorial ; celui-ci est PROVISOIRE :
 * un banc par palier. La structure porte une LISTE de bancs par palier pour que
 * le placement définitif puisse en poser plusieurs sans réécriture.
 */
using System;
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Paliers
    {
        public static string IdDeBanc(string espece, int palier) => espece + "@" + palier;

        public static readonly IReadOnlyList<Palier> Liste = Construire();

        public static readonly IReadOnlyList<Banc> Bancs = ConstruireBancs();

        private static readonly Dictionary<string, Banc> ParId = ConstruireIndex();

        private static IReadOnlyList<Palier> Construire()
        {
            var paliers = new List<Palier>();
            foreach (var assise in Assises.Liste)
            {
                var especes = Especes.DeLAssise(assise.Id);
                for (var local = 0; local < assise.NombreDePaliers; local += 1)
                {
                    var index = assise.IndexPremierPalier + local;
                    var espece = especes[local * especes.Count / assise.NombreDePaliers];
                    var banc = new Banc(IdDeBanc(espece.Id, index), espece.Id, index);
                    paliers.Add(new Palier(index, assise.Id, new[] { banc }));
                }
            }
            if (paliers.Count != Constantes.NombreDePaliers)
            {
                throw new InvalidOperationException(
                    "Compte de paliers incohérent : " + paliers.Count + " au lieu de " + Constantes.NombreDePaliers);
            }
            return paliers.ToArray();
        }

        private static IReadOnlyList<Banc> ConstruireBancs()
        {
            var bancs = new List<Banc>();
            foreach (var palier in Liste) bancs.AddRange(palier.Bancs);
            return bancs.ToArray();
        }

        private static Dictionary<string, Banc> ConstruireIndex()
        {
            var index = new Dictionary<string, Banc>(StringComparer.Ordinal);
            foreach (var banc in Bancs) index[banc.Id] = banc;
            return index;
        }

        public static IReadOnlyList<Banc> BancsDuPalier(int index) => Liste[index].Bancs;

        /// <summary>Le banc de cet identifiant, ou null.</summary>
        public static Banc BancParId(string id) => id != null && ParId.TryGetValue(id, out var banc) ? banc : null;
    }
}
