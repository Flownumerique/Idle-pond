using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using Newtonsoft.Json;
using NUnit.Framework;
using static IdlePond.Simulateur.Simulateur;

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

        [Test, Description("deux simulations de même graine sont identiques")]
        public void Deux_simulations_de_meme_graine_sont_identiques()
        {
            var a = Simuler(3, null, 7);
            var b = Simuler(3, null, 7);
            Assert.That(Instantane.De(a.Etat).ToString(Formatting.None), Is.EqualTo(Instantane.De(b.Etat).ToString(Formatting.None)));
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
            foreach (var etat in new[] { a, b })
            {
                Assert.That(etat.Cycle.Especes["vairon"].Niveau, Is.EqualTo(100));
                Assert.That(etat.Cycle.Especes["loche"].Niveau, Is.EqualTo(100));
            }
            // L'ordre propre des tables d'état, pas seulement celui de l'instantané (qui
            // les réordonne de toute façon par le registre).
            Assert.That(a.Cycle.Especes.Keys.ToArray(), Is.EqualTo(b.Cycle.Especes.Keys.ToArray()));
            Assert.That(a.Permanent.EspecesAyantAtteintCent, Is.EqualTo(new[] { "vairon", "loche" }));
            Assert.That(b.Permanent.EspecesAyantAtteintCent, Is.EqualTo(new[] { "vairon", "loche" }));
            Assert.That(Instantane.De(a)["cycle"]["especes"].ToString(), Is.EqualTo(Instantane.De(b)["cycle"]["especes"].ToString()));
            Assert.That(Instantane.De(a)["permanent"]["especesAyantAtteintCent"].ToString(),
                Is.EqualTo(Instantane.De(b)["permanent"]["especesAyantAtteintCent"].ToString()));
            Assert.That(Instantane.De(a)["permanent"]["succes"].ToString(), Is.EqualTo(Instantane.De(b)["permanent"]["succes"].ToString()));
        }

        [Test, Description("les clefs des améliorations suivent l'ordre du registre quel que soit l'ordre d'achat")]
        public void Les_clefs_des_ameliorations_suivent_l_ordre_du_registre_quel_que_soit_l_ordre_d_achat()
        {
            // L'instantané (Instantane.De) réordonne tout par le registre à l'écriture :
            // il ne peut donc jamais révéler un désordre du dictionnaire D'ÉTAT lui-même.
            // Ici on lit directement `Permanent.AmeliorationsDeRenaissance.Keys`, reconstruit à chaque
            // achat par `Reducteur.AcheterUneAmelioration` (~l.332-337), jamais accumulé dans l'ordre
            // d'arrivée — sans quoi deux parties qui améliorent les deux mêmes choses dans
            // un ordre opposé sérialiseraient des chaînes de save différentes.
            var riche = Reducteur.EtatInitial(5) with { Permanent = Reducteur.EtatInitial(5).Permanent with { Souffle = new Decimal(1e6) } };
            var ciblee = AmeliorationsDeRenaissance.CibleeDe("loche").Id;
            var a = Reducteur.AcheterUneAmelioration(Reducteur.AcheterUneAmelioration(riche, ciblee), AmeliorationsDeRenaissance.GLOBALE_ID);
            var b = Reducteur.AcheterUneAmelioration(Reducteur.AcheterUneAmelioration(riche, AmeliorationsDeRenaissance.GLOBALE_ID), ciblee);
            var attendu = AmeliorationsDeRenaissance.Toutes.Select(i => i.Id).Where(id => id == ciblee || id == AmeliorationsDeRenaissance.GLOBALE_ID).ToArray();
            Assert.That(a.Permanent.AmeliorationsDeRenaissance.Keys.ToArray(), Is.EqualTo(attendu));
            Assert.That(b.Permanent.AmeliorationsDeRenaissance.Keys.ToArray(), Is.EqualTo(attendu));
            Assert.That(a.Permanent.AmeliorationsDeRenaissance.Keys.ToArray(), Is.EqualTo(b.Permanent.AmeliorationsDeRenaissance.Keys.ToArray()));
        }

        [Test, Description("les clefs des succes suivent l'ordre du registre quel que soit l'ordre de declenchement")]
        public void Les_clefs_des_succes_suivent_l_ordre_du_registre_quel_que_soit_l_ordre_de_declenchement()
        {
            // Même lecture directe, côté succès : `Permanent.Succes.Keys`, reconstruit à
            // chaque tick par `RegleDesSucces.EnOrdreDuRegistre` (~l.159-171). Un pas de 8 h
            // franchit les deux seuils ensemble ; 480 pas de 60 s les voient l'un après
            // l'autre — ici on force l'ordre inverse d'un tick à l'autre pour le vérifier.
            EtatJeu Riche() => Reducteur.EtatInitial(5) with { Cycle = Reducteur.EtatInitial(5).Cycle with { ManaCourant = Decimal.Parse("1e12") } };

            // Ordre 1 : débloquer une espèce (acte-premiere-conviction), puis creuser un
            // second palier — ce qui franchit DEUX seuils à la fois (acte-premier-creusement,
            // et seuil-palier-sature-1 : le palier 1 n'ouvre aucune espèce, §6.1, donc il
            // est « au complet » dès qu'il est ouvert).
            var a = Riche();
            a = Reducteur.Tick(Reducteur.Debloquer(a, "vairon"), 0.1);
            a = Reducteur.Tick(Reducteur.Creuser(a), 0.1);

            // Ordre 2 : les deux mêmes actes, dans l'ordre inverse — les deux succès du
            // creusement arrivent donc groupés AVANT celui du déblocage, au lieu d'après.
            var b = Riche();
            b = Reducteur.Tick(Reducteur.Creuser(b), 0.1);
            b = Reducteur.Tick(Reducteur.Debloquer(b, "vairon"), 0.1);

            var attendu = RegistreDesSucces.Tous.Select(s => s.Id)
                .Where(id => id == "acte-premiere-conviction" || id == "acte-premier-creusement" || id == "seuil-palier-sature-1")
                .ToArray();
            Assert.That(a.Permanent.Succes.Keys.ToArray(), Is.EqualTo(attendu));
            Assert.That(b.Permanent.Succes.Keys.ToArray(), Is.EqualTo(attendu));
            Assert.That(a.Permanent.Succes.Keys.ToArray(), Is.EqualTo(b.Permanent.Succes.Keys.ToArray()));
        }
    }
}
