using System;
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — les espèces de base.
    ///
    /// 21 espèces réparties 2 / 4 / 4 / 4 / 4 / 3 sur six assises. Ce n'est pas un
    /// choix : c'est le décompte de « une espèce tous les trois paliers, à partir du
    /// premier de chaque assise » appliqué à 6/12/12/12/12/8 (RESULTATS.md,
    /// finding 4). Le chiffre de 24 du noyau v1.0 était une estimation.
    ///
    /// Les divergences (~6) sont du contenu v0.5 et ne sont pas déclarées ici.
    ///
    /// Les trois espèces nommées par l'amendement v1.1 §2.E le restent — noms réels
    /// du français d'eau douce, aucun qualificatif, aucune invention :
    ///
    ///   `vairon`    amorçage. Débit minuscule, foule énorme. La première chose qui
    ///               accepte. Assise I, palier 0.
    ///   `loche`     fouisseuse — le lien avec le creusement est gratuit. Assise I,
    ///               palier 3.
    ///   `epinoche`  dure, tient dans une eau qui se charge. Prépare la contrainte
    ///               de l'assise II — et elle y est passée : la Noue n'en porte plus
    ///               que deux, l'épinoche ouvre l'assise suivante. Elle est nommée
    ///               et justifiée au canon, donc elle n'est pas engendrée.
    ///
    /// `tanche` est RÉSERVÉE et ne doit être assignée à aucun générateur :
    /// longévité, faible débit, très forte contenance, c'est le portrait du héros
    /// (`especes-cadre.md` §2).
    ///
    /// [P] P3 — les dix-sept espèces plus profondes attendent la charte. Leur
    /// identifiant reste neutre ; le placement reste autorial (§4.2).
    /// </summary>
    public static class Especes
    {
        /// 21 espèces : 2 / 4 / 4 / 4 / 4 / 3 (RESULTATS.md, finding 4).
        static readonly IReadOnlyList<int> ESPECES_PAR_ASSISE = new[] { 2, 4, 4, 4, 4, 3 };

        /// Réservé au héros, jamais à un générateur (§2.E).
        public const string ESPECE_RESERVEE = "tanche";

        /// <summary>
        /// Les espèces déjà nommées au canon, par assise puis par rang dans l'assise.
        ///
        /// Une case vide laisse l'identifiant s'engendrer. C'est la charte phonétique du
        /// §2.E qui descend, assise après assise ; rien de générique ne s'affiche à
        /// l'écran, les noms d'écran vivant dans `Textes.cs`.
        /// </summary>
        static readonly IReadOnlyList<IReadOnlyList<string>> ESPECES_NOMMEES = new IReadOnlyList<string>[]
        {
            new[] { "vairon", "loche" },
            // Le Gour (spec du 2026-10-07) : trois façons de tenir dans le courant — sous
            // les pierres, accrochée à la roche, en le remontant.
            new[] { "epinoche", "chabot", "lamproie", "ombre" },
        };

        static IReadOnlyList<Espece> Construire()
        {
            var especes = new List<Espece>();
            for (var rangAssise = 0; rangAssise < Assises.Toutes.Count; rangAssise += 1)
            {
                var assise = Assises.Toutes[rangAssise];
                for (var i = 0; i < ESPECES_PAR_ASSISE[rangAssise]; i += 1)
                {
                    var nommees = rangAssise < ESPECES_NOMMEES.Count ? ESPECES_NOMMEES[rangAssise] : null;
                    var id = nommees != null && i < nommees.Count ? nommees[i] : $"espece-{assise.Rang}-{i + 1}";
                    if (id == ESPECE_RESERVEE) throw new InvalidOperationException("`tanche` est réservée au héros (§2.E)");
                    especes.Add(new Espece(
                        id,
                        assise.Id,
                        especes.Count,
                        // une espèce tous les 3 paliers, à partir du premier de l'assise
                        assise.IndexPremierPalier + i * Constantes.ESPECE_TOUS_LES_N_PALIERS));
                }
            }
            if (especes.Count != Constantes.NOMBRE_D_ESPECES_DE_BASE)
                throw new InvalidOperationException($"Compte d'espèces incohérent : {especes.Count} au lieu de {Constantes.NOMBRE_D_ESPECES_DE_BASE}");
            return especes;
        }

        public static readonly IReadOnlyList<Espece> Toutes = Construire();

        static readonly IReadOnlyDictionary<string, Espece> PAR_ID = Toutes.ToDictionary(e => e.Id);

        public static Espece ParId(string id) => PAR_ID.TryGetValue(id, out var espece) ? espece : null;

        public static IReadOnlyList<Espece> DeLAssise(string assiseId) => Toutes.Where(e => e.Assise == assiseId).ToList();
    }
}
