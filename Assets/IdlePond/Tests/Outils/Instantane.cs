using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Newtonsoft.Json.Linq;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Un état de jeu en arbre JSON, clef pour clef celui du générateur de
    /// références. Les dictionnaires sont énumérés dans l'ORDRE DU REGISTRE, jamais
    /// dans celui du dictionnaire : c'est ce qui rend deux instantanés comparables
    /// à la chaîne près.
    /// </summary>
    public static class Instantane
    {
        static string D(Decimal x) => x.EnMantisseExposant();
        static string Registre(PalierDeVoix p) => p.ToString().ToLowerInvariant();

        public static JObject De(EtatJeu etat, bool avecDerives = true)
        {
            var c = etat.Cycle;
            var p = etat.Permanent;
            var t = etat.Telemetrie;

            var especes = new JObject();
            foreach (var e in Especes.Toutes)
                if (c.Especes.TryGetValue(e.Id, out var v))
                    especes[e.Id] = new JObject { ["debloquee"] = v.Debloquee, ["niveau"] = v.Niveau };

            var succes = new JObject();
            foreach (var s in RegistreDesSucces.Tous)
                if (p.Succes.TryGetValue(s.Id, out var entree))
                    succes[s.Id] = new JObject { ["obtenuAuCycle"] = entree.ObtenuAuCycle, ["registre"] = Registre(entree.Registre) };

            var insufflations = new JObject();
            foreach (var i in Insufflations.Toutes)
                if (p.Insufflations.TryGetValue(i.Id, out var rang))
                    insufflations[i.Id] = rang;

            double Compteur(BrancheTechnique b) => p.CompteursTechnique.TryGetValue(b, out var x) ? x : 0;

            var racine = new JObject
            {
                ["tempsJeuSecondes"] = etat.TempsJeuSecondes,
                ["limiteDeContenu"] = etat.LimiteDeContenu,
                ["prng"] = new JObject { ["graine"] = etat.Prng.Graine },
                ["cycle"] = new JObject
                {
                    ["manaCourant"] = D(c.ManaCourant),
                    ["paliersOuverts"] = c.PaliersOuverts,
                    ["especes"] = especes,
                    ["productionPicParSeconde"] = D(c.ProductionPicParSeconde),
                    ["dureeSecondes"] = c.DureeSecondes,
                    ["acquisDeSejour"] = c.AcquisDeSejour,
                    ["niveauDuHeros"] = c.NiveauDuHeros,
                },
                ["permanent"] = new JObject
                {
                    ["densites"] = new JArray(p.Densites.Select(x => (object)x)),
                    ["souffle"] = D(p.Souffle),
                    ["contenanceMana"] = D(p.ContenanceMana),
                    ["couches"] = new JArray(p.Couches),
                    ["profondeurMaxAtteinte"] = p.ProfondeurMaxAtteinte,
                    ["compteursTechnique"] = new JObject
                    {
                        ["creusement"] = Compteur(BrancheTechnique.Creusement),
                        ["amelioration"] = Compteur(BrancheTechnique.Amelioration),
                        ["recrutement"] = Compteur(BrancheTechnique.Recrutement),
                        ["entretien"] = Compteur(BrancheTechnique.Entretien),
                        ["construction"] = Compteur(BrancheTechnique.Construction),
                        ["renaissance"] = Compteur(BrancheTechnique.Renaissance),
                    },
                    ["noeudsTechnique"] = new JArray(p.NoeudsTechnique),
                    ["succes"] = succes,
                    ["nombreDeRenaissances"] = p.NombreDeRenaissances,
                    ["especesAyantAtteintCent"] = new JArray(p.EspecesAyantAtteintCent),
                    ["manaAmbiant"] = D(p.ManaAmbiant),
                    ["heuresHorsLigneCreditees"] = p.HeuresHorsLigneCreditees,
                    ["insufflations"] = insufflations,
                },
                ["telemetrie"] = new JObject
                {
                    ["cycles"] = new JArray(t.Cycles.Select(m => new JObject
                    {
                        ["index"] = m.Index,
                        ["dureeEcouleeSecondes"] = m.DureeEcouleeSecondes,
                        ["paliersOuverts"] = m.PaliersOuverts,
                        ["productionPicParSeconde"] = D(m.ProductionPicParSeconde),
                        ["souffleGagne"] = D(m.SouffleGagne),
                    })),
                    ["secondesDepuisDernierSucces"] = t.SecondesDepuisDernierSucces,
                    ["intervallesEntreSucces"] = new JArray(t.IntervallesEntreSucces.Select(x => (object)x)),
                },
            };
            if (avecDerives)
            {
                racine["derives"] = new JObject
                {
                    ["production"] = D(Economie.ProductionTotaleParSeconde(etat)),
                    ["contenance"] = D(Economie.Contenance(etat)),
                    ["partDeContenance"] = Economie.PartDeContenance(etat),
                    ["eauTroublee"] = Economie.EauTroublee(etat),
                    ["estBloque"] = Economie.EstBloque(etat),
                    ["gainDeSoufflePrevu"] = D(Renaissance.GainDeSoufflePrevu(etat)),
                };
            }
            return racine;
        }
    }
}
