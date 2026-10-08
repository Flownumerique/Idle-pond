using System;
using System.IO;
using IdlePond.Jeu;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le magasin des réglages : il notifie tout de suite, et n'écrit qu'après un court
    /// silence — un curseur qu'on fait glisser ne doit pas réécrire le fichier à chaque cran.
    /// </summary>
    public class MagasinDeReglagesTests
    {
        string dossier;

        [SetUp]
        public void Preparer() => dossier = Path.Combine(Path.GetTempPath(), "idlepond-reglages-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void Ranger()
        {
            if (Directory.Exists(dossier)) Directory.Delete(dossier, true);
        }

        string Chemin => Path.Combine(dossier, MagasinDeReglages.NOM_DU_FICHIER);

        [Test, Description("au premier lancement : les défauts, et rien d'écrit")]
        public void Au_premier_lancement_les_defauts_et_rien_d_ecrit()
        {
            var magasin = MagasinDeReglages.Ouvrir(dossier);
            Assert.That(magasin.Courants, Is.EqualTo(Reglages.ParDefaut));
            Assert.That(magasin.FichierIllisible, Is.False);
            magasin.Avancer(10);
            Assert.That(File.Exists(Chemin), Is.False);
        }

        [Test, Description("modifier notifie une fois, avec les réglages bornés ; une modification sans effet ne notifie pas")]
        public void Modifier_notifie_une_fois_et_borne()
        {
            var magasin = MagasinDeReglages.Ouvrir(dossier);
            var recus = 0;
            Reglages dernier = null;
            magasin.Change += r => { recus++; dernier = r; };

            magasin.Modifier(r => r with { VolumeMusique = 3 });
            Assert.That(recus, Is.EqualTo(1));
            Assert.That(dernier.VolumeMusique, Is.EqualTo(1));
            Assert.That(magasin.Courants.VolumeMusique, Is.EqualTo(1));

            magasin.Modifier(r => r with { VolumeMusique = 1 });
            Assert.That(recus, Is.EqualTo(1));
        }

        [Test, Description("rien n'est écrit avant le délai ; après, le fichier se relit à l'identique")]
        public void L_ecriture_attend_le_delai_puis_se_relit()
        {
            var magasin = MagasinDeReglages.Ouvrir(dossier);
            magasin.Modifier(r => r with { Muet = true, Affichage = FormatDAffichage.Pc });
            magasin.Avancer(MagasinDeReglages.DELAI_D_ENREGISTREMENT_S / 2);
            Assert.That(File.Exists(Chemin), Is.False);
            magasin.Avancer(MagasinDeReglages.DELAI_D_ENREGISTREMENT_S);
            Assert.That(File.Exists(Chemin), Is.True);

            var relu = MagasinDeReglages.Ouvrir(dossier);
            Assert.That(relu.Courants, Is.EqualTo(magasin.Courants));
        }

        [Test, Description("un nouveau changement relance le délai")]
        public void Un_nouveau_changement_relance_le_delai()
        {
            var magasin = MagasinDeReglages.Ouvrir(dossier);
            magasin.Modifier(r => r with { Muet = true });
            magasin.Avancer(MagasinDeReglages.DELAI_D_ENREGISTREMENT_S * 0.8);
            magasin.Modifier(r => r with { Muet = false, VolumeGeneral = 0.1 });
            magasin.Avancer(MagasinDeReglages.DELAI_D_ENREGISTREMENT_S * 0.8);
            Assert.That(File.Exists(Chemin), Is.False);
        }

        [Test, Description("Enregistrer écrit tout de suite ce qui attendait")]
        public void Enregistrer_ecrit_tout_de_suite()
        {
            var magasin = MagasinDeReglages.Ouvrir(dossier);
            magasin.Modifier(r => r with { ContrasteRenforce = true });
            magasin.Enregistrer();
            Assert.That(MagasinDeReglages.Ouvrir(dossier).Courants.ContrasteRenforce, Is.True);
        }

        [Test, Description("un fichier corrompu rend les défauts, sans exception, et le signale")]
        public void Un_fichier_corrompu_rend_les_defauts()
        {
            Directory.CreateDirectory(dossier);
            File.WriteAllText(Chemin, "{ pas du json");
            MagasinDeReglages magasin = null;
            Assert.DoesNotThrow(() => magasin = MagasinDeReglages.Ouvrir(dossier));
            Assert.That(magasin.Courants, Is.EqualTo(Reglages.ParDefaut));
            Assert.That(magasin.FichierIllisible, Is.True);
        }
    }
}
