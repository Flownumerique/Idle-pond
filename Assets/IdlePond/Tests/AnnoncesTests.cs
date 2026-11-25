using IdlePond.Jeu.UI;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class AnnoncesTests
    {
        [Test, Description("deux succès qui disent la même phrase ne s'empilent pas : une ligne, la première arrivée")]
        public void Deux_annonces_au_meme_texte_n_en_montrent_qu_une()
        {
            var ids = new[] { "a", "b", "c", "d" };
            string Texte(string id) => id == "b" || id == "d" ? "Celui-là ne prendra plus personne." : id;
            Assert.That(Annonces.AMontrer(ids, Texte, 3), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test, Description("seules les dernières se voient, comptées après le dédoublonnage")]
        public void Seules_les_dernieres_se_voient()
        {
            var ids = new[] { "a", "b", "b2", "c", "d" };
            string Texte(string id) => id == "b2" ? "b" : id;
            Assert.That(Annonces.AMontrer(ids, Texte, 3), Is.EqualTo(new[] { "b", "c", "d" }));
        }
    }
}
