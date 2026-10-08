using System;
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — le registre des améliorations.
    ///
    /// Contenu pur, engendré depuis les espèces : une ciblée par espèce, une
    /// globale. Aucune valeur ici — les graines vivent dans `Constantes.cs`, les
    /// formules dans `Economie.cs`. Les identifiants entrent dans les saves : figés.
    /// </summary>
    public static class AmeliorationsDeRenaissance
    {
        public const string GLOBALE_ID = "amelioration-globale";

        static IReadOnlyList<AmeliorationDeRenaissance> Construire()
        {
            var globale = new AmeliorationDeRenaissance(GLOBALE_ID, PorteeDAmelioration.Globale, null);
            var ciblees = Especes.Toutes.Select(espece => new AmeliorationDeRenaissance(
                $"amelioration-{espece.Id}", PorteeDAmelioration.Ciblee, espece.Id));
            return new[] { globale }.Concat(ciblees).ToList();
        }

        public static readonly IReadOnlyList<AmeliorationDeRenaissance> Toutes = Construire();

        public static AmeliorationDeRenaissance ParId(string id) => Toutes.FirstOrDefault(i => i.Id == id);

        /// La ciblée d'une espèce. Lance si l'espèce n'existe pas : le registre est engendré depuis elles.
        public static AmeliorationDeRenaissance CibleeDe(string espece)
        {
            var ciblee = Toutes.FirstOrDefault(i => i.Portee == PorteeDAmelioration.Ciblee && i.Espece == espece);
            if (ciblee == null) throw new InvalidOperationException($"Aucune amélioration ciblée pour l'espèce {espece}");
            return ciblee;
        }
    }
}
