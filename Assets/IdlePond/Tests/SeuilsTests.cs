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
         * N'appellent ni etatInitial, ni tick, ni un acte, ni etatDeTravail. Le reste
         * de seuils.test.ts est porté à la tâche 7 (via EtatDeTravail). */

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

        /* ─── Cas purs portés depuis tests/especes.test.ts ──────────────────────────
         * Le reste d'especes.test.ts est porté à la tâche 7 (via EtatDeTravail). */

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
    }
}
