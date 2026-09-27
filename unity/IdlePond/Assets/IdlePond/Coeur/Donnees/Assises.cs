/*
 * IdlePond — les six assises. Port de `src/donnees/assises.ts`.
 *
 * Contenu pur, sans logique. L'assise I est nommée par l'amendement v1.1 §2.E :
 * `la Noue`, identifiant `noue`. Les cinq autres attendent la charte phonétique
 * — leur identifiant reste neutre, et rien de générique ne s'affiche (§3).
 *
 * [P] — répartition des paliers : la Noue est courte parce qu'elle enseigne,
 * les 56 paliers restants se répartissent sur cinq assises. À confirmer.
 */
using System;
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Assises
    {
        /// <summary>Nommées au fur et à mesure que la charte phonétique descend (§2.E).</summary>
        private static readonly string[] IdentifiantsDAssise = { "noue" };

        private static readonly int[] PaliersParAssise = { 6, 11, 11, 11, 11, 12 };

        public static readonly IReadOnlyList<Assise> Liste = Construire();

        private static IReadOnlyList<Assise> Construire()
        {
            var assises = new List<Assise>();
            var index = 0;
            for (var rang = 0; rang < PaliersParAssise.Length; rang += 1)
            {
                var nombreDePaliers = PaliersParAssise[rang];
                var id = rang < IdentifiantsDAssise.Length ? IdentifiantsDAssise[rang] : "assise-" + (rang + 1);
                // Chaque assise a son type de mana propre. [P] identifiants provisoires.
                assises.Add(new Assise(id, rang + 1, "type-mana-" + (rang + 1), index, nombreDePaliers));
                index += nombreDePaliers;
            }
            if (index != Constantes.NombreDePaliers)
            {
                throw new InvalidOperationException(
                    "Distribution des paliers incohérente : " + index + " au lieu de " + Constantes.NombreDePaliers);
            }
            return assises.ToArray();
        }

        public static Assise DuPalier(int index)
        {
            foreach (var assise in Liste)
            {
                if (index >= assise.IndexPremierPalier && index < assise.IndexPremierPalier + assise.NombreDePaliers) return assise;
            }
            throw new ArgumentOutOfRangeException(nameof(index), "Palier hors des assises : " + index);
        }

        public static string TypeManaNatal => Liste[0].TypeMana;

        /// <summary>
        /// Ce que le jalon v0.2 livre réellement : l'assise I, et elle seule. « Aucune
        /// assise n'est produite avant que la précédente ait été mesurée » (§12).
        /// </summary>
        public static int PaliersLivres => Liste[0].NombreDePaliers;
    }
}
