using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — registre des succès, un fichier par famille.
    ///
    /// Le registre est FIGÉ : un identifiant qui est entré ici n'en sort plus et ne
    /// change plus de sens, parce qu'il est écrit dans les sauvegardes. Le §8 range
    /// cette propriété, avec le typage par famille et l'état de visibilité, parmi
    /// celles qui « ne se rétrofitent pas ».
    ///
    /// Le jalon v0.2 livre l'assise I. Les assises II à VI viendront avec leur
    /// contenu, jamais avant qu'elles soient produites.
    /// </summary>
    public static class RegistreDesSucces
    {
        public static readonly IReadOnlyList<Succes> Tous = Actes.Tous.Concat(Seuils.Tous).Concat(Franchissements.Tous).ToList();
    }
}
