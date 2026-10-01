using System.Collections.Generic;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — les paliers de voix (GDD §13.1).
    ///
    /// « Axe de progression le plus important du jeu : il porte le tutoriel,
    /// l'interface et le récit en même temps. »
    ///
    /// | Palier      | Ce que l'UI montre                          | Déclencheur     |
    /// |-------------|---------------------------------------------|-----------------|
    /// | La pente    | Aucun chiffre. Une jauge sans graduation.   | Début           |
    /// | Les signes  | Le lexique. Barres graduées, ordres de gr.  | 1ᵉʳ franchis.   |
    /// | Les direct. | Chiffres complets, débits, taux.            | 3ᵉ franchis.    |
    /// | Le dialogue | Tout.                                       | Le relais       |
    ///
    /// Ce module ne fait qu'une chose : dire à quel palier le héros en est. La
    /// numérisation de l'UI qui en découle appartient à l'UI, et le registre figé
    /// des succès (§14.5) le lit au moment où une entrée tombe.
    ///
    /// Pourquoi une dérivation et non un champ d'état : le palier est entièrement
    /// fonction du nombre de renaissances survécues, et le contrat du §5.1
    /// n'admet aucun état hors du reducer. Le jour où le relais existera (§12.2),
    /// c'est l'ÉVÉNEMENT qui entrera dans l'état permanent — pas le palier, qui
    /// continuera de s'en déduire ici.
    /// </summary>
    public static class Voix
    {
        /// Ordre du plus pauvre au plus riche. Une voix ne redescend jamais.
        public static readonly IReadOnlyList<PalierDeVoix> PALIERS = new[]
        {
            PalierDeVoix.Pente, PalierDeVoix.Signes, PalierDeVoix.Directives, PalierDeVoix.Dialogue,
        };

        /// <summary>
        /// Le palier courant.
        ///
        /// `Dialogue` n'est pas atteignable : il est déclenché par le relais (§12.2),
        /// qui est du contenu de la v1.0. Il figure dans le type parce que le registre
        /// d'un succès est figé POUR TOUJOURS au moment où il tombe — un type qui
        /// gagnerait une valeur plus tard rendrait les saves d'aujourd'hui ambiguës.
        /// </summary>
        public static PalierDeVoix PalierDe(EtatJeu etat) => PalierApres(etat.Permanent.NombreDeRenaissances);

        /// <summary>
        /// Le même, depuis le seul nombre de renaissances.
        ///
        /// Existe pour la migration de save, qui n'a pas d'`EtatJeu` sous la main — elle
        /// travaille sur du contenu sérialisé, et fabriquer un état complet pour lire
        /// une seule dérivation reviendrait à dupliquer la règle.
        /// </summary>
        public static PalierDeVoix PalierApres(int franchissements)
        {
            if (franchissements >= Constantes.FRANCHISSEMENTS_POUR_LES_DIRECTIVES) return PalierDeVoix.Directives;
            if (franchissements >= Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES) return PalierDeVoix.Signes;
            return PalierDeVoix.Pente;
        }

        static int IndexDe(PalierDeVoix palier)
        {
            for (var i = 0; i < PALIERS.Count; i += 1) if (PALIERS[i] == palier) return i;
            return -1;
        }

        /// Vrai si le palier `atteint` est au moins `requis`. Pour la numérisation de l'UI.
        public static bool AuMoins(PalierDeVoix atteint, PalierDeVoix requis) => IndexDe(atteint) >= IndexDe(requis);
    }
}
