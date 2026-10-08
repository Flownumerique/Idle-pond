using IdlePond.Jeu.UI;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les gestes qui effacent (réinitialiser la partie, rétablir les réglages) se
    /// confirment par un second toucher, dans les trois secondes.
    /// </summary>
    public class ConfirmationTests
    {
        [Test, Description("un seul toucher ne fait rien, mais met en attente")]
        public void Un_seul_toucher_ne_fait_que_mettre_en_attente()
        {
            var c = new Confirmation();
            Assert.That(c.Toucher(1000), Is.False);
            Assert.That(c.EnAttente(1000), Is.True);
        }

        [Test, Description("un second toucher dans le délai confirme, et l'attente retombe")]
        public void Un_second_toucher_dans_le_delai_confirme()
        {
            var c = new Confirmation();
            c.Toucher(1000);
            Assert.That(c.Toucher(1000 + Confirmation.DELAI_MS), Is.True);
            Assert.That(c.EnAttente(1000 + Confirmation.DELAI_MS), Is.False);
        }

        [Test, Description("passé le délai, le second toucher n'est qu'un nouveau premier")]
        public void Passe_le_delai_le_toucher_recommence()
        {
            var c = new Confirmation();
            c.Toucher(1000);
            Assert.That(c.EnAttente(1001 + Confirmation.DELAI_MS), Is.False);
            Assert.That(c.Toucher(1001 + Confirmation.DELAI_MS), Is.False);
            Assert.That(c.EnAttente(1001 + Confirmation.DELAI_MS), Is.True);
        }

        [Test, Description("annuler oublie l'attente")]
        public void Annuler_oublie_l_attente()
        {
            var c = new Confirmation();
            c.Toucher(1000);
            c.Annuler();
            Assert.That(c.Toucher(1500), Is.False);
        }
    }
}
