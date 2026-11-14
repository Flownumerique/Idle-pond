using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// « Lire l'eau » (nœud verbe de la branche Renaissance) : « tu vois toujours ce que
    /// rentrer te vaudrait ». Sans le nœud, le gain de Souffle ne se lit qu'au moment de
    /// décider, tiroir de l'œuf ouvert ; avec, l'écran le montre en permanence.
    /// </summary>
    public class LireLEauTests
    {
        static EtatJeu AvecPic(EtatJeu etat, string pic) =>
            etat with { Cycle = etat.Cycle with { ProductionPicParSeconde = Decimal.Parse(pic) } };

        static EtatJeu SachantLireLEau(EtatJeu etat) =>
            etat with { Permanent = etat.Permanent with { NoeudsTechnique = new[] { "renaissance-lire-l-eau" } } };

        [Test, Description("sans le nœud, le gain ne se lit pas en permanence")]
        public void Sans_le_noeud_rien_ne_se_lit()
        {
            var etat = AvecPic(Reducteur.EtatInitial(1), "1e9");
            Assert.That(Renaissance.GainLisible(etat), Is.Null);
        }

        [Test, Description("avec le nœud, le gain lisible est le gain prévu")]
        public void Avec_le_noeud_le_gain_prevu_se_lit()
        {
            var etat = SachantLireLEau(AvecPic(EtatDeTravail.Creer(), "1e9"));
            var lisible = Renaissance.GainLisible(etat);
            Assert.That(lisible, Is.Not.Null);
            Assert.That(lisible.Value.Eq(Renaissance.GainDeSoufflePrevu(etat)), Is.True);
            Assert.That(lisible.Value.Gt(0), Is.True);
        }

        [Test, Description("avec le nœud, un gain nul se lit aussi : rentrer ne vaudrait rien encore")]
        public void Un_gain_nul_se_lit_aussi()
        {
            var etat = SachantLireLEau(Reducteur.EtatInitial(1));
            Assert.That(Renaissance.GainLisible(etat)?.Eq(0), Is.True);
        }
    }
}
