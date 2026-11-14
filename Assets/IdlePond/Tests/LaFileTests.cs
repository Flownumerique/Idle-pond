using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// « La file » (nœud verbe de Creusement, décidé le 2026-11-14) : « tu peux demander
    /// plusieurs creux d'avance ». Toucher « Creuser » sans de quoi payer met un creux en
    /// attente, trois au plus ; chacun se paie seul dès que le mana arrive, même hors ligne.
    /// </summary>
    public class LaFileTests
    {
        const long DEPART = 1_700_000_000_000;
        const long H = 3_600_000;

        static EtatJeu Avec(string mana, bool sachant = true, int demandes = 0)
        {
            var etat = Reducteur.EtatInitial(1, Assises.PALIERS_LIVRES);
            return etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse(mana), CreuxDemandes = demandes },
                Permanent = etat.Permanent with
                {
                    ContenanceMana = Decimal.Parse("1e30"),
                    NoeudsTechnique = sachant ? new[] { "creusement-file" } : new string[0],
                },
            };
        }

        static Decimal CoutDuProchain(EtatJeu etat) => Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);

        [Test, Description("sans le nœud, demander un creux impayable ne fait rien")]
        public void Sans_le_noeud_rien_n_attend()
        {
            var etat = Avec("0", sachant: false);
            Assert.That(Reducteur.DemanderUnCreux(etat), Is.SameAs(etat));
        }

        [Test, Description("avec le nœud, un creux impayable se met en attente, sans rien payer")]
        public void Un_creux_impayable_attend()
        {
            var etat = Avec("0");
            var apres = Reducteur.DemanderUnCreux(etat);
            Assert.That(apres.Cycle.CreuxDemandes, Is.EqualTo(1));
            Assert.That(apres.Cycle.PaliersOuverts, Is.EqualTo(etat.Cycle.PaliersOuverts));
            Assert.That(apres.Cycle.ManaCourant.Eq(0), Is.True);
        }

        [Test, Description("trois creux en attente, pas un de plus")]
        public void Trois_au_plus()
        {
            var etat = Avec("0");
            for (var i = 0; i < 3; i++) etat = Reducteur.DemanderUnCreux(etat);
            Assert.That(etat.Cycle.CreuxDemandes, Is.EqualTo(Constantes.CREUX_EN_ATTENTE_AU_PLUS));
            Assert.That(Reducteur.DemanderUnCreux(etat), Is.SameAs(etat));
        }

        [Test, Description("payable, demander un creux le creuse tout de suite")]
        public void Payable_on_creuse()
        {
            var etat = Avec("1e6");
            var apres = Reducteur.DemanderUnCreux(etat);
            Assert.That(apres.Cycle.PaliersOuverts, Is.EqualTo(etat.Cycle.PaliersOuverts + 1));
            Assert.That(apres.Cycle.CreuxDemandes, Is.EqualTo(0));
        }

        [Test, Description("on ne demande pas plus de creux qu'il n'en reste à creuser")]
        public void Pas_plus_qu_il_n_en_reste()
        {
            var etat = Avec("0");
            etat = etat with { Cycle = etat.Cycle with { PaliersOuverts = etat.LimiteDeContenu - 2 } };
            etat = Reducteur.DemanderUnCreux(Reducteur.DemanderUnCreux(etat));
            Assert.That(etat.Cycle.CreuxDemandes, Is.EqualTo(2));
            Assert.That(Reducteur.DemanderUnCreux(etat), Is.SameAs(etat));
        }

        [Test, Description("un creux demandé se creuse seul dès que le mana le paie, un par pas")]
        public void Un_creux_demande_se_creuse_seul()
        {
            var etat = Avec("0", demandes: 2);
            Assert.That(Automatismes.Appliquer(etat), Is.SameAs(etat), "rien n'est payable encore");
            var paye = etat with { Cycle = etat.Cycle with { ManaCourant = CoutDuProchain(etat) } };
            var apres = Automatismes.Appliquer(paye);
            Assert.That(apres.Cycle.PaliersOuverts, Is.EqualTo(etat.Cycle.PaliersOuverts + 1));
            Assert.That(apres.Cycle.CreuxDemandes, Is.EqualTo(1));
            Assert.That(apres.Permanent.CompteursTechnique[BrancheTechnique.Creusement], Is.GreaterThan(0), "le creux demandé est un vrai creux");
        }

        [Test, Description("la renaissance vide la file")]
        public void La_renaissance_vide_la_file()
        {
            Assert.That(Renaissance.Renaitre(Avec("0", demandes: 3)).Cycle.CreuxDemandes, Is.EqualTo(0));
        }

        [Test, Description("au retour d'absence, la file se vide sur ce que l'absence a amassé, sans « Le peuple continue »")]
        public void La_file_se_vide_au_retour()
        {
            var etat = Avec("0", demandes: 2);
            etat = etat with { Cycle = etat.Cycle with { ManaCourant = CoutDuProchain(etat).Mul(1000) } };
            var retour = HorsLigne.Crediter(etat, DEPART, DEPART + H);
            Assert.That(retour.Etat.Cycle.PaliersOuverts, Is.EqualTo(etat.Cycle.PaliersOuverts + 2));
            Assert.That(retour.Etat.Cycle.CreuxDemandes, Is.EqualTo(0));
        }

        [Test, Description("la file survit à la sauvegarde ; une save qui ne la connaît pas la lit vide")]
        public void La_file_se_sauvegarde()
        {
            var etat = Avec("0", demandes: 2);
            var save = Persistance.Serialiser(etat);
            Assert.That(Persistance.Deserialiser(save, Reducteur.EtatInitial(0)).Cycle.CreuxDemandes, Is.EqualTo(2));
            ((Newtonsoft.Json.Linq.JObject)save.Contenu["cycle"]).Remove("creuxDemandes");
            Assert.That(Persistance.Deserialiser(save, Reducteur.EtatInitial(0)).Cycle.CreuxDemandes, Is.EqualTo(0));
        }
    }
}
