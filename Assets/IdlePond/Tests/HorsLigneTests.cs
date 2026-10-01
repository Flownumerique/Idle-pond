using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le crédit hors ligne (§10).
    ///
    /// « Le calcul est un appel unique à Tick avec un grand `dt` — c'est précisément ce
    /// que garantit le filtre du §5.2. » Le test le prend au mot : ce que le crédit
    /// produit doit être, à l'identique, ce que produirait un seul tick de la même durée.
    /// </summary>
    public class HorsLigneTests
    {
        const long H = 3600_000;

        [Test, Description("un seul appel à tick suffit pour toute l’absence")]
        public void Un_seul_appel_a_tick_suffit_pour_toute_l_absence()
        {
            var depart = EtatDeTravail.Creer();
            var credit = HorsLigne.Crediter(depart, 0, 3 * H);
            Assert.That(credit.SecondesCreditees, Is.EqualTo(3 * 3600.0));
            var sansCompteur = credit.Etat with
            {
                Permanent = credit.Etat.Permanent with { HeuresHorsLigneCreditees = 0 },
            };
            Comparateur.ComparerATolerance(Instantane.De(sansCompteur), Instantane.De(Reducteur.Tick(depart, 3 * 3600)), 0);
        }

        [Test, Description("le plafond borne ce que l’absence rapporte, il ne retire rien")]
        public void Le_plafond_borne_ce_que_l_absence_rapporte_il_ne_retire_rien()
        {
            var depart = EtatDeTravail.Creer();
            var credit = HorsLigne.Crediter(depart, 0, 40 * H);
            Assert.That(credit.SecondesCreditees, Is.EqualTo(Constantes.CAP_HORS_LIGNE_HEURES_INITIAL * 3600));
            Assert.That(credit.Etat.Cycle.ManaCourant.Gte(depart.Cycle.ManaCourant), Is.True);
        }

        [Test, Description("le compteur Entretien ne lit que les heures créditées")]
        public void Le_compteur_Entretien_ne_lit_que_les_heures_creditees()
        {
            // Sans ça, avancer son horloge farme l'arbre permanent — et c'est la seule
            // protection anti-triche nécessaire dans tout le jeu.
            var depart = EtatDeTravail.Creer();
            var honnete = HorsLigne.Crediter(depart, 0, 3 * H);
            var tricheur = HorsLigne.Crediter(depart, 0, 400 * H);
            Assert.That(honnete.Etat.Permanent.HeuresHorsLigneCreditees, Is.EqualTo(3.0).Within(1e-6));
            Assert.That(tricheur.Etat.Permanent.HeuresHorsLigneCreditees, Is.EqualTo(Constantes.CAP_HORS_LIGNE_HEURES_INITIAL).Within(1e-6));
        }

        [Test, Description("un recul d’horloge ne crédite rien et ne retire rien")]
        public void Un_recul_d_horloge_ne_credite_rien_et_ne_retire_rien()
        {
            var depart = EtatDeTravail.Creer();
            var credit = HorsLigne.Crediter(depart, 10 * H, 2 * H);
            Assert.That(credit.SecondesCreditees, Is.EqualTo(0.0));
            Assert.That(credit.Etat, Is.SameAs(depart));
        }

        [Test, Description("le plafond démarre à 6 h, avant toute technique")]
        public void Le_plafond_demarre_a_6_h_avant_toute_technique()
        {
            Assert.That(HorsLigne.CapHorsLigneCourantHeures(EtatDeTravail.Creer()), Is.EqualTo(Constantes.CAP_HORS_LIGNE_HEURES_INITIAL));
        }
    }
}
