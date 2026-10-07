using System.IO;
using System.Linq;
using System.Text;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Régénère les références de parité DEPUIS LE C# — `donnees.json`, `parties.json`,
    /// `hors-ligne.json`.
    ///
    /// Jusqu'au 2026-10-07, ces fichiers venaient du TypeScript archivé, et ils gardaient
    /// le portage honnête. L'assise II (le Gour) a changé le contenu et l'économie
    /// volontairement ; l'utilisateur a choisi de basculer la référence en C#. Depuis, la
    /// règle de `PariteTests` vaut pour CES fichiers : une parité rouge se comprend avant de
    /// se régénérer. On ne relance ce générateur que pour un changement voulu, nommé dans le
    /// message de commit et dans `docs/RESULTATS.md`.
    ///
    /// Les scénarios sont ceux des tests, à la lettre ; pour le hors-ligne, les instants
    /// d'entrée sont relus dans le fichier existant, seules les sorties sont recalculées.
    /// </summary>
    public static class GenerateurDeReferences
    {
        static string Dossier => Path.GetFullPath(Path.Combine("Assets", "IdlePond", "Tests", "Reference"));

        [MenuItem("IdlePond/Parité/Régénérer les références (changement voulu seulement)")]
        public static void Regenerer()
        {
            Ecrire("donnees", Donnees());
            Ecrire("parties", Parties());
            Ecrire("hors-ligne", HorsLigneJson());
            AssetDatabase.Refresh();
            Debug.Log("IdlePond : références de parité régénérées depuis le C#.");
        }

        static void Ecrire(string nom, JObject contenu)
        {
            var texte = new StringBuilder();
            using (var ecrivain = new JsonTextWriter(new StringWriter(texte)) { Formatting = Formatting.Indented, Indentation = 1, IndentChar = ' ' })
                contenu.WriteTo(ecrivain);
            File.WriteAllText(Path.Combine(Dossier, nom + ".json"), texte.ToString().Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
        }

        static string Serpent(string pascal) => string.Concat(pascal.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

        static JToken Chaine(string s) => s == null ? JValue.CreateNull() : new JValue(s);

        /* ─── donnees.json ─────────────────────────────────────────────────────────── */

        static JObject Donnees() => new JObject
        {
            ["paliersLivres"] = Assises.PALIERS_LIVRES,
            ["assises"] = new JArray(Assises.Toutes.Select(a => new JObject
            {
                ["id"] = a.Id, ["rang"] = a.Rang, ["typeMana"] = a.TypeMana,
                ["indexPremierPalier"] = a.IndexPremierPalier, ["nombreDePaliers"] = a.NombreDePaliers,
            })),
            ["especes"] = new JArray(Especes.Toutes.Select(e => new JObject
            {
                ["id"] = e.Id, ["assise"] = e.Assise, ["rang"] = e.Rang, ["palier"] = e.Palier,
            })),
            ["paliers"] = new JArray(Paliers.Tous.Select(p => new JObject
            {
                ["index"] = p.Index, ["assise"] = p.Assise, ["espece"] = Chaine(p.Espece),
            })),
            ["insufflations"] = new JArray(Insufflations.Toutes.Select(i => new JObject
            {
                ["id"] = i.Id, ["portee"] = i.Portee == PorteeDInsufflation.Globale ? "globale" : "ciblee", ["espece"] = Chaine(i.Espece),
            })),
            ["succes"] = new JArray(RegistreDesSucces.Tous.Select(Succes)),
        };

        static JObject Succes(Succes s)
        {
            var declencheur = new JObject { ["quoi"] = Serpent(s.Declencheur.Quoi.ToString()) };
            if (s.Declencheur.Seuil != 0) declencheur["seuil"] = s.Declencheur.Seuil;
            if (s.Declencheur.Espece != null) declencheur["espece"] = s.Declencheur.Espece;
            if (s.Declencheur.Palier >= 0) declencheur["palier"] = s.Declencheur.Palier;
            JToken effet = JValue.CreateNull();
            if (s.Effet != null)
            {
                var objet = new JObject { ["genre"] = Serpent(s.Effet.Genre.ToString()) };
                if (s.Effet.Terme.HasValue) objet["terme"] = Termes.Identifiant(s.Effet.Terme.Value);
                if (s.Effet.Part != 0) objet["part"] = s.Effet.Part;
                if (s.Effet.Capacite.HasValue) objet["capacite"] = s.Effet.Capacite.Value.ToString();
                effet = objet;
            }
            return new JObject
            {
                ["id"] = s.Id, ["famille"] = Serpent(s.Famille.ToString()), ["visibilite"] = Serpent(s.Visibilite.ToString()),
                ["assise"] = s.Assise, ["declencheur"] = declencheur, ["effet"] = effet,
            };
        }

        /* ─── parties.json : les scénarios de `PariteTests`, à la lettre ────────────── */

        static JObject Parties()
        {
            var sequence = new JArray();
            var etat = EtatDeTravail.Creer(4242);
            sequence.Add(Instantane.De(etat));
            foreach (var dt in new[] { 0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900 })
            {
                etat = Reducteur.Tick(etat, dt);
                sequence.Add(Instantane.De(etat));
            }

            var joueur = new JArray(new[] { 60.0, 600, 1800, 3600 }.Select(s => Instantane.De(Joueur.Rejoue(s))));

            var renaissances = new JArray();
            etat = EtatDeTravail.Creer(777);
            renaissances.Add(Instantane.De(etat));
            for (var cycle = 0; cycle < 3; cycle++)
            {
                etat = Reducteur.Tick(etat, 3600);
                for (var i = 0; i < 3; i++) etat = Reducteur.Grandir(etat);
                etat = Renaissance.Renaitre(etat);
                renaissances.Add(Instantane.De(etat));
                etat = Reducteur.Insuffler(etat, Insufflations.GLOBALE_ID);
                etat = Reducteur.Insuffler(etat, "insufflation-vairon");
                etat = Reducteur.Creuser(etat);
                etat = Reducteur.Debloquer(etat, "vairon");
                for (var n = 0; n < 20; n++) etat = Reducteur.Ameliorer(etat, "vairon");
                etat = Reducteur.Tick(etat, 600);
                renaissances.Add(Instantane.De(etat));
            }

            var simulation = Simuler(15, null, 7);
            return new JObject
            {
                ["scenarios"] = new JArray(
                    new JObject { ["nom"] = "sequence", ["instants"] = sequence },
                    new JObject { ["nom"] = "joueur", ["instants"] = joueur },
                    new JObject { ["nom"] = "renaissances", ["instants"] = renaissances }),
                ["simulation"] = new JObject
                {
                    ["instant"] = Instantane.De(simulation.Etat),
                    ["cyclesAcheves"] = simulation.CyclesAcheves,
                    ["secondesActives"] = simulation.SecondesActives,
                    ["secondesEcoulees"] = simulation.SecondesEcoulees,
                },
            };
        }

        /* ─── hors-ligne.json : les entrées relues, les sorties recalculées ─────────── */

        static JObject HorsLigneJson()
        {
            var entrees = (JArray)References.Lire("hors-ligne")["pas"];
            var etat = EtatDeTravail.Creer(9001);
            etat = Reducteur.Tick(etat, 600);
            etat = Reducteur.Creuser(etat);
            etat = Reducteur.Debloquer(etat, "vairon");
            for (var n = 0; n < 10; n++) etat = Reducteur.Ameliorer(etat, "vairon");
            etat = Reducteur.Tick(etat, 600);

            var pas = new JArray { new JObject { ["etape"] = "depart", ["instant"] = Instantane.De(etat) } };
            for (var i = 1; i < entrees.Count; i++)
            {
                var entree = entrees[i];
                if ((string)entree["etape"] == "renaissance")
                {
                    etat = Reducteur.Tick(etat, 3600);
                    for (var k = 0; k < 3; k++) etat = Reducteur.Grandir(etat);
                    etat = Renaissance.Renaitre(etat);
                    pas.Add(new JObject { ["etape"] = "renaissance", ["instant"] = Instantane.De(etat) });
                    continue;
                }
                var dernier = (long)entree["dernierInstantMs"];
                var maintenant = (long)entree["maintenantMs"];
                var cap = IdlePond.Jeu.HorsLigne.CapHorsLigneCourantHeures(etat);
                var retour = IdlePond.Jeu.HorsLigne.Crediter(etat, dernier, maintenant);
                etat = retour.Etat;
                pas.Add(new JObject
                {
                    ["etape"] = "absence", ["dernierInstantMs"] = dernier, ["maintenantMs"] = maintenant,
                    ["capHeures"] = cap, ["secondesCreditees"] = retour.SecondesCreditees,
                    ["instant"] = Instantane.De(etat),
                });
            }
            return new JObject { ["pas"] = pas };
        }
    }
}
