using System;
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — les six assises.
    ///
    /// Contenu pur, sans logique.
    ///
    /// L'assise I est nommée par l'amendement v1.1 §2.E : `la Noue`, /nu/,
    /// hydronyme réel désignant une dépression humide, monosyllabe. Identifiant
    /// `noue`. L'assise II est `le Gour`, /guʁ/ (spec du 2026-10-07) : la vasque que
    /// l'eau creuse dans la roche des grottes, et d'où elle déborde vers la suivante —
    /// monosyllabe comme la Noue, voyelle plus sourde : la charte descend. Identifiant
    /// `gour`. Les quatre autres attendent encore la charte phonétique — leur
    /// identifiant reste neutre, et rien de générique ne s'affiche à l'écran (§3).
    ///
    /// [P] — répartition des paliers. Le §5.1 fixe 62 paliers en « distribution
    /// plate (~4,4 par cycle) » et le §6 donne six paliers à la Noue. Les deux ne
    /// se tiennent que si « plate » qualifie la distribution PAR CYCLE et non par
    /// assise — sinon il en faudrait ~10,3 chacune. C'est la lecture retenue : la
    /// Noue est courte parce qu'elle enseigne, les 56 paliers restants se
    /// répartissent sur cinq assises. À confirmer.
    /// </summary>
    public static class Assises
    {
        /// Nommées au fur et à mesure que la charte phonétique descend (§2.E).
        static readonly IReadOnlyList<string> IDENTIFIANTS_D_ASSISE = new[] { "noue", "gour" };

        /// <summary>
        /// 6 / 12 / 12 / 12 / 12 / 8 — la géométrie du spec §3, et elle n'est pas
        /// arbitraire : c'est la seule répartition à 62 paliers où « une espèce tous
        /// les trois paliers, à partir du premier de l'assise » tombe exactement sur
        /// `3 × rang`. L'ancre d'une espèce et son rang cessent alors de dériver l'un
        /// par rapport à l'autre, ce qui est la condition pour que son débit de base
        /// suive `D` palier par palier. Elle donne 2 + 4 + 4 + 4 + 4 + 3 = 21 espèces
        /// (RESULTATS.md, finding 4).
        /// </summary>
        static readonly IReadOnlyList<int> PALIERS_PAR_ASSISE = new[] { 6, 12, 12, 12, 12, 8 };

        static IReadOnlyList<Assise> Construire()
        {
            var assises = new List<Assise>();
            var index = 0;
            for (var rang = 0; rang < PALIERS_PAR_ASSISE.Count; rang += 1)
            {
                var nombreDePaliers = PALIERS_PAR_ASSISE[rang];
                assises.Add(new Assise(
                    rang < IDENTIFIANTS_D_ASSISE.Count ? IDENTIFIANTS_D_ASSISE[rang] : $"assise-{rang + 1}",
                    rang + 1,
                    // Chaque assise a son type de mana propre. [P] mana-typologie.md (Tier 1)
                    // n'est pas disponible dans le dépôt : les identifiants sont provisoires.
                    $"type-mana-{rang + 1}",
                    index,
                    nombreDePaliers));
                index += nombreDePaliers;
            }
            if (index != Constantes.NOMBRE_DE_PALIERS)
                throw new InvalidOperationException($"Distribution des paliers incohérente : {index} au lieu de {Constantes.NOMBRE_DE_PALIERS}");
            return assises;
        }

        public static readonly IReadOnlyList<Assise> Toutes = Construire();

        public static Assise ParId(string id) => Toutes.FirstOrDefault(a => a.Id == id);

        public static Assise DuPalier(int index)
        {
            var assise = Toutes.FirstOrDefault(a => index >= a.IndexPremierPalier && index < a.IndexPremierPalier + a.NombreDePaliers);
            if (assise == null) throw new InvalidOperationException($"Palier hors des assises : {index}");
            return assise;
        }

        /// <summary>
        /// Ce que le jalon v0.2 livre réellement : l'assise I, et elle seule.
        ///
        /// « Règle d'engagement : aucune assise n'est produite avant que la précédente
        /// ait été mesurée. On coupe au milieu, jamais à la fin » (§12). Les 62 paliers
        /// existent dans la donnée parce que c'est l'économie que le simulateur mesure ;
        /// le jeu, lui, s'arrête où le contenu s'arrête.
        ///
        /// La Noue et le Gour depuis le 2026-10-07 (spec « assise II, le Gour »). La
        /// règle d'engagement a été levée par l'utilisateur pour le Gour : la Noue n'est
        /// mesurée qu'au simulateur, et la mesure en jeu reste à faire pour les deux.
        /// </summary>
        public static readonly int PALIERS_LIVRES = Toutes[0].NombreDePaliers + Toutes[1].NombreDePaliers;
    }
}
