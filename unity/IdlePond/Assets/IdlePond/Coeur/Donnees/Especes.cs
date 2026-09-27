/*
 * IdlePond — les espèces de base. Port de `src/donnees/especes.ts`.
 *
 * ~21 espèces réparties sur six assises (§13.1). Les trois de la Noue sont
 * nommées par l'amendement v1.1 §2.E : `vairon`, `loche`, `epinoche`.
 *
 * `tanche` est RÉSERVÉE et ne doit être assignée à aucun générateur : c'est le
 * portrait du héros (`especes-cadre.md` §2).
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class Especes
    {
        /// <summary>3 espèces sur l'assise I : c'est le contenu du jalon v0.2 (§12).</summary>
        private static readonly int[] EspecesParAssise = { 3, 3, 4, 4, 4, 3 };

        /// <summary>Réservé au héros, jamais à un générateur (§2.E).</summary>
        public const string EspeceReservee = "tanche";

        private static readonly string[] EspecesDeLaNoue = { "vairon", "loche", "epinoche" };

        public static readonly IReadOnlyList<Espece> Liste = Construire();

        private static IReadOnlyList<Espece> Construire()
        {
            var especes = new List<Espece>();
            for (var rang = 0; rang < Assises.Liste.Count; rang += 1)
            {
                var assise = Assises.Liste[rang];
                for (var i = 0; i < EspecesParAssise[rang]; i += 1)
                {
                    var id = rang == 0 ? EspecesDeLaNoue[i] : "espece-" + assise.Rang + "-" + (i + 1);
                    if (id == EspeceReservee) throw new InvalidOperationException("`tanche` est réservée au héros (§2.E)");
                    especes.Add(new Espece(id, assise.Id));
                }
            }
            if (especes.Count != Constantes.NombreDEspecesDeBase)
            {
                throw new InvalidOperationException(
                    "Compte d'espèces incohérent : " + especes.Count + " au lieu de " + Constantes.NombreDEspecesDeBase);
            }
            return especes.ToArray();
        }

        public static IReadOnlyList<Espece> DeLAssise(string assiseId) => Liste.Where(e => e.Assise == assiseId).ToArray();
    }
}
