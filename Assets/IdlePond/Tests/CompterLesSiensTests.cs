using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// « Compter les siens » (2026-11-14) : sans le nœud, l'écran dit la part de chaque espèce
    /// en pente, sans chiffre ; avec, il montre les débits exacts et le détail de captation.
    /// </summary>
    public class CompterLesSiensTests
    {
        [Test, Description("les parts des espèces et de l'ambiant font le tout")]
        public void Les_parts_font_le_tout()
        {
            var etat = EtatDeTravail.Creer();
            var somme = Especes.Toutes.Sum(e => Economie.PartDeLEspece(etat, e)) + Economie.PartDeLAmbiant(etat);
            Assert.That(somme, Is.EqualTo(1).Within(1e-9));
            Assert.That(Economie.PartDeLEspece(etat, Especes.Toutes[0]), Is.GreaterThan(0));
        }

        [Test, Description("une espèce qui ne donne rien a une part nulle")]
        public void Une_espece_qui_ne_donne_rien_a_une_part_nulle()
        {
            var etat = Reducteur.EtatInitial(1);
            Assert.That(Economie.PartDeLEspece(etat, Especes.Toutes[0]), Is.EqualTo(0));
        }

        [Test, Description("lire les débits suit le nœud entretien-compte")]
        public void Lire_les_debits_suit_le_noeud()
        {
            var etat = Reducteur.EtatInitial(1);
            Assert.That(Economie.LitLesDebits(etat), Is.False);
            var sachant = etat with { Permanent = etat.Permanent with { NoeudsTechnique = new[] { "entretien-compte" } } };
            Assert.That(Economie.LitLesDebits(sachant), Is.True);
        }
    }
}
