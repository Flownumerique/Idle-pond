using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les six branches de l'arbre de technique (2026-11-13) : ce que chaque nœud fait
    /// réellement une fois appris. `ArbreDeTechniqueTests` couvre l'apprentissage et la
    /// branche Recrutement ; ici, les effets des cinq autres.
    /// </summary>
    public class BranchesDeLArbreTests
    {
        const long H = 3600_000;
        const long DEPART = 1_790_000_000_000L;

        static readonly Espece Vairon = Especes.Toutes[0];
        static readonly Espece Loche = Especes.Toutes[1];

        static EtatJeu Sachant(EtatJeu etat, params string[] noeuds) =>
            etat with { Permanent = etat.Permanent with { NoeudsTechnique = noeuds } };

        /// Une partie neuve, avec du mana, de quoi le porter, et `paliers` creux ouverts.
        static EtatJeu Depart(string mana = "1e6", int paliers = 1)
        {
            var etat = Reducteur.EtatInitial(7);
            return etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse(mana), PaliersOuverts = paliers },
                Permanent = etat.Permanent with { ContenanceMana = Decimal.Parse("1e300") },
            };
        }

        static EtatJeu AvecDensite(EtatJeu etat, params int[] paliers)
        {
            var densites = etat.Permanent.Densites.ToArray();
            foreach (var palier in paliers) densites[palier] = 1;
            return etat with { Permanent = etat.Permanent with { Densites = densites } };
        }

        static IReadOnlyList<string> Branche(BrancheTechnique branche, int combien) =>
            NoeudsTechnique.DeLaBranche(branche).Take(combien).Select(n => n.Id).ToArray();

        /* ─── Le registre ─────────────────────────────────────────────────────── */

        [Test, Description("six branches de cinq nœuds, aux rangs 1 à 5, chacun avec son texte")]
        public void Six_branches_de_cinq_noeuds()
        {
            Assert.That(NoeudsTechnique.Tous.Count, Is.EqualTo(30));
            foreach (BrancheTechnique branche in Enum.GetValues(typeof(BrancheTechnique)))
            {
                var noeuds = NoeudsTechnique.DeLaBranche(branche);
                Assert.That(noeuds.Select(n => n.Rang), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }), branche.ToString());
                Assert.That(noeuds.Select(n => n.Cout), Is.EqualTo(Constantes.COUTS_DE_NOEUD), branche.ToString());
            }
            foreach (var noeud in NoeudsTechnique.Tous)
            {
                Assert.That(NoeudsTechnique.ParId(noeud.Id), Is.SameAs(noeud));
                Assert.That(Textes.DuNoeud(noeud.Id).Effet, Is.Not.Empty, noeud.Id);
            }
        }

        [Test, Description("chaque branche a de quoi rendre des points, sauf la Construction, muette")]
        public void Chaque_branche_rend_des_points_sauf_la_Construction()
        {
            foreach (BrancheTechnique branche in Enum.GetValues(typeof(BrancheTechnique)))
            {
                var parle = Technique.COUPLES_A_B.ContainsKey(branche) || Technique.CALENDRIER_PAR_BRANCHE_BORNEE.ContainsKey(branche);
                Assert.That(parle, Is.EqualTo(branche != BrancheTechnique.Construction), branche.ToString());
            }
            Assert.That(Technique.PointsDeBranche(BrancheTechnique.Construction, 1e9), Is.EqualTo(0));
        }

        /* ─── Creusement ──────────────────────────────────────────────────────── */

        [Test, Description("les remises de creusement sont des paliers : −10 %, −20 %, −30 %")]
        public void Les_remises_de_creusement_sont_des_paliers()
        {
            var depart = Depart();
            double Facteur(int noeuds) => Technique.FacteurDeTechnique(
                Sachant(depart, Branche(BrancheTechnique.Creusement, noeuds).ToArray()), TermeDeFormule.CoutCreuser);
            Assert.That(Facteur(1), Is.EqualTo(0.90).Within(1e-12));
            Assert.That(Facteur(2), Is.EqualTo(0.80).Within(1e-12));
            Assert.That(Facteur(3), Is.EqualTo(0.80).Within(1e-12), "la file est un verbe");
            Assert.That(Facteur(4), Is.EqualTo(0.70).Within(1e-12));
        }

        [Test, Description("creuser apprend à creuser")]
        public void Creuser_apprend_a_creuser()
        {
            var etat = Depart("1e30");
            for (var i = 0; i < 4; i += 1) etat = Reducteur.Creuser(etat);
            Assert.That(etat.Permanent.NoeudsTechnique, Has.Member("creusement-etaiement"));
            // Et le prix du palier suivant s'en ressent tout de suite.
            var sansRienSavoir = Sachant(etat);
            Assert.That(Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts)
                .Lt(Economie.CoutDeDescente(sansRienSavoir, etat.Cycle.PaliersOuverts)), Is.True);
        }

        /* ─── Amélioration ────────────────────────────────────────────────────── */

        [Test, Description("l'élan : une espèce convaincue arrive au cran 5 ; sans lui, au cran 1")]
        public void L_elan_fait_arriver_au_cran_5()
        {
            var sans = Depart();
            Assert.That(Economie.NiveauDeDepart(sans), Is.EqualTo(1));
            Assert.That(Reducteur.Debloquer(sans, Vairon.Id).Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(1));

            var avec = Sachant(sans, "amelioration-elan");
            Assert.That(Economie.NiveauDeDepart(avec), Is.EqualTo(5));
            Assert.That(Reducteur.Debloquer(avec, Vairon.Id).Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(5));
        }

        [Test, Description("tout le monde suit : plusieurs crans d'un coup, jusqu'au seuil, sans vider la réserve")]
        public void Tout_le_monde_suit_monte_jusqu_au_seuil()
        {
            var etat = Reducteur.Debloquer(Depart("1e9"), Vairon.Id);
            var manaAvant = etat.Cycle.ManaCourant;
            var apres = Automatismes.Appliquer(Sachant(etat, "amelioration-suite"));
            Assert.That(apres.Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(10), "le lot s'arrête au seuil");
            // Elle ne dépense jamais plus que sa part du mana.
            var depense = manaAvant.Sub(apres.Cycle.ManaCourant);
            Assert.That(depense.Lte(manaAvant.Mul(Constantes.PART_DU_MANA_POUR_UN_ACHAT_AUTO)), Is.True);

            // La main sûre, elle, ne monte qu'un cran.
            var unCran = Automatismes.Appliquer(Sachant(etat, "amelioration-main-sure"));
            Assert.That(unCran.Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(2));
        }

        /* ─── Renaissance ─────────────────────────────────────────────────────── */

        [Test, Description("la coquille épaisse double ce qu'on peut porter, sans se composer d'une renaissance à l'autre")]
        public void La_coquille_double_la_contenance_sans_se_composer()
        {
            var sans = Depart();
            var avec = Sachant(sans, "renaissance-coquille");
            Assert.That(Economie.Contenance(avec).Div(Economie.Contenance(sans)).ToNumber(), Is.EqualTo(2).Within(1e-12));
            Assert.That(Economie.ContenanceAFixer(avec).ToString(), Is.EqualTo(Economie.ContenanceAFixer(sans).ToString()));

            // Ce que la renaissance fixe ne contient pas la coquille : deux renaissances de
            // suite la laissent à ×2, pas à ×4.
            var deuxFois = Renaissance.Renaitre(Renaissance.Renaitre(avec));
            var temoin = Renaissance.Renaitre(Renaissance.Renaitre(sans));
            Assert.That(deuxFois.Permanent.ContenanceMana.ToString(), Is.EqualTo(temoin.Permanent.ContenanceMana.ToString()));
        }

        [Test, Description("rentrer à l'heure relève l'exposant du gain de densité, et ce qui reste en garde 10 % de plus")]
        public void Rentrer_a_l_heure_et_ce_qui_reste()
        {
            var sans = Depart();
            Assert.That(Densite.AlphaCourant(sans), Is.EqualTo(Constantes.ALPHA_GAIN_DE_DENSITE));
            var aLHeure = Sachant(sans, "renaissance-heure");
            Assert.That(Densite.AlphaCourant(aLHeure), Is.EqualTo(Constantes.ALPHA_GAIN_DE_DENSITE + 0.05).Within(1e-12));

            var pic = Decimal.Parse("1e6");
            var deBase = Densite.AppliquerGain(sans, 3, pic)[0];
            Assert.That(deBase, Is.EqualTo(Math.Pow(1e6, 0.6)).Within(1e-6));
            Assert.That(Densite.AppliquerGain(aLHeure, 3, pic)[0], Is.EqualTo(Math.Pow(1e6, 0.65)).Within(1e-6));
            Assert.That(Densite.AppliquerGain(Sachant(sans, "renaissance-ce-qui-reste"), 3, pic)[0], Is.EqualTo(deBase * 1.10).Within(1e-6));
            // Les paliers jamais ouverts ne prennent rien.
            Assert.That(Densite.AppliquerGain(aLHeure, 3, pic)[3], Is.EqualTo(0));
        }

        [Test, Description("rentrer apprend à rentrer : un nœud à la première renaissance, le suivant à la deuxième")]
        public void Rentrer_apprend_a_rentrer()
        {
            var une = Renaissance.Renaitre(Depart());
            Assert.That(une.Permanent.NoeudsTechnique, Is.EqualTo(new[] { "renaissance-heure" }));
            var deux = Renaissance.Renaitre(une);
            Assert.That(deux.Permanent.NoeudsTechnique, Is.EqualTo(new[] { "renaissance-heure", "renaissance-lire-l-eau" }));
        }

        [Test, Description("le chemin connu refait seul ce qu'on avait fait : la roche connue, les espèces connues, les bonus")]
        public void Le_chemin_connu_refait_seul_la_redescente()
        {
            // Une vie passée a ouvert quatre paliers ; on repart du premier.
            var etat = AvecDensite(Sachant(Depart("1e9"), "renaissance-chemin-connu"), 0, 1, 2, 3);
            for (var pas = 0; pas < 200; pas += 1)
            {
                var suivant = Automatismes.Appliquer(etat);
                if (ReferenceEquals(suivant, etat)) break;
                etat = suivant;
            }
            Assert.That(etat.Cycle.PaliersOuverts, Is.EqualTo(4), "il s'arrête à la roche neuve");
            Assert.That(etat.Cycle.Especes[Vairon.Id].Debloquee, Is.True);
            Assert.That(etat.Cycle.Especes[Loche.Id].Debloquee, Is.True);
            Assert.That(etat.Cycle.Especes[Vairon.Id].Niveau, Is.GreaterThan(1), "les crans remontent");
            Assert.That(etat.Cycle.Bonus.Values.Sum(), Is.GreaterThan(0), "les bonus sont rachetés");
            Assert.That(etat.Cycle.ManaCourant.Gte(0), Is.True);
        }

        /* ─── Entretien ───────────────────────────────────────────────────────── */

        [Test, Description("la relève puis la veille doublent deux fois le plafond hors ligne : 6 h, 12 h, 24 h")]
        public void La_releve_et_la_veille_doublent_le_plafond()
        {
            var depart = Depart();
            Assert.That(HorsLigne.CapHorsLigneCourantHeures(depart), Is.EqualTo(6));
            Assert.That(HorsLigne.CapHorsLigneCourantHeures(Sachant(depart, "entretien-releve")), Is.EqualTo(12));
            Assert.That(HorsLigne.CapHorsLigneCourantHeures(Sachant(depart, "entretien-releve", "entretien-veille")), Is.EqualTo(24));
        }

        [Test, Description("s'absenter apprend à s'absenter, d'après les heures créditées seulement")]
        public void S_absenter_apprend_a_s_absenter()
        {
            Assert.That(Technique.CompteurDe(Depart(), BrancheTechnique.Entretien), Is.EqualTo(0));
            // Quarante-huit heures d'horloge, six heures créditées : c'est ce qui compte.
            var retour = HorsLigne.Crediter(Depart(), DEPART, DEPART + 48 * H);
            Assert.That(Technique.CompteurDe(retour.Etat, BrancheTechnique.Entretien), Is.EqualTo(6).Within(1e-9));
            Assert.That(retour.Etat.Permanent.NoeudsTechnique, Has.No.Member("entretien-compte"), "six heures ne suffisent pas tout à fait");

            // Une seconde absence fait tomber le premier nœud : compter les siens (2026-11-14).
            var second = HorsLigne.Crediter(retour.Etat, DEPART, DEPART + 48 * H);
            Assert.That(second.Etat.Permanent.NoeudsTechnique, Has.Member("entretien-compte"));
        }

        [Test, Description("compter les siens ouvre Entretien ; la relève et la veille suivent")]
        public void Compter_les_siens_ouvre_Entretien()
        {
            var entretien = NoeudsTechnique.Tous.Where(n => n.Branche == BrancheTechnique.Entretien).OrderBy(n => n.Rang).Select(n => n.Id);
            Assert.That(entretien, Is.EqualTo(new[] { "entretien-compte", "entretien-releve", "entretien-veille", "entretien-peuple", "entretien-rapport" }));
        }

        [Test, Description("le peuple continue : au retour, les verbes ont joué sur ce que l'absence a laissé")]
        public void Le_peuple_continue_joue_au_retour()
        {
            var etat = Reducteur.Debloquer(Depart("1e3"), Vairon.Id);
            var sansLePeuple = Sachant(etat, "amelioration-main-sure");
            var avecLePeuple = Sachant(etat, "amelioration-main-sure", "entretien-peuple");

            var seul = HorsLigne.Crediter(sansLePeuple, DEPART, DEPART + 3 * H).Etat;
            var suivi = HorsLigne.Crediter(avecLePeuple, DEPART, DEPART + 3 * H).Etat;

            Assert.That(seul.Cycle.Especes[Vairon.Id].Niveau, Is.EqualTo(1), "sans le nœud, rien ne se fait pendant l'absence");
            Assert.That(suivi.Cycle.Especes[Vairon.Id].Niveau, Is.GreaterThan(1));
            Assert.That(suivi.Cycle.ManaCourant.Lt(seul.Cycle.ManaCourant), Is.True, "les crans ont été payés");
            // Le temps crédité est le même : le peuple ne fabrique pas d'heures.
            Assert.That(suivi.TempsJeuSecondes, Is.EqualTo(seul.TempsJeuSecondes));
            // Et sans aucun verbe à faire jouer, le nœud seul ne change rien.
            Assert.That(HorsLigne.LePeupleContinue(sansLePeuple), Is.SameAs(sansLePeuple));
        }
    }
}
