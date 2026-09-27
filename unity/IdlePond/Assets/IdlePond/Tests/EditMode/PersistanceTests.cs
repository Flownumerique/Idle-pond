/*
 * Versionnage de save et aller-retour des grands nombres (§10, §12). Port de
 * `tests/persistance.test.ts`, plus ce que le port ajoute : le JSON lui-même, et
 * la compatibilité avec les saves du web.
 */
using System;
using System.Linq;
using IdlePond.Adaptateurs;
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class PersistanceTests
    {
        private static SaveSerialisee Save(string json) => Persistance.Enveloppe(Json.Lire(json));

        [Test]
        public void UnGrandNombreFaitLAllerRetourALExact()
        {
            var valeurs = new[]
            {
                GrandNombre.Zero,
                GrandNombre.Un,
                GrandNombre.Lire("1.2345678901234567e30"),
                GrandNombre.DepuisNombre(2.4).Pow(61),
                GrandNombre.Un.Div(3),
            };
            foreach (var valeur in valeurs)
            {
                var retour = Persistance.DeserialiserGrandNombre(Persistance.SerialiserGrandNombre(valeur), GrandNombre.DepuisNombre(-1));
                Assert.IsTrue(retour.Eq(valeur), valeur + " n'est pas revenu identique");
            }
        }

        [Test]
        public void UnGrandNombreIllisibleRetombeSurLeRepliPlutotQueSurNaN()
        {
            Assert.IsTrue(Persistance.DeserialiserGrandNombre(null, 7).Eq(7));
            Assert.IsTrue(Persistance.DeserialiserGrandNombre(new JsonObjet(), 7).Eq(7));
            Assert.IsTrue(Persistance.DeserialiserGrandNombre(new JsonTexte("pas un nombre"), 7).Eq(7));
        }

        [Test]
        public void UnEtatCompletSurvitAUnAllerRetourParJson()
        {
            var depart = EtatDeTravail.Creer();
            var texte = Persistance.Serialiser(depart).EnTexte();
            var retour = Persistance.DeserialiserTexte(texte, Reducteur.EtatInitial(0));
            OutilsDeTest.ComparerAToleranceFlottante(retour, depart, 0);
            Assert.AreEqual(texte, Persistance.Serialiser(retour).EnTexte(), "la chaîne elle-même revient identique");
        }

        [Test]
        public void LaSavePorteUneVersionEtLaChaineDeMigrationsExiste()
        {
            var save = Persistance.Enveloppe(Persistance.Serialiser(Reducteur.EtatInitial(1)));
            Assert.AreEqual(Constantes.VersionSave, save.VersionSave);
            Assert.DoesNotThrow(() => Persistance.Migrer(save));
        }

        [Test]
        public void UneSaveDUneVersionInconnueRefuseDeSeChargerEnSilence()
        {
            var erreur = Assert.Throws<InvalidOperationException>(() => Persistance.Migrer(Save("{\"versionSave\":-1,\"contenu\":{}}")));
            StringAssert.Contains("Migration de save manquante", erreur.Message);
        }

        [Test]
        public void UneSaveDuJalonPrecedentSeReveilleAuSortirDeLOeuf()
        {
            var repli = Reducteur.EtatInitial(0);
            var reprise = Persistance.Deserialiser(Save(
                "{\"versionSave\":1,\"contenu\":{\"permanent\":{\"succesDebloques\":[\"seuil-espece-1-1-10\",\"acte-premiere-conviction\"],\"couches\":[\"assise-1\"]}}}"),
                repli);
            Assert.AreEqual(Constantes.VersionSave, reprise.VersionSave);
            Assert.IsTrue(reprise.Permanent.Succes.Contient("seuil-vairon-10"));
            CollectionAssert.AreEqual(new[] { "noue" }, reprise.Permanent.Couches);
            Assert.AreEqual(repli.Cycle.PaliersOuverts, reprise.Cycle.PaliersOuverts);
        }

        [Test]
        public void DeuxVersTroisLesSuccesAcquisRecoiventUnRegistreLesBenedictionsDisparaissent()
        {
            var save = Save(
                "{\"versionSave\":2,\"contenu\":{\"permanent\":{\"nombreEclosions\":3,\"succesDebloques\":[\"seuil-vairon-10\",\"acte-premiere-conviction\"],\"benedictions\":{\"quelque-chose\":2}}}}");
            var reprise = Persistance.Deserialiser(save, Reducteur.EtatInitial(0));

            Assert.AreEqual(new EntreeDeSucces(3, PalierDeVoix.Directives), reprise.Permanent.Succes["seuil-vairon-10"]);
            Assert.AreEqual(2, reprise.Permanent.Succes.Count);

            var migre = (JsonObjet)((JsonObjet)Persistance.Migrer(save)).Lire("permanent");
            Assert.IsFalse(migre.Contient("benedictions"));
            Assert.IsFalse(migre.Contient("succesDebloques"));
        }

        [Test]
        public void UneSaveMigreeSeSerialiseCommeUneSaveNativeDeMemeContenu()
        {
            var ids = new[] { "acte-premiere-conviction", "seuil-vairon-10" };
            string Json2(string[] liste) =>
                "{\"versionSave\":2,\"contenu\":{\"permanent\":{\"nombreEclosions\":0,\"succesDebloques\":[" +
                string.Join(",", liste.Select(i => "\"" + i + "\"")) + "]}}}";
            var migree = Persistance.Deserialiser(Save(Json2(ids)), Reducteur.EtatInitial(0));
            var inverse = Persistance.Deserialiser(Save(Json2(ids.Reverse().ToArray())), Reducteur.EtatInitial(0));
            CollectionAssert.AreEqual(migree.Permanent.Succes.Clefs, inverse.Permanent.Succes.Clefs);
        }

        [Test]
        public void TroisVersQuatreLesPartsMuresEntrentAUn()
        {
            var reprise = Persistance.Deserialiser(Save("{\"versionSave\":3,\"contenu\":{\"permanent\":{}}}"), Reducteur.EtatInitial(0));
            Assert.AreEqual(Constantes.NombreDePaliers, reprise.Permanent.PartsMures.Count);
            Assert.IsTrue(reprise.Permanent.PartsMures.All(p => p == 1));
        }

        [Test]
        public void UneProfondeurHorsDeLaRocheEstRameneeDansLaRoche()
        {
            var save = Save("{\"versionSave\":4,\"contenu\":{\"cycle\":{\"paliersOuverts\":500},\"permanent\":{\"profondeurMaxAtteinte\":-3}}}");
            var reprise = Persistance.Deserialiser(save, Reducteur.EtatInitial(0));
            Assert.AreEqual(Constantes.NombreDePaliers, reprise.Cycle.PaliersOuverts);
            Assert.AreEqual(0, reprise.Permanent.ProfondeurMaxAtteinte);
            Assert.DoesNotThrow(() => Reducteur.Tick(reprise, 60), "la boucle ne doit pas sortir de la table des paliers");
        }

        [Test]
        public void UnChampIllisibleNePerdQueCeChamp()
        {
            var depart = EtatDeTravail.Creer();
            var save = Persistance.Serialiser(depart);
            var contenu = (JsonObjet)save.Lire("contenu");
            ((JsonObjet)contenu.Lire("permanent")).Poser("foi", new JsonTexte("ceci n'est pas de la Foi"));
            var repli = Reducteur.EtatInitial(0);
            var reprise = Persistance.Deserialiser(Persistance.Enveloppe(save), repli);
            Assert.IsTrue(reprise.Permanent.Foi.Eq(repli.Permanent.Foi));
            Assert.IsTrue(reprise.Permanent.ContenanceMana.Eq(depart.Permanent.ContenanceMana));
        }
    }

    [TestFixture]
    public sealed class JsonTests
    {
        [Test]
        public void LesClefsGardentLOrdreDInsertion()
        {
            var objet = new JsonObjet().Poser("z", new JsonNombre(1)).Poser("a", new JsonNombre(2)).Poser("z", new JsonNombre(3));
            Assert.AreEqual("{\"z\":3,\"a\":2}", objet.EnTexte());
        }

        [Test]
        public void LesNombresSEcriventCommeJavaScript()
        {
            Assert.AreEqual("[1,0.1,1e+21,1.5e-7,123456789012345680000,-0.000001,null]", new JsonTableau(new JsonValeur[]
            {
                new JsonNombre(1), new JsonNombre(0.1), new JsonNombre(1e21), new JsonNombre(1.5e-7),
                new JsonNombre(1.2345678901234568e20), new JsonNombre(-1e-6), new JsonNombre(double.NaN),
            }).EnTexte());
        }

        [Test]
        public void LesChainesSEchappentCommeJsonStringify()
        {
            Assert.AreEqual("\"l’épinoche \\\"\\\\\\n\\u0001\"", new JsonTexte("l’épinoche \"\\\n\u0001").EnTexte());
        }

        [Test]
        public void UnAllerRetourRendLeMemeTexte()
        {
            const string texte = "{\"a\":[1,2.5,{\"b\":null,\"c\":true,\"d\":\"x\\u00e9\"}],\"e\":-3e-7}";
            var relu = Json.Lire(texte).EnTexte();
            Assert.AreEqual("{\"a\":[1,2.5,{\"b\":null,\"c\":true,\"d\":\"xé\"}],\"e\":-3e-7}", relu);
        }

        [Test]
        public void UnTexteMalFormeEstRefuse()
        {
            foreach (var faux in new[] { "", "{", "[1,]", "{\"a\" 1}", "01", "\"sans fin", "tru", "{} {}" })
            {
                Assert.IsFalse(Json.EssayerDeLire(faux, out _), "« " + faux + " » aurait dû être refusé");
            }
        }
    }
}
