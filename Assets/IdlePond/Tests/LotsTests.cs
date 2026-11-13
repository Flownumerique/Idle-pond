using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les achats en lot (2026-11-13) : monter jusqu'au seuil, tout améliorer. Un lot n'est
    /// pas une règle de plus — c'est l'achat simple, répété. Ces tests le vérifient en
    /// comparant chaque lot à la même suite d'achats faits un à un.
    /// </summary>
    public class LotsTests
    {
        static readonly Espece Vairon = Especes.Toutes[0];

        static EtatJeu Riche(string mana)
        {
            var etat = EtatDeTravail.Creer(contenance: "1e300");
            return etat with { Cycle = etat.Cycle with { ManaCourant = Decimal.Parse(mana) } };
        }

        static EtatJeu AuNiveau(EtatJeu etat, string espece, int niveau)
        {
            var especes = etat.Cycle.Especes.ToDictionary(p => p.Key, p => p.Value);
            especes[espece] = new EtatEspece(true, niveau);
            return etat with { Cycle = etat.Cycle with { Especes = especes } };
        }

        [Test, Description("le seuil visé est le prochain seuil de jalon, puis la centaine suivante")]
        public void Le_seuil_vise_est_le_prochain_jalon_puis_la_centaine_suivante()
        {
            Assert.That(Economie.ProchainSeuilDeNiveau(1), Is.EqualTo(10));
            Assert.That(Economie.ProchainSeuilDeNiveau(10), Is.EqualTo(25));
            Assert.That(Economie.ProchainSeuilDeNiveau(49), Is.EqualTo(50));
            Assert.That(Economie.ProchainSeuilDeNiveau(99), Is.EqualTo(100));
            Assert.That(Economie.ProchainSeuilDeNiveau(100), Is.EqualTo(200));
            Assert.That(Economie.ProchainSeuilDeNiveau(250), Is.EqualTo(300));
        }

        [Test, Description("monter jusqu'au seuil vaut exactement les mêmes crans achetés un à un")]
        public void Monter_jusqu_au_seuil_vaut_les_memes_crans_un_a_un()
        {
            var depart = AuNiveau(Riche("1e200"), Vairon.Id, 3);
            var enLot = Reducteur.AmeliorerJusquAuSeuil(depart, Vairon.Id);

            var unAUn = depart;
            for (var i = 0; i < 7; i += 1) unAUn = Reducteur.Ameliorer(unAUn, Vairon.Id);

            Assert.That(enLot.Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(10));
            Assert.That(enLot.Cycle.ManaCourant.ToString(), Is.EqualTo(unAUn.Cycle.ManaCourant.ToString()));
            Assert.That(enLot.Permanent.CompteursTechnique[BrancheTechnique.Amelioration],
                Is.EqualTo(unAUn.Permanent.CompteursTechnique[BrancheTechnique.Amelioration]));
        }

        [Test, Description("le lot annoncé est celui qui est acheté : mêmes crans, même prix")]
        public void Le_lot_annonce_est_celui_qui_est_achete()
        {
            var depart = AuNiveau(Riche("1e200"), Vairon.Id, 12);
            var lot = Economie.LotDeNiveaux(depart, Vairon);
            var apres = Reducteur.AmeliorerJusquAuSeuil(depart, Vairon.Id);

            Assert.That(lot.Crans, Is.EqualTo(13));
            Assert.That(apres.Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(25));
            // Le prix se relit sur la somme des crans, pas sur la différence de deux stocks
            // de 1e200, où il serait noyé dans l'arrondi.
            var somme = Decimal.Zero;
            for (var niveau = 12; niveau < 25; niveau += 1) somme = somme.Add(Economie.CoutDeNiveau(depart, Vairon, niveau));
            Assert.That(lot.Cout.Div(somme).ToNumber(), Is.EqualTo(1).Within(1e-12));
        }

        [Test, Description("un lot s'arrête au dernier cran payable, sans dépasser le mana")]
        public void Un_lot_s_arrete_au_dernier_cran_payable()
        {
            var auNiveau = AuNiveau(Riche("1e200"), Vairon.Id, 12);
            // De quoi payer trois crans, pas quatre.
            var troisCrans = Economie.CoutDeNiveau(auNiveau, Vairon, 12)
                .Add(Economie.CoutDeNiveau(auNiveau, Vairon, 13))
                .Add(Economie.CoutDeNiveau(auNiveau, Vairon, 14));
            var depart = auNiveau with { Cycle = auNiveau.Cycle with { ManaCourant = troisCrans.Mul(1.01) } };

            Assert.That(Economie.LotDeNiveaux(depart, Vairon).Crans, Is.EqualTo(3));
            var apres = Reducteur.AmeliorerJusquAuSeuil(depart, Vairon.Id);
            Assert.That(apres.Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(15));
            Assert.That(apres.Cycle.ManaCourant.Gte(0), Is.True);
        }

        [Test, Description("sans mana pour un seul cran, le lot est vide, montre le prix du cran, et rien ne change")]
        public void Sans_mana_le_lot_est_vide_et_rien_ne_change()
        {
            var auNiveau = AuNiveau(Riche("1e200"), Vairon.Id, 12);
            var depart = auNiveau with { Cycle = auNiveau.Cycle with { ManaCourant = Decimal.Zero } };
            var lot = Economie.LotDeNiveaux(depart, Vairon);
            Assert.That(lot.Crans, Is.EqualTo(0));
            Assert.That(lot.Cout.ToString(), Is.EqualTo(Economie.CoutDeNiveau(depart, Vairon, 12).ToString()));
            Assert.That(Reducteur.AmeliorerJusquAuSeuil(depart, Vairon.Id), Is.SameAs(depart));
        }

        [Test, Description("une espèce inconnue ou pas encore convaincue ne se monte pas en lot")]
        public void Une_espece_pas_convaincue_ne_se_monte_pas_en_lot()
        {
            var depart = Reducteur.EtatInitial(1);
            Assert.That(Reducteur.AmeliorerJusquAuSeuil(depart, Vairon.Id), Is.SameAs(depart));
            Assert.That(Reducteur.AmeliorerJusquAuSeuil(depart, "inconnue"), Is.SameAs(depart));
            Assert.That(Reducteur.AmeliorerJusquAuSeuil(depart, null), Is.SameAs(depart));
            Assert.That(Economie.LotDeNiveaux(depart, Vairon).Crans, Is.EqualTo(0));
        }

        [Test, Description("tout améliorer dépense le Souffle jusqu'à ne plus pouvoir payer le rang le moins cher")]
        public void Tout_ameliorer_depense_jusqu_au_dernier_rang_payable()
        {
            var depart = EtatDeTravail.Creer();
            depart = depart with { Permanent = depart.Permanent with { Souffle = new Decimal(5000) } };
            var apres = Reducteur.AcheterToutesLesAmeliorations(depart);

            Assert.That(apres.Permanent.AmeliorationsDeRenaissance.Values.Sum(), Is.GreaterThan(1));
            Assert.That(apres.Permanent.Souffle.Gte(0), Is.True);
            foreach (var amelioration in AmeliorationsDeRenaissance.Toutes)
            {
                if (Economie.AmeliorationAuMaximum(apres, amelioration)) continue;
                Assert.That(Economie.CoutDAmelioration(apres, amelioration).Gt(apres.Permanent.Souffle), Is.True, amelioration.Id);
            }
            // Le séjour en fait partie, et ne dépasse jamais son dernier rang.
            var tresRiche = depart with { Permanent = depart.Permanent with { Souffle = Decimal.Parse("1e80") } };
            var comble = Reducteur.AcheterToutesLesAmeliorations(tresRiche);
            Assert.That(Economie.RangDAmelioration(comble, AmeliorationsDeRenaissance.SEJOUR_ID),
                Is.EqualTo(Constantes.RANG_MAX_DE_L_AMELIORATION_DE_SEJOUR));
        }

        [Test, Description("tout améliorer sans Souffle ne change rien")]
        public void Tout_ameliorer_sans_Souffle_ne_change_rien()
        {
            var depart = Reducteur.EtatInitial(1);
            Assert.That(Reducteur.AcheterToutesLesAmeliorations(depart), Is.SameAs(depart));
        }
    }
}
