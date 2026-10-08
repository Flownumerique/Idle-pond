using IdlePond.Jeu;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le plein écran n'est imposé qu'au démarrage et quand le joueur change CE réglage :
    /// toucher au volume ne doit pas défaire un Alt+Entrée.
    /// </summary>
    public class AffichageDeLApplicationTests
    {
        [Test, Description("au démarrage, le choix du joueur s'impose ; sans choix, rien")]
        public void Au_demarrage_le_choix_s_impose()
        {
            Assert.That(AffichageDeLApplication.PleinEcranAImposer(null, Reglages.ParDefaut with { PleinEcran = true }), Is.True);
            Assert.That(AffichageDeLApplication.PleinEcranAImposer(null, Reglages.ParDefaut), Is.Null);
        }

        [Test, Description("un autre réglage qui change ne réimpose pas le plein écran")]
        public void Un_autre_reglage_ne_reimpose_rien()
        {
            var avant = Reglages.ParDefaut with { PleinEcran = true };
            Assert.That(AffichageDeLApplication.PleinEcranAImposer(avant, avant with { VolumeGeneral = 0.2 }), Is.Null);
        }

        [Test, Description("changer le plein écran l'impose")]
        public void Changer_le_plein_ecran_l_impose()
        {
            var avant = Reglages.ParDefaut with { PleinEcran = true };
            Assert.That(AffichageDeLApplication.PleinEcranAImposer(avant, avant with { PleinEcran = false }), Is.False);
        }
    }
}
