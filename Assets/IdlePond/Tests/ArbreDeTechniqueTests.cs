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
    /// L'arbre de technique, branche Recrutement (2026-11-13) : ce qui s'apprend en
    /// convainquant, pour toujours. Appris seul, par l'usage — jamais acheté.
    /// </summary>
    public class ArbreDeTechniqueTests
    {
        const long DEPART = 1_790_000_000_000L;

        static readonly IReadOnlyList<NoeudTechnique> Recrutement =
            NoeudsTechnique.Tous.Where(n => n.Branche == BrancheTechnique.Recrutement).OrderBy(n => n.Rang).ToList();

        static EtatJeu AvecLeCompteur(EtatJeu etat, double convaincues)
        {
            var compteurs = etat.Permanent.CompteursTechnique.ToDictionary(p => p.Key, p => p.Value);
            compteurs[BrancheTechnique.Recrutement] = convaincues;
            return etat with { Permanent = etat.Permanent with { CompteursTechnique = compteurs } };
        }

        static EtatJeu Riche(EtatJeu etat, string mana = "1e60") =>
            etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse(mana) },
                Permanent = etat.Permanent with { ContenanceMana = Decimal.Parse("1e300") },
            };

        [Test, Description("la branche a cinq nœuds, aux rangs 1 à 5 et aux coûts du noyau")]
        public void La_branche_a_cinq_noeuds_aux_couts_du_noyau()
        {
            Assert.That(Recrutement.Select(n => n.Rang), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(Recrutement.Select(n => n.Cout), Is.EqualTo(Constantes.COUTS_DE_NOEUD));
            Assert.That(NoeudsTechnique.Tous.Select(n => n.Id).Distinct().Count(), Is.EqualTo(NoeudsTechnique.Tous.Count));
            foreach (var noeud in NoeudsTechnique.Tous) Assert.That(Textes.DuNoeud(noeud.Id).Nom, Is.Not.Empty, noeud.Id);
        }

        [Test, Description("un nœud est appris exactement quand le compteur atteint sa date du calendrier")]
        public void Un_noeud_est_appris_a_sa_date_du_calendrier()
        {
            var calendrier = Technique.CALENDRIER_PAR_BRANCHE_BORNEE[BrancheTechnique.Recrutement];
            var depart = Reducteur.EtatInitial(1);
            for (var rang = 0; rang < calendrier.Count; rang += 1)
            {
                var juste = Technique.Apprendre(AvecLeCompteur(depart, calendrier[rang]), BrancheTechnique.Recrutement);
                var avant = Technique.Apprendre(AvecLeCompteur(depart, calendrier[rang] - 1), BrancheTechnique.Recrutement);
                Assert.That(juste.Permanent.NoeudsTechnique.Count, Is.EqualTo(rang + 1), $"rang {rang + 1}");
                Assert.That(avant.Permanent.NoeudsTechnique.Count, Is.EqualTo(rang), $"juste avant le rang {rang + 1}");
                Assert.That(juste.Permanent.NoeudsTechnique, Is.EqualTo(Recrutement.Take(rang + 1).Select(n => n.Id)));
            }
        }

        [Test, Description("rien à apprendre : l'état revient inchangé, la même référence")]
        public void Rien_a_apprendre_rend_le_meme_etat()
        {
            var depart = Reducteur.EtatInitial(1);
            Assert.That(Technique.Apprendre(depart, BrancheTechnique.Recrutement), Is.SameAs(depart));
            Assert.That(Technique.Apprendre(depart, BrancheTechnique.Creusement), Is.SameAs(depart));
        }

        [Test, Description("convaincre fait apprendre, et le reste à faire se compte en espèces")]
        public void Convaincre_fait_apprendre()
        {
            var etat = AvecLeCompteur(Riche(Reducteur.EtatInitial(1)), 2);
            Assert.That(Technique.ResteAvantLeProchainNoeud(etat, BrancheTechnique.Recrutement), Is.EqualTo(1));

            var apres = Reducteur.Debloquer(etat, Especes.Toutes[0].Id);
            Assert.That(apres.Permanent.NoeudsTechnique, Is.EqualTo(new[] { "recrutement-approche" }));
            Assert.That(Technique.ResteAvantLeProchainNoeud(apres, BrancheTechnique.Recrutement), Is.EqualTo(3));
        }

        [Test, Description("les remises de conviction sont des paliers : −15 %, −30 %, −50 %, et la réputation touche le cran")]
        public void Les_remises_de_conviction_sont_des_paliers()
        {
            var depart = Reducteur.EtatInitial(1);
            double Facteur(int noeuds, TermeDeFormule terme) => Technique.FacteurDeTechnique(
                depart with { Permanent = depart.Permanent with { NoeudsTechnique = Recrutement.Take(noeuds).Select(n => n.Id).ToArray() } },
                terme);

            Assert.That(Facteur(0, TermeDeFormule.CoutDeblocage), Is.EqualTo(1));
            Assert.That(Facteur(1, TermeDeFormule.CoutDeblocage), Is.EqualTo(0.85).Within(1e-12));
            Assert.That(Facteur(3, TermeDeFormule.CoutDeblocage), Is.EqualTo(0.70).Within(1e-12));
            Assert.That(Facteur(5, TermeDeFormule.CoutDeblocage), Is.EqualTo(0.50).Within(1e-12));
            Assert.That(Facteur(3, TermeDeFormule.CoutNiveau), Is.EqualTo(1));
            Assert.That(Facteur(4, TermeDeFormule.CoutNiveau), Is.EqualTo(0.95).Within(1e-12));
        }

        [Test, Description("ce qu'on sait traverse la renaissance")]
        public void Ce_qu_on_sait_traverse_la_renaissance()
        {
            var sachant = Technique.Apprendre(AvecLeCompteur(Reducteur.EtatInitial(1), 6), BrancheTechnique.Recrutement);
            Assert.That(sachant.Permanent.NoeudsTechnique.Count, Is.EqualTo(2));
            // Rien ne se perd ; rentrer apprend en plus le premier nœud de la branche Renaissance.
            Assert.That(Renaissance.Renaitre(sachant).Permanent.NoeudsTechnique, Is.SupersetOf(sachant.Permanent.NoeudsTechnique));
            Assert.That(Renaissance.Renaitre(sachant).Permanent.NoeudsTechnique, Has.Member("renaissance-heure"));
        }

        [Test, Description("les habitudes tiennent : une espèce connue revient seule ; une espèce jamais rencontrée attend le joueur")]
        public void Les_habitudes_tiennent()
        {
            // Un stock modeste : le prix payé doit rester lisible dans la différence.
            var depart = Riche(Reducteur.EtatInitial(1), "1e6");
            // Trois paliers ouverts : le vairon (palier 0) est à portée.
            depart = depart with { Cycle = depart.Cycle with { PaliersOuverts = 4 } };
            var vairon = Especes.Toutes[0];
            var loche = Especes.Toutes[1];
            Assert.That(loche.Palier, Is.LessThan(4));

            // Sans le verbe : rien ne se fait seul.
            Assert.That(Automatismes.Appliquer(depart), Is.SameAs(depart));

            var avecLeVerbe = depart with
            {
                Permanent = depart.Permanent with { NoeudsTechnique = new[] { "recrutement-approche", "recrutement-habitudes" } },
            };
            // Avec le verbe, mais dans une eau qui ne se souvient de rien : toujours rien.
            Assert.That(Automatismes.EstConnue(avecLeVerbe, vairon), Is.False);
            Assert.That(Automatismes.Appliquer(avecLeVerbe), Is.SameAs(avecLeVerbe));

            // Une vie passée a chargé le palier du vairon, pas celui de la loche.
            var densites = avecLeVerbe.Permanent.Densites.ToArray();
            densites[vairon.Palier] = 2.5;
            var seSouvient = avecLeVerbe with { Permanent = avecLeVerbe.Permanent with { Densites = densites } };

            var apres = Automatismes.Appliquer(seSouvient);
            Assert.That(apres.Cycle.Especes[vairon.Id].Debloquee, Is.True);
            Assert.That(apres.Cycle.Especes.TryGetValue(loche.Id, out var l) && l.Debloquee, Is.False);
            // Et c'est payé au prix normal : le verbe ne fait pas de cadeau.
            var cout = Economie.CoutDeDeblocage(seSouvient, vairon);
            Assert.That(seSouvient.Cycle.ManaCourant.Sub(apres.Cycle.ManaCourant).Div(cout).ToNumber(), Is.EqualTo(1).Within(1e-9));
            // Une fois tout le monde revenu, plus rien à faire.
            Assert.That(Automatismes.Appliquer(apres), Is.SameAs(apres));
        }

        [Test, Description("sans mana, les habitudes attendent")]
        public void Sans_mana_les_habitudes_attendent()
        {
            var depart = Reducteur.EtatInitial(1);
            var densites = depart.Permanent.Densites.ToArray();
            densites[0] = 1;
            var etat = depart with
            {
                Cycle = depart.Cycle with { ManaCourant = Decimal.Zero },
                Permanent = depart.Permanent with { Densites = densites, NoeudsTechnique = new[] { "recrutement-approche", "recrutement-habitudes" } },
            };
            Assert.That(Automatismes.Appliquer(etat), Is.SameAs(etat));
        }

        [Test, Description("la partie signale une technique apprise, et un retour fait seul")]
        public void La_partie_signale_une_technique_apprise()
        {
            var etat = AvecLeCompteur(Riche(Reducteur.EtatInitial(1)), 2);
            var partie = new Partie(new HorlogeFigee(DEPART), etat);
            var recus = new List<EvenementDeJeu>();
            partie.EvenementsDeJeu += evenements => recus.AddRange(evenements);

            partie.Convaincre(Especes.Toutes[0].Id);
            var apprise = recus.Single(e => e.Genre == GenreDEvenement.TechniqueApprise);
            Assert.That(apprise.Sujet, Is.EqualTo("recrutement-approche"));
        }
    }
}
