using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class SeuilsTests
    {
        [Test, Description("les seuils sont lus sur le niveau et CUMULÉS : ×2, ×4, ×8, ×16")]
        public void Les_seuils_sont_lus_sur_le_niveau_et_CUMULES()
        {
            Assert.That(Economie.MultiplicateurDeSeuil(9), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDeSeuil(10), Is.EqualTo(2));
            Assert.That(Economie.MultiplicateurDeSeuil(25), Is.EqualTo(4));
            Assert.That(Economie.MultiplicateurDeSeuil(50), Is.EqualTo(8));
            Assert.That(Economie.MultiplicateurDeSeuil(100), Is.EqualTo(16));
            Assert.That(Economie.MultiplicateurDeSeuil(1000), Is.EqualTo(16));
        }

        [Test, Description("la densité nulle ne multiplie rien, et la voix suit les franchissements")]
        public void La_densite_nulle_ne_multiplie_rien_et_la_voix_suit_les_franchissements()
        {
            Assert.That(Densite.Multiplicateur(0), Is.EqualTo(1));
            Assert.That(Voix.PalierApres(0), Is.EqualTo(PalierDeVoix.Pente));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES), Is.EqualTo(PalierDeVoix.Signes));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_DIRECTIVES), Is.EqualTo(PalierDeVoix.Directives));
            Assert.That(Voix.AuMoins(PalierDeVoix.Directives, PalierDeVoix.Signes), Is.True);
        }

        [Test, Description("le coût de base d’un palier est g puissance palier moins un, fois le premier creusement")]
        public void Le_cout_de_base_d_un_palier()
        {
            Assert.That(Economie.CoutBaseDuPalier(0).ToNumber(), Is.EqualTo(Constantes.COUT_CREUSER_AU_PALIER_1));
            Assert.That(Economie.CoutBaseDuPalier(1).ToNumber(), Is.EqualTo(Constantes.COUT_CREUSER_AU_PALIER_1));
            Assert.That(Economie.CoutBaseDuPalier(3).Eq(Echelles.PuissanceDeG(2).Mul(Constantes.COUT_CREUSER_AU_PALIER_1)), Is.True);
        }

        /* ─── Cas purs portés depuis tests/seuils.test.ts ───────────────────────────
         * N'appellent ni etatInitial, ni tick, ni un acte, ni etatDeTravail. */

        [Test, Description("la table est cumulée : on lit le multiplicateur du seuil franchi")]
        public void La_table_est_cumulee_on_lit_le_multiplicateur_du_seuil_franchi()
        {
            Assert.That(Economie.MultiplicateurDeSeuil(0), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDeSeuil(9), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDeSeuil(10), Is.EqualTo(2));
            Assert.That(Economie.MultiplicateurDeSeuil(24), Is.EqualTo(2));
            Assert.That(Economie.MultiplicateurDeSeuil(25), Is.EqualTo(4));
            Assert.That(Economie.MultiplicateurDeSeuil(50), Is.EqualTo(8));
            Assert.That(Economie.MultiplicateurDeSeuil(99), Is.EqualTo(8));
            Assert.That(Economie.MultiplicateurDeSeuil(100), Is.EqualTo(16));
            Assert.That(Economie.MultiplicateurDeSeuil(10_000), Is.EqualTo(16));
        }

        [Test, Description("cent ne vaut JAMAIS ×1024")]
        public void Cent_ne_vaut_JAMAIS_1024()
        {
            // 2 × 4 × 8 × 16 : la lecture multiplicative, celle contre laquelle il ne
            // faut pas calibrer. Si elle repassait, `D = 2.31` serait faux.
            Assert.That(Economie.MultiplicateurDeSeuil(100), Is.Not.EqualTo(1024));
        }

        /* ─── Cas portés depuis tests/seuils.test.ts à la tâche 7 (via EtatInitial) ─── */

        static readonly Espece ESPECE = Especes.Toutes[0];

        static EtatJeu AuNiveau(int niveau)
        {
            var depart = Reducteur.EtatInitial(1);
            return depart with
            {
                Cycle = depart.Cycle with
                {
                    Especes = new Dictionary<string, EtatEspece> { [ESPECE.Id] = new EtatEspece(true, niveau) },
                },
            };
        }

        [Test, Description("une espèce au niveau cent produit exactement ×16 sa base")]
        public void Une_espece_au_niveau_cent_produit_exactement_16_sa_base()
        {
            var etat = AuNiveau(Constantes.SEUIL_DU_DRAPEAU_PERMANENT);
            var @base = Economie.DebitBaseDeLEspece(ESPECE).Mul(Constantes.SEUIL_DU_DRAPEAU_PERMANENT);
            var obtenue = Economie.ProductionDeLEspece(etat, ESPECE);
            // Aucun drapeau posé : il tombe à l'achat, et personne n'a rien acheté ici.
            Assert.That(Economie.MultiplicateurDesDrapeaux(etat), Is.EqualTo(1));
            Assert.That(obtenue.Div(@base).ToNumber(), Is.EqualTo(16).Within(0.5e-9));
        }

        [Test, Description("le seuil se lit sur le niveau, et le temps n’y change rien")]
        public void Le_seuil_se_lit_sur_le_niveau_et_le_temps_n_y_change_rien()
        {
            var neuf = AuNiveau(9);
            var attendu = Economie.DebitBaseDeLEspece(ESPECE).Mul(9);
            Assert.That(Economie.ProductionDeLEspece(neuf, ESPECE).Div(attendu).ToNumber(), Is.EqualTo(1).Within(0.5e-9));
            // Huit heures plus tard, toujours ×1 : rien ne franchit un seuil tout seul.
            Assert.That(Economie.ProductionDeLEspece(Reducteur.Tick(neuf, 8 * 3600), ESPECE).Eq(Economie.ProductionDeLEspece(neuf, ESPECE)), Is.True);
        }

        [Test, Description("le centième niveau pose le drapeau permanent À L’ACHAT, et il vaut +3 %")]
        public void Le_centieme_niveau_pose_le_drapeau_permanent_A_L_ACHAT_et_il_vaut_3()
        {
            var depart = Reducteur.EtatInitial(1);
            var etat = depart with
            {
                Cycle = depart.Cycle with { ManaCourant = Decimal.Parse("1e30") },
                Permanent = depart.Permanent with { ContenanceMana = Decimal.Parse("1e40") },
            };
            etat = Reducteur.Debloquer(etat, ESPECE.Id);
            while (etat.Cycle.Especes[ESPECE.Id].Niveau < Constantes.SEUIL_DU_DRAPEAU_PERMANENT)
                etat = Reducteur.Ameliorer(etat, ESPECE.Id);
            Assert.That(etat.Permanent.EspecesAyantAtteintCent, Has.Member(ESPECE.Id));
            Assert.That(Economie.MultiplicateurDesDrapeaux(etat), Is.EqualTo(1 + Constantes.BONUS_GLOBAL_A_CENT_INDIVIDUS).Within(0.5e-9));
        }

        [Test, Description("le drapeau survit à la renaissance, le multiplicateur de seuil non")]
        public void Le_drapeau_survit_a_la_renaissance_le_multiplicateur_de_seuil_non()
        {
            var depart = Reducteur.EtatInitial(1);
            var avecDrapeau = AuNiveau(Constantes.SEUIL_DU_DRAPEAU_PERMANENT) with
            {
                Permanent = depart.Permanent with { EspecesAyantAtteintCent = new[] { ESPECE.Id } },
            };
            var apresRenaissance = Renaissance.Renaitre(avecDrapeau);
            Assert.That(apresRenaissance.Permanent.EspecesAyantAtteintCent, Has.Member(ESPECE.Id));
            Assert.That(apresRenaissance.Cycle.Especes, Is.Empty);
            Assert.That(Economie.ProductionDeLEspece(apresRenaissance, ESPECE).Eq(new Decimal(0)), Is.True);
        }
    }
}
