using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// RESULTATS.md, finding 3 : le contenu réel de finding 3 est l'état
    /// DÉGÉNÉRÉ — si plus rien ne produisait après que la charge de départ
    /// (`MANA_A_LA_SORTIE_DE_L_OEUF`) a été dépensée, le mana resterait à zéro
    /// pour toujours et aucune espèce ne serait plus jamais débloquée.
    /// `DEBIT_HEROS` (§13.3) ferme ce trou-là : le héros capte l'ambiant tout
    /// seul, sans attendre aucune espèce.
    ///
    /// Les deux mécanismes — la charge et le débit — coexistent délibérément
    /// (voir le commentaire de `MANA_A_LA_SORTIE_DE_L_OEUF` dans `Constantes.cs`
    /// pour la mesure qui l'a établi) : la charge tient §8.4 (premier succès
    /// quasi immédiat), le débit tient finding 3 (jamais de zéro permanent). Cette
    /// classe teste les deux séparément — le second test ci-dessous ANNULE la
    /// charge dans sa propre fixture pour isoler le débit, sans y toucher dans le
    /// jeu réel.
    ///
    /// Le test « trois paliers valent D³ » du brief n'est pas repris ici : c'est un
    /// doublon exact de `CanonTests` (« le multiplicateur de palier porte la part
    /// de D que le bestiaire ne porte pas »), qui verrouille déjà
    /// `MultiplicateurDePalier`. Cette classe ne couvre que l'amorçage.
    /// </summary>
    public class AmorcageTests
    {
        [Test, Description("à l’état initial, sans aucune espèce, la production est strictement positive")]
        public void A_l_etat_initial_sans_aucune_espece_la_production_est_strictement_positive()
        {
            Assert.That(Economie.ProductionTotaleParSeconde(Reducteur.EtatInitial(1)).Gt(0), Is.True);
        }

        [Test, Description("le débit du héros, SEUL, finance la première espèce en moins de deux heures")]
        public void Le_debit_du_heros_SEUL_finance_la_premiere_espece_en_moins_de_deux_heures()
        {
            // Fixture : la charge de départ est mise à zéro ICI, dans ce seul test, pour
            // isoler le mécanisme que ce test discrimine — `DEBIT_HEROS` — du mécanisme
            // qui tient normalement le premier achat, `MANA_A_LA_SORTIE_DE_L_OEUF`. Le
            // jeu réel garde les deux (`EtatInitial` n'est pas touché) : voir
            // `Constantes.cs` pour pourquoi ils ne sont pas interchangeables.
            var etat = Reducteur.EtatInitial(1);
            etat = etat with { Cycle = etat.Cycle with { ManaCourant = new Decimal(0) } };
            var premiere = Especes.Toutes[0];

            // À t = 0 le mana est nul dans cette fixture : sans ce plancher,
            // l'assertion suivante serait vraie même si le héros ne produisait rien,
            // et le test ne prouverait rien.
            Assert.That(etat.Cycle.ManaCourant.Lt(Economie.CoutDeDeblocage(etat, premiere)), Is.True);

            etat = Reducteur.Tick(etat, 2 * 3600);
            Assert.That(etat.Cycle.ManaCourant.Gte(Economie.CoutDeDeblocage(etat, premiere)), Is.True);

            etat = Reducteur.Debloquer(etat, premiere.Id);
            Assert.That(etat.Cycle.Especes[premiere.Id].Debloquee, Is.True);
        }

        [Test, Description("le débit du héros devient négligeable dès la première espèce montée")]
        public void Le_debit_du_heros_devient_negligeable_des_la_premiere_espece_montee()
        {
            var etat = Reducteur.EtatInitial(1);
            var heros = Economie.ProductionTotaleParSeconde(etat);

            etat = Reducteur.Tick(etat, 2 * 3600);
            etat = Reducteur.Debloquer(etat, Especes.Toutes[0].Id);

            // Le brief ne montait aucun niveau ici, et son ratio > 10 était donc
            // inatteignable à 0.05 mana/s + 0.2 mana/s (ratio 5). « Négligeable dès la
            // première espèce MONTÉE » exige d'acheter des niveaux tant que le mana le
            // permet, pas seulement de laisser filer le temps.
            for (var n = 0; n < 20; n += 1)
            {
                etat = Reducteur.Tick(etat, 600);
                var suivant = Reducteur.Ameliorer(etat, Especes.Toutes[0].Id);
                while (!ReferenceEquals(suivant, etat))
                {
                    etat = suivant;
                    suivant = Reducteur.Ameliorer(etat, Especes.Toutes[0].Id);
                }
            }

            Assert.That(Economie.ProductionTotaleParSeconde(etat).Div(heros).Gt(10), Is.True);
        }
    }
}
