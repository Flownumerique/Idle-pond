using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// La partie : l'équivalent du magasin Zustand. Elle ne porte aucune règle — chaque
    /// acte est un appel au réducteur du noyau, et c'est ce que ces tests vérifient : le
    /// même état que le réducteur appelé à la main, puis les événements qui l'annoncent.
    /// </summary>
    public class PartieTests
    {
        const long H = 3600_000;
        const long DEPART = 1_790_000_000_000L;

        sealed class Ecoute
        {
            public readonly List<EtatJeu> Changements = new List<EtatJeu>();
            public readonly List<IReadOnlyList<string>> Succes = new List<IReadOnlyList<string>>();
            public readonly List<AbsenceCreditee> Retours = new List<AbsenceCreditee>();

            public Ecoute(Partie partie)
            {
                partie.EtatChange += etat => Changements.Add(etat);
                partie.SuccesDeclenches += declenches => Succes.Add(declenches);
                partie.RetourAffiche += retour => Retours.Add(retour);
            }
        }

        /// Un état qui peut tout acheter : du mana et de la place pour le garder.
        static EtatJeu EtatAise()
        {
            var etat = Partie.NouvelEtat(new HorlogeFigee(DEPART));
            return etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e12") },
                Permanent = etat.Permanent with
                {
                    ContenanceMana = Decimal.Parse("1e14"),
                    Souffle = Decimal.Parse("1e12"),
                },
            };
        }

        /// Le même, sans un sou : tout achat y est refusé.
        static EtatJeu EtatPauvre()
        {
            var etat = Partie.NouvelEtat(new HorlogeFigee(DEPART));
            return etat with { Cycle = etat.Cycle with { ManaCourant = Decimal.Zero } };
        }

        static void AssertEtatsIdentiques(EtatJeu obtenu, EtatJeu attendu) =>
            Comparateur.ComparerATolerance(Instantane.De(obtenu), Instantane.De(attendu), 0);

        static void DansUnDossier(Action<string> corps)
        {
            var dossier = Path.Combine(Path.GetTempPath(), "idlepond-tests-" + Guid.NewGuid().ToString("N"));
            try { corps(dossier); }
            finally { if (Directory.Exists(dossier)) Directory.Delete(dossier, true); }
        }

        [Test, Description("une partie neuve part de ce que le jeu livre, avec la graine de l’horloge")]
        public void Une_partie_neuve_part_de_ce_que_le_jeu_livre()
        {
            var partie = new Partie(new HorlogeFigee(DEPART));
            Assert.That(partie.Etat.LimiteDeContenu, Is.EqualTo(Assises.PALIERS_LIVRES));
            Assert.That(partie.Etat.VersionSave, Is.EqualTo(Constantes.VERSION_SAVE));
            Assert.That((long)partie.Etat.Prng.Graine, Is.EqualTo(DEPART & 0xFFFFFFFF));
            Assert.That(partie.DernierInstantMs, Is.EqualTo(DEPART));
            Assert.That(partie.Retour, Is.Null);
            Assert.That(partie.AAnnoncer, Is.Empty);
        }

        [Test, Description("creuser applique le réducteur du noyau et annonce le nouvel état")]
        public void Creuser_applique_le_reducteur_du_noyau_et_annonce_le_nouvel_etat()
        {
            var depart = EtatAise();
            var partie = new Partie(new HorlogeFigee(DEPART), depart);
            var ecoute = new Ecoute(partie);
            partie.Creuser();
            Assert.That(partie.Etat.Cycle.PaliersOuverts, Is.EqualTo(depart.Cycle.PaliersOuverts + 1));
            AssertEtatsIdentiques(partie.Etat, Reducteur.Creuser(depart));
            Assert.That(ecoute.Changements, Has.Count.EqualTo(1));
            Assert.That(ecoute.Changements[0], Is.SameAs(partie.Etat));
        }

        [Test, Description("convaincre, monter, grandir, insuffler et renaître sont les actes du noyau")]
        public void Convaincre_monter_grandir_insuffler_et_renaitre_sont_les_actes_du_noyau()
        {
            var depart = EtatAise();
            var partie = new Partie(new HorlogeFigee(DEPART), depart);
            var ecoute = new Ecoute(partie);
            var espece = Especes.Toutes.First(e => e.Palier < depart.Cycle.PaliersOuverts).Id;
            var insufflation = Insufflations.Toutes[0].Id;

            var attendu = Reducteur.Debloquer(depart, espece);
            partie.Convaincre(espece);
            AssertEtatsIdentiques(partie.Etat, attendu);
            Assert.That(partie.Etat.Cycle.Especes[espece].Debloquee, Is.True);

            attendu = Reducteur.Ameliorer(attendu, espece);
            partie.Monter(espece);
            AssertEtatsIdentiques(partie.Etat, attendu);
            Assert.That(partie.Etat.Cycle.Especes[espece].Niveau, Is.EqualTo(2));

            attendu = Reducteur.Grandir(attendu);
            partie.Grandir();
            AssertEtatsIdentiques(partie.Etat, attendu);
            Assert.That(partie.Etat.Cycle.NiveauDuHeros, Is.EqualTo(depart.Cycle.NiveauDuHeros + 1));

            attendu = Reducteur.Insuffler(attendu, insufflation);
            partie.Insuffler(insufflation);
            AssertEtatsIdentiques(partie.Etat, attendu);
            Assert.That(partie.Etat.Permanent.Insufflations[insufflation], Is.EqualTo(1));

            attendu = Renaissance.Renaitre(attendu);
            partie.Renaitre();
            AssertEtatsIdentiques(partie.Etat, attendu);
            Assert.That(partie.Etat.Permanent.NombreDeRenaissances, Is.EqualTo(1));

            Assert.That(ecoute.Changements, Has.Count.EqualTo(5), "un événement par acte accepté");
        }

        [Test, Description("un acte refusé rend le même état, sans exception et sans événement")]
        public void Un_acte_refuse_rend_le_meme_etat_sans_exception_et_sans_evenement()
        {
            var pauvre = EtatPauvre();
            var partie = new Partie(new HorlogeFigee(DEPART), pauvre);
            var ecoute = new Ecoute(partie);
            var espece = Especes.Toutes[0].Id;

            Assert.That(() =>
            {
                partie.Creuser();
                partie.Convaincre(espece);
                partie.Convaincre("espece-inconnue");
                partie.Monter(espece);
                partie.Monter("espece-inconnue");
                partie.Grandir();
                partie.Insuffler(Insufflations.Toutes[0].Id);
                partie.Insuffler("insufflation-inconnue");
            }, Throws.Nothing);

            Assert.That(partie.Etat, Is.SameAs(pauvre));
            Assert.That(ecoute.Changements, Is.Empty);
        }

        [Test, Description("avancer, c’est le tick du noyau plus l’intervalle entre deux succès")]
        public void Avancer_c_est_le_tick_du_noyau_plus_l_intervalle_entre_deux_succes()
        {
            var depart = EtatDeTravail.Creer();
            var partie = new Partie(new HorlogeFigee(DEPART), depart);
            var ecoute = new Ecoute(partie);

            var tick = Reducteur.TickDetaille(depart, 0.1);
            Assert.That(tick.Declenches, Is.Not.Empty, "l'état d'essai doit faire tomber des succès au premier pas");
            partie.Avancer(0.1);

            AssertEtatsIdentiques(partie.Etat, RegleDesSucces.EnregistrerIntervalleDeSucces(tick.Etat, tick.Declenches));
            Assert.That(partie.Etat.Telemetrie.IntervallesEntreSucces, Has.Count.EqualTo(1));
            Assert.That(partie.Etat.Telemetrie.SecondesDepuisDernierSucces, Is.EqualTo(0.0));
            Assert.That(ecoute.Changements, Has.Count.EqualTo(1));
            Assert.That(ecoute.Succes, Has.Count.EqualTo(1));
            Assert.That(ecoute.Succes[0], Is.EqualTo(tick.Declenches));
            Assert.That(partie.AAnnoncer, Is.EqualTo(tick.Declenches));

            partie.OublierAnnonce(tick.Declenches[0]);
            Assert.That(partie.AAnnoncer, Does.Not.Contain(tick.Declenches[0]));
            Assert.That(partie.AAnnoncer, Has.Count.EqualTo(tick.Declenches.Count - 1));
        }

        [Test, Description("un pas sans succès ne déclenche que le changement d’état")]
        public void Un_pas_sans_succes_ne_declenche_que_le_changement_d_etat()
        {
            var partie = new Partie(new HorlogeFigee(DEPART), EtatPauvre());
            var ecoute = new Ecoute(partie);
            partie.Avancer(0.1);
            Assert.That(ecoute.Changements, Has.Count.EqualTo(1));
            Assert.That(ecoute.Succes, Is.Empty);
            Assert.That(partie.AAnnoncer, Is.Empty);
            Assert.That(partie.Etat.TempsJeuSecondes, Is.EqualTo(0.1).Within(1e-12));
        }

        [Test, Description("un dt non positif ou NaN est ignoré, jamais rattrapé à l’envers")]
        public void Un_dt_non_positif_ou_NaN_est_ignore()
        {
            var depart = EtatDeTravail.Creer();
            var partie = new Partie(new HorlogeFigee(DEPART), depart);
            var ecoute = new Ecoute(partie);
            foreach (var dt in new[] { 0.0, -1.0, double.NaN, double.NegativeInfinity })
                partie.Avancer(dt);
            Assert.That(partie.Etat, Is.SameAs(depart));
            Assert.That(ecoute.Changements, Is.Empty);
        }

        [Test, Description("reprendre crédite l’absence en un pas et annonce le retour")]
        public void Reprendre_credite_l_absence_en_un_pas_et_annonce_le_retour()
        {
            var depart = EtatDeTravail.Creer();
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, depart);
            var ecoute = new Ecoute(partie);

            horloge.AvancerMs(3 * H);
            partie.Reprendre();

            var attendu = HorsLigne.Crediter(depart, DEPART, DEPART + 3 * H);
            AssertEtatsIdentiques(partie.Etat, attendu.Etat);
            Assert.That(partie.DernierInstantMs, Is.EqualTo(DEPART + 3 * H));
            Assert.That(partie.Retour, Is.EqualTo(new AbsenceCreditee(3 * 3600.0)));
            Assert.That(ecoute.Retours, Is.EqualTo(new[] { new AbsenceCreditee(3 * 3600.0) }));
            Assert.That(ecoute.Changements, Has.Count.EqualTo(1));

            partie.OublierRetour();
            Assert.That(partie.Retour, Is.Null);
        }

        [Test, Description("une absence trop brève est créditée mais pas annoncée")]
        public void Une_absence_trop_breve_est_creditee_mais_pas_annoncee()
        {
            // Recharger la page n'est pas revenir de quelque part, et « la mare a tourné
            // sans toi pendant 0 s » n'apprend rien à personne.
            var depart = EtatDeTravail.Creer();
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, depart);
            var ecoute = new Ecoute(partie);

            horloge.AvancerMs(30_000);
            partie.Reprendre();

            Assert.That(partie.Etat.TempsJeuSecondes, Is.EqualTo(depart.TempsJeuSecondes + 30).Within(1e-9));
            Assert.That(partie.Retour, Is.Null);
            Assert.That(ecoute.Retours, Is.Empty);
        }

        [Test, Description("un recul d’horloge ne crédite rien, ne retire rien et ne réécrit pas le passé")]
        public void Un_recul_d_horloge_ne_credite_rien_ne_retire_rien()
        {
            var depart = EtatDeTravail.Creer();
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, depart);
            var ecoute = new Ecoute(partie);

            horloge.AvancerMs(-5 * H);
            partie.Reprendre();

            Assert.That(partie.Etat, Is.SameAs(depart));
            Assert.That(partie.Retour, Is.Null);
            Assert.That(ecoute.Changements, Is.Empty);
            Assert.That(ecoute.Retours, Is.Empty);
        }

        [Test, Description("remplacer pose l’état d’un bloc et prend l’instant présent")]
        public void Remplacer_pose_l_etat_d_un_bloc_et_prend_l_instant_present()
        {
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge);
            var ecoute = new Ecoute(partie);
            var autre = EtatDeTravail.Creer();
            horloge.AvancerMs(1234);
            partie.Remplacer(autre);
            Assert.That(partie.Etat, Is.SameAs(autre));
            Assert.That(partie.DernierInstantMs, Is.EqualTo(DEPART + 1234));
            Assert.That(ecoute.Changements, Has.Count.EqualTo(1));
        }

        [Test, Description("sauvegarder puis rouvrir rend la même partie, et l’absence est créditée une fois")]
        public void Sauvegarder_puis_rouvrir_rend_la_meme_partie_et_l_absence_est_creditee_une_fois()
        {
            DansUnDossier(dossier =>
            {
                var horloge = new HorlogeFigee(DEPART);
                var partie = new Partie(horloge, EtatDeTravail.Creer());
                // Une heure de jeu, puis on quitte : la sauvegarde prend l'instant de sortie.
                horloge.AvancerMs(H);
                partie.Avancer(3600);
                partie.Sauvegarder(dossier);
                Assert.That(partie.DernierInstantMs, Is.EqualTo(DEPART + H));

                // Deux heures plus tard : seules ces deux heures sont une absence. Sans
                // l'instant de sortie, l'heure jouée serait créditée une seconde fois.
                horloge.AvancerMs(2 * H);
                var rouverte = Partie.Ouvrir(horloge, dossier);
                var attendu = HorsLigne.Crediter(partie.Etat, DEPART + H, DEPART + 3 * H);
                AssertEtatsIdentiques(rouverte.Etat, attendu.Etat);
                Assert.That(rouverte.Retour, Is.EqualTo(new AbsenceCreditee(2 * 3600.0)));
                Assert.That(rouverte.DernierInstantMs, Is.EqualTo(DEPART + 3 * H));
            });
        }

        [Test, Description("ouvrir sans sauvegarde démarre une partie neuve, sans retour")]
        public void Ouvrir_sans_sauvegarde_demarre_une_partie_neuve_sans_retour()
        {
            DansUnDossier(dossier =>
            {
                var partie = Partie.Ouvrir(new HorlogeFigee(DEPART), dossier);
                AssertEtatsIdentiques(partie.Etat, Partie.NouvelEtat(new HorlogeFigee(DEPART)));
                Assert.That(partie.Retour, Is.Null);
            });
        }

        [Test, Description("ouvrir une sauvegarde illisible démarre une partie neuve et garde le fichier")]
        public void Ouvrir_une_sauvegarde_illisible_demarre_une_partie_neuve_et_garde_le_fichier()
        {
            DansUnDossier(dossier =>
            {
                Directory.CreateDirectory(dossier);
                File.WriteAllText(Persistance.CheminDeLaSauvegarde(dossier), "{ tronqué");
                var partie = Partie.Ouvrir(new HorlogeFigee(DEPART), dossier);
                Assert.That(partie.Etat.Permanent.NombreDeRenaissances, Is.EqualTo(0));
                Assert.That(Directory.GetFiles(dossier, "idlepond.corrompue-*.json").Length, Is.EqualTo(1));
            });
        }

        [Test, Description("les services portent la partie d’une scène à l’autre")]
        public void Les_services_portent_la_partie_d_une_scene_a_l_autre()
        {
            ServicesDePartie.Oublier();
            try
            {
                Assert.That(ServicesDePartie.EstInstallee, Is.False);
                Assert.Throws<InvalidOperationException>(() => { var _ = ServicesDePartie.Partie; });

                var amorce = new Partie(new HorlogeFigee(DEPART));
                ServicesDePartie.Installer(amorce);
                Assert.That(ServicesDePartie.Partie, Is.SameAs(amorce));
                // Mare.unity lancée après l'Amorce reprend la partie, elle n'en crée pas une autre.
                Assert.That(ServicesDePartie.ObtenirOuCreer(() => new Partie(new HorlogeFigee(0))), Is.SameAs(amorce));

                ServicesDePartie.Oublier();
                var seule = ServicesDePartie.ObtenirOuCreer(() => new Partie(new HorlogeFigee(DEPART)));
                Assert.That(ServicesDePartie.Partie, Is.SameAs(seule));
            }
            finally { ServicesDePartie.Oublier(); }
        }

        [Test, Description("le collecteur silencieux accepte un relevé sans rien en faire")]
        public void Le_collecteur_silencieux_accepte_un_releve_sans_rien_en_faire()
        {
            ICollecteur collecteur = CollecteurSilencieux.Instance;
            var releve = Telemetrie.Relever(EtatDeTravail.Creer());
            Assert.That(() => { collecteur.Publier(releve); }, Throws.Nothing);
        }
    }
}
