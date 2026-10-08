using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Tests
{
    /// <summary>
    /// Ce que l'écran d'accueil dit de la partie (spec du 2026-10-08) : rien à la première
    /// partie, sinon où l'on en est, ce qu'on porte et combien de temps on est parti.
    /// </summary>
    public class ResumeDeLaPartieTests
    {
        static readonly IdlePond.Noyau.EtatJeu Neuf = Partie.NouvelEtat(new HorlogeFigee(1_700_000_000_000L));

        [SetUp, TearDown]
        public void RemettreLaNotation() => Format.Notation = Notation.Suffixes;

        [Test, Description("à la première partie : Commencer, et rien d'autre")]
        public void A_la_premiere_partie_commencer_et_rien_d_autre()
        {
            var r = ResumeDeLaPartie.De(Neuf, null, premiereFois: true);
            Assert.That(r.Principal, Is.EqualTo(E.COMMENCER));
            Assert.That(r.NouvellePartiePossible, Is.False);
            Assert.That(r.Lieu, Is.Null);
            Assert.That(r.Mana, Is.Null);
            Assert.That(r.Absence, Is.Null);
        }

        [Test, Description("une partie en cours : Continuer, le lieu le plus bas ouvert et le mana")]
        public void Une_partie_en_cours_dit_le_lieu_et_le_mana()
        {
            var etat = Neuf with { Cycle = Neuf.Cycle with { ManaCourant = new Decimal(1.5e6) } };
            var r = ResumeDeLaPartie.De(etat, null, premiereFois: false);
            Assert.That(r.Principal, Is.EqualTo(E.CONTINUER));
            Assert.That(r.NouvellePartiePossible, Is.True);
            Assert.That(r.Lieu, Is.EqualTo(Format.NomDeLAssiseCapitale(Assises.DuPalier(0).Id)));
            Assert.That(r.Mana, Does.Contain("1.50 M"));
            Assert.That(r.Absence, Is.Null);
        }

        [Test, Description("une absence créditée se dit en durée")]
        public void Une_absence_creditee_se_dit_en_duree()
        {
            var r = ResumeDeLaPartie.De(Neuf, new AbsenceCreditee(3 * 3600), premiereFois: false);
            Assert.That(r.Absence, Does.Contain(Format.Duree(3 * 3600)));
        }
    }
}
