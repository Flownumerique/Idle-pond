/*
 * IdlePond — les paliers de voix (GDD §13.1). Port de `src/noyau/voix.ts`.
 *
 * | Palier      | Ce que l'UI montre                          | Déclencheur     |
 * |-------------|---------------------------------------------|-----------------|
 * | La pente    | Aucun chiffre. Une jauge sans graduation.   | Début           |
 * | Les signes  | Le lexique. Barres graduées, ordres de gr.  | 1ᵉʳ franchis.   |
 * | Les direct. | Chiffres complets, débits, taux.            | 3ᵉ franchis.    |
 * | Le dialogue | Tout.                                       | Le relais       |
 *
 * Une dérivation et non un champ d'état : le palier est entièrement fonction du
 * nombre de franchissements survécus.
 */
using System.Collections.Generic;

namespace IdlePond.Noyau
{
    public static class Voix
    {
        /// <summary>Ordre du plus pauvre au plus riche. Une voix ne redescend jamais.</summary>
        public static readonly IReadOnlyList<PalierDeVoix> PaliersDeVoix = new[]
        {
            PalierDeVoix.Pente, PalierDeVoix.Signes, PalierDeVoix.Directives, PalierDeVoix.Dialogue,
        };

        /// <summary>Le palier courant. `Dialogue` n'est pas atteignable : il vient du relais (§12.2).</summary>
        public static PalierDeVoix PalierCourant(EtatJeu etat) => PalierDeVoixApres(etat.Permanent.NombreEclosions);

        /// <summary>Le même, depuis le seul nombre de franchissements — pour la migration de save.</summary>
        public static PalierDeVoix PalierDeVoixApres(int franchissements)
        {
            if (franchissements >= Constantes.FranchissementsPourLesDirectives) return PalierDeVoix.Directives;
            if (franchissements >= Constantes.FranchissementsPourLesSignes) return PalierDeVoix.Signes;
            return PalierDeVoix.Pente;
        }

        /// <summary>Vrai si le palier `atteint` est au moins `requis`.</summary>
        public static bool VoixAuMoins(PalierDeVoix atteint, PalierDeVoix requis) => (int)atteint >= (int)requis;
    }
}
