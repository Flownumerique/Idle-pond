/*
 * Parité avec la version web.
 *
 * Tant que le TypeScript reste la référence, le port C# doit rendre, sur les
 * mêmes entrées, ce que rend le web. La référence est PRODUITE par le code
 * TypeScript (`npm run reference:unity`, qui exécute
 * `tests/parite/generer-reference-unity.ts`) et relue ici :
 *
 *   - le registre des succès et chaque texte, à l'octet près ;
 *   - le PRNG et l'arithmétique des grands nombres ;
 *   - la mise en forme de l'écran (montants, coûts, durées) ;
 *   - des états complets après 100 ms, 8 h, une divergence non choisie, une
 *     demi-heure jouée, une vie jouée à la main, deux et quinze cycles simulés —
 *     à 1e-9 près, la tolérance de l'équivalence de pas ;
 *   - les instants de déclenchement de la première demi-heure ;
 *   - les écrans : chaque chaîne que les composants React afficheraient ;
 *   - les saves web anciennes (v1, v2, v3), migrées puis réécrites : la chaîne
 *     obtenue doit être celle que le web écrit, à l'octet près.
 *
 * Un échec ici veut dire que les deux ports ont divergé. Si le changement est
 * voulu, il se fait DES DEUX CÔTÉS, puis la référence se régénère.
 */
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdlePond.Adaptateurs;
using IdlePond.Donnees;
using IdlePond.Etat;
using IdlePond.Nombres;
using IdlePond.Noyau;
using IdlePond.Presentation;
using IdlePond.Simulateur;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class PariteAvecLeWebTests
    {
        private const double H = OutilsDeTest.H;

        private static JsonObjet _reference;

        private static JsonObjet Reference => _reference ??= (JsonObjet)Json.Lire(File.ReadAllText(Path.Combine(
            OutilsDeTest.DossierDuJeu(), "Tests", "EditMode", "Parite", "reference-web.json")));

        private static JsonValeur Chemin(string chemin)
        {
            JsonValeur courant = Reference;
            foreach (var morceau in chemin.Split('.')) courant = ((JsonObjet)courant).Lire(morceau);
            Assert.IsNotNull(courant, "la référence n'a pas de « " + chemin + " » — la régénérer : npm run reference:unity");
            return courant;
        }

        private static IEnumerable<string> Textes(JsonValeur valeur) => ((JsonTableau)valeur).Elements.Select(e => ((JsonTexte)e).Valeur);

        private static IEnumerable<double> Nombres(JsonValeur valeur) => ((JsonTableau)valeur).Elements.Select(e => ((JsonNombre)e).Valeur);

        private static void ComparerEtat(EtatJeu etat, string chemin)
        {
            OutilsDeTest.ComparerJson(Persistance.Serialiser(etat), Chemin(chemin), OutilsDeTest.ToleranceRelative, chemin);
        }

        /* ─── Les scénarios, rejoués à l'identique du générateur ────────────── */

        private static EtatJeu JaugePleine()
        {
            var etat = EtatDeTravail.Creer();
            return etat with { Cycle = etat.Cycle with { ManaCourant = etat.Permanent.ContenanceMana } };
        }

        private static EtatJeu SequenceDeterministe()
        {
            var etat = EtatDeTravail.Creer(4242);
            foreach (var dt in new[] { 0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900 }) etat = Reducteur.Tick(etat, dt);
            return etat;
        }

        private static EtatJeu PartieJouee()
        {
            var etat = Reducteur.EtatInitial(77, Assises.PaliersLivres);
            var vairon = Paliers.Liste[0].Bancs[0].Id;
            etat = Reducteur.Convaincre(etat, vairon);
            etat = Reducteur.Tick(etat, 90);
            for (var i = 0; i < 4; i += 1) etat = Reducteur.AcheterPlace(Reducteur.Tick(etat, 60), vairon);
            etat = Reducteur.Tick(etat, 20 * 60);
            etat = Reducteur.Creuser(etat);
            etat = Reducteur.Convaincre(etat, Paliers.Liste[1].Bancs[0].Id);
            etat = Reducteur.Tick(etat, 3 * H);
            etat = Reducteur.Eclore(etat);
            return Reducteur.Tick(etat, 600);
        }

        /* ─── Registre, textes, PRNG ────────────────────────────────────────── */

        [Test]
        public void LeRegistreDesSuccesEstLeMemeDansLeMemeOrdre()
        {
            CollectionAssert.AreEqual(Textes(Chemin("registreDesSucces")).ToList(), RegistreDesSucces.Liste.Select(s => s.Id).ToList());
        }

        [Test]
        public void ChaqueTexteDeSuccesEstLeMemeALOctetPres()
        {
            var textes = (JsonObjet)Chemin("textes");
            foreach (var id in textes.Clefs)
            {
                var attendu = (JsonObjet)textes.Lire(id);
                var obtenu = TextesProvisoires.TexteDuSucces(id);
                Assert.AreEqual(((JsonTexte)attendu.Lire("nom")).Valeur, obtenu.Nom, id + ".nom");
                Assert.AreEqual(((JsonTexte)attendu.Lire("condition")).Valeur, obtenu.Condition, id + ".condition");
                Assert.AreEqual(((JsonTexte)attendu.Lire("rapport")).Valeur, obtenu.Rapport, id + ".rapport");
            }
        }

        [Test]
        public void LePrngTireLaMemeSuiteBitPourBit()
        {
            var prng = new EtatPrng(12345);
            foreach (var attendu in Nombres(Chemin("prng")))
            {
                var (valeur, suivant) = Reducteur.Tirer(prng);
                Assert.AreEqual(attendu, valeur);
                prng = suivant;
            }
        }

        /* ─── Grands nombres et mise en forme ───────────────────────────────── */

        private static void MemeGrandNombre(string attendu, GrandNombre obtenu, string contexte)
        {
            if (attendu == obtenu.ToString()) return;
            // Math.pow et Math.log10 de V8 et de .NET peuvent différer d'un ulp :
            // l'écriture exacte est exigée partout, sauf là où l'une d'elles entre.
            Assert.IsTrue(GrandNombre.EssayerDeLire(attendu, out var a), contexte + " : référence illisible « " + attendu + " »");
            var ecart = a.Sub(obtenu).Abs().Div(GrandNombre.Max(a.Abs(), obtenu.Abs())).ToNumber();
            Assert.Less(ecart, 1e-14, contexte + " : " + obtenu + " au lieu de " + attendu);
        }

        [Test]
        public void LArithmetiqueDesGrandsNombresEstCelleDeBreakInfinity()
        {
            foreach (var element in ((JsonTableau)Chemin("grandsNombres.binaires")).Elements.Cast<JsonObjet>())
            {
                var a = GrandNombre.Lire(((JsonTexte)element.Lire("a")).Valeur);
                var b = GrandNombre.Lire(((JsonTexte)element.Lire("b")).Valeur);
                var contexte = a + " et " + b;
                Assert.AreEqual(((JsonTexte)element.Lire("somme")).Valeur, a.Add(b).ToString(), contexte + " : somme");
                Assert.AreEqual(((JsonTexte)element.Lire("difference")).Valeur, a.Sub(b).ToString(), contexte + " : différence");
                Assert.AreEqual(((JsonTexte)element.Lire("produit")).Valeur, a.Mul(b).ToString(), contexte + " : produit");
                if (element.Lire("quotient") is JsonTexte quotient) Assert.AreEqual(quotient.Valeur, a.Div(b).ToString(), contexte + " : quotient");
                Assert.AreEqual(((JsonTexte)element.Lire("max")).Valeur, GrandNombre.Max(a, b).ToString(), contexte + " : max");
                Assert.AreEqual(((JsonBooleen)element.Lire("lt")).Valeur, a.Lt(b), contexte + " : lt");
                Assert.AreEqual(((JsonBooleen)element.Lire("gte")).Valeur, a.Gte(b), contexte + " : gte");
            }
            foreach (var element in ((JsonTableau)Chemin("grandsNombres.unaires")).Elements.Cast<JsonObjet>())
            {
                var a = GrandNombre.Lire(((JsonTexte)element.Lire("a")).Valeur);
                MemeGrandNombre(((JsonTexte)element.Lire("racine")).Valeur, a.Pow(0.5), a + " : racine");
                Assert.AreEqual(((JsonTexte)element.Lire("plancher")).Valeur, a.Floor().ToString(), a + " : plancher");
                Assert.AreEqual(((JsonTexte)element.Lire("fois115")).Valeur, a.Mul(1.15).ToString(), a + " : ×1.15");
                Assert.AreEqual(((JsonTexte)element.Lire("exponentielle")).Valeur, a.ToExponential(2), a + " : toExponential");
                Assert.AreEqual(((JsonNombre)element.Lire("nombre")).Valeur, a.ToNumber(), a + " : toNumber");
            }
        }

        [Test]
        public void LaMiseEnFormeEstCelleDeFormatTs()
        {
            void Pareil(string chemin, IEnumerable<string> obtenus) =>
                CollectionAssert.AreEqual(Textes(Chemin(chemin)).ToList(), obtenus.ToList(), chemin);

            Pareil("format.montant", new[] { 0, 1, 7.25, 9.99, 10.5, 999.9, 1000, 1234.5, 9999, 12345, 999999, 1.5e6, 2.4e9, 7.77e12, 3.3e15, 9.9e18, 1.2e21, 5e24 }
                .Select(v => Format.Montant(v)));
            Pareil("format.cout", new[] { 0.4, 1, 5.55, 9.96, 10.2, 58.8, 999.1, 1000, 45678.9, 3.2e8 }.Select(v => Format.Cout(v)));
            Pareil("format.duree", new[] { -1, 0, 12.5, 89.4, 90, 125, 3599, 5399, 5400, 7200, 86399, 86400, 400000 }.Select(Format.Duree));
            Pareil("format.profondeur", new[] { 0, 1, 2, 5, 11 }.Select(Format.Profondeur));

            var toFixed = ((JsonTableau)Chemin("format.toFixed")).Elements.Cast<JsonTableau>().ToList();
            var valeurs = new[] { 0, 0.05, 0.25, 1.005, 2.675, 1.45, 123.456, 9.9999, 0.0001234, 5e-7 };
            for (var i = 0; i < valeurs.Length; i += 1)
            {
                Assert.AreEqual(((JsonTexte)toFixed[i][0]).Valeur, NombreJs.ToFixed(valeurs[i], 1), valeurs[i] + ".toFixed(1)");
                Assert.AreEqual(((JsonTexte)toFixed[i][1]).Valeur, NombreJs.ToFixed(valeurs[i], 3), valeurs[i] + ".toFixed(3)");
            }
        }

        [Test]
        public void LesCoutsSontLesMemes()
        {
            var descente = Textes(Chemin("couts.descente")).ToList();
            for (var i = 0; i < descente.Count; i += 1) MemeGrandNombre(descente[i], Economie.CoutDeDescente(Reducteur.EtatInitial(1), i), "descente " + i);

            var initial = Reducteur.EtatInitial(1);
            var amenage = initial with { Permanent = initial.Permanent with { ProfondeurMaxAtteinte = 40 } };
            var descenteAmenagee = Textes(Chemin("couts.descenteAmenagee")).ToList();
            for (var i = 0; i < descenteAmenagee.Count; i += 1) MemeGrandNombre(descenteAmenagee[i], Economie.CoutDeDescente(amenage, i), "aménagement " + i);

            var place = Textes(Chemin("couts.place")).ToList();
            for (var i = 0; i < place.Count; i += 1) MemeGrandNombre(place[i], Economie.CoutDePlace(initial, Paliers.Bancs[3], i + 1), "place " + (i + 1));

            var travail = EtatDeTravail.Creer();
            var conviction = Textes(Chemin("couts.conviction")).ToList();
            for (var i = 0; i < conviction.Count; i += 1) MemeGrandNombre(conviction[i], Economie.CoutDeConviction(travail, Paliers.Bancs[i]), "conviction " + Paliers.Bancs[i].Id);
        }

        /* ─── États ─────────────────────────────────────────────────────────── */

        [Test]
        public void LEtatDeTravailEstLeMeme() => ComparerEtat(EtatDeTravail.Creer(), "etats.etatDeTravail");

        [Test]
        public void HuitHeuresDUnSeulPasDonnentLeMemeEtat() => ComparerEtat(Reducteur.Tick(EtatDeTravail.Creer(), 8 * H), "etats.apresHuitHeures");

        [Test]
        public void CentMillisecondesDonnentLeMemeEtat() => ComparerEtat(Reducteur.Tick(EtatDeTravail.Creer(), 0.1), "etats.apresCentMillisecondes");

        [Test]
        public void LaSequenceDeterministeDonneLeMemeEtat() => ComparerEtat(SequenceDeterministe(), "etats.sequenceDeterministe");

        [Test]
        public void LaDivergenceNonChoisieTombeDeLaMemeFacon() =>
            ComparerEtat(Reducteur.Tick(JaugePleine(), Constantes.DelaiDeDivergenceNonChoisieHeures * H + 2 * H), "etats.divergenceNonChoisie");

        [Test]
        public void UneDemiHeureRejoueeDonneLeMemeEtat() => ComparerEtat(Joueur.Rejoue(1800), "etats.rejoueUneDemiHeure");

        [Test]
        public void UneVieJoueeALaMainDonneLeMemeEtat() => ComparerEtat(PartieJouee(), "etats.partieJouee");

        [Test]
        public void LaPremiereEclosionDeclencheLesMemesSucces()
        {
            CollectionAssert.AreEqual(
                Textes(Chemin("etats.declenchesAuPremierTick")).ToList(),
                Reducteur.TickDetaille(Reducteur.Eclore(Reducteur.EtatInitial(1)), 0.1).Declenches.ToList());
        }

        [Test]
        public void LaPremiereDemiHeureDeclencheLesMemesSuccesAuxMemesInstants()
        {
            var attendus = ((JsonTableau)Chemin("demiHeure")).Elements.Cast<JsonObjet>()
                .Select(d => ((JsonTexte)d.Lire("id")).Valeur + "@" + NombreJs.VersTexte(((JsonNombre)d.Lire("instantSecondes")).Valeur))
                .ToList();
            var obtenus = Joueur.JoueUneDemiHeure().Select(d => d.Id + "@" + NombreJs.VersTexte(d.InstantSecondes)).ToList();
            CollectionAssert.AreEqual(attendus, obtenus);
        }

        [Test]
        public void DeuxCyclesSimulesSurLeMondeLivreDonnentLeMemeEtat()
        {
            var partie = Simulation.Simuler(2, null, 1, null, Assises.PaliersLivres);
            ComparerEtat(partie.Etat, "simulations.livree.etat");
            Assert.AreEqual(((JsonNombre)Chemin("simulations.livree.sessions")).Valeur, partie.Sessions.Count);
            Assert.AreEqual(((JsonNombre)Chemin("simulations.livree.tempsActifSecondes")).Valeur, partie.TempsActifSecondes,
                1e-9 * partie.TempsActifSecondes);
        }

        [Test]
        public void QuinzeCyclesSimulesDonnentLaMemePartie()
        {
            // Le test le plus exigeant : quinze vies de décisions discrètes. Une
            // divergence d'un ulp qui changerait UN achat ferait bifurquer toute la
            // suite — c'est exactement ce qu'il doit attraper.
            var partie = Simulation.Simuler(Constantes.NombreDEclosionsVise);
            var cycles = ((JsonTableau)Chemin("simulations.quinze.cycles")).Elements.Cast<JsonObjet>().ToList();
            Assert.AreEqual(cycles.Count, partie.Releve.Cycles.Count);
            for (var i = 0; i < cycles.Count; i += 1)
            {
                var attendu = cycles[i];
                var obtenu = partie.Releve.Cycles[i];
                Assert.AreEqual(((JsonNombre)attendu.Lire("paliersOuverts")).Valeur, obtenu.PaliersOuverts, "cycle " + i + " : paliers ouverts");
                var duree = ((JsonNombre)attendu.Lire("dureeEcouleeSecondes")).Valeur;
                Assert.AreEqual(duree, obtenu.DureeEcouleeSecondes, 1e-9 * duree, "cycle " + i + " : durée");
            }
            Assert.AreEqual(((JsonNombre)Chemin("simulations.quinze.sessions")).Valeur, partie.Sessions.Count);
            ComparerEtat(partie.Etat, "simulations.quinze.etat");
        }

        /* ─── Écrans ─────────────────────────────────────────────────────────── */

        private static JsonValeur T(string texte) => texte == null ? (JsonValeur)JsonNul.Valeur : new JsonTexte(texte);

        /// <summary>L'écran que la présentation C# produit, dans la forme du générateur.</summary>
        private static JsonObjet Ecran(EtatJeu etat)
        {
            var enTete = Modeles.EnTete(etat);
            var contenance = Modeles.Contenance(etat);
            var mare = Modeles.Mare(etat);
            var eclosion = Modeles.Eclosion(etat);
            var succes = Modeles.Succes(etat);

            JsonObjet Ligne(LigneAffichee l) => new JsonObjet().Poser("terme", T(l.Terme)).Poser("valeur", T(l.Valeur)).Poser("source", T(l.Source));

            return new JsonObjet()
                .Poser("enTete", new JsonObjet().Poser("foi", T(enTete.Foi)).Poser("eclosions", T(enTete.Eclosions)))
                .Poser("contenance", new JsonObjet()
                    .Poser("mana", T(contenance.Mana))
                    .Poser("plafond", T(contenance.Plafond))
                    .Poser("part", new JsonNombre(contenance.Part))
                    .Poser("trouble", new JsonBooleen(contenance.Trouble))
                    .Poser("plein", new JsonBooleen(contenance.Plein))
                    .Poser("debit", T(contenance.Debit))
                    .Poser("bloque", new JsonBooleen(contenance.MessageBloque != null)))
                .Poser("mare", new JsonObjet()
                    .Poser("titre", T(mare.Titre))
                    .Poser("bancs", new JsonTableau(mare.Bancs.Select(b => (JsonValeur)new JsonObjet()
                        .Poser("bancId", T(b.BancId))
                        .Poser("nom", T(b.Nom))
                        .Poser("profondeur", T(b.Profondeur))
                        .Poser("effectif", T(b.Effectif))
                        .Poser("production", T(b.Production))
                        .Poser("action", T(b.Action))
                        .Poser("cout", T(b.Cout))
                        .Poser("payable", new JsonBooleen(b.Payable)))))
                    .Poser("creusement", new JsonObjet()
                        .Poser("libelle", T(mare.Creusement.Libelle))
                        .Poser("cout", T(mare.Creusement.Cout))
                        .Poser("possible", new JsonBooleen(mare.Creusement.Possible))))
                .Poser("captations", new JsonTableau(mare.Bancs.Select(b =>
                {
                    var c = Modeles.Captation(etat, b.BancId);
                    return (JsonValeur)new JsonObjet()
                        .Poser("bancId", T(b.BancId))
                        .Poser("lignes", new JsonTableau(c.Lignes.Select(l => (JsonValeur)Ligne(l))))
                        .Poser("natif", T(c.Natif))
                        .Poser("lignesAcclimatees", new JsonTableau(c.LignesAcclimatees.Select(l => (JsonValeur)Ligne(l))))
                        .Poser("acclimate", T(c.Acclimate));
                })))
                .Poser("eclosion", new JsonObjet().Poser("foi", T(eclosion.Foi)).Poser("bloque", new JsonBooleen(eclosion.Bloque)))
                .Poser("succes", new JsonObjet()
                    .Poser("acquis", new JsonTableau(succes.Acquis.Select(a => T(a.Id))))
                    .Poser("enChemin", new JsonTableau(succes.EnChemin.Select(e => (JsonValeur)new JsonObjet()
                        .Poser("id", T(e.Id))
                        .Poser("progression", e.Progression == null ? (JsonValeur)JsonNul.Valeur : new JsonNombre(e.Progression.Value)))))
                    .Poser("plusLoin", new JsonTableau(succes.PlusLoin.Select(T)))
                    .Poser("secrets", new JsonNombre(succes.Secrets)));
        }

        private static void ComparerEcran(EtatJeu etat, string nom)
        {
            // Les chaînes à l'identique ; seuls la part de jauge et les barres de
            // progression, qui ne s'affichent qu'en largeur, gardent la tolérance.
            OutilsDeTest.ComparerJson(Ecran(etat), Chemin("ecrans." + nom), OutilsDeTest.ToleranceRelative, "ecrans." + nom, false);
        }

        [Test]
        public void LEcranDeDepartEstLeMeme() => ComparerEcran(Reducteur.EtatInitial(1, Assises.PaliersLivres), "depart");

        [Test]
        public void LEcranDeLEtatDeTravailEstLeMeme() => ComparerEcran(EtatDeTravail.Creer(), "etatDeTravail");

        [Test]
        public void LEcranApresUneDemiHeureEstLeMeme() => ComparerEcran(Joueur.Rejoue(1800), "demiHeure");

        [Test]
        public void LEcranDUneVieJoueeEstLeMeme() => ComparerEcran(PartieJouee(), "partieJouee");

        [Test]
        public void LEcranDUneJaugePleineEstLeMeme() => ComparerEcran(JaugePleine(), "jaugePleine");

        [Test]
        public void LEcranApresDeuxCyclesSimulesEstLeMeme() =>
            ComparerEcran(Simulation.Simuler(2, null, 1, null, Assises.PaliersLivres).Etat, "simulationLivree");

        /* ─── Sauvegardes ───────────────────────────────────────────────────── */

        [Test]
        public void LesSavesWebAnciennesSeMigrentEtSeReecriventALOctetPres()
        {
            foreach (var cas in ((JsonTableau)Chemin("migrations")).Elements.Cast<JsonObjet>())
            {
                var save = Persistance.Enveloppe(cas.Lire("save"));
                var reecrite = Persistance.Serialiser(Persistance.Deserialiser(save, Reducteur.EtatInitial(0, Assises.PaliersLivres))).EnTexte();
                Assert.AreEqual(((JsonTexte)cas.Lire("reecrite")).Valeur, reecrite, "save v" + save.VersionSave);
            }
        }

        [Test]
        public void UneSaveDuMagasinWebSeRelitEtSeReecritALOctetPres()
        {
            var texte = ((JsonTexte)Chemin("saveDuMagasin")).Valeur;
            var stockage = new StockageEnMemoire();
            stockage.Ecrire(Magasin.ClefDeSauvegarde, texte);
            var magasin = new Magasin(stockage, new HorlogeFigee(1_700_000_000_000));
            Assert.AreEqual(1_700_000_000_000, magasin.DernierInstantMs);
            OutilsDeTest.ComparerAToleranceFlottante(magasin.Etat, PartieJouee());
            Assert.AreEqual(texte, magasin.Exporter());
        }
    }
}
