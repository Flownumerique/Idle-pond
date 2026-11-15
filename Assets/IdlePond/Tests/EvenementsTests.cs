using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les événements de jeu et le rapport de retour (2026-11-13) : ce que l'écran et le son
    /// lisent pour savoir ce qui vient d'arriver. Ils se déduisent de deux états et
    /// n'ajoutent aucune règle.
    /// </summary>
    public class EvenementsTests
    {
        const long H = 3600_000;
        const long DEPART = 1_790_000_000_000L;

        static readonly Espece Vairon = Especes.Toutes[0];

        static EtatJeu Riche()
        {
            var etat = EtatDeTravail.Creer(contenance: "1e300");
            return etat with { Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e200") } };
        }

        static List<GenreDEvenement> Genres(IReadOnlyList<EvenementDeJeu> evenements) => evenements.Select(e => e.Genre).ToList();

        [Test, Description("deux fois le même état : aucun événement")]
        public void Le_meme_etat_ne_dit_rien()
        {
            var etat = Riche();
            Assert.That(Evenements.Entre(etat, etat), Is.Empty);
            Assert.That(Evenements.Entre(null, etat), Is.Empty);
        }

        [Test, Description("un pas de temps sans achat ne dit rien")]
        public void Un_pas_sans_achat_ne_dit_rien()
        {
            // Un premier pas constate les succès que l’état de départ portait déjà ; le
            // suivant n’a plus rien à dire.
            var etat = Reducteur.Tick(Riche(), 0.1);
            Assert.That(Evenements.Entre(etat, Reducteur.Tick(etat, 0.1)), Is.Empty);
        }

        [Test, Description("creuser dit le creux ouvert, et son index")]
        public void Creuser_dit_le_creux_ouvert()
        {
            var avant = Riche();
            var evenements = Evenements.Entre(avant, Reducteur.Creuser(avant));
            var creux = evenements.Single(e => e.Genre == GenreDEvenement.CreuxOuvert);
            Assert.That(creux.Valeur, Is.EqualTo(avant.Cycle.PaliersOuverts));
            Assert.That(creux.Automatique, Is.False);
        }

        [Test, Description("monter dit les crans, et chaque seuil franchi en chemin")]
        public void Monter_dit_les_crans_et_les_seuils_franchis()
        {
            var avant = Riche();
            var especes = avant.Cycle.Especes.ToDictionary(p => p.Key, p => p.Value);
            especes[Vairon.Id] = new EtatEspece(true, 8);
            avant = avant with { Cycle = avant.Cycle with { Especes = especes } };

            var unCran = Evenements.Entre(avant, Reducteur.Ameliorer(avant, Vairon.Id));
            Assert.That(Genres(unCran), Is.EqualTo(new[] { GenreDEvenement.CransMontes }));
            Assert.That(unCran[0].Sujet, Is.EqualTo(Vairon.Id));
            Assert.That(unCran[0].Valeur, Is.EqualTo(9));

            var auSeuil = Evenements.Entre(avant, Reducteur.AmeliorerJusquAuSeuil(avant, Vairon.Id));
            Assert.That(Genres(auSeuil), Is.EqualTo(new[] { GenreDEvenement.CransMontes, GenreDEvenement.SeuilFranchi }));
            Assert.That(auSeuil[1].Valeur, Is.EqualTo(10));
        }

        [Test, Description("convaincre une espèce se dit autrement que la monter")]
        public void Convaincre_se_dit_autrement_que_monter()
        {
            var depart = Reducteur.EtatInitial(1);
            depart = depart with { Cycle = depart.Cycle with { ManaCourant = Decimal.Parse("1e6") } };
            var evenements = Evenements.Entre(depart, Reducteur.Debloquer(depart, Vairon.Id));
            Assert.That(Genres(evenements), Has.Member(GenreDEvenement.EspeceConvaincue));
            Assert.That(Genres(evenements), Has.No.Member(GenreDEvenement.CransMontes));
        }

        [Test, Description("grandir, un bonus, une amélioration : chacun son événement, avec son rang")]
        public void Grandir_un_bonus_une_amelioration()
        {
            var avant = Riche();
            Assert.That(Genres(Evenements.Entre(avant, Reducteur.Grandir(avant))), Has.Member(GenreDEvenement.HerosGrandi));

            var bonus = Evenements.Entre(avant, Reducteur.AcheterUnBonus(avant, "noue-vase")).Single(e => e.Genre == GenreDEvenement.BonusAchete);
            Assert.That(bonus.Sujet, Is.EqualTo("noue-vase"));
            Assert.That(bonus.Valeur, Is.EqualTo(1));

            var souffle = avant with { Permanent = avant.Permanent with { Souffle = new Decimal(100) } };
            var amelioration = Evenements.Entre(souffle, Reducteur.AcheterUneAmelioration(souffle, AmeliorationsDeRenaissance.GLOBALE_ID))
                .Single(e => e.Genre == GenreDEvenement.AmeliorationAchetee);
            Assert.That(amelioration.Sujet, Is.EqualTo(AmeliorationsDeRenaissance.GLOBALE_ID));
        }

        [Test, Description("une renaissance se dit seule : la vie d'après ne se compare pas à celle d'avant")]
        public void Une_renaissance_se_dit_seule()
        {
            var avant = Riche();
            var evenements = Evenements.Entre(avant, Renaissance.Renaitre(avant));
            Assert.That(evenements[0].Genre, Is.EqualTo(GenreDEvenement.Renaissance));
            Assert.That(evenements[0].Valeur, Is.EqualTo(avant.Permanent.NombreDeRenaissances + 1));
            Assert.That(Genres(evenements), Has.No.Member(GenreDEvenement.CreuxOuvert));
            Assert.That(Genres(evenements), Has.No.Member(GenreDEvenement.DescenteRouverte));
        }

        [Test, Description("la partie publie les événements d'un geste, et marque ceux d'un verbe")]
        public void La_partie_publie_les_evenements()
        {
            var partie = new Partie(new HorlogeFigee(DEPART), Riche());
            var recus = new List<EvenementDeJeu>();
            partie.EvenementsDeJeu += evenements => recus.AddRange(evenements);

            partie.Creuser();
            Assert.That(recus.Count(e => e.Genre == GenreDEvenement.CreuxOuvert), Is.EqualTo(1));
            Assert.That(recus.All(e => !e.Automatique), Is.True);

            // Avec la sonde, c'est le pas de jeu qui creuse : l'événement est marqué.
            recus.Clear();
            var densites = partie.Etat.Permanent.Densites.ToArray();
            densites[partie.Etat.Cycle.PaliersOuverts] = 1;
            var avecLaSonde = partie.Etat with
            {
                Permanent = partie.Etat.Permanent with { Densites = densites, NoeudsTechnique = new[] { "creusement-galeries" } },
            };
            partie.Remplacer(avecLaSonde);
            partie.Avancer(0.1);
            var creux = recus.Where(e => e.Genre == GenreDEvenement.CreuxOuvert).ToList();
            Assert.That(creux, Has.Count.EqualTo(1));
            Assert.That(creux[0].Automatique, Is.True);
        }

        [Test, Description("le rapport de retour dit ce que l'absence a rapporté")]
        public void Le_rapport_de_retour_dit_ce_que_l_absence_a_rapporte()
        {
            var depart = EtatDeTravail.Creer();
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, depart);
            Assert.That(partie.Rapport, Is.Null);

            horloge.AvancerMs(3 * H);
            partie.Reprendre();

            var rapport = partie.Rapport;
            Assert.That(rapport, Is.Not.Null);
            Assert.That(rapport.SecondesCreditees, Is.EqualTo(3 * 3600.0));
            Assert.That(rapport.SecondesAbsentes, Is.EqualTo(3 * 3600.0));
            Assert.That(rapport.Plafonnee, Is.False);
            Assert.That(rapport.ContenanceGagnee.Eq(Decimal.Zero), Is.True, "absent, on ne se fait pas à l'eau : la contenance ne bouge pas");
            var gagne = partie.Etat.Cycle.ManaCourant.Sub(depart.Cycle.ManaCourant);
            Assert.That(rapport.ManaGagne.ToString(), Is.EqualTo((gagne.Gt(0) ? gagne : Decimal.Zero).ToString()));
            Assert.That(rapport.StockPlein, Is.EqualTo(Economie.EstSature(partie.Etat)));

            partie.OublierRetour();
            Assert.That(partie.Rapport, Is.Null);
        }

        [Test, Description("une absence plus longue que le plafond hors ligne est dite plafonnée")]
        public void Une_absence_trop_longue_est_dite_plafonnee()
        {
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, EtatDeTravail.Creer());
            horloge.AvancerMs(48 * H);
            partie.Reprendre();
            Assert.That(partie.Rapport.Plafonnee, Is.True);
            Assert.That(partie.Rapport.SecondesAbsentes, Is.EqualTo(48 * 3600.0));
            Assert.That(partie.Rapport.SecondesCreditees, Is.EqualTo(HorsLigne.CapHorsLigneCourantHeures(EtatDeTravail.Creer()) * 3600).Within(1e-6));
        }

        [Test, Description("une absence trop brève n'a ni retour ni rapport")]
        public void Une_absence_trop_breve_n_a_pas_de_rapport()
        {
            var horloge = new HorlogeFigee(DEPART);
            var partie = new Partie(horloge, EtatDeTravail.Creer());
            horloge.AvancerMs(5_000);
            partie.Reprendre();
            Assert.That(partie.Retour, Is.Null);
            Assert.That(partie.Rapport, Is.Null);
        }
    }
}
