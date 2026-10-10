using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les bonus de lieu et les techniques — spec du 2026-10-10, l'onglet « Débloquer ».
    ///
    /// Des achats au mana, perdus à la renaissance. Un bonus de lieu ne touche que son lieu ;
    /// une technique vaut partout. Les règles du canon qui les bornent sont dans CanonTests.
    /// </summary>
    public class BonusTests
    {
        static Bonus B(string id) => RegistreDesBonus.ParId(id);

        static EtatJeu AvecRangs(EtatJeu etat, params (string Id, int Rang)[] rangs)
        {
            var table = new Dictionary<string, int>();
            foreach (var paire in etat.Cycle.Bonus) table[paire.Key] = paire.Value;
            foreach (var (id, rang) in rangs) table[id] = rang;
            return etat with { Cycle = etat.Cycle with { Bonus = table } };
        }

        /// Une partie neuve, chargée de mana et creusée jusqu'au deuxième palier du Gour.
        static EtatJeu Creusee(int paliers = 8)
        {
            var etat = Reducteur.EtatInitial(7);
            etat = etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e15") },
                Permanent = etat.Permanent with { ContenanceMana = Decimal.Parse("1e18") },
            };
            while (etat.Cycle.PaliersOuverts < paliers) etat = Reducteur.Creuser(etat);
            return etat;
        }

        /* ─── Le registre ─────────────────────────────────────────────────────── */

        [Test, Description("chaque bonus a un identifiant unique, un lieu qui existe, et un texte")]
        public void Chaque_bonus_a_un_identifiant_unique_un_lieu_qui_existe_et_un_texte()
        {
            Assert.That(RegistreDesBonus.Tous.Select(b => b.Id).Distinct().Count(), Is.EqualTo(RegistreDesBonus.Tous.Count));
            foreach (var bonus in RegistreDesBonus.Tous)
            {
                if (bonus.Assise != null) Assert.That(Assises.ParId(bonus.Assise), Is.Not.Null, bonus.Id);
                Assert.That(Textes.DuBonus(bonus.Id), Is.Not.SameAs(Textes.BONUS_INCONNU), bonus.Id);
                Assert.That(bonus.RangMax, Is.GreaterThanOrEqualTo(1), bonus.Id);
                Assert.That(bonus.Part, Is.GreaterThanOrEqualTo(0), bonus.Id);
                Assert.That(bonus.Part, Is.LessThan(1), bonus.Id);
                Assert.That(bonus.PalierDePrix, Is.GreaterThanOrEqualTo(0), bonus.Id);
                Assert.That(bonus.PalierDePrix, Is.LessThan(Constantes.NOMBRE_DE_PALIERS), bonus.Id);
            }
        }

        [Test, Description("seuls les lieux livrés portent des bonus")]
        public void Seuls_les_lieux_livres_portent_des_bonus()
        {
            foreach (var bonus in RegistreDesBonus.Tous.Where(b => b.Assise != null))
                Assert.That(Assises.ParId(bonus.Assise).IndexPremierPalier, Is.LessThan(Assises.PALIERS_LIVRES), bonus.Id);
        }

        /* ─── L'achat ─────────────────────────────────────────────────────────── */

        [Test, Description("une vie neuve n'a aucun bonus")]
        public void Une_vie_neuve_n_a_aucun_bonus()
        {
            Assert.That(Reducteur.EtatInitial(1).Cycle.Bonus, Is.Empty);
        }

        [Test, Description("acheter un rang retire son prix, et le prix suivant est multiplié par le ratio")]
        public void Acheter_un_rang_retire_son_prix_et_le_prix_suivant_est_multiplie_par_le_ratio()
        {
            var avant = Creusee();
            var bonus = B("noue-vase");
            var cout = Economie.CoutDeBonus(avant, bonus);
            var apres = Reducteur.AcheterUnBonus(avant, bonus.Id);
            Assert.That(Economie.RangDeBonus(apres, bonus.Id), Is.EqualTo(1));
            Comparateur.ComparerATolerance(apres.Cycle.ManaCourant, avant.Cycle.ManaCourant.Sub(cout));
            Comparateur.ComparerATolerance(Economie.CoutDeBonus(apres, bonus), cout.Mul(Constantes.RATIO_COUT_DE_BONUS));
        }

        [Test, Description("le premier rang coûte ce que coûte creuser son palier de prix")]
        public void Le_premier_rang_coute_ce_que_coute_creuser_son_palier_de_prix()
        {
            foreach (var bonus in RegistreDesBonus.Tous)
                Comparateur.ComparerATolerance(
                    Economie.CoutDeBonus(Reducteur.EtatInitial(1), bonus),
                    Economie.CoutBaseDuPalier(bonus.PalierDePrix).Mul(Constantes.COUT_DE_BONUS_RELATIF));
        }

        [Test, Description("un bonus d'un lieu pas encore atteint est refusé, l'état revient inchangé")]
        public void Un_bonus_d_un_lieu_pas_encore_atteint_est_refuse()
        {
            var etat = Creusee(3);
            Assert.That(Economie.BonusOuvert(etat, B("gour-roche-tendre")), Is.False);
            Assert.That(Reducteur.AcheterUnBonus(etat, "gour-roche-tendre"), Is.SameAs(etat));
        }

        [Test, Description("un bonus s'ouvre à sa maîtrise : les paliers ouverts dans son lieu")]
        public void Un_bonus_s_ouvre_a_sa_maitrise()
        {
            var herbier = B("noue-herbier");
            var etat = Creusee(herbier.MaitriseRequise - 1 < 1 ? 1 : herbier.MaitriseRequise - 1);
            Assert.That(Economie.BonusOuvert(etat, herbier), Is.False);
            Assert.That(Reducteur.AcheterUnBonus(etat, herbier.Id), Is.SameAs(etat));
            etat = Reducteur.Creuser(etat);
            Assert.That(Economie.MaitriseDuLieu(etat, Assises.ParId("noue")), Is.EqualTo(herbier.MaitriseRequise));
            Assert.That(Economie.BonusOuvert(etat, herbier), Is.True);
        }

        [Test, Description("la maîtrise d'un lieu va de 0 à son nombre de paliers")]
        public void La_maitrise_d_un_lieu_va_de_0_a_son_nombre_de_paliers()
        {
            var noue = Assises.ParId("noue");
            var gour = Assises.ParId("gour");
            var etat = Creusee(8);
            Assert.That(Economie.MaitriseDuLieu(etat, noue), Is.EqualTo(noue.NombreDePaliers));
            Assert.That(Economie.MaitriseDuLieu(etat, gour), Is.EqualTo(2));
            Assert.That(Economie.MaitriseDuLieu(Creusee(2), gour), Is.EqualTo(0));
        }

        [Test, Description("un rang au maximum ne s'achète plus")]
        public void Un_rang_au_maximum_ne_s_achete_plus()
        {
            var bonus = B("noue-racines");
            var etat = AvecRangs(Creusee(), (bonus.Id, bonus.RangMax));
            Assert.That(Economie.BonusAuMaximum(etat, bonus), Is.True);
            Assert.That(Reducteur.AcheterUnBonus(etat, bonus.Id), Is.SameAs(etat));
        }

        [Test, Description("sans assez de mana, l'achat est refusé")]
        public void Sans_assez_de_mana_l_achat_est_refuse()
        {
            var etat = Creusee();
            etat = etat with { Cycle = etat.Cycle with { ManaCourant = new Decimal(0) } };
            Assert.That(Reducteur.AcheterUnBonus(etat, "noue-vase"), Is.SameAs(etat));
            Assert.That(Reducteur.AcheterUnBonus(etat, "inconnu"), Is.SameAs(etat));
        }

        [Test, Description("la table des rangs suit l'ordre du registre, pas celui des achats")]
        public void La_table_des_rangs_suit_l_ordre_du_registre()
        {
            var etat = Creusee();
            var a = Reducteur.AcheterUnBonus(Reducteur.AcheterUnBonus(etat, "technique-pelle"), "noue-vase");
            var b = Reducteur.AcheterUnBonus(Reducteur.AcheterUnBonus(etat, "noue-vase"), "technique-pelle");
            Assert.That(a.Cycle.Bonus.Keys.ToList(), Is.EqualTo(b.Cycle.Bonus.Keys.ToList()));
            Assert.That(a.Cycle.Bonus.Keys.First(), Is.EqualTo("noue-vase"));
        }

        /* ─── Les effets ──────────────────────────────────────────────────────── */

        [Test, Description("un bonus de production ne monte que les espèces de son lieu")]
        public void Un_bonus_de_production_ne_monte_que_les_especes_de_son_lieu()
        {
            var etat = EtatDeTravail.Creer();
            var vairon = Especes.ParId("vairon");
            var epinoche = Especes.ParId("epinoche");
            var avec = AvecRangs(etat, ("noue-herbier", 2));
            Comparateur.ComparerATolerance(
                Economie.ProductionDeLEspece(avec, vairon),
                Economie.ProductionDeLEspece(etat, vairon).Mul(1.25 * 1.25));
            Assert.That(Economie.ProductionDeLEspece(avec, epinoche).Eq(Economie.ProductionDeLEspece(etat, epinoche)), Is.True);
            Assert.That(Economie.MultiplicateurDeLieu(avec, epinoche), Is.EqualTo(1));
        }

        [Test, Description("avec des bonus, le détail de captation explique toujours la production")]
        public void Avec_des_bonus_le_detail_de_captation_explique_toujours_la_production()
        {
            var etat = AvecRangs(EtatDeTravail.Creer(), ("noue-herbier", 3), ("gour-courant", 1));
            foreach (var espece in Especes.Toutes.Where(e => e.Palier < etat.Cycle.PaliersOuverts))
            {
                var produit = Economie.DetailDeCaptation(etat, espece).Aggregate(new Decimal(1), (acc, ligne) => acc.Mul(ligne.Valeur));
                Comparateur.ComparerATolerance(produit, Economie.ProductionDeLEspece(etat, espece));
            }
        }

        [Test, Description("un bonus de coût de lieu ne baisse que ce qui se paie dans son lieu")]
        public void Un_bonus_de_cout_de_lieu_ne_baisse_que_ce_qui_se_paie_dans_son_lieu()
        {
            var etat = Creusee(4);
            var avec = AvecRangs(etat, ("noue-vase", 2), ("noue-eau-calme", 1), ("noue-racines", 1));
            Comparateur.ComparerATolerance(Economie.CoutDeDescente(avec, 4), Economie.CoutDeDescente(etat, 4).Mul(0.92 * 0.92));
            Assert.That(Economie.CoutDeDescente(avec, 7).Eq(Economie.CoutDeDescente(etat, 7)), Is.True);
            var loche = Especes.ParId("loche");
            var chabot = Especes.ParId("chabot");
            Comparateur.ComparerATolerance(Economie.CoutDeNiveau(avec, loche, 3), Economie.CoutDeNiveau(etat, loche, 3).Mul(0.9));
            Assert.That(Economie.CoutDeNiveau(avec, chabot, 3).Eq(Economie.CoutDeNiveau(etat, chabot, 3)), Is.True);
            Comparateur.ComparerATolerance(Economie.CoutDeDeblocage(avec, loche), Economie.CoutDeDeblocage(etat, loche).Mul(0.8));
            Assert.That(Economie.CoutDeDeblocage(avec, chabot).Eq(Economie.CoutDeDeblocage(etat, chabot)), Is.True);
        }

        [Test, Description("une technique de coût vaut partout")]
        public void Une_technique_de_cout_vaut_partout()
        {
            var etat = Creusee(4);
            var avec = AvecRangs(etat, ("technique-pelle", 3), ("technique-croissance", 1));
            var facteur = System.Math.Pow(0.95, 3);
            Comparateur.ComparerATolerance(Economie.CoutDeDescente(avec, 4), Economie.CoutDeDescente(etat, 4).Mul(facteur));
            Comparateur.ComparerATolerance(Economie.CoutDeDescente(avec, 12), Economie.CoutDeDescente(etat, 12).Mul(facteur));
            Comparateur.ComparerATolerance(Economie.CoutDeCroissance(avec, 3), Economie.CoutDeCroissance(etat, 3).Mul(0.95));
        }

        [Test, Description("la patience allonge l'absence comptée, sans dépasser le maximum")]
        public void La_patience_allonge_l_absence_comptee()
        {
            var etat = Creusee();
            Assert.That(HorsLigne.CapHorsLigneCourantHeures(AvecRangs(etat, ("technique-patience", 2))),
                Is.EqualTo(Constantes.CAP_HORS_LIGNE_HEURES_INITIAL * 2).Within(1e-9));
            Assert.That(Horloge.CapHorsLigneSecondes(HorsLigne.CapHorsLigneCourantHeures(AvecRangs(etat, ("technique-patience", 4)))),
                Is.LessThanOrEqualTo(Constantes.CAP_HORS_LIGNE_HEURES_MAXIMUM * 3600));
        }

        /* ─── La renaissance et la sauvegarde ─────────────────────────────────── */

        [Test, Description("les bonus se perdent à la renaissance")]
        public void Les_bonus_se_perdent_a_la_renaissance()
        {
            var etat = AvecRangs(Creusee(), ("noue-vase", 2), ("technique-pelle", 1));
            Assert.That(Renaissance.Renaitre(etat).Cycle.Bonus, Is.Empty);
        }

        [Test, Description("la sauvegarde garde les rangs, et une save sans bonus se lit comme une vie neuve")]
        public void La_sauvegarde_garde_les_rangs()
        {
            var etat = AvecRangs(Creusee(), ("noue-vase", 2), ("technique-pelle", 1));
            var relu = Persistance.Deserialiser(Persistance.Serialiser(etat), Reducteur.EtatInitial(1));
            Assert.That(relu.Cycle.Bonus["noue-vase"], Is.EqualTo(2));
            Assert.That(relu.Cycle.Bonus["technique-pelle"], Is.EqualTo(1));

            var save = Persistance.Serialiser(etat);
            ((JObject)save.Contenu["cycle"]).Remove("bonus");
            Assert.That(Persistance.Deserialiser(save, Reducteur.EtatInitial(1)).Cycle.Bonus, Is.Empty);
        }

        [Test, Description("un rang lu au-delà du maximum est ramené au maximum")]
        public void Un_rang_lu_au_dela_du_maximum_est_ramene_au_maximum()
        {
            var save = Persistance.Serialiser(Creusee());
            ((JObject)save.Contenu["cycle"])["bonus"] = new JObject { ["noue-racines"] = 99, ["inconnu"] = 3 };
            var relu = Persistance.Deserialiser(save, Reducteur.EtatInitial(1));
            Assert.That(relu.Cycle.Bonus["noue-racines"], Is.EqualTo(B("noue-racines").RangMax));
            Assert.That(relu.Cycle.Bonus.ContainsKey("inconnu"), Is.False);
        }

        /* ─── Les verbes ──────────────────────────────────────────────────────── */

        [Test, Description("sans verbe, les automatismes rendent le même état")]
        public void Sans_verbe_les_automatismes_rendent_le_meme_etat()
        {
            var etat = EtatDeTravail.Creer();
            Assert.That(Automatismes.Appliquer(etat), Is.SameAs(etat));
        }

        [Test, Description("la sonde creuse seule quand la roche le permet")]
        public void La_sonde_creuse_seule()
        {
            var etat = AvecRangs(Creusee(3), ("technique-sonde", 1));
            Assert.That(Economie.CapacitesDesBonus(etat), Has.Member(CapaciteId.CreusementAuto));
            Assert.That(Automatismes.Appliquer(etat).Cycle.PaliersOuverts, Is.EqualTo(4));
        }

        [Test, Description("la main sûre monte le cran le moins cher, et garde la réserve")]
        public void La_main_sure_monte_le_cran_le_moins_cher()
        {
            var etat = Reducteur.Debloquer(Reducteur.Debloquer(Creusee(4), "vairon"), "loche");
            etat = AvecRangs(etat, ("technique-main-sure", 1));
            var apres = Automatismes.Appliquer(etat);
            Assert.That(apres.Cycle.Especes["vairon"].Niveau, Is.EqualTo(2));
            Assert.That(apres.Cycle.Especes["loche"].Niveau, Is.EqualTo(1));

            var pauvre = etat with { Cycle = etat.Cycle with { ManaCourant = Economie.CoutDeNiveau(etat, Especes.ParId("vairon"), 1).Mul(5) } };
            Assert.That(Automatismes.Appliquer(pauvre), Is.SameAs(pauvre));
        }

        [Test, Description("le pas ne touche pas aux bonus : un pas de 8 h vaut 480 pas d'une minute")]
        public void Le_pas_ne_touche_pas_aux_bonus()
        {
            var etat = AvecRangs(EtatDeTravail.Creer(), ("noue-herbier", 2), ("technique-pelle", 1));
            var unPas = Reducteur.Tick(etat, 8 * 3600);
            var pasAPas = etat;
            for (var i = 0; i < 480; i++) pasAPas = Reducteur.Tick(pasAPas, 60);
            Assert.That(unPas.Cycle.Bonus, Is.EqualTo(etat.Cycle.Bonus));
            Comparateur.ComparerATolerance(unPas.Cycle.ManaCourant, pasAPas.Cycle.ManaCourant);
        }
    }
}
