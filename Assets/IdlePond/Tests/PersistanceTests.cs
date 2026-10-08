using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Versionnage de save et aller-retour Decimal (§10, §12 jalon v0.1).
    ///
    /// Rétrofiter un versionnage sur des saves existantes coûte un wipe : le champ et la
    /// chaîne de migrations existent dès la v1, même vides. Les tests des migrations
    /// v1 → v7 du TypeScript disparaissent avec elles (spec §3) : la sauvegarde Unity
    /// repart en version 1.
    ///
    /// Les dossiers sont temporaires et jetés à la fin : aucun test ne touche à un
    /// dossier du joueur.
    /// </summary>
    public class PersistanceTests
    {
        static readonly HorlogeFigee Horloge2026 = new HorlogeFigee(1_790_000_000_000L);

        /// Joue `corps` dans un dossier neuf, puis le supprime, quoi qu'il arrive.
        static void DansUnDossier(Action<string> corps)
        {
            var dossier = Path.Combine(Path.GetTempPath(), "idlepond-tests-" + Guid.NewGuid().ToString("N"));
            try { corps(dossier); }
            finally { if (Directory.Exists(dossier)) Directory.Delete(dossier, true); }
        }

        static void AssertEtatsIdentiques(EtatJeu obtenu, EtatJeu attendu) =>
            Comparateur.ComparerATolerance(Instantane.De(obtenu), Instantane.De(attendu), 0);

        /// Un état qui porte un peu de tout : succès, amélioration, renaissances, télémétrie.
        static EtatJeu EtatRiche()
        {
            var etat = EtatDeTravail.Creer();
            var resultat = Reducteur.TickDetaille(etat, 3600);
            etat = RegleDesSucces.EnregistrerIntervalleDeSucces(resultat.Etat, resultat.Declenches);
            etat = Renaissance.Renaitre(etat);
            etat = etat with { Permanent = etat.Permanent with { Souffle = Decimal.Parse("1.2345678901234567e30") } };
            etat = Reducteur.AcheterUneAmelioration(etat, AmeliorationsDeRenaissance.Toutes[0].Id);
            return Reducteur.Tick(etat, 61.5);
        }

        [Test, Description("le réglage n’est PAS persisté : il appartient à la version du jeu, pas à la partie")]
        public void Le_reglage_n_est_PAS_persiste()
        {
            // R41. Un réglage persisté figerait l'ancienne courbe dans les saves
            // existantes au moment même où l'on recalibre — et le jour où la croissance du
            // séjour change de valeur, une partie en cours doit suivre.
            Assert.That(Constantes.REGLAGE_CANONIQUE.CroissanceDuSejourParPalier, Is.GreaterThan(0.0));
            var regle = Reducteur.EtatInitial(1) with { Reglage = new Reglage(1.5) };
            var save = Persistance.Serialiser(regle);
            Assert.That(save.Contenu.ToString(), Does.Not.Contain("croissanceDuSejour"));
            Assert.That(Persistance.Deserialiser(save, Reducteur.EtatInitial(1)).Reglage, Is.EqualTo(Constantes.REGLAGE_CANONIQUE));
        }

        [Test, Description("un Decimal fait l’aller-retour à l’exact")]
        public void Un_Decimal_fait_l_aller_retour_a_l_exact()
        {
            var valeurs = new[]
            {
                new Decimal(0),
                new Decimal(1),
                Decimal.Parse("1.2345678901234567e30"),
                new Decimal(2.4).Pow(61),
                new Decimal(1).Div(3),
            };
            foreach (var valeur in valeurs)
            {
                var retour = Persistance.DeserialiserDecimal(new JValue(Persistance.SerialiserDecimal(valeur)), new Decimal(-1));
                Assert.That(retour.Eq(valeur), Is.True, $"{valeur} n'est pas revenu identique");
            }
        }

        [Test, Description("un Decimal illisible retombe sur le repli plutôt que sur NaN")]
        public void Un_Decimal_illisible_retombe_sur_le_repli_plutot_que_sur_NaN()
        {
            Assert.That(Persistance.DeserialiserDecimal(null, new Decimal(7)).Eq(7), Is.True);
            Assert.That(Persistance.DeserialiserDecimal(new JObject(), new Decimal(7)).Eq(7), Is.True);
            Assert.That(Persistance.DeserialiserDecimal(new JValue("pas un nombre"), new Decimal(7)).Eq(7), Is.True);
            Assert.That(Persistance.DeserialiserDecimal(new JValue("NaN"), new Decimal(7)).Eq(7), Is.True);
        }

        [Test, Description("un état complet survit à un aller-retour par JSON")]
        public void Un_etat_complet_survit_a_un_aller_retour_par_JSON()
        {
            var depart = EtatRiche();
            Assert.That(depart.Permanent.Succes, Is.Not.Empty, "l'état d'essai doit porter des succès");
            Assert.That(depart.Permanent.AmeliorationsDeRenaissance, Is.Not.Empty);
            Assert.That(depart.Telemetrie.Cycles, Is.Not.Empty);

            var texte = Persistance.ComposerLeFichier(Persistance.Serialiser(depart), 42).ToString();
            var racine = JObject.Parse(texte);
            var retour = Persistance.Deserialiser(new SaveSerialisee((int)racine["versionSave"], (JObject)racine["contenu"]), Reducteur.EtatInitial(0));
            AssertEtatsIdentiques(retour, depart);
            Assert.That(retour.Prng, Is.EqualTo(depart.Prng));
        }

        [Test, Description("un état complet survit à un aller-retour par le fichier")]
        public void Un_etat_complet_survit_a_un_aller_retour_par_le_fichier()
        {
            DansUnDossier(dossier =>
            {
                var depart = EtatRiche();
                Persistance.Sauvegarder(dossier, depart, 123_456_789_012L);
                var charge = Persistance.Charger(dossier, Horloge2026);
                Assert.That(charge.NouvellePartie, Is.False);
                Assert.That(charge.FichierMisDeCote, Is.Null);
                AssertEtatsIdentiques(charge.Etat, depart);
            });
        }

        [Test, Description("les doubles font l’aller-retour au bit près, sous n’importe quelle culture")]
        public void Les_doubles_font_l_aller_retour_au_bit_pres()
        {
            // Persistés en chaînes : un nombre JSON passerait par le `double.Parse` du runtime,
            // pas toujours correctement arrondi sur le Mono d'Unity.
            var etat = Reducteur.EtatInitial(1);
            var tordu = etat with
            {
                TempsJeuSecondes = 0.1 + 0.2,
                Cycle = etat.Cycle with { AcquisDeSejour = 1.0 / 3.0, DureeSecondes = Math.PI * 1e7 },
            };
            var anterieure = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
                var save = Persistance.Serialiser(tordu);
                var texte = save.Contenu.ToString();
                var retour = Persistance.Deserialiser(new SaveSerialisee(1, JObject.Parse(texte)), Reducteur.EtatInitial(9));
                Assert.That(retour.TempsJeuSecondes.Equals(tordu.TempsJeuSecondes), Is.True);
                Assert.That(retour.Cycle.AcquisDeSejour.Equals(tordu.Cycle.AcquisDeSejour), Is.True);
                Assert.That(retour.Cycle.DureeSecondes.Equals(tordu.Cycle.DureeSecondes), Is.True);
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = anterieure; }
        }

        [Test, Description("la save porte une version et la chaîne de migrations existe")]
        public void La_save_porte_une_version_et_la_chaine_de_migrations_existe()
        {
            var save = Persistance.Serialiser(Reducteur.EtatInitial(1));
            Assert.That(save.VersionSave, Is.EqualTo(Constantes.VERSION_SAVE));
            Assert.That(Constantes.VERSION_SAVE, Is.EqualTo(2));
            Assert.That(Persistance.MIGRATIONS.Keys, Is.EquivalentTo(new[] { 1 }));
            Assert.That(() => { Persistance.Migrer(save); }, Throws.Nothing);
        }

        [Test, Description("une save v1 retrouve ses insufflations en améliorations de renaissance, rangs intacts")]
        public void Une_save_v1_retrouve_ses_insufflations_en_ameliorations_de_renaissance()
        {
            var riche = Reducteur.EtatInitial(0) with
            {
                Permanent = Reducteur.EtatInitial(0).Permanent with
                {
                    AmeliorationsDeRenaissance = new Dictionary<string, int>
                    {
                        [AmeliorationsDeRenaissance.GLOBALE_ID] = 3,
                        [AmeliorationsDeRenaissance.CibleeDe("vairon").Id] = 2,
                    },
                },
            };
            // Une save v1 : la même, écrite avec l'ancien vocabulaire.
            var contenu = Persistance.Serialiser(riche).Contenu;
            var permanent = (JObject)contenu["permanent"];
            var v1 = new JObject();
            foreach (var paire in ((JObject)permanent["ameliorations"]).Properties())
                v1[Persistance.PREFIXE_V1_DES_AMELIORATIONS + paire.Name.Substring("amelioration-".Length)] = paire.Value;
            permanent.Remove("ameliorations");
            permanent[Persistance.CLEF_V1_DES_AMELIORATIONS] = v1;

            var retour = Persistance.Deserialiser(new SaveSerialisee(1, contenu), Reducteur.EtatInitial(0));

            Assert.That(retour.Permanent.AmeliorationsDeRenaissance, Is.EquivalentTo(riche.Permanent.AmeliorationsDeRenaissance));
            Assert.That(retour.VersionSave, Is.EqualTo(Constantes.VERSION_SAVE));
        }

        [Test, Description("une save d’une version inconnue refuse de se charger en silence")]
        public void Une_save_d_une_version_inconnue_refuse_de_se_charger_en_silence()
        {
            // Une version SANS migration déclarée. Un trou dans la chaîne doit crier, pas
            // charger à moitié : rétrofiter un versionnage coûte un wipe, mais charger une
            // save qu'on ne sait pas lire en coûte un aussi.
            var save = new SaveSerialisee(-1, new JObject());
            var ex = Assert.Throws<InvalidOperationException>(() => Persistance.Migrer(save));
            Assert.That(ex.Message, Does.Match("Migration de save manquante"));
        }

        [Test, Description("une save plus récente que le jeu refuse de se charger à moitié")]
        public void Une_save_plus_recente_que_le_jeu_refuse_de_se_charger_a_moitie()
        {
            var save = new SaveSerialisee(Constantes.VERSION_SAVE + 1, new JObject());
            Assert.Throws<InvalidOperationException>(() => Persistance.Migrer(save));
        }

        [Test, Description("un champ inconnu est ignoré")]
        public void Un_champ_inconnu_est_ignore()
        {
            var depart = EtatRiche();
            var racine = Persistance.ComposerLeFichier(Persistance.Serialiser(depart), 1);
            racine["unChampDeLAvenir"] = "peu importe";
            var contenu = (JObject)racine["contenu"];
            contenu["cycle"]["unAutreChamp"] = 12;
            contenu["permanent"]["especes-fantomes"] = new JArray("x");
            contenu["permanent"]["succes"]["succes-qui-n-existe-pas"] = new JObject { ["obtenuAuCycle"] = 0, ["registre"] = "pente" };
            contenu["reglage"] = new JObject { ["croissanceDuSejourParPalier"] = 9 };

            var retour = Persistance.Deserialiser(new SaveSerialisee(1, contenu), Reducteur.EtatInitial(0));
            AssertEtatsIdentiques(retour, depart);
            Assert.That(retour.Permanent.Succes.ContainsKey("succes-qui-n-existe-pas"), Is.False);
        }

        [Test, Description("un champ absent est remis à sa valeur par défaut")]
        public void Un_champ_absent_est_remis_a_sa_valeur_par_defaut()
        {
            var repli = Reducteur.EtatInitial(77, Assises.PALIERS_LIVRES);
            var contenu = Persistance.Serialiser(EtatRiche()).Contenu;
            ((JObject)contenu["cycle"]).Remove("niveauDuHeros");
            ((JObject)contenu["permanent"]).Remove("souffle");
            ((JObject)contenu["permanent"]).Remove("ameliorations");
            contenu.Remove("telemetrie");
            contenu.Remove("prng");

            var retour = Persistance.Deserialiser(new SaveSerialisee(1, contenu), repli);
            Assert.That(retour.Cycle.NiveauDuHeros, Is.EqualTo(repli.Cycle.NiveauDuHeros));
            Assert.That(retour.Permanent.Souffle.Eq(repli.Permanent.Souffle), Is.True);
            Assert.That(retour.Permanent.AmeliorationsDeRenaissance, Is.Empty);
            Assert.That(retour.Telemetrie.Cycles, Is.Empty);
            Assert.That(retour.Prng, Is.EqualTo(repli.Prng));
            // Ce que la save portait reste lu : un champ absent n'en emporte pas d'autres.
            Assert.That(retour.Permanent.NombreDeRenaissances, Is.EqualTo(1));
            Assert.That(retour.Permanent.Succes, Is.Not.Empty);
        }

        [Test, Description("une save vide se charge sur la partie neuve")]
        public void Une_save_vide_se_charge_sur_la_partie_neuve()
        {
            var repli = Reducteur.EtatInitial(5, Assises.PALIERS_LIVRES);
            var retour = Persistance.Deserialiser(new SaveSerialisee(1, new JObject()), repli);
            AssertEtatsIdentiques(retour, repli);
        }

        [Test, Description("sans fichier, le chargement rend une nouvelle partie")]
        public void Sans_fichier_le_chargement_rend_une_nouvelle_partie()
        {
            DansUnDossier(dossier =>
            {
                var charge = Persistance.Charger(dossier, Horloge2026);
                Assert.That(charge.NouvellePartie, Is.True);
                Assert.That(charge.FichierMisDeCote, Is.Null);
                Assert.That(charge.DernierInstantMs, Is.EqualTo(Horloge2026.MaintenantMs()));
                Assert.That(charge.Etat.LimiteDeContenu, Is.EqualTo(Assises.PALIERS_LIVRES));
                Assert.That((long)charge.Etat.Prng.Graine, Is.EqualTo(Horloge.GrainePourNouvellePartie(Horloge2026)));
            });
        }

        [Test, Description("dernierInstantMs est écrit à côté de l’état et relu")]
        public void DernierInstantMs_est_ecrit_a_cote_de_l_etat_et_relu()
        {
            DansUnDossier(dossier =>
            {
                Persistance.Sauvegarder(dossier, EtatRiche(), 1_234_567_890_123L);
                var racine = JObject.Parse(File.ReadAllText(Persistance.CheminDeLaSauvegarde(dossier)));
                Assert.That((long)racine["dernierInstantMs"], Is.EqualTo(1_234_567_890_123L));
                Assert.That((int)racine["versionSave"], Is.EqualTo(Constantes.VERSION_SAVE));
                Assert.That(Persistance.Charger(dossier, Horloge2026).DernierInstantMs, Is.EqualTo(1_234_567_890_123L));
            });
        }

        [Test, Description("une save illisible démarre une nouvelle partie et le fichier est mis de côté")]
        public void Une_save_illisible_demarre_une_nouvelle_partie_et_le_fichier_est_mis_de_cote()
        {
            var illisibles = new[]
            {
                "{ ceci n'est pas du JSON",
                "",
                "[1, 2, 3]",
                "{ \"contenu\": {} }",
                "{ \"versionSave\": 99, \"contenu\": {} }",
                "{ \"versionSave\": -4, \"contenu\": {} }",
                "{ \"versionSave\": 1, \"contenu\": [] }",
            };
            foreach (var texte in illisibles)
            {
                DansUnDossier(dossier =>
                {
                    Directory.CreateDirectory(dossier);
                    var chemin = Persistance.CheminDeLaSauvegarde(dossier);
                    File.WriteAllText(chemin, texte);

                    var charge = Persistance.Charger(dossier, Horloge2026);

                    Assert.That(charge.NouvellePartie, Is.True, texte);
                    Assert.That(charge.Etat.Permanent.NombreDeRenaissances, Is.EqualTo(0), texte);
                    Assert.That(File.Exists(chemin), Is.False, "le fichier fautif ne doit plus être à sa place : " + texte);
                    Assert.That(charge.FichierMisDeCote, Is.Not.Null, texte);
                    Assert.That(Path.GetFileName(charge.FichierMisDeCote), Does.Match(@"^idlepond\.corrompue-\d{8}-\d{9}\.json$"), texte);
                    Assert.That(File.ReadAllText(charge.FichierMisDeCote), Is.EqualTo(texte), "le contenu est conservé, pas écrasé");
                });
            }
        }

        [Test, Description("deux saves illisibles ne s’écrasent pas entre elles")]
        public void Deux_saves_illisibles_ne_s_ecrasent_pas_entre_elles()
        {
            DansUnDossier(dossier =>
            {
                Directory.CreateDirectory(dossier);
                var chemin = Persistance.CheminDeLaSauvegarde(dossier);
                File.WriteAllText(chemin, "premiere");
                var premiere = Persistance.Charger(dossier, Horloge2026).FichierMisDeCote;
                File.WriteAllText(chemin, "seconde");
                var seconde = Persistance.Charger(dossier, Horloge2026).FichierMisDeCote;

                Assert.That(seconde, Is.Not.EqualTo(premiere));
                Assert.That(File.ReadAllText(premiere), Is.EqualTo("premiere"));
                Assert.That(File.ReadAllText(seconde), Is.EqualTo("seconde"));
            });
        }

        [Test, Description("l’écriture passe par un fichier temporaire, puis un remplacement")]
        public void L_ecriture_passe_par_un_fichier_temporaire_puis_un_remplacement()
        {
            DansUnDossier(dossier =>
            {
                var chemin = Persistance.CheminDeLaSauvegarde(dossier);
                var premier = EtatRiche();
                Persistance.Sauvegarder(dossier, premier, 1);
                Assert.That(Directory.GetFiles(dossier).Select(Path.GetFileName), Is.EqualTo(new[] { Persistance.NOM_DU_FICHIER }),
                    "aucun fichier temporaire ne reste après l'écriture");

                // Une coupure en pleine écriture précédente a laissé un `.tmp` bancal : il ne
                // doit ni être lu à la place de la sauvegarde, ni empêcher la suivante.
                File.WriteAllText(chemin + ".tmp", "{ tronqué");
                var charge = Persistance.Charger(dossier, Horloge2026);
                Assert.That(charge.NouvellePartie, Is.False);
                AssertEtatsIdentiques(charge.Etat, premier);

                var second = Reducteur.Tick(premier, 10);
                Persistance.Sauvegarder(dossier, second, 2);
                AssertEtatsIdentiques(Persistance.Charger(dossier, Horloge2026).Etat, second);
                Assert.That(Directory.GetFiles(dossier).Select(Path.GetFileName), Is.EqualTo(new[] { Persistance.NOM_DU_FICHIER }));
            });
        }
    }
}
