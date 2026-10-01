using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// f = 1 — reset complet. Le noyau v1.0 §3.1 ferme [P5] : « Tout se repaie au
    /// prix d'origine. Il n'y a pas de tarif réduit à la redescente, comme dans
    /// n'importe quel idle. »
    /// </summary>
    public class RedescenteTests
    {
        [Test, Description("un palier déjà atteint dans une vie passée coûte exactement ce qu’il coûtait")]
        public void Un_palier_deja_atteint_dans_une_vie_passee_coute_exactement_ce_qu_il_coutait()
        {
            var etat = Reducteur.EtatInitial(7) with
            {
                Cycle = Reducteur.EtatInitial(7).Cycle with { ManaCourant = Decimal.Parse("1e30") },
                Permanent = Reducteur.EtatInitial(7).Permanent with { ContenanceMana = Decimal.Parse("1e40") },
            };
            var coutNeuf = Economie.CoutDeDescente(etat, 1);
            for (var i = 0; i < 5; i += 1) etat = Reducteur.Creuser(etat);
            Assert.That(etat.Permanent.ProfondeurMaxAtteinte, Is.GreaterThanOrEqualTo(6));

            var apres = Renaissance.Renaitre(etat) with
            {
                Cycle = Renaissance.Renaitre(etat).Cycle with { ManaCourant = Decimal.Parse("1e30") },
            };
            Assert.That(Economie.CoutDeDescente(apres, 1).Eq(coutNeuf), Is.True);
        }

        [Test, Description("creuser un palier neuf et le recreuser après renaissance coûtent le même prix")]
        public void Creuser_un_palier_neuf_et_le_recreuser_apres_renaissance_coutent_le_meme_prix()
        {
            var neuf = Reducteur.EtatInitial(7);
            var riche = neuf with
            {
                Cycle = neuf.Cycle with { ManaCourant = Decimal.Parse("1e30") },
                Permanent = neuf.Permanent with
                {
                    ContenanceMana = Decimal.Parse("1e40"),
                    ProfondeurMaxAtteinte = 40, // une vie passée est allée très bas
                },
            };
            // la profondeur déjà atteinte ne doit rien changer au prix
            Assert.That(Economie.CoutDeDescente(riche, 3).Eq(Economie.CoutDeDescente(neuf, 3)), Is.True);
        }
    }
}
