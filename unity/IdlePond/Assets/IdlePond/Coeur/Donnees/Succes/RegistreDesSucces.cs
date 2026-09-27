/*
 * IdlePond — registre des succès, un fichier par famille. Port de
 * `src/donnees/succes/index.ts`.
 *
 * Le registre est FIGÉ : un identifiant qui est entré ici n'en sort plus et ne
 * change plus de sens, parce qu'il est écrit dans les sauvegardes.
 */
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Donnees
{
    public static class RegistreDesSucces
    {
        public static readonly IReadOnlyList<Noyau.Succes> Liste =
            Actes.Liste.Concat(Seuils.Liste).Concat(Franchissements.Liste).ToArray();
    }
}
