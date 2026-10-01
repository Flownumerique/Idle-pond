using System;
using System.Collections.Generic;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — puissances tabulées des ratios géométriques.
    ///
    /// Contenu pur, sans logique : deux suites entièrement déterminées par les
    /// constantes du §13. Elles sont tabulées une fois parce que `Decimal.Pow` est
    /// appelé des centaines de milliers de fois par simulation, et qu'un simulateur
    /// lent est un simulateur qu'on ne lance pas.
    ///
    /// Ce n'est pas un cache au sens du §5.1 — rien ici ne dépend d'un état de jeu,
    /// rien ne se met à jour, rien ne se souvient d'un tick : ce sont des constantes
    /// dérivées de constantes, au même titre que la liste des paliers. Le noyau
    /// reste sans état hors du reducer.
    /// </summary>
    public static class Echelles
    {
        static IReadOnlyList<Decimal> Tabuler(double ratio, int longueur)
        {
            var table = new List<Decimal> { Decimal.Un };
            for (var i = 1; i < longueur; i += 1) table.Add(table[i - 1].Mul(ratio));
            return table;
        }

        /// g^p, pour tous les paliers.
        static readonly IReadOnlyList<Decimal> PUISSANCES_DE_G = Tabuler(Constantes.G_COUT_PALIER, Constantes.NOMBRE_DE_PALIERS + 1);

        /// <summary>
        /// 1.15^n. Le niveau n'est pas borné par le canon, seulement par ce que le
        /// joueur peut porter : au-delà de la table, on retombe sur le calcul direct.
        /// </summary>
        const int NIVEAUX_TABULES = 2048;
        static readonly IReadOnlyList<Decimal> PUISSANCES_DU_COUT_DE_NIVEAU = Tabuler(Constantes.RATIO_COUT_NIVEAU, NIVEAUX_TABULES);

        public static Decimal PuissanceDeG(int exposant) =>
            exposant >= 0 && exposant < PUISSANCES_DE_G.Count ? PUISSANCES_DE_G[exposant] : Decimal.Pow(Constantes.G_COUT_PALIER, exposant);

        public static Decimal PuissanceDuCoutDeNiveau(int exposant) =>
            exposant >= 0 && exposant < PUISSANCES_DU_COUT_DE_NIVEAU.Count ? PUISSANCES_DU_COUT_DE_NIVEAU[exposant] : Decimal.Pow(Constantes.RATIO_COUT_NIVEAU, exposant);

        /// <summary>
        /// `m_p ^ paliers`, le multiplicateur global de profondeur.
        ///
        /// Tabulé, et non calculé par `Tabuler` : les deux suites plus haut se
        /// construisent par multiplications successives, celle-ci par `Decimal.Pow`
        /// comme l'écrivait `puissanceDuPalier`. Ce n'est pas un détail de forme —
        /// les deux chemins ne rendent pas le même flottant, et toute mesure déjà
        /// prise bougerait sous nos pieds. La table mémorise l'expression exacte,
        /// elle ne la réécrit pas.
        /// </summary>
        static Decimal PuissanceDuPalier(int exposant) => Decimal.Pow(Constantes.MultiplicateurDePalier(), exposant);

        // `NOMBRE_DE_PALIERS + 2` : l'exposant vaut `paliersOuverts - 1`, et le gain de
        // creusement en demande un de plus, sur un état où le palier suivant est ouvert.
        static readonly IReadOnlyList<Decimal> PUISSANCES_DU_MULTIPLICATEUR_DE_PALIER = BatirPuissancesDuPalier();

        static IReadOnlyList<Decimal> BatirPuissancesDuPalier()
        {
            var table = new List<Decimal>();
            for (var p = 0; p < Constantes.NOMBRE_DE_PALIERS + 2; p += 1) table.Add(PuissanceDuPalier(p));
            return table;
        }

        public static Decimal PuissanceDuMultiplicateurDePalier(int exposant) =>
            exposant >= 0 && exposant < PUISSANCES_DU_MULTIPLICATEUR_DE_PALIER.Count
                ? PUISSANCES_DU_MULTIPLICATEUR_DE_PALIER[exposant]
                : PuissanceDuPalier(exposant);

        /// <summary>
        /// Débit de base par RANG d'espèce — `TAUX_BASE_AU_PALIER_0 × ratio^rang`.
        ///
        /// Même raison que les puissances de `g` : `DebitBaseDeLEspece` est appelée deux
        /// fois par espèce et par décision d'achat, soit des dizaines de millions de
        /// fois par `Simuler(45)`, pour rendre à chaque fois l'un d'une vingtaine de
        /// nombres. La table porte l'expression telle quelle, `Math.Pow` compris.
        /// </summary>
        static Decimal DebitBaseDuRangCalcule(int rang) =>
            new Decimal(Constantes.TAUX_BASE_AU_PALIER_0).Mul(Math.Pow(Constantes.DEBIT_RATIO_ESPECE, rang));

        // Le rang est l'index dans `Especes.Toutes`, pas un palier : une vingtaine
        // aujourd'hui. `NOMBRE_DE_PALIERS` n'est ici qu'une borne supérieure commode et
        // large — une espèce par palier au plus —, et le repli couvre le reste. Importer
        // `Especes` pour la dimensionner au plus juste ajouterait une dépendance de
        // données que ce fichier n'a pas.
        static readonly IReadOnlyList<Decimal> DEBITS_DE_BASE = BatirDebitsDeBase();

        static IReadOnlyList<Decimal> BatirDebitsDeBase()
        {
            var table = new List<Decimal>();
            for (var rang = 0; rang < Constantes.NOMBRE_DE_PALIERS + 1; rang += 1) table.Add(DebitBaseDuRangCalcule(rang));
            return table;
        }

        public static Decimal DebitBaseDuRang(int rang) =>
            rang >= 0 && rang < DEBITS_DE_BASE.Count ? DEBITS_DE_BASE[rang] : DebitBaseDuRangCalcule(rang);
    }
}
