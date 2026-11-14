using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Toucher la mare — spec 2026-10-11, amendée le 2026-11-13 (sans l'achat groupé).
    ///
    /// Le héros devient actif : ce qu'il capte, il le capte quand on touche. Son ancien débit
    /// passif quitte le héros et devient un AMBIANT fixe, indépendant du niveau, qui garde le
    /// trou dégénéré du finding 3 fermé. Une amélioration de renaissance rend le toucher plus fort.
    /// </summary>
    public class ToucherTests
    {
        static EtatJeu AuNiveau(EtatJeu etat, int niveauDuHeros) =>
            etat with { Cycle = etat.Cycle with { NiveauDuHeros = niveauDuHeros } };

        static EtatJeu AvecMana(EtatJeu etat, Decimal mana) =>
            etat with { Cycle = etat.Cycle with { ManaCourant = mana } };

        static EtatJeu AuRangDuToucher(EtatJeu etat, int rang) =>
            etat with
            {
                Permanent = etat.Permanent with
                {
                    AmeliorationsDeRenaissance = new Dictionary<string, int> { [AmeliorationsDeRenaissance.TOUCHER_ID] = rang },
                },
            };

        /* ─── l'ambiant ───────────────────────────────────────────────────────────── */

        [Test, Description("l'ambiant vaut DEBIT_AMBIANT fois les multiplicateurs globaux, sans facteur de niveau")]
        public void L_ambiant_ne_porte_aucun_facteur_de_niveau()
        {
            foreach (var niveau in new[] { 1, 3, 7 })
            {
                var etat = AuNiveau(EtatDeTravail.Creer(), niveau);
                var attendu = new Decimal(Constantes.DEBIT_AMBIANT).Mul(Economie.MultiplicateursGlobaux(etat));
                Assert.That(Economie.ProductionAmbiante(etat).Eq(attendu), Is.True, $"niveau {niveau}");
            }
        }

        [Test, Description("le total est la somme des espèces plus l'ambiant")]
        public void Le_total_est_la_somme_des_especes_plus_l_ambiant()
        {
            var etat = AuNiveau(EtatDeTravail.Creer(), 4);
            var especes = Especes.Toutes.Aggregate(new Decimal(0), (s, e) => s.Add(Economie.ProductionDeLEspece(etat, e)));
            Comparateur.ComparerATolerance(especes.Add(Economie.ProductionAmbiante(etat)), Economie.ProductionTotaleParSeconde(etat));
        }

        [Test, Description("le détail de l'ambiant nomme debit_ambiant ; celui du héros ne porte plus que son multiplicateur")]
        public void Les_details_de_l_ambiant_et_du_heros()
        {
            var etat = AuNiveau(Reducteur.EtatInitial(1), 4);
            var ambiant = Economie.DetailDeLAmbiant(etat);
            Assert.That(ambiant.Select(l => l.Terme), Is.EqualTo(new[] { TermeDeFormule.DebitAmbiant }));
            Assert.That(ambiant[0].Valeur, Is.EqualTo(Constantes.DEBIT_AMBIANT));
            Assert.That(Economie.DetailDuHeros(etat).Select(l => l.Terme), Is.EqualTo(new[] { TermeDeFormule.MultiplicateurHeros }));
        }

        [Test, Description("debit_ambiant et les termes du toucher sont des termes de production")]
        public void Les_termes_du_toucher_sont_de_production()
        {
            Assert.That(Termes.Identifiant(TermeDeFormule.DebitAmbiant), Is.EqualTo("debit_ambiant"));
            Assert.That(Termes.DE_PRODUCTION, Has.Member(TermeDeFormule.DebitAmbiant));
            Assert.That(Termes.DE_PRODUCTION, Has.Member(TermeDeFormule.BaseDuToucher));
            Assert.That(Termes.DE_PRODUCTION, Has.Member(TermeDeFormule.MultiplicateurDuToucher));
        }

        /* ─── la valeur d'un toucher ──────────────────────────────────────────────── */

        [Test, Description("un toucher vaut base × niveau × multiplicateurs globaux × multiplicateur du toucher")]
        public void La_valeur_du_toucher()
        {
            var etat = AuRangDuToucher(AuNiveau(EtatDeTravail.Creer(), 5), 2);
            var attendu = new Decimal(Constantes.BASE_DU_TOUCHER).Mul(5)
                .Mul(Economie.MultiplicateursGlobaux(etat)).Mul(Economie.MultiplicateurDuToucher(etat));
            Comparateur.ComparerATolerance(attendu, Economie.ValeurDuToucher(etat));
        }

        [Test, Description("le produit des lignes du détail du toucher égale sa valeur")]
        public void Le_detail_du_toucher_explique_sa_valeur()
        {
            var etat = AuRangDuToucher(AuNiveau(EtatDeTravail.Creer(), 6), 3);
            var produit = Economie.DetailDuToucher(etat).Aggregate(new Decimal(1), (acc, l) => acc.Mul(l.Valeur));
            Comparateur.ComparerATolerance(produit, Economie.ValeurDuToucher(etat));
            Assert.That(Economie.DetailDuToucher(etat).All(l => Termes.EstDeProduction(l.Terme)), Is.True);
        }

        [Test, Description("le multiplicateur du toucher vaut 1 au rang 0, (1 + part)^rang ensuite")]
        public void Le_multiplicateur_du_toucher_suit_le_rang()
        {
            Assert.That(Economie.MultiplicateurDuToucher(Reducteur.EtatInitial(1)), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDuToucher(AuRangDuToucher(Reducteur.EtatInitial(1), 3)),
                Is.EqualTo(Math.Pow(1 + Constantes.PART_PAR_RANG_DU_TOUCHER, 3)).Within(1e-12));
        }

        /* ─── toucher ─────────────────────────────────────────────────────────────── */

        [Test, Description("toucher ajoute la valeur du toucher au mana, et la rend")]
        public void Toucher_ajoute_la_valeur_au_mana()
        {
            var etat = AvecMana(AuNiveau(EtatDeTravail.Creer(), 3), new Decimal(100));
            var valeur = Economie.ValeurDuToucher(etat);
            var (apres, gain) = Reducteur.Toucher(etat);
            Assert.That(gain.Eq(valeur), Is.True);
            Assert.That(apres.Cycle.ManaCourant.Eq(new Decimal(100).Add(valeur)), Is.True);
        }

        [Test, Description("le gain est plafonné par la place qui reste sous la contenance")]
        public void Le_gain_est_plafonne_par_la_contenance()
        {
            // Une petite contenance : à 1e14, la moitié d'un toucher se perd dans la mantisse.
            var etat = EtatDeTravail.Creer();
            etat = etat with { Permanent = etat.Permanent with { ContenanceMana = new Decimal(1000) } };
            var contenance = Economie.Contenance(etat);
            var presquePlein = AvecMana(etat, contenance.Sub(Economie.ValeurDuToucher(etat).Div(2)));
            var (apres, gain) = Reducteur.Toucher(presquePlein);
            Assert.That(apres.Cycle.ManaCourant.Eq(contenance), Is.True);
            Comparateur.ComparerATolerance(Economie.ValeurDuToucher(etat).Div(2), gain);
        }

        [Test, Description("mare pleine : l'état revient inchangé, gain nul")]
        public void Mare_pleine_rien_ne_change()
        {
            var etat = EtatDeTravail.Creer();
            var plein = AvecMana(etat, Economie.Contenance(etat));
            var (apres, gain) = Reducteur.Toucher(plein);
            Assert.That(apres, Is.SameAs(plein));
            Assert.That(gain.Eq(0), Is.True);
        }

        [Test, Description("un toucher n'est pas une dépense : aucun compteur de technique ne bouge")]
        public void Toucher_ne_credite_aucun_compteur()
        {
            var etat = AvecMana(EtatDeTravail.Creer(), new Decimal(0));
            var (apres, _) = Reducteur.Toucher(etat);
            Assert.That(apres.Permanent, Is.SameAs(etat.Permanent));
        }

        /* ─── l'amélioration du toucher ───────────────────────────────────────────── */

        [Test, Description("le registre porte amelioration-toucher, en queue, de portée Toucher")]
        public void Le_registre_porte_l_amelioration_du_toucher()
        {
            var toucher = AmeliorationsDeRenaissance.ParId("amelioration-toucher");
            Assert.That(toucher, Is.Not.Null);
            Assert.That(toucher.Portee, Is.EqualTo(PorteeDAmelioration.Toucher));
            Assert.That(AmeliorationsDeRenaissance.Toutes.Last(), Is.SameAs(toucher));
        }

        [Test, Description("son prix en Souffle est géométrique, base × ratio^rang")]
        public void Son_prix_est_geometrique()
        {
            var toucher = AmeliorationsDeRenaissance.ParId(AmeliorationsDeRenaissance.TOUCHER_ID);
            var zero = Reducteur.EtatInitial(1);
            Assert.That(Economie.CoutDAmelioration(zero, toucher).ToNumber(),
                Is.EqualTo(Constantes.SOUFFLE_COUT_D_AMELIORATION_DU_TOUCHER).Within(1e-9));
            Assert.That(Economie.CoutDAmelioration(AuRangDuToucher(zero, 2), toucher).ToNumber(),
                Is.EqualTo(Constantes.SOUFFLE_COUT_D_AMELIORATION_DU_TOUCHER * Math.Pow(Constantes.RATIO_COUT_D_AMELIORATION_DU_TOUCHER, 2)).Within(1e-6));
        }

        [Test, Description("l'acheter monte son rang et rend le toucher plus fort, sans changer la production")]
        public void L_acheter_rend_le_toucher_plus_fort()
        {
            var etat = EtatDeTravail.Creer();
            etat = etat with { Permanent = etat.Permanent with { Souffle = new Decimal(1e9) } };
            var apres = Reducteur.AcheterUneAmelioration(etat, AmeliorationsDeRenaissance.TOUCHER_ID);
            Assert.That(Economie.RangDAmelioration(apres, AmeliorationsDeRenaissance.TOUCHER_ID), Is.EqualTo(1));
            Assert.That(Economie.ValeurDuToucher(apres).Gt(Economie.ValeurDuToucher(etat)), Is.True);
            Assert.That(Economie.ProductionTotaleParSeconde(apres).Eq(Economie.ProductionTotaleParSeconde(etat)), Is.True);
        }

        /* ─── le geste dans le jeu ────────────────────────────────────────────────── */

        const long DEPART = 1_700_000_000_000;

        [Test, Description("Partie.Toucher applique le toucher, rend le gain et émet EtatChange")]
        public void Partie_toucher_rend_le_gain()
        {
            var etat = AvecMana(EtatDeTravail.Creer(), new Decimal(0));
            var partie = new Partie(new HorlogeFigee(DEPART), etat);
            var emis = 0;
            partie.EtatChange += _ => emis += 1;
            var gain = partie.Toucher();
            Assert.That(gain.Eq(Economie.ValeurDuToucher(etat)), Is.True);
            Assert.That(partie.Etat.Cycle.ManaCourant.Eq(gain), Is.True);
            Assert.That(emis, Is.EqualTo(1));
        }

        [Test, Description("le 11e toucher dans la même seconde est ignoré ; la fenêtre glisse")]
        public void Au_plus_dix_touchers_par_seconde()
        {
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, AvecMana(EtatDeTravail.Creer(), new Decimal(0)));
            for (var i = 0; i < 10; i++)
            {
                Assert.That(partie.Toucher().Gt(0), Is.True, $"toucher {i + 1}");
                horloge.AvancerMs(50);
            }
            var emis = 0;
            partie.EtatChange += _ => emis += 1;
            var mana = partie.Etat.Cycle.ManaCourant;
            Assert.That(partie.Toucher().Eq(0), Is.True);
            Assert.That(partie.Etat.Cycle.ManaCourant.Eq(mana), Is.True);
            Assert.That(emis, Is.EqualTo(0));
            // Le premier toucher est à DEPART ; une seconde après lui, sa place se libère.
            horloge.AvancerMs(1000 - 500);
            Assert.That(partie.Toucher().Gt(0), Is.True);
        }
    }
}
