using IdlePond.Jeu;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// L'horloge (§10) — le seul module autorisé à lire l'heure système.
    ///
    /// Le compteur Entretien ne lit que les heures CRÉDITÉES, jamais le temps écoulé.
    /// Sans ça, avancer son horloge farme l'arbre permanent — et c'est la seule
    /// protection anti-triche nécessaire dans tout le jeu.
    /// </summary>
    public class HorlogeTests
    {
        const long H = 3600_000;

        [Test, Description("crédite le temps écoulé tant qu’il reste sous le plafond")]
        public void Credite_le_temps_ecoule_tant_qu_il_reste_sous_le_plafond()
        {
            Assert.That(Horloge.SecondesHorsLigneCreditees(0, 2 * H, 6), Is.EqualTo(2 * 3600.0));
        }

        [Test, Description("plafonne l’avance : une horloge poussée ne rapporte pas plus")]
        public void Plafonne_l_avance_une_horloge_poussee_ne_rapporte_pas_plus()
        {
            Assert.That(Horloge.SecondesHorsLigneCreditees(0, 40 * H, 6), Is.EqualTo(6 * 3600.0));
            Assert.That(Horloge.SecondesHorsLigneCreditees(0, 400 * H, 24), Is.EqualTo(24 * 3600.0));
        }

        [Test, Description("ignore le recul d’horloge plutôt que de retirer quoi que ce soit")]
        public void Ignore_le_recul_d_horloge_plutot_que_de_retirer_quoi_que_ce_soit()
        {
            // On ne punit jamais l'absence, et on ne punit pas davantage une horloge
            // qui recule : elle ne crédite rien, elle ne retire rien.
            Assert.That(Horloge.SecondesHorsLigneCreditees(10 * H, 2 * H, 6), Is.EqualTo(0.0));
            Assert.That(Horloge.SecondesHorsLigneCreditees(10 * H, 10 * H, 6), Is.EqualTo(0.0));
        }

        [Test, Description("le plafond reste entre 6 h et 24 h, quoi qu’on lui demande")]
        public void Le_plafond_reste_entre_6_h_et_24_h_quoi_qu_on_lui_demande()
        {
            Assert.That(Horloge.CapHorsLigneSecondes(1), Is.EqualTo(Constantes.CAP_HORS_LIGNE_HEURES_INITIAL * 3600));
            Assert.That(Horloge.CapHorsLigneSecondes(999), Is.EqualTo(Constantes.CAP_HORS_LIGNE_HEURES_MAXIMUM * 3600));
        }

        [Test, Description("l’horloge figée rend les tests déterministes")]
        public void L_horloge_figee_rend_les_tests_deterministes()
        {
            var horloge = new HorlogeFigee(1000);
            Assert.That(horloge.MaintenantMs(), Is.EqualTo(1000L));
            horloge.AvancerMs(500);
            Assert.That(horloge.MaintenantMs(), Is.EqualTo(1500L));
        }

        [Test, Description("la graine d’une nouvelle partie garde les 32 bits bas de l’instant")]
        public void La_graine_d_une_nouvelle_partie_garde_les_32_bits_bas_de_l_instant()
        {
            // `>>> 0` en TypeScript : un instant Unix de 2026 dépasse 2^32 millisecondes.
            Assert.That(Horloge.GrainePourNouvellePartie(new HorlogeFigee(0x1_0000_0005L)), Is.EqualTo(5L));
            Assert.That(Horloge.GrainePourNouvellePartie(new HorlogeFigee(1_800_000_000_000L)), Is.EqualTo(1_800_000_000_000L & 0xFFFFFFFF));
        }

        [Test, Description("l’horloge système rend un instant plausible")]
        public void L_horloge_systeme_rend_un_instant_plausible()
        {
            // Après le 1er janvier 2026 : le seul test qui lit la vraie heure, et il ne
            // vérifie que son ordre de grandeur.
            Assert.That(new HorlogeSysteme().MaintenantMs(), Is.GreaterThan(1_767_225_600_000L));
        }
    }
}
