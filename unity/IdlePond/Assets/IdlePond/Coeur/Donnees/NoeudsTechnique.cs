/*
 * IdlePond — registre des nœuds de technique. Port de
 * `src/donnees/noeuds-technique.ts`.
 *
 * VIDE À CE JALON, et c'est délibéré : le §12 place la technique au jalon v0.4.
 * Le registre existe dès maintenant parce que les tests de canon qui le
 * parcourent doivent être en place AVANT le contenu, pas ajoutés après.
 */
using System;
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Donnees
{
    public static class NoeudsTechnique
    {
        public static readonly IReadOnlyList<NoeudTechnique> Liste = Array.Empty<NoeudTechnique>();
    }
}
