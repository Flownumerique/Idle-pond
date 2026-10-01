using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Une espèce est un générateur avec un niveau — noyau v1.0 §1.3.
    ///
    /// Le joueur n'achète plus de la place en attendant que la population monte :
    /// il débloque une fois, il monte un niveau, et l'effet est immédiat. Plus
    /// aucune population n'est simulée, donc plus rien ne converge pendant un pas.
    /// </summary>
    public class EspecesTests
    {
        static EtatJeu Riche(long graine = 1)
        {
            var @base = Reducteur.EtatInitial(graine);
            return @base with
            {
                Cycle = @base.Cycle with { ManaCourant = Decimal.Parse("1e30") },
                Permanent = @base.Permanent with { ContenanceMana = Decimal.Parse("1e40") },
            };
        }

        [Test, Description("21 espèces, réparties 2/4/4/4/4/3")]
        public void _21_especes_reparties_2_4_4_4_4_3()
        {
            Assert.That(Especes.Toutes.Count, Is.EqualTo(21));
            var parAssise = new System.Collections.Generic.List<(string Assise, int Compte)>();
            foreach (var e in Especes.Toutes)
            {
                var index = parAssise.FindIndex(p => p.Assise == e.Assise);
                if (index >= 0) parAssise[index] = (e.Assise, parAssise[index].Compte + 1);
                else parAssise.Add((e.Assise, 1));
            }
            Assert.That(parAssise.Select(p => p.Compte), Is.EqualTo(new[] { 2, 4, 4, 4, 4, 3 }));
        }

        [Test, Description("une espèce tous les 3 paliers, à partir du premier de son assise")]
        public void Une_espece_tous_les_3_paliers_a_partir_du_premier_de_son_assise()
        {
            foreach (var espece in Especes.Toutes)
            {
                var palier = Paliers.Tous[espece.Palier];
                Assert.That(palier.Espece, Is.EqualTo(espece.Id));
            }
            var porteurs = Paliers.Tous.Where(p => p.Espece != null).ToList();
            Assert.That(porteurs.Count, Is.EqualTo(21));
        }

        [Test, Description("débloquer met le niveau à 1 et produit immédiatement")]
        public void Debloquer_met_le_niveau_a_1_et_produit_immediatement()
        {
            var etat = Riche();
            var premiere = Especes.Toutes[0];
            // Depuis la tâche 9, le héros seul produit déjà (RESULTATS.md, finding 3) :
            // ce n'est plus le départ de 0 qui prouve l'achat, c'est la hausse.
            var avant = Economie.ProductionTotaleParSeconde(etat);
            etat = Reducteur.Debloquer(etat, premiere.Id);
            Assert.That(etat.Cycle.Especes[premiere.Id].Niveau, Is.EqualTo(1));
            Assert.That(Economie.ProductionTotaleParSeconde(etat).Gt(avant), Is.True);
        }

        // Le titre nomme le modèle retiré (« aucune population ne converge ») : la
        // Description le garde, le nom de méthode s'arrête avant le mot mort.
        [Test, Description("le niveau agit sans délai : aucune population ne converge")]
        public void Le_niveau_agit_sans_delai()
        {
            var etat = Riche();
            etat = Reducteur.Debloquer(etat, Especes.Toutes[0].Id);
            var avant = Economie.ProductionTotaleParSeconde(etat);
            etat = Reducteur.Ameliorer(etat, Especes.Toutes[0].Id);
            var apres = Economie.ProductionTotaleParSeconde(etat);
            Assert.That(apres.Gt(avant), Is.True);
            // et le simple écoulement du temps ne change rien à la production
            Assert.That(Economie.ProductionTotaleParSeconde(Reducteur.Tick(etat, 3600)).Eq(apres), Is.True);
        }

        [Test, Description("les seuils lisent le niveau, cumulés, et cent vaut ×16")]
        public void Les_seuils_lisent_le_niveau_cumules_et_cent_vaut_16()
        {
            Assert.That(Economie.MultiplicateurDeSeuil(1), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDeSeuil(9), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDeSeuil(10), Is.EqualTo(2));
            Assert.That(Economie.MultiplicateurDeSeuil(25), Is.EqualTo(4));
            Assert.That(Economie.MultiplicateurDeSeuil(50), Is.EqualTo(8));
            Assert.That(Economie.MultiplicateurDeSeuil(100), Is.EqualTo(16));
            Assert.That(Economie.MultiplicateurDeSeuil(5000), Is.EqualTo(16));
        }

        [Test, Description("une espèce dont le palier n’est pas ouvert ne se débloque pas")]
        public void Une_espece_dont_le_palier_n_est_pas_ouvert_ne_se_debloque_pas()
        {
            var etat = Riche();
            var profonde = Especes.Toutes[Especes.Toutes.Count - 1];
            etat = Reducteur.Debloquer(etat, profonde.Id);
            Assert.That(etat.Cycle.Especes.TryGetValue(profonde.Id, out var vivante) && vivante.Debloquee, Is.False);
        }

        [Test, Description("creuser jusqu’au palier de la deuxième espèce la rend débloquable")]
        public void Creuser_jusqu_au_palier_de_la_deuxieme_espece_la_rend_debloquable()
        {
            var etat = Riche();
            var seconde = Especes.Toutes[1];
            while (etat.Cycle.PaliersOuverts <= seconde.Palier) etat = Reducteur.Creuser(etat);
            etat = Reducteur.Debloquer(etat, seconde.Id);
            Assert.That(etat.Cycle.Especes[seconde.Id].Debloquee, Is.True);
        }
    }
}
