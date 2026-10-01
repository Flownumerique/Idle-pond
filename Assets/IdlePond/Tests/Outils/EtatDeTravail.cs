using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Un état de jeu non trivial pour les tests du noyau : plusieurs paliers
    /// ouverts, des espèces débloquées à des niveaux différents, des densités
    /// inégales. Un état plat ne prouverait pas grand-chose.
    ///
    /// Plus aucun `Tick` de mise en route à la fin : un niveau agit à l'instant où
    /// il est payé, donc l'état est complet dès le dernier achat. C'est exactement
    /// ce que le passage au modèle à niveau a supprimé — le régime transitoire où
    /// l'exponentielle pouvait mentir.
    ///
    /// La première espèce ouverte est montée au drapeau permanent (niveau 100) :
    /// sans ça, `MultiplicateurDesDrapeaux` vaut exactement 1 dans cette fixture,
    /// et aucun test bâti sur elle ne peut voir ce terme global (revue de qualité
    /// de la tâche 9, minor B).
    /// </summary>
    public static class EtatDeTravail
    {
        public static EtatJeu Creer(long graine = 12345, string contenance = "1e14")
        {
            var etat = Reducteur.EtatInitial(graine);
            etat = etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e12") },
                Permanent = etat.Permanent with
                {
                    ContenanceMana = Decimal.Parse(contenance),
                    Densites = etat.Permanent.Densites.Select((_, index) => index * 0.13).ToArray(),
                    ProfondeurMaxAtteinte = 9,
                },
            };
            for (var i = 0; i < 6; i++) etat = Reducteur.Creuser(etat);
            var ouvertes = Especes.Toutes.Where(e => e.Palier < etat.Cycle.PaliersOuverts).ToList();
            for (var index = 0; index < ouvertes.Count; index++)
            {
                var espece = ouvertes[index];
                etat = Reducteur.Debloquer(etat, espece.Id);
                var niveauCible = index == 0 ? Constantes.SEUIL_DU_DRAPEAU_PERMANENT : 12 + espece.Palier;
                for (var n = 0; n < niveauCible; n++) etat = Reducteur.Ameliorer(etat, espece.Id);
            }
            return etat;
        }
    }
}
