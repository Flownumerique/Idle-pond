using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class DonneesTests
    {
        static readonly JObject Reference = References.Lire("donnees");

        static string Snake(string pascal) => string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

        /// Une chaîne nulle doit devenir un null JSON, pas une JValue de type String.
        static JToken Chaine(string s) => s == null ? JValue.CreateNull() : new JValue(s);

        static void Egal(string quoi, System.Collections.Generic.IEnumerable<JObject> obtenus, JToken attendus)
        {
            var tableau = new JArray(obtenus);
            Assert.That(JToken.DeepEquals(tableau, attendus), Is.True, $"{quoi} :\n{tableau}\n≠\n{attendus}");
        }

        [Test, Description("les assises, espèces et paliers sont ceux du TypeScript")]
        public void Les_assises_especes_et_paliers_sont_ceux_du_TypeScript()
        {
            Assert.That(Assises.PALIERS_LIVRES, Is.EqualTo((int)Reference["paliersLivres"]));
            Egal("assises", Assises.Toutes.Select(a => new JObject
            {
                ["id"] = a.Id, ["rang"] = a.Rang, ["typeMana"] = a.TypeMana,
                ["indexPremierPalier"] = a.IndexPremierPalier, ["nombreDePaliers"] = a.NombreDePaliers,
            }), Reference["assises"]);
            Egal("especes", Especes.Toutes.Select(e => new JObject
            {
                ["id"] = e.Id, ["assise"] = e.Assise, ["rang"] = e.Rang, ["palier"] = e.Palier,
            }), Reference["especes"]);
            Egal("paliers", Paliers.Tous.Select(p => new JObject
            {
                ["index"] = p.Index, ["assise"] = p.Assise, ["espece"] = Chaine(p.Espece),
            }), Reference["paliers"]);
        }

        [Test, Description("les insufflations sont les bénédictions renommées")]
        public void Les_insufflations_sont_les_benedictions_renommees()
        {
            Egal("insufflations", Insufflations.Toutes.Select(i => new JObject
            {
                ["id"] = i.Id, ["portee"] = i.Portee == PorteeDInsufflation.Globale ? "globale" : "ciblee", ["espece"] = Chaine(i.Espece),
            }), Reference["insufflations"]);
        }

        [Test, Description("le registre des succès est celui du TypeScript, dans le même ordre")]
        public void Le_registre_des_succes_est_celui_du_TypeScript_dans_le_meme_ordre()
        {
            var attendus = (JArray)Reference["succes"];
            Assert.That(RegistreDesSucces.Tous.Count, Is.EqualTo(attendus.Count));
            for (var i = 0; i < attendus.Count; i++)
            {
                var s = RegistreDesSucces.Tous[i];
                var a = attendus[i];
                Assert.That(s.Id, Is.EqualTo((string)a["id"]));
                Assert.That(Snake(s.Famille.ToString()), Is.EqualTo((string)a["famille"]), s.Id);
                Assert.That(Snake(s.Visibilite.ToString()), Is.EqualTo((string)a["visibilite"]), s.Id);
                Assert.That(s.Assise, Is.EqualTo((string)a["assise"]), s.Id);
                var dec = a["declencheur"];
                Assert.That(Snake(s.Declencheur.Quoi.ToString()), Is.EqualTo((string)dec["quoi"]), s.Id);
                if (dec["seuil"] != null) Assert.That(s.Declencheur.Seuil, Is.EqualTo(References.Double(dec["seuil"])), s.Id);
                if (dec["espece"] != null) Assert.That(s.Declencheur.Espece, Is.EqualTo((string)dec["espece"]), s.Id);
                if (dec["palier"] != null) Assert.That(s.Declencheur.Palier, Is.EqualTo((int)dec["palier"]), s.Id);
                var effet = a["effet"];
                if (effet.Type == JTokenType.Null) { Assert.That(s.Effet, Is.Null, s.Id); continue; }
                Assert.That(Snake(s.Effet.Genre.ToString()), Is.EqualTo((string)effet["genre"]), s.Id);
                if (effet["terme"] != null) Assert.That(Termes.Identifiant(s.Effet.Terme.Value), Is.EqualTo((string)effet["terme"]), s.Id);
                if (effet["part"] != null) Assert.That(s.Effet.Part, Is.EqualTo(References.Double(effet["part"])), s.Id);
            }
        }

        [Test, Description("les échelles tabulées valent le calcul direct au-delà de la table")]
        public void Les_echelles_tabulees_valent_le_calcul_direct_au_dela_de_la_table()
        {
            Assert.That(Echelles.PuissanceDeG(0).Eq(Decimal.Un), Is.True);
            Assert.That(Echelles.PuissanceDeG(200).Eq(Decimal.Pow(Constantes.G_COUT_PALIER, 200)), Is.True);
            Assert.That(Echelles.PuissanceDuCoutDeNiveau(5000).Eq(Decimal.Pow(Constantes.RATIO_COUT_NIVEAU, 5000)), Is.True);
            Assert.That(Echelles.DebitBaseDuRang(0).ToNumber(), Is.EqualTo(Constantes.TAUX_BASE_AU_PALIER_0));
        }

        [Test, Description("les textes n’ont aucune clef d’espèce ou de succès orpheline")]
        public void Les_textes_n_ont_aucune_clef_orpheline()
        {
            foreach (var id in Textes.NOM_DES_ESPECES.Keys) Assert.That(Especes.ParId(id), Is.Not.Null, id);
            foreach (var s in RegistreDesSucces.Tous) Assert.That(Textes.DuSucces(s.Id), Is.Not.SameAs(Textes.SUCCES_INCONNU), s.Id);
        }
    }
}
