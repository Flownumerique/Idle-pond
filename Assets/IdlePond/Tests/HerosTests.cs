using System;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// L'axe héros — spec 2026-09-17 §3.1.
    ///
    /// Le héros GRANDIT pendant la vie, avec du mana : c'est le quatrième achat du
    /// noyau v1.0 §1.2 amendé. Son niveau se reperd à la renaissance (f = 1), et ce
    /// qu'il apporte est un multiplicateur global NOMMÉ, jamais un facteur flottant.
    /// </summary>
    public class HerosTests
    {
        static EtatJeu AuNiveau(EtatJeu etat, int niveauDuHeros) =>
            etat with { Cycle = etat.Cycle with { NiveauDuHeros = niveauDuHeros } };

        /* ─── A1 — les graines de l'axe héros ─────────────────────────────────── */

        [Test, Description("le héros démarre au niveau 1, dans le cycle")]
        public void Le_heros_demarre_au_niveau_1_dans_le_cycle()
        {
            Assert.That(Constantes.NIVEAU_DU_HEROS_AU_DEPART, Is.EqualTo(1));
            Assert.That(Reducteur.EtatInitial(1).Cycle.NiveauDuHeros, Is.EqualTo(Constantes.NIVEAU_DU_HEROS_AU_DEPART));
        }

        [Test, Description("le bonus par niveau est une graine strictement positive et modeste")]
        public void Le_bonus_par_niveau_est_une_graine_strictement_positive_et_modeste()
        {
            Assert.That(Constantes.BONUS_PAR_NIVEAU_DU_HEROS, Is.GreaterThan(0));
            Assert.That(Constantes.BONUS_PAR_NIVEAU_DU_HEROS, Is.LessThan(0.5));
        }

        [Test, Description("D par palier ne bouge pas : le héros en prend une part, la profondeur le reste")]
        public void D_par_palier_ne_bouge_pas_le_heros_en_prend_une_part_la_profondeur_le_reste()
        {
            // Contrainte globale du plan. Avant ce plan : m_p³ × ratio = D³. Après :
            // (m_p × (1 + b))³ × ratio = D³. Un niveau de héros par palier, et la
            // croissance par palier est inchangée.
            var parTroisPaliers =
                Math.Pow(Constantes.MultiplicateurDePalier() * (1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS), Constantes.ESPECE_TOUS_LES_N_PALIERS) *
                Constantes.DEBIT_RATIO_ESPECE;
            Assert.That(parTroisPaliers,
                Is.EqualTo(Math.Pow(Constantes.D_PRODUCTION_PAR_PALIER, Constantes.ESPECE_TOUS_LES_N_PALIERS)).Within(0.5e-6));
        }

        [Test, Description("le premier niveau coûte plus que la charge de l'œuf")]
        public void Le_premier_niveau_coute_plus_que_la_charge_de_l_oeuf()
        {
            // Sinon le joueur naïf (moins cher d'abord) grandit avant de convaincre le
            // vairon, et le plancher de cadence du §8.4 tombe dès la première minute.
            Assert.That(Constantes.COUT_CREUSER_AU_PALIER_1 * Constantes.RATIO_COUT_DE_CROISSANCE,
                Is.GreaterThan(Constantes.MANA_A_LA_SORTIE_DE_L_OEUF));
        }

        [Test, Description("les termes du héros sont nommés au registre")]
        public void Les_termes_du_heros_sont_nommes_au_registre()
        {
            Assert.That(Termes.DE_PRODUCTION, Has.Member(TermeDeFormule.MultiplicateurHeros));
            Assert.That(Termes.DE_COUT, Has.Member(TermeDeFormule.CoutCroissance));
        }

        /* ─── A2 — ce que le niveau du héros vaut ─────────────────────────────── */

        [Test, Description("au niveau 1 le multiplicateur vaut exactement 1")]
        public void Au_niveau_1_le_multiplicateur_vaut_exactement_1()
        {
            Assert.That(Economie.MultiplicateurDuHeros(AuNiveau(Reducteur.EtatInitial(1), 1)), Is.EqualTo(1));
        }

        [Test, Description("chaque niveau multiplie par (1 + b), et c'est un terme global")]
        public void Chaque_niveau_multiplie_par_1_b_et_c_est_un_terme_global()
        {
            var @base = EtatDeTravail.Creer();
            var un = AuNiveau(@base, 1);
            var cinq = AuNiveau(@base, 5);
            Assert.That(Economie.MultiplicateurDuHeros(cinq), Is.EqualTo(Math.Pow(1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS, 4)).Within(0.5e-12));
            var rapport = Economie.MultiplicateursGlobaux(cinq).Div(Economie.MultiplicateursGlobaux(un)).ToNumber();
            Assert.That(rapport, Is.EqualTo(Math.Pow(1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS, 4)).Within(0.5e-9));
            // La production TOTALE suit le même rapport, espèces comprises.
            var total = Economie.ProductionTotaleParSeconde(cinq).Div(Economie.ProductionTotaleParSeconde(un)).ToNumber();
            Assert.That(total, Is.GreaterThan(rapport)); // > : son débit propre monte aussi avec le niveau
        }

        [Test, Description("son débit propre monte avec le niveau, multiplicateurs compris")]
        public void Son_debit_propre_monte_avec_le_niveau_multiplicateurs_compris()
        {
            var un = AuNiveau(Reducteur.EtatInitial(1), 1);
            var trois = AuNiveau(Reducteur.EtatInitial(1), 3);
            Assert.That(Economie.ProductionDuHeros(un).Eq(new Decimal(Constantes.DEBIT_HEROS).Mul(Economie.MultiplicateursGlobaux(un))), Is.True);
            Assert.That(Economie.ProductionDuHeros(trois).Eq(new Decimal(Constantes.DEBIT_HEROS).Mul(3).Mul(Economie.MultiplicateursGlobaux(trois))), Is.True);
        }

        [Test, Description("le coût de croissance suit g, en fraction du coût du palier de même rang")]
        public void Le_cout_de_croissance_suit_g_en_fraction_du_cout_du_palier_de_meme_rang()
        {
            var etat = Reducteur.EtatInitial(1);
            var premier = Economie.CoutDeCroissance(etat, 1);
            Assert.That(premier.ToNumber(), Is.EqualTo(Constantes.COUT_CREUSER_AU_PALIER_1 * Constantes.RATIO_COUT_DE_CROISSANCE).Within(0.5e-9));
            Assert.That(Economie.CoutDeCroissance(etat, 4).Div(Economie.CoutDeCroissance(etat, 3)).ToNumber(),
                Is.EqualTo(Constantes.G_COUT_PALIER).Within(0.5e-9));
        }

        [Test, Description("le détail du héros nomme ses deux termes, et sa source porte le niveau")]
        public void Le_detail_du_heros_nomme_ses_deux_termes_et_sa_source_porte_le_niveau()
        {
            var lignes = Economie.DetailDuHeros(AuNiveau(Reducteur.EtatInitial(1), 4));
            Assert.That(lignes.Select(l => l.Terme), Is.EqualTo(new[] { TermeDeFormule.DebitHeros, TermeDeFormule.MultiplicateurHeros }));
            Assert.That(lignes[0].Source, Is.EqualTo(new SourceDeTerme(QuoiSource.Heros, 4)));
            Assert.That(lignes[1].Valeur, Is.EqualTo(Math.Pow(1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS, 3)).Within(0.5e-12));
        }

        /* ─── A3 — grandir ────────────────────────────────────────────────────── */

        [Test, Description("paie le coût, monte d'un niveau, crédite le compteur Amélioration")]
        public void Paie_le_cout_monte_d_un_niveau_credite_le_compteur_Amelioration()
        {
            var avant = Reducteur.EtatInitial(1) with { Cycle = Reducteur.EtatInitial(1).Cycle with { ManaCourant = new Decimal(1000) } };
            var cout = Economie.CoutDeCroissance(avant, avant.Cycle.NiveauDuHeros);
            var apres = Reducteur.Grandir(avant);
            Assert.That(apres.Cycle.NiveauDuHeros, Is.EqualTo(2));
            Assert.That(apres.Cycle.ManaCourant.Eq(avant.Cycle.ManaCourant.Sub(cout)), Is.True);
            Assert.That(apres.Permanent.CompteursTechnique[BrancheTechnique.Amelioration],
                Is.EqualTo(avant.Permanent.CompteursTechnique[BrancheTechnique.Amelioration] + cout.ToNumber()).Within(0.5e-9));
        }

        [Test, Description("refuse sans rien changer si le mana manque")]
        public void Refuse_sans_rien_changer_si_le_mana_manque()
        {
            var pauvre = Reducteur.EtatInitial(1) with { Cycle = Reducteur.EtatInitial(1).Cycle with { ManaCourant = new Decimal(1) } };
            Assert.That(Reducteur.Grandir(pauvre), Is.SameAs(pauvre));
        }

        [Test, Description("le niveau se reperd à la renaissance : il ressort alevin")]
        public void Le_niveau_se_reperd_a_la_renaissance_il_ressort_alevin()
        {
            var grandi = AuNiveau(EtatDeTravail.Creer(), 9);
            Assert.That(Renaissance.Renaitre(grandi).Cycle.NiveauDuHeros, Is.EqualTo(Constantes.NIVEAU_DU_HEROS_AU_DEPART));
        }

        [Test, Description("le niveau ne bouge jamais pendant un tick — le pas reste homogène")]
        public void Le_niveau_ne_bouge_jamais_pendant_un_tick_le_pas_reste_homogene()
        {
            var depart = AuNiveau(EtatDeTravail.Creer(), 6);
            var petits = depart;
            for (var i = 0; i < 480; i += 1) petits = Reducteur.Tick(petits, 60);
            var grand = Reducteur.Tick(depart, 8 * 3600);
            Assert.That(grand.Cycle.NiveauDuHeros, Is.EqualTo(6));
            Comparateur.ComparerATolerance(Instantane.De(petits, false), Instantane.De(grand, false));
        }
    }
}
