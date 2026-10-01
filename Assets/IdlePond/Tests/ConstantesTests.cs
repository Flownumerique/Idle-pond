using System;
using System.Reflection;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class ConstantesTests
    {
        static double Lire(string nom)
        {
            var champ = typeof(Constantes).GetField(nom, BindingFlags.Public | BindingFlags.Static);
            Assert.That(champ, Is.Not.Null, $"Constantes.{nom} manque");
            return Convert.ToDouble(champ.GetValue(null));
        }

        /// 1e-14 relatif : les constantes dérivées passent par Math.Pow, qui peut
        /// différer d'un ulp entre V8 et .NET. Une graine saisie doit, elle, être exacte.
        static void Proche(string nom, double attendu, double obtenu)
        {
            if (attendu.Equals(obtenu)) return;
            var ecart = Math.Abs(attendu - obtenu) / Math.Max(Math.Abs(attendu), 1e-300);
            Assert.That(ecart, Is.LessThanOrEqualTo(1e-14), $"{nom} : {obtenu} au lieu de {attendu}");
        }

        [Test, Description("chaque constante vaut ce qu’elle valait en TypeScript")]
        public void Chaque_constante_vaut_ce_qu_elle_valait_en_TypeScript()
        {
            var reference = References.Lire("constantes");
            foreach (var (nom, valeur) in (JObject)reference["nombres"])
                Proche(nom, References.Double(valeur), Lire(nom));
            Proche("multiplicateurDePalier", References.Double(reference["multiplicateurDePalier"]), Constantes.MultiplicateurDePalier());
            Proche("densiteExposant", References.Double(reference["densiteExposant"]), Constantes.DensiteExposant());
            var seuils = (JArray)reference["SEUILS_DE_JALON"];
            Assert.That(Constantes.SEUILS_DE_JALON.Count, Is.EqualTo(seuils.Count));
            for (var i = 0; i < seuils.Count; i++)
            {
                Assert.That(Constantes.SEUILS_DE_JALON[i].Seuil, Is.EqualTo((int)seuils[i]["seuil"]));
                Assert.That(Constantes.SEUILS_DE_JALON[i].MultiplicateurCumule, Is.EqualTo(References.Double(seuils[i]["multiplicateurCumule"])));
            }
            Assert.That(Constantes.COUTS_DE_NOEUD, Is.EqualTo(reference["COUTS_DE_NOEUD"].ToObject<int[]>()));
        }

        [Test, Description("la sauvegarde Unity repart à la version 1")]
        public void La_sauvegarde_Unity_repart_a_la_version_1()
        {
            Assert.That(Constantes.VERSION_SAVE, Is.EqualTo(1));
        }

        [Test, Description("chaque terme a un identifiant, et un seul registre")]
        public void Chaque_terme_a_un_identifiant_et_un_seul_registre()
        {
            foreach (TermeDeFormule terme in Enum.GetValues(typeof(TermeDeFormule)))
            {
                var registres = (Termes.EstDeProduction(terme) ? 1 : 0) + (Termes.EstDeCout(terme) ? 1 : 0) + (Termes.EstDeConfort(terme) ? 1 : 0);
                Assert.That(registres, Is.EqualTo(1), terme.ToString());
                Assert.That(Termes.Identifiant(terme), Does.Match("^[a-z_]+$"));
            }
            Assert.That(Termes.Identifiant(TermeDeFormule.CoutInsufflation), Is.EqualTo("cout_insufflation"));
        }
    }
}
