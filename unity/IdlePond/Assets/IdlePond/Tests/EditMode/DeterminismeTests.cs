/*
 * Test de déterminisme (§12, jalon v0.1). Port de `tests/determinisme.test.ts`.
 *
 * « Même graine + même séquence de dt ⇒ même état final », bit pour bit. Aucune
 * tolérance ici : une divergence, même minuscule, veut dire qu'un état vit hors
 * du réducteur.
 */
using IdlePond.Adaptateurs;
using IdlePond.Noyau;
using IdlePond.Simulateur;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class DeterminismeTests
    {
        private static readonly double[] Sequence = { 0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900 };

        private static string Jouer()
        {
            var etat = EtatDeTravail.Creer(4242);
            foreach (var dt in Sequence) etat = Reducteur.Tick(etat, dt);
            return Persistance.Serialiser(etat).EnTexte();
        }

        [Test]
        public void MemeGraineEtMemeSequenceDonnentLeMemeEtatBitPourBit()
        {
            Assert.AreEqual(Jouer(), Jouer());
        }

        [Test]
        public void DeuxSimulationsDeMemeGraineSontIdentiques()
        {
            var a = Simulation.Simuler(3, null, 7);
            var b = Simulation.Simuler(3, null, 7);
            Assert.AreEqual(Persistance.Serialiser(a.Etat).EnTexte(), Persistance.Serialiser(b.Etat).EnTexte());
        }

        [Test]
        public void LePrngEstPurIlRendUneValeurEtUnEtatSuivantSansMuter()
        {
            var depart = new EtatPrng(12345);
            var (valeur, suivant) = Reducteur.Tirer(depart);
            var (memeValeur, memeSuivant) = Reducteur.Tirer(depart);
            Assert.AreEqual(12345u, depart.Graine);
            Assert.AreEqual(valeur, memeValeur);
            Assert.AreEqual(suivant.Graine, memeSuivant.Graine);
            Assert.GreaterOrEqual(valeur, 0);
            Assert.Less(valeur, 1);
        }

        [Test]
        public void LeCheminContinuNeConsommeJamaisDeHasard()
        {
            var depart = EtatDeTravail.Creer();
            Assert.AreEqual(depart.Prng.Graine, Reducteur.Tick(depart, 28800).Prng.Graine);
            var parPas = depart;
            for (var i = 0; i < 100; i += 1) parPas = Reducteur.Tick(parPas, 60);
            Assert.AreEqual(depart.Prng.Graine, parPas.Prng.Graine);
        }
    }
}
