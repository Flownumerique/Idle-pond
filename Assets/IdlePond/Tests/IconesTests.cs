using System.Linq;
using IdlePond.Jeu.UI;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// Les icônes du dock (spec du 2026-10-07, §2) : des grilles de pixels écrites en données.
    public class IconesTests
    {
        [Test]
        public void Chaque_icone_est_une_grille_carree_de_la_bonne_taille()
        {
            foreach (var (nom, grille) in Icones.Toutes.Concat(Icones.Autres))
            {
                Assert.That(grille.Length, Is.EqualTo(Icones.COTE), nom);
                Assert.That(grille.All(ligne => ligne.Length == Icones.COTE), Is.True, nom);
            }
        }

        [Test]
        public void Une_icone_ne_porte_que_des_pixels_pleins_ou_vides_et_n_est_pas_vide()
        {
            foreach (var (nom, grille) in Icones.Toutes.Concat(Icones.Autres))
            {
                var caracteres = string.Concat(grille);
                Assert.That(caracteres.All(c => c == '#' || c == '.'), Is.True, nom);
                Assert.That(caracteres.Count(c => c == '#'), Is.GreaterThan(10), nom);
            }
        }

        [Test]
        public void Les_cinq_icones_du_dock_existent_et_sont_distinctes()
        {
            Assert.That(Icones.Toutes.Count, Is.EqualTo(5));
            var toutes = Icones.Toutes.Concat(Icones.Autres).ToList();
            Assert.That(toutes.Select(i => string.Concat(i.Grille)).Distinct().Count(), Is.EqualTo(toutes.Count));
        }

        [Test]
        public void Les_pixels_pleins_d_une_grille_deviennent_des_rectangles()
        {
            var cases = Icones.Pleines(new[] { "#.", ".#" }).ToList();
            Assert.That(cases, Is.EquivalentTo(new[] { (0, 0), (1, 1) }));
        }
    }
}
