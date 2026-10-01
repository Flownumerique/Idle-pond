using System;
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — le registre des insufflations.
    ///
    /// Contenu pur, engendré depuis les espèces : une ciblée par espèce, une
    /// globale. Aucune valeur ici — les graines vivent dans `Constantes.cs`, les
    /// formules dans `Economie.cs`. Les identifiants entrent dans les saves : figés.
    /// </summary>
    public static class Insufflations
    {
        public const string GLOBALE_ID = "insufflation-globale";

        static IReadOnlyList<Insufflation> Construire()
        {
            var globale = new Insufflation(GLOBALE_ID, PorteeDInsufflation.Globale, null);
            var ciblees = Especes.Toutes.Select(espece => new Insufflation(
                $"insufflation-{espece.Id}", PorteeDInsufflation.Ciblee, espece.Id));
            return new[] { globale }.Concat(ciblees).ToList();
        }

        public static readonly IReadOnlyList<Insufflation> Toutes = Construire();

        public static Insufflation ParId(string id) => Toutes.FirstOrDefault(i => i.Id == id);

        /// La ciblée d'une espèce. Lance si l'espèce n'existe pas : le registre est engendré depuis elles.
        public static Insufflation CibleeDe(string espece)
        {
            var ciblee = Toutes.FirstOrDefault(i => i.Portee == PorteeDInsufflation.Ciblee && i.Espece == espece);
            if (ciblee == null) throw new InvalidOperationException($"Aucune insufflation ciblée pour l'espèce {espece}");
            return ciblee;
        }
    }
}
