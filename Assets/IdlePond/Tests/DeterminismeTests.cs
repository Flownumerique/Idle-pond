using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Test de déterminisme (§12, jalon v0.1).
    ///
    /// « Même graine + même séquence de dt ⇒ même état final. »
    ///
    /// Le PRNG est à graine et vit dans l'état ; deux exécutions du même état avec
    /// le même dt donnent le même résultat, bit pour bit. Aucune tolérance ici,
    /// contrairement à l'équivalence de pas : une divergence, même minuscule, veut
    /// dire qu'un état vit hors du reducer.
    ///
    /// Le cas « deux simulations de même graine sont identiques » vient avec le
    /// simulateur (tâche 8).
    /// </summary>
    public class DeterminismeTests
    {
        static readonly double[] SEQUENCE = { 0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900 };

        static string Jouer()
        {
            var etat = EtatDeTravail.Creer(4242);
            foreach (var dt in SEQUENCE) etat = Reducteur.Tick(etat, dt);
            return Instantane.De(etat).ToString(Formatting.None);
        }

        [Test, Description("même graine et même séquence de dt donnent le même état, bit pour bit")]
        public void Meme_graine_et_meme_sequence_de_dt_donnent_le_meme_etat_bit_pour_bit()
        {
            Assert.That(Jouer(), Is.EqualTo(Jouer()));
        }

        [Test, Description("le PRNG est pur : il rend une valeur et un état suivant, sans muter")]
        public void Le_PRNG_est_pur_il_rend_une_valeur_et_un_etat_suivant_sans_muter()
        {
            var depart = new EtatPrng(12345);
            var (valeur, suivant) = Prng.Tirer(depart);
            var (memeValeur, memeSuivant) = Prng.Tirer(depart);
            Assert.That(depart.Graine, Is.EqualTo(12345));
            Assert.That(valeur, Is.EqualTo(memeValeur));
            Assert.That(suivant.Graine, Is.EqualTo(memeSuivant.Graine));
            Assert.That(valeur, Is.GreaterThanOrEqualTo(0));
            Assert.That(valeur, Is.LessThan(1));
        }

        [Test, Description("le chemin continu ne consomme jamais de hasard")]
        public void Le_chemin_continu_ne_consomme_jamais_de_hasard()
        {
            // Sinon 480 pas de 60 s tireraient 480 fois là où un pas de 8 h tire une
            // fois, et l'équivalence de pas tomberait avec le hors ligne et le
            // simulateur. Le hasard n'a droit de cité que sur des événements discrets.
            var depart = EtatDeTravail.Creer();
            Assert.That(Reducteur.Tick(depart, 28800).Prng.Graine, Is.EqualTo(depart.Prng.Graine));
            var parPas = depart;
            for (var i = 0; i < 100; i += 1) parPas = Reducteur.Tick(parPas, 60);
            Assert.That(parPas.Prng.Graine, Is.EqualTo(depart.Prng.Graine));
        }

        [Test, Description("l’ordre des achats ne change pas l’instantané")]
        public void L_ordre_des_achats_ne_change_pas_l_instantane()
        {
            // 1e12, pas 1e9 : les 2 × 99 niveaux coûtent ≈ 2,1e9, et à 1e9 le mana manque
            // à des niveaux différents selon l'ordre (95/94 contre 83/97, vérifié sur le
            // TypeScript) — la dépense ne serait plus la même et le test, faux par
            // construction.
            var depart = Reducteur.EtatInitial(9);
            depart = depart with { Cycle = depart.Cycle with { ManaCourant = Decimal.Parse("1e12") } };
            depart = Reducteur.Creuser(Reducteur.Creuser(Reducteur.Creuser(Reducteur.Creuser(depart))));
            var a = Reducteur.Debloquer(Reducteur.Debloquer(depart, "vairon"), "loche");
            var b = Reducteur.Debloquer(Reducteur.Debloquer(depart, "loche"), "vairon");
            for (var i = 0; i < 99; i++) { a = Reducteur.Ameliorer(a, "vairon"); a = Reducteur.Ameliorer(a, "loche"); }
            for (var i = 0; i < 99; i++) b = Reducteur.Ameliorer(b, "loche");
            for (var i = 0; i < 99; i++) b = Reducteur.Ameliorer(b, "vairon");
            // Même dépense totale, dans un autre ordre : seuls le mana et les compteurs
            // peuvent différer d'arrondi ; l'ORDRE des clefs, jamais.
            Assert.That(Instantane.De(a)["cycle"]["especes"].ToString(), Is.EqualTo(Instantane.De(b)["cycle"]["especes"].ToString()));
            Assert.That(Instantane.De(a)["permanent"]["especesAyantAtteintCent"].ToString(),
                Is.EqualTo(Instantane.De(b)["permanent"]["especesAyantAtteintCent"].ToString()));
            Assert.That(Instantane.De(a)["permanent"]["succes"].ToString(), Is.EqualTo(Instantane.De(b)["permanent"]["succes"].ToString()));
        }
    }
}
