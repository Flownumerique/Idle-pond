using IdlePond.Jeu;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les réglages du joueur (spec du 2026-10-08) : bornés, et relus sans jamais lever —
    /// un fichier écrit à la main, tronqué ou venu d'une version future rend les défauts,
    /// champ par champ quand c'est possible.
    /// </summary>
    public class ReglagesTests
    {
        static Reglages Relire(string json)
        {
            var reglages = Reglages.Deserialiser(json, out _);
            return reglages;
        }

        [Test, Description("les défauts sont ceux de la spec")]
        public void Les_defauts_sont_ceux_de_la_spec()
        {
            var d = Reglages.ParDefaut;
            Assert.That(d.VolumeGeneral, Is.EqualTo(0.8));
            Assert.That(d.VolumeMusique, Is.EqualTo(0.7));
            Assert.That(d.VolumeEffets, Is.EqualTo(0.8));
            Assert.That(d.Muet, Is.False);
            Assert.That(d.MuetEnArrierePlan, Is.True);
            Assert.That(d.Affichage, Is.EqualTo(FormatDAffichage.Auto));
            Assert.That(d.TailleDInterface, Is.EqualTo(1.0));
            Assert.That(d.PleinEcran, Is.Null, "non choisi : on garde l'état de la fenêtre");
            Assert.That(d.Images, Is.Null, "non choisi : le défaut de la plateforme");
            Assert.That(d.Notation, Is.EqualTo(Notation.Suffixes));
            Assert.That(d.AnnoncesAffichees, Is.True);
            Assert.That(d.MouvementReduit, Is.False);
            Assert.That(d.ContrasteRenforce, Is.False);
        }

        [Test, Description("les volumes sont ramenés entre 0 et 1, NaN rend le défaut")]
        public void Les_volumes_sont_bornes()
        {
            var r = (Reglages.ParDefaut with { VolumeGeneral = -1, VolumeMusique = 2, VolumeEffets = double.NaN }).Borner();
            Assert.That(r.VolumeGeneral, Is.EqualTo(0));
            Assert.That(r.VolumeMusique, Is.EqualTo(1));
            Assert.That(r.VolumeEffets, Is.EqualTo(Reglages.ParDefaut.VolumeEffets));
        }

        [Test, Description("la taille de l'interface reste entre 90 et 130 %, au pas de 5")]
        public void La_taille_est_bornee_au_pas_de_cinq()
        {
            Assert.That((Reglages.ParDefaut with { TailleDInterface = 1.27 }).Borner().TailleDInterface, Is.EqualTo(1.25).Within(1e-9));
            Assert.That((Reglages.ParDefaut with { TailleDInterface = 0.5 }).Borner().TailleDInterface, Is.EqualTo(0.9).Within(1e-9));
            Assert.That((Reglages.ParDefaut with { TailleDInterface = 2 }).Borner().TailleDInterface, Is.EqualTo(1.3).Within(1e-9));
            Assert.That((Reglages.ParDefaut with { TailleDInterface = double.NaN }).Borner().TailleDInterface, Is.EqualTo(1.0));
        }

        [Test, Description("un énuméré hors de ses valeurs rend le défaut")]
        public void Un_enumere_hors_de_ses_valeurs_rend_le_defaut()
        {
            var r = (Reglages.ParDefaut with { Affichage = (FormatDAffichage)42, Notation = (Notation)(-1), Images = (LimiteDImages)9 }).Borner();
            Assert.That(r.Affichage, Is.EqualTo(FormatDAffichage.Auto));
            Assert.That(r.Notation, Is.EqualTo(Notation.Suffixes));
            Assert.That(r.Images, Is.Null);
        }

        [Test, Description("ce qui est écrit se relit à l'identique")]
        public void Ce_qui_est_ecrit_se_relit_a_l_identique()
        {
            var r = new Reglages
            {
                VolumeGeneral = 0.3, VolumeMusique = 0, VolumeEffets = 1, Muet = true, MuetEnArrierePlan = false,
                Affichage = FormatDAffichage.Tablette, TailleDInterface = 1.15, PleinEcran = true,
                Images = LimiteDImages.Trente, Notation = Notation.Ingenieur, AnnoncesAffichees = false,
                MouvementReduit = true, ContrasteRenforce = true,
            };
            var relu = Reglages.Deserialiser(Reglages.Serialiser(r), out var lisible);
            Assert.That(lisible, Is.True);
            Assert.That(relu, Is.EqualTo(r));
        }

        [Test, Description("un champ absent ou du mauvais type prend son défaut, les autres sont gardés")]
        public void Un_champ_absent_ou_faux_prend_son_defaut()
        {
            var r = Relire("{\"version\":1,\"volumeMusique\":\"fort\",\"muet\":true,\"affichage\":\"Ecran\",\"notation\":\"Scientifique\"}");
            Assert.That(r.VolumeMusique, Is.EqualTo(Reglages.ParDefaut.VolumeMusique));
            Assert.That(r.VolumeGeneral, Is.EqualTo(Reglages.ParDefaut.VolumeGeneral));
            Assert.That(r.Muet, Is.True);
            Assert.That(r.Affichage, Is.EqualTo(FormatDAffichage.Auto));
            Assert.That(r.Notation, Is.EqualTo(Notation.Scientifique));
        }

        [Test, Description("une valeur hors bornes dans le fichier est bornée à la lecture")]
        public void Une_valeur_hors_bornes_est_bornee_a_la_lecture()
        {
            var r = Relire("{\"version\":1,\"volumeGeneral\":7,\"tailleDInterface\":0.1}");
            Assert.That(r.VolumeGeneral, Is.EqualTo(1));
            Assert.That(r.TailleDInterface, Is.EqualTo(0.9).Within(1e-9));
        }

        [TestCase("{\"version\":2,\"muet\":true}", Description = "version future")]
        [TestCase("{\"muet\":true}", Description = "sans version")]
        [TestCase("{\"version\":1,\"muet\":tr", Description = "tronqué")]
        [TestCase("[1,2]", Description = "racine qui n'est pas un objet")]
        [TestCase("", Description = "vide")]
        [TestCase("{\"version\":99999999999999999999,\"muet\":true}", Description = "version entière trop grande pour un long")]
        [TestCase("{\"version\":1e30,\"muet\":true}", Description = "version flottante")]
        public void Un_fichier_illisible_rend_les_defauts(string json)
        {
            var r = Reglages.Deserialiser(json, out var lisible);
            Assert.That(lisible, Is.False);
            Assert.That(r, Is.EqualTo(Reglages.ParDefaut));
        }
    }

    public class SonTests
    {
        [Test, Description("le volume d'un canal est le produit du général et du sien")]
        public void Le_volume_d_un_canal_est_le_produit_du_general_et_du_sien()
        {
            var r = Reglages.ParDefaut with { VolumeGeneral = 0.5, VolumeMusique = 0.4, VolumeEffets = 1 };
            Assert.That(Son.VolumeEffectif(Canal.Musique, r), Is.EqualTo(0.2f).Within(1e-6));
            Assert.That(Son.VolumeEffectif(Canal.Effets, r), Is.EqualTo(0.5f).Within(1e-6));
        }

        [Test, Description("couper le son rend zéro sur tous les canaux")]
        public void Couper_le_son_rend_zero()
        {
            var r = Reglages.ParDefaut with { Muet = true };
            Assert.That(Son.VolumeEffectif(Canal.Musique, r), Is.EqualTo(0f));
            Assert.That(Son.VolumeEffectif(Canal.Effets, r), Is.EqualTo(0f));
        }
    }
}
