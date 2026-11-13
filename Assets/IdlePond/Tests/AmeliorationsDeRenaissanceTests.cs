using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les améliorations — noyau v1.0 §4, spec 2026-09-17 §3.2.
    ///
    /// Permanentes, payées en Souffle, et l'unique chose que le Souffle achète tant que les
    /// miracles sont gelés. Ciblée : multiplicateur sur une espèce. Globale :
    /// additif sur le débit de base de toutes les espèces, présentes et futures.
    /// </summary>
    public class AmeliorationsDeRenaissanceTests
    {
        static EtatJeu Amelioree(EtatJeu etat, IReadOnlyDictionary<string, int> rangs)
        {
            var fusion = new Dictionary<string, int>();
            foreach (var paire in etat.Permanent.AmeliorationsDeRenaissance) fusion[paire.Key] = paire.Value;
            foreach (var paire in rangs) fusion[paire.Key] = paire.Value;
            return etat with { Permanent = etat.Permanent with { AmeliorationsDeRenaissance = fusion } };
        }

        static readonly Espece Vairon = Especes.Toutes[0];

        /* ─── B1 — le registre ────────────────────────────────────────────────── */

        [Test, Description("la globale est trouvable par son identifiant, et chaque espèce a sa ciblée")]
        public void La_globale_est_trouvable_par_son_identifiant_et_chaque_espece_a_sa_ciblee()
        {
            Assert.That(AmeliorationsDeRenaissance.ParId(AmeliorationsDeRenaissance.GLOBALE_ID)?.Portee, Is.EqualTo(PorteeDAmelioration.Globale));
            foreach (var espece in Especes.Toutes)
            {
                var ciblee = AmeliorationsDeRenaissance.CibleeDe(espece.Id);
                Assert.That(ciblee.Portee, Is.EqualTo(PorteeDAmelioration.Ciblee));
                Assert.That(ciblee.Espece, Is.EqualTo(espece.Id));
                Assert.That(AmeliorationsDeRenaissance.ParId(ciblee.Id), Is.SameAs(ciblee));
            }
            Assert.That(AmeliorationsDeRenaissance.Toutes, Has.Count.EqualTo(Especes.Toutes.Count + 2));
        }

        [Test, Description("les graines sont positives, et le coût croît")]
        public void Les_graines_sont_positives_et_le_cout_croit()
        {
            Assert.That(Constantes.AMELIORATION_CIBLEE_PAR_RANG, Is.GreaterThan(0));
            Assert.That(Constantes.AMELIORATION_GLOBALE_PAR_RANG, Is.GreaterThan(0));
            Assert.That(Constantes.SOUFFLE_COUT_D_AMELIORATION_CIBLEE, Is.GreaterThan(0));
            Assert.That(Constantes.SOUFFLE_COUT_D_AMELIORATION_GLOBALE, Is.GreaterThan(0));
            Assert.That(Constantes.RATIO_COUT_D_AMELIORATION, Is.GreaterThan(1));
        }

        /* ─── B2 — ce qu'une amélioration vaut ────────────────────────────────── */

        [Test, Description("sans amélioration : rang 0, débit amélioré = débit de base, multiplicateur 1")]
        public void Sans_amelioration_rang_0_debit_ameliore_debit_de_base_multiplicateur_1()
        {
            var etat = EtatDeTravail.Creer();
            Assert.That(Economie.RangDAmelioration(etat, AmeliorationsDeRenaissance.GLOBALE_ID), Is.EqualTo(0));
            Assert.That(Economie.DebitAmeliore(etat, Vairon).Eq(Economie.DebitBaseDeLEspece(Vairon)), Is.True);
            Assert.That(Economie.MultiplicateurDAmelioration(etat, Vairon), Is.EqualTo(1));
        }

        [Test, Description("la globale ajoute k × rang au débit de base de toute espèce")]
        public void La_globale_ajoute_k_rang_au_debit_de_base_de_toute_espece()
        {
            var etat = Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int> { [AmeliorationsDeRenaissance.GLOBALE_ID] = 3 });
            foreach (var espece in Especes.Toutes.Take(3))
            {
                var attendu = Economie.DebitBaseDeLEspece(espece).Add(Constantes.AMELIORATION_GLOBALE_PAR_RANG * 3);
                Assert.That(Economie.DebitAmeliore(etat, espece).Eq(attendu), Is.True);
            }
        }

        [Test, Description("la ciblée multiplie SON espèce par (1 + c)^rang, et aucune autre")]
        public void La_ciblee_multiplie_SON_espece_par_1_c_rang_et_aucune_autre()
        {
            var etat = Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int> { [AmeliorationsDeRenaissance.CibleeDe(Vairon.Id).Id] = 2 });
            Assert.That(Economie.MultiplicateurDAmelioration(etat, Vairon),
                Is.EqualTo(Math.Pow(1 + Constantes.AMELIORATION_CIBLEE_PAR_RANG, 2)).Within(0.5e-12));
            Assert.That(Economie.MultiplicateurDAmelioration(etat, Especes.Toutes[1]), Is.EqualTo(1));
        }

        [Test, Description("la production de l’espèce lit les deux")]
        public void La_production_de_l_espece_lit_les_deux()
        {
            var nue = EtatDeTravail.Creer();
            var etat = Amelioree(nue, new Dictionary<string, int>
            {
                [AmeliorationsDeRenaissance.GLOBALE_ID] = 1,
                [AmeliorationsDeRenaissance.CibleeDe(Vairon.Id).Id] = 1,
            });
            var rapport = Economie.ProductionDeLEspece(etat, Vairon).Div(Economie.ProductionDeLEspece(nue, Vairon)).ToNumber();
            var attendu =
                (Economie.DebitBaseDeLEspece(Vairon).ToNumber() + Constantes.AMELIORATION_GLOBALE_PAR_RANG) /
                Economie.DebitBaseDeLEspece(Vairon).ToNumber() *
                (1 + Constantes.AMELIORATION_CIBLEE_PAR_RANG);
            Assert.That(rapport, Is.EqualTo(attendu).Within(0.5e-9));
        }

        [Test, Description("améliorer ne renchérit pas le niveau : le coût suit le débit NON amélioré")]
        public void AcheterUneAmelioration_ne_rencherit_pas_le_niveau_le_cout_suit_le_debit_NON_ameliore()
        {
            var nue = EtatDeTravail.Creer();
            var etat = Amelioree(nue, new Dictionary<string, int> { [AmeliorationsDeRenaissance.GLOBALE_ID] = 5 });
            Assert.That(Economie.CoutDeNiveau(etat, Vairon, 7).Eq(Economie.CoutDeNiveau(nue, Vairon, 7)), Is.True);
        }

        [Test, Description("le coût en Souffle est géométrique, et la globale et la ciblée ont chacune leur base")]
        public void Le_cout_en_Souffle_est_geometrique_et_la_globale_et_la_ciblee_ont_chacune_leur_base()
        {
            var etat = Reducteur.EtatInitial(1);
            var globale = AmeliorationsDeRenaissance.ParId(AmeliorationsDeRenaissance.GLOBALE_ID);
            var ciblee = AmeliorationsDeRenaissance.CibleeDe(Vairon.Id);
            Assert.That(Economie.CoutDAmelioration(etat, globale).ToNumber(),
                Is.EqualTo(Constantes.SOUFFLE_COUT_D_AMELIORATION_GLOBALE).Within(0.5e-9));
            Assert.That(Economie.CoutDAmelioration(etat, ciblee).ToNumber(),
                Is.EqualTo(Constantes.SOUFFLE_COUT_D_AMELIORATION_CIBLEE).Within(0.5e-9));
            var deuxRangs = Amelioree(etat, new Dictionary<string, int> { [ciblee.Id] = 2 });
            Assert.That(Economie.CoutDAmelioration(deuxRangs, ciblee).ToNumber(),
                Is.EqualTo(Constantes.SOUFFLE_COUT_D_AMELIORATION_CIBLEE * Math.Pow(Constantes.RATIO_COUT_D_AMELIORATION, 2)).Within(0.5e-9));
        }

        [Test, Description("le détail de captation nomme les deux termes, chacun à sa source")]
        public void Le_detail_de_captation_nomme_les_deux_termes_chacun_a_sa_source()
        {
            var etat = Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int>
            {
                [AmeliorationsDeRenaissance.GLOBALE_ID] = 2,
                [AmeliorationsDeRenaissance.CibleeDe(Vairon.Id).Id] = 1,
            });
            var lignes = Economie.DetailDeCaptation(etat, Vairon);
            var globale = lignes.FirstOrDefault(l => l.Terme == TermeDeFormule.AmeliorationGlobale);
            var ciblee = lignes.FirstOrDefault(l => l.Terme == TermeDeFormule.MultiplicateurAmelioration);
            var attenduRatioGlobale = Economie.DebitAmeliore(etat, Vairon).Div(Economie.DebitBaseDeLEspece(Vairon)).ToNumber();
            Assert.That(globale?.Valeur, Is.EqualTo(attenduRatioGlobale).Within(0.5e-9));
            Assert.That(globale?.Source, Is.EqualTo(new SourceDeTerme(QuoiSource.AmeliorationDeRenaissance, 2)));
            Assert.That(ciblee?.Valeur, Is.EqualTo(1 + Constantes.AMELIORATION_CIBLEE_PAR_RANG).Within(0.5e-12));
            Assert.That(ciblee?.Source, Is.EqualTo(new SourceDeTerme(QuoiSource.AmeliorationDeRenaissance, 1)));
        }

        [Test, Description("le produit des lignes de détail reste exact quand la globale est active")]
        public void Le_produit_des_lignes_de_detail_reste_exact_quand_la_globale_est_active()
        {
            // Régression : `amelioration_globale` comptait deux fois son effet (une
            // fois figé dans `taux_base`, une fois comme ligne additive) — le produit
            // valait 0.15 au lieu de 1.00 quand une globale était active. Aucun test,
            // ni ici ni dans `CaptationTests`, n'exerçait alors cette combinaison.
            var etat = Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int> { [AmeliorationsDeRenaissance.GLOBALE_ID] = 3 });
            var lignes = Economie.DetailDeCaptation(etat, Vairon);
            var produit = lignes.Aggregate(new Decimal(1), (acc, ligne) => acc.Mul(ligne.Valeur));
            Comparateur.ComparerATolerance(produit, Economie.ProductionDeLEspece(etat, Vairon));
        }

        /* ─── B3 — améliorer ──────────────────────────────────────────────────── */

        [Test, Description("paie le Souffle, monte le rang d’un")]
        public void Paie_le_Souffle_monte_le_rang_d_un()
        {
            var riche = Reducteur.EtatInitial(1) with { Permanent = Reducteur.EtatInitial(1).Permanent with { Souffle = new Decimal(100) } };
            var globale = AmeliorationsDeRenaissance.ParId(AmeliorationsDeRenaissance.GLOBALE_ID);
            var prix = Economie.CoutDAmelioration(riche, globale);
            var apres = Reducteur.AcheterUneAmelioration(riche, globale.Id);
            Assert.That(Economie.RangDAmelioration(apres, globale.Id), Is.EqualTo(1));
            Assert.That(apres.Permanent.Souffle.Eq(riche.Permanent.Souffle.Sub(prix)), Is.True);
            // Le mana n'est pas touché : le Souffle n'est pas une seconde monnaie de mana.
            Assert.That(apres.Cycle.ManaCourant.Eq(riche.Cycle.ManaCourant), Is.True);
        }

        [Test, Description("refuse sans rien changer si le Souffle manque, ou si l’identifiant est inconnu")]
        public void Refuse_sans_rien_changer_si_le_Souffle_manque_ou_si_l_identifiant_est_inconnu()
        {
            var pauvre = Reducteur.EtatInitial(1);
            Assert.That(pauvre.Permanent.Souffle.Eq(0), Is.True);
            Assert.That(Reducteur.AcheterUneAmelioration(pauvre, AmeliorationsDeRenaissance.GLOBALE_ID), Is.SameAs(pauvre));
            var riche = pauvre with { Permanent = pauvre.Permanent with { Souffle = new Decimal(100) } };
            Assert.That(Reducteur.AcheterUneAmelioration(riche, "amelioration-qui-n-existe-pas"), Is.SameAs(riche));
        }

        [Test, Description("le rang traverse la renaissance — c’est permanent")]
        public void Le_rang_traverse_la_renaissance_c_est_permanent()
        {
            var avecUneCiblee = Reducteur.AcheterUneAmelioration(
                EtatDeTravail.Creer() with { Permanent = EtatDeTravail.Creer().Permanent with { Souffle = new Decimal(1000) } },
                AmeliorationsDeRenaissance.CibleeDe(Vairon.Id).Id);
            Assert.That(Economie.RangDAmelioration(avecUneCiblee, AmeliorationsDeRenaissance.CibleeDe(Vairon.Id).Id), Is.EqualTo(1));
            Assert.That(Renaissance.Renaitre(avecUneCiblee).Permanent.AmeliorationsDeRenaissance, Is.EqualTo(avecUneCiblee.Permanent.AmeliorationsDeRenaissance));
        }

        [Test, Description("les rangs sont sérialisés dans l’ordre du registre, pas de l’achat")]
        public void Les_rangs_sont_serialises_dans_l_ordre_du_registre_pas_de_l_achat()
        {
            // Deux parties qui améliorent les mêmes choses dans un ordre différent
            // doivent produire la même chaîne de save (déterminisme).
            var riche = Reducteur.EtatInitial(1) with { Permanent = Reducteur.EtatInitial(1).Permanent with { Souffle = new Decimal(1e6) } };
            var a = Reducteur.AcheterUneAmelioration(Reducteur.AcheterUneAmelioration(riche, "amelioration-loche"), AmeliorationsDeRenaissance.GLOBALE_ID);
            var b = Reducteur.AcheterUneAmelioration(Reducteur.AcheterUneAmelioration(riche, AmeliorationsDeRenaissance.GLOBALE_ID), "amelioration-loche");
            Assert.That(a.Permanent.AmeliorationsDeRenaissance.Keys.ToArray(), Is.EqualTo(b.Permanent.AmeliorationsDeRenaissance.Keys.ToArray()));
        }

        /* ─── L'amélioration de séjour — 2026-11-13 ───────────────────────────── */

        static readonly AmeliorationDeRenaissance Sejour = AmeliorationsDeRenaissance.ParId(AmeliorationsDeRenaissance.SEJOUR_ID);

        static EtatJeu AvecDuSouffle(EtatJeu etat, string souffle) =>
            etat with { Permanent = etat.Permanent with { Souffle = Decimal.Parse(souffle) } };

        [Test, Description("sans rang de séjour, le temps du séjour est exactement celui d'avant")]
        public void Sans_rang_de_sejour_le_temps_du_sejour_ne_bouge_pas()
        {
            var etat = EtatDeTravail.Creer();
            Assert.That(Economie.FacteurDuSejour(etat), Is.EqualTo(1));
            Assert.That(Reducteur.TauDuSejourSecondes(etat), Is.EqualTo(
                Constantes.TAU_SEJOUR_HEURES * 3600 * Math.Pow(etat.Reglage.CroissanceDuSejourParPalier, etat.Permanent.ProfondeurMaxAtteinte)));
        }

        [Test, Description("chaque rang de séjour raccourcit le temps du séjour de sa part, et rien d'autre ne bouge")]
        public void Chaque_rang_de_sejour_raccourcit_le_temps_du_sejour()
        {
            var sans = EtatDeTravail.Creer();
            var avec = Amelioree(sans, new Dictionary<string, int> { [Sejour.Id] = 3 });
            Assert.That(Reducteur.TauDuSejourSecondes(avec), Is.EqualTo(
                Reducteur.TauDuSejourSecondes(sans) * Math.Pow(1 - Constantes.AMELIORATION_DE_SEJOUR_PAR_RANG, 3)).Within(1e-9));
            // Ni la production ni la valeur vers laquelle l'acquis sature : seulement le temps.
            Assert.That(Economie.ProductionTotaleParSeconde(avec).ToString(), Is.EqualTo(Economie.ProductionTotaleParSeconde(sans).ToString()));
            Assert.That(Reducteur.Tick(avec, 1e9).Cycle.AcquisDeSejour, Is.EqualTo(Constantes.ACQUIS_MAX).Within(1e-9));
            Assert.That(Reducteur.Tick(avec, 3600).Cycle.AcquisDeSejour, Is.GreaterThan(Reducteur.Tick(sans, 3600).Cycle.AcquisDeSejour));
        }

        [Test, Description("le séjour a un dernier rang : au-delà, l'achat est refusé et le Souffle reste")]
        public void Le_sejour_a_un_dernier_rang()
        {
            var plein = AvecDuSouffle(
                Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int> { [Sejour.Id] = Constantes.RANG_MAX_DE_L_AMELIORATION_DE_SEJOUR }),
                "1e300");
            Assert.That(Economie.AmeliorationAuMaximum(plein, Sejour), Is.True);
            Assert.That(Reducteur.AcheterUneAmelioration(plein, Sejour.Id), Is.SameAs(plein));
            // Les autres n'ont pas de dernier rang.
            Assert.That(Economie.AmeliorationAuMaximum(plein, AmeliorationsDeRenaissance.CibleeDe(Vairon.Id)), Is.False);
            // Le temps du séjour est donc borné par le bas.
            Assert.That(Economie.FacteurDuSejour(plein), Is.EqualTo(
                Math.Pow(1 - Constantes.AMELIORATION_DE_SEJOUR_PAR_RANG, Constantes.RANG_MAX_DE_L_AMELIORATION_DE_SEJOUR)).Within(1e-12));
            Assert.That(Economie.FacteurDuSejour(plein), Is.GreaterThan(0.1));
        }

        [Test, Description("le séjour se paie en Souffle, à son propre prix et à son propre ratio")]
        public void Le_sejour_se_paie_en_Souffle_a_son_propre_ratio()
        {
            var riche = AvecDuSouffle(EtatDeTravail.Creer(), "1e9");
            var premier = Economie.CoutDAmelioration(riche, Sejour);
            var apres = Reducteur.AcheterUneAmelioration(riche, Sejour.Id);
            Assert.That(Economie.RangDAmelioration(apres, Sejour.Id), Is.EqualTo(1));
            Assert.That(apres.Permanent.Souffle.ToNumber(), Is.EqualTo(riche.Permanent.Souffle.Sub(premier).ToNumber()).Within(1e-3));
            Assert.That(Economie.CoutDAmelioration(apres, Sejour).Div(premier).ToNumber(),
                Is.EqualTo(Constantes.RATIO_COUT_D_AMELIORATION_DE_SEJOUR).Within(1e-9));
            // Trop pauvre : refusé.
            var pauvre = AvecDuSouffle(EtatDeTravail.Creer(), "1");
            Assert.That(Reducteur.AcheterUneAmelioration(pauvre, Sejour.Id), Is.SameAs(pauvre));
        }

        [Test, Description("avec des rangs de séjour, 480 pas de 60 s valent toujours un pas de 8 h")]
        public void Avec_des_rangs_de_sejour_l_equivalence_de_pas_tient()
        {
            var depart = Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int> { [Sejour.Id] = 4 });
            var enUnPas = Reducteur.Tick(depart, 8 * 3600);
            var enPetitsPas = depart;
            for (var i = 0; i < 480; i += 1) enPetitsPas = Reducteur.Tick(enPetitsPas, 60);
            Assert.That(enPetitsPas.Cycle.AcquisDeSejour, Is.EqualTo(enUnPas.Cycle.AcquisDeSejour).Within(1e-9));
            Assert.That(enPetitsPas.Cycle.ManaCourant.Div(enUnPas.Cycle.ManaCourant).ToNumber(), Is.EqualTo(1).Within(1e-9));
        }

        [Test, Description("le rang de séjour traverse la renaissance et la sauvegarde")]
        public void Le_rang_de_sejour_traverse_la_renaissance()
        {
            var avec = Amelioree(EtatDeTravail.Creer(), new Dictionary<string, int> { [Sejour.Id] = 2 });
            Assert.That(Economie.RangDAmelioration(Renaissance.Renaitre(avec), Sejour.Id), Is.EqualTo(2));
        }
    }
}
