using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// La parité du crédit hors ligne avec le TypeScript — `hors-ligne.json`, produit par
    /// `tests/parite/generer-hors-ligne.ts`. Elle comble le trou laissé par `parties.json`,
    /// où `heuresHorsLigneCreditees` vaut toujours 0 : plafond, millisecondes qui ne tombent
    /// pas rond, horloge reculée, absence nulle, et heures créditées gardées à travers une
    /// renaissance. Même règle que `PariteTests` : rouge, on cherche l'écart.
    /// </summary>
    public class PariteHorsLigneTests
    {
        static readonly JObject Reference = References.Lire("hors-ligne");

        static void Comparer(string nom, JToken obtenu, JToken attendu) =>
            Comparateur.ComparerATolerance(obtenu, attendu, Comparateur.TOLERANCE_RELATIVE, nom);

        /// Le même départ que `partieEnCours()` côté TypeScript.
        static EtatJeu PartieEnCours()
        {
            var etat = EtatDeTravail.Creer(9001);
            etat = Reducteur.Tick(etat, 600);
            etat = Reducteur.Creuser(etat);
            etat = Reducteur.Debloquer(etat, "vairon");
            for (var n = 0; n < 10; n++) etat = Reducteur.Ameliorer(etat, "vairon");
            return Reducteur.Tick(etat, 600);
        }

        [Test, Description("les absences créditées rendent les états du TypeScript")]
        public void Les_absences_creditees_rendent_les_etats_du_TypeScript()
        {
            var pas = (JArray)Reference["pas"];
            var etat = PartieEnCours();
            Comparer("pas[0]", Instantane.De(etat), pas[0]["instant"]);

            for (var i = 1; i < pas.Count; i++)
            {
                var attendu = pas[i];
                if ((string)attendu["etape"] == "renaissance")
                {
                    etat = Reducteur.Tick(etat, 3600);
                    for (var k = 0; k < 3; k++) etat = Reducteur.Grandir(etat);
                    etat = Renaissance.Renaitre(etat);
                }
                else
                {
                    Comparer($"pas[{i}].capHeures", HorsLigne.CapHorsLigneCourantHeures(etat), attendu["capHeures"]);
                    var retour = HorsLigne.Crediter(etat, (long)attendu["dernierInstantMs"], (long)attendu["maintenantMs"]);
                    Comparer($"pas[{i}].secondesCreditees", retour.SecondesCreditees, attendu["secondesCreditees"]);
                    etat = retour.Etat;
                }
                Comparer($"pas[{i}]", Instantane.De(etat), attendu["instant"]);
            }
        }
    }
}
