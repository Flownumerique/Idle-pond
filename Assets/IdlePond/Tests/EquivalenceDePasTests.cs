using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Test d'équivalence de pas (§12, jalon v0.1).
    ///
    /// « 480 appels à dt = 60 s et 1 appel à dt = 8 h donnent le même état, à la
    /// tolérance flottante près. C'est le test qui garantit le hors ligne et le
    /// simulateur d'un seul coup. »
    ///
    /// Il est ici l'expression exécutable du filtre du §5.2 : toute mécanique du
    /// cœur doit se calculer en un seul pas pour dt = 8 heures. Une mécanique qui
    /// suivrait un individu, itérerait sur une file d'événements ou vérifierait à
    /// chaque tick une contrainte qui change une fois par heure ferait tomber ce
    /// test — c'est précisément à ça qu'il sert.
    ///
    /// Il est devenu facile à tenir le 2026-09-09, et c'est le bénéfice principal du
    /// modèle à niveau : la production ne dépend plus que de quantités qui ne
    /// changent qu'à l'achat, donc le pas est homogène par construction. Le noyau
    /// n'a plus de coupure de pas du tout.
    /// </summary>
    public class EquivalenceDePasTests
    {
        const double HUIT_HEURES = 8 * 3600;
        const double PAS = 60;
        const int NOMBRE_DE_PAS = (int)(HUIT_HEURES / PAS);

        static void Comparer(EtatJeu obtenu, EtatJeu attendu) =>
            Comparateur.ComparerATolerance(Instantane.De(obtenu, false), Instantane.De(attendu, false));

        [Test, Description("480 pas de 60 s valent un pas de 8 h")]
        public void _480_pas_de_60_s_valent_un_pas_de_8_h()
        {
            var depart = EtatDeTravail.Creer();
            var parPetitsPas = depart;
            for (var i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, PAS);
            var enUnPas = Reducteur.Tick(depart, HUIT_HEURES);
            Comparer(parPetitsPas, enUnPas);
        }

        [Test, Description("la croissance du séjour ne coupe pas le pas")]
        public void La_croissance_du_sejour_ne_coupe_pas_le_pas()
        {
            // `τ` dépend maintenant de la profondeur ATTEINTE (amendement v1.2), qui ne
            // bouge que sur un acte du joueur — jamais pendant un tick. La forme
            // exponentielle reste donc exacte pour n'importe quel `dt`. Si un jour `τ`
            // se mettait à dépendre d'une grandeur qui bouge DANS le pas, c'est ici que
            // cela se verrait, et c'est tout l'objet du §5.2.
            var depart = EtatDeTravail.Creer() with { Reglage = new Reglage(1.2) };
            Assert.That(depart.Permanent.ProfondeurMaxAtteinte, Is.GreaterThan(0));
            var parPetitsPas = depart;
            for (var i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, PAS);
            Comparer(parPetitsPas, Reducteur.Tick(depart, HUIT_HEURES));
        }

        [Test, Description("la cadence de jeu à 100 ms vaut elle aussi un seul pas")]
        public void La_cadence_de_jeu_a_100_ms_vaut_elle_aussi_un_seul_pas()
        {
            var depart = EtatDeTravail.Creer();
            const int duree = 600;
            var parTicks = depart;
            for (var i = 0; i < duree * 10; i += 1) parTicks = Reducteur.Tick(parTicks, 0.1);
            Comparer(parTicks, Reducteur.Tick(depart, duree));
        }

        [Test, Description("le plafonnement du stock par la contenance compose lui aussi")]
        public void Le_plafonnement_du_stock_par_la_contenance_compose_lui_aussi()
        {
            // Contenance basse : le mana sature en cours d'intervalle. C'est le cas où
            // une contenance dérivée de la production courante ferait diverger les deux
            // chemins ; elle est un état permanent, donc constante pendant le pas.
            var depart = EtatDeTravail.Creer(999, "1e5");
            var parPetitsPas = depart;
            for (var i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, PAS);
            var enUnPas = Reducteur.Tick(depart, HUIT_HEURES);
            Assert.That(enUnPas.Cycle.ManaCourant.Eq(Economie.Contenance(enUnPas)), Is.True);
            Assert.That(enUnPas.Permanent.ManaAmbiant.Gt(0), Is.True);
            Comparer(parPetitsPas, enUnPas);
        }

        [Test, Description("le hors ligne à 6 h se crédite en un seul appel")]
        public void Le_hors_ligne_a_6_h_se_credite_en_un_seul_appel()
        {
            var depart = EtatDeTravail.Creer();
            var parPetitsPas = depart;
            for (var i = 0; i < 6 * 60; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, 60);
            Comparer(parPetitsPas, Reducteur.Tick(depart, 6 * 3600));
        }

        [Test, Description("l'état de départ produit bien quelque chose, sinon le test ne prouve rien")]
        public void L_etat_de_depart_produit_bien_quelque_chose_sinon_le_test_ne_prouve_rien()
        {
            Assert.That(Economie.ProductionTotaleParSeconde(EtatDeTravail.Creer()).Gt(0), Is.True);
        }

        [Test, Description("plus aucun seuil ne peut tomber À L’INTÉRIEUR d’un pas")]
        public void Plus_aucun_seuil_ne_peut_tomber_A_L_INTERIEUR_d_un_pas()
        {
            // Le cas qui a cassé à l'amendement v1.1 §2.C — les petits pas
            // franchissaient 10, 25 et 50 tôt et produisaient plus que le grand pas —
            // n'existe plus : le multiplicateur de seuil lit un NIVEAU, et un niveau ne
            // change qu'à l'achat. Ce qui se vérifie ici est donc l'inverse de ce qui
            // se vérifiait avant : le pas est homogène, et il le reste.
            var depart = Reducteur.EtatInitial(1);
            var espece = Especes.Toutes[0];
            var proche = depart with
            {
                Cycle = depart.Cycle with
                {
                    ManaCourant = new Decimal(0),
                    Especes = new Dictionary<string, EtatEspece> { [espece.Id] = new EtatEspece(true, 99) },
                },
                Permanent = depart.Permanent with { ContenanceMana = Decimal.Parse("1e30") },
            };

            var parPetitsPas = proche;
            for (var i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, PAS);
            var enUnPas = Reducteur.Tick(proche, HUIT_HEURES);

            // Ni le seuil de cent, ni le drapeau qu'il pose, ne sont tombés tout seuls.
            Assert.That(Economie.MultiplicateurDeSeuil(enUnPas.Cycle.Especes[espece.Id].Niveau), Is.EqualTo(8));
            Assert.That(enUnPas.Permanent.EspecesAyantAtteintCent, Is.Empty);
            Comparer(parPetitsPas, enUnPas);
        }
    }
}
