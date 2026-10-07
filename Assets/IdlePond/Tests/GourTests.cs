using System.IO;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// L'assise II, le Gour (spec du 2026-10-07) : nommée, peuplée, livrée.
    public class GourTests
    {
        [Test]
        public void La_deuxieme_assise_est_le_Gour_et_le_jeu_le_livre()
        {
            Assert.That(Assises.Toutes[1].Id, Is.EqualTo("gour"));
            Assert.That(Assises.Toutes[1].NombreDePaliers, Is.EqualTo(12), "douze paliers, comme le noyau le fixe");
            Assert.That(Assises.PALIERS_LIVRES, Is.EqualTo(18), "la Noue et le Gour");
            Assert.That(Textes.NOM_DES_ASSISES["gour"], Is.EqualTo("le Gour"));
        }

        [Test]
        public void Le_Gour_porte_l_epinoche_le_chabot_la_lamproie_et_l_ombre()
        {
            var especes = Especes.DeLAssise("gour");
            Assert.That(especes.Select(e => e.Id), Is.EqualTo(new[] { "epinoche", "chabot", "lamproie", "ombre" }));
            Assert.That(especes.Select(e => e.Palier), Is.EqualTo(new[] { 6, 9, 12, 15 }));
            foreach (var espece in especes) Assert.That(Textes.NOM_DES_ESPECES.ContainsKey(espece.Id), Is.True, espece.Id);
        }

        [Test]
        public void Une_sauvegarde_d_avant_le_Gour_se_charge_avec_le_Gour()
        {
            var dossier = Path.Combine(Path.GetTempPath(), "idlepond-gour-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dossier);
            try
            {
                var horloge = new HorlogeFigee(1_700_000_000_000L);
                var ancienne = Partie.NouvelEtat(horloge) with { LimiteDeContenu = 6 };
                Persistance.Sauvegarder(dossier, ancienne, horloge.MaintenantMs());
                var chargee = Persistance.Charger(dossier, horloge);
                Assert.That(chargee.Etat.LimiteDeContenu, Is.EqualTo(Assises.PALIERS_LIVRES),
                    "une partie existante ne doit pas rester bloquée au fond de la Noue");
            }
            finally
            {
                Directory.Delete(dossier, true);
            }
        }

        [Test]
        public void Chaque_espece_du_Gour_a_ses_seuils_et_le_fond_du_Gour_existe()
        {
            var ids = RegistreDesSucces.Tous.Select(s => s.Id).ToList();
            foreach (var espece in new[] { "chabot", "lamproie", "ombre" })
                foreach (var seuil in new[] { 10, 25, 50, 100 })
                    Assert.That(ids, Does.Contain($"seuil-{espece}-{seuil}"));
            var fond = RegistreDesSucces.Tous.Single(s => s.Id == "franchissement-fond-du-gour");
            Assert.That(fond.Assise, Is.EqualTo("gour"));
            Assert.That(fond.Declencheur.Seuil, Is.EqualTo(18), "toute la Noue et tout le Gour ouverts");
        }

        [Test]
        public void Chaque_succes_du_Gour_a_ses_trois_textes()
        {
            foreach (var succes in RegistreDesSucces.Tous.Where(s => s.Assise == "gour"))
            {
                var texte = Textes.DuSucces(succes.Id);
                Assert.That(texte, Is.Not.Null, succes.Id);
                Assert.That(texte.Nom, Is.Not.Empty, succes.Id);
                Assert.That(texte.Condition, Is.Not.Empty, succes.Id);
                Assert.That(texte.Rapport, Is.Not.Empty, succes.Id);
            }
        }

        [Test]
        public void Le_Gour_a_sa_lumiere_froide_et_sa_marque_de_membranes()
        {
            var decor = RegistreDArt.DecorDe("gour");
            Assert.That(decor.Assise, Is.EqualTo("gour"));
            Assert.That(decor.Lumieres.Count, Is.EqualTo(12), "une lumière par palier");
            Assert.That(decor.Lumieres.First(), Is.LessThan(RegistreDArt.DecorDe("noue").Lumieres.Last()), "plus sombre que le fond de la Noue");
            Assert.That(decor.Lumieres, Is.Ordered.Descending);
            Assert.That(decor.Berge, Is.False, "pas de berge sous terre");
            Assert.That(RegistreDArt.ASSISES_DESSINEES, Does.Contain("gour"));
            Assert.That(RegistreDArt.MarqueDe("gour").Ancrage, Is.EqualTo(AncrageId.Dos));
        }
    }
}
