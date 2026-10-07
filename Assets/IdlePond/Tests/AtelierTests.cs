using System.Linq;
using IdlePond.Atelier;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// Les ateliers (spec du 2026-10-07) : chaque état d'essai rend ce qu'il annonce, et la
    /// sauvegarde du joueur n'est jamais la cible d'un atelier.
    public class AtelierTests
    {
        const long DEPART_MS = 1_700_000_000_000L;

        static EtatJeu Neuf() => Partie.NouvelEtat(new HorlogeFigee(DEPART_MS));

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        [Test]
        public void La_redirection_de_la_sauvegarde_est_oubliee_avec_la_partie()
        {
            ServicesDePartie.RedirigerLaSauvegarde("ailleurs");
            Assert.That(ServicesDePartie.DossierDeSauvegardeRedirige, Is.EqualTo("ailleurs"));
            ServicesDePartie.Oublier();
            Assert.That(ServicesDePartie.DossierDeSauvegardeRedirige, Is.Null);
        }

        [Test]
        public void Aucun_atelier_n_entre_dans_un_build()
        {
            var fautes = UnityEditor.EditorBuildSettings.scenes.Where(s => s.path.Contains("/Ateliers/")).Select(s => s.path);
            Assert.That(fautes, Is.Empty);
        }

        [Test]
        public void L_horloge_decalee_avance_de_son_decalage()
        {
            var horloge = new HorlogeDecalee(new HorlogeFigee(DEPART_MS));
            horloge.Decaler(8 * 3_600_000L);
            Assert.That(horloge.MaintenantMs(), Is.EqualTo(DEPART_MS + 8 * 3_600_000L));
        }

        [Test]
        public void La_Noue_ouvre_les_paliers_livres_et_porte_la_marque_de_l_assise()
        {
            var e = EtatsDEssai.Noue(Neuf());
            Assert.That(e.Cycle.PaliersOuverts, Is.EqualTo(Assises.PALIERS_LIVRES));
            Assert.That(e.Permanent.Couches, Does.Contain(Assises.Toutes[0].Id));
        }

        [TestCase(1, 0)]
        [TestCase(4, 1)]
        [TestCase(16, 2)]
        [TestCase(256, 3)]
        public void Le_niveau_du_heros_donne_son_stade(int niveau, int stade)
        {
            var e = EtatsDEssai.AvecNiveauDuHeros(Neuf(), niveau);
            Assert.That(Gabarits.StadeDuNiveau(e.Cycle.NiveauDuHeros), Is.EqualTo(stade));
        }

        [Test]
        public void Le_niveau_du_heros_ne_descend_pas_sous_1()
        {
            Assert.That(EtatsDEssai.AvecNiveauDuHeros(Neuf(), -5).Cycle.NiveauDuHeros, Is.EqualTo(1));
        }

        [TestCase(1, 4)]
        [TestCase(5, 16)]
        [TestCase(16, 256)]
        public void La_mue_mene_au_seuil_de_stade_suivant(int niveau, int attendu)
        {
            Assert.That(EtatsDEssai.NiveauDuStadeSuivant(niveau), Is.EqualTo(attendu));
        }

        [Test]
        public void Apres_le_dernier_stade_la_mue_ne_change_rien()
        {
            Assert.That(EtatsDEssai.NiveauDuStadeSuivant(300), Is.EqualTo(300));
        }

        [Test]
        public void Toutes_les_especes_de_l_assise_I_sont_debloquees()
        {
            var e = EtatsDEssai.AvecToutesLesEspeces(Neuf(), 10);
            var attendues = EtatsDEssai.EspecesLivrees.Select(s => s.Id).ToList();
            Assert.That(attendues, Is.Not.Empty);
            foreach (var id in attendues)
            {
                Assert.That(e.Cycle.Especes[id].Debloquee, Is.True, id);
                Assert.That(e.Cycle.Especes[id].Niveau, Is.EqualTo(10), id);
            }
        }

        [Test]
        public void Une_espece_reverrouillee_disparait_de_l_etat()
        {
            var id = EtatsDEssai.EspecesLivrees[0].Id;
            var e = EtatsDEssai.AvecEspece(EtatsDEssai.AvecEspece(Neuf(), id, 5), id, 0);
            Assert.That(e.Cycle.Especes.ContainsKey(id), Is.False);
        }

        [Test]
        public void La_part_de_contenance_trouble_l_eau_au_dela_du_seuil()
        {
            var e = EtatsDEssai.Noue(Neuf());
            Assert.That(Economie.EauTroublee(EtatsDEssai.AvecPartDeContenance(e, 0.95)), Is.True);
            Assert.That(Economie.EauTroublee(EtatsDEssai.AvecPartDeContenance(e, 0.1)), Is.False);
        }

        [Test]
        public void Les_paliers_ouverts_restent_dans_le_contenu_livre()
        {
            var e = Neuf();
            Assert.That(EtatsDEssai.AvecPaliersOuverts(e, 999).Cycle.PaliersOuverts, Is.EqualTo(e.LimiteDeContenu));
            Assert.That(EtatsDEssai.AvecPaliersOuverts(e, 0).Cycle.PaliersOuverts, Is.EqualTo(1));
        }

        [Test]
        public void Le_Souffle_s_ajoute()
        {
            var e = Neuf();
            var apres = EtatsDEssai.AvecSouffleEnPlus(e, 100);
            Assert.That(apres.Permanent.Souffle.Sub(e.Permanent.Souffle).ToNumber(), Is.EqualTo(100).Within(1e-9));
        }

        [Test]
        public void Les_etats_types_sont_ceux_qu_ils_annoncent()
        {
            var horloge = new HorlogeFigee(DEPART_MS);
            Assert.That(Economie.EauTroublee(EtatsDEssai.ContenancePleine(horloge)), Is.True);
            Assert.That(EtatsDEssai.ApresRenaissance(horloge).Permanent.NombreDeRenaissances, Is.EqualTo(1));
            var mi = EtatsDEssai.MiPartie(horloge);
            Assert.That(mi.Cycle.Especes.Values.Count(s => s.Debloquee), Is.GreaterThan(1));
            Assert.That(Gabarits.StadeDuNiveau(mi.Cycle.NiveauDuHeros), Is.EqualTo(2));
        }
    }
}
