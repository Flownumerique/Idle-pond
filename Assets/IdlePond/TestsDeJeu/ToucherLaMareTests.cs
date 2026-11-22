using System.Collections;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using IdlePond.Noyau;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Toucher la mare (spec du 2026-10-11, §2) : la couche `#toucher` couvre la mare sous ses
    /// boutons ; la toucher monte le mana et fait s'envoler le gain.
    /// </summary>
    public class ToucherLaMareTests
    {
        const long DEPART_MS = 1_700_000_000_000L;

        HorlogeFigee horloge;
        Partie partie;

        [SetUp]
        public void Preparer()
        {
            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            horloge = new HorlogeFigee(DEPART_MS);
            partie = new Partie(horloge);
            ServicesDePartie.Installer(partie);
        }

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        [UnityTest]
        public IEnumerator Toucher_la_mare_monte_le_mana_et_montre_le_gain()
        {
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;

            VisualElement couche = null;
            for (var i = 0; i < 120 && couche == null; i++)
            {
                yield return null;
                var document = Object.FindAnyObjectByType<UIDocument>();
                if (document != null && document.rootVisualElement != null && document.rootVisualElement.Q<Label>("mana-valeur") != null)
                    couche = document.rootVisualElement.Q<VisualElement>("toucher");
            }
            Assert.That(couche, Is.Not.Null, "la couche #toucher manque");
            yield return null;

            var avant = partie.Etat.Cycle.ManaCourant;
            var valeur = Economie.ValeurDuToucher(partie.Etat);
            // Loin du bouton « Creuser plus bas », posé au milieu de la mare.
            var cadre = couche.worldBound;
            yield return Doigt.ToucherEn(couche, new Vector2(cadre.x + cadre.width * 0.2f, cadre.y + cadre.height * 0.2f));

            // La boucle tourne en temps réel et ne fait qu'ajouter : au moins un toucher entier.
            Assert.That(partie.Etat.Cycle.ManaCourant.Sub(avant).Gte(valeur.Mul(0.999)), Is.True, "toucher la mare n'a rien rapporté");
            var envols = couche.Query<Label>(className: "envol").ToList().Where(l => l.resolvedStyle.display == DisplayStyle.Flex).ToList();
            Assert.That(envols, Has.Count.EqualTo(1), "aucun chiffre ne s'envole");
            Assert.That(envols[0].text, Is.EqualTo("+" + Format.Montant(valeur)));
        }

        /// La mare chargée, la couche trouvée ; null si elle manque.
        static IEnumerator Charger(System.Action<VisualElement> rendre)
        {
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;
            VisualElement couche = null;
            for (var i = 0; i < 120 && couche == null; i++)
            {
                yield return null;
                var document = Object.FindAnyObjectByType<UIDocument>();
                if (document != null && document.rootVisualElement != null && document.rootVisualElement.Q<Label>("mana-valeur") != null)
                    couche = document.rootVisualElement.Q<VisualElement>("toucher");
            }
            yield return null;
            rendre(couche);
        }

        static System.Collections.Generic.List<Label> Envols(VisualElement couche) =>
            couche.Query<Label>(className: "envol").ToList().Where(l => l.resolvedStyle.display == DisplayStyle.Flex).ToList();

        static IEnumerator Toucher(VisualElement couche)
        {
            var cadre = couche.worldBound;
            yield return Doigt.ToucherEn(couche, new Vector2(cadre.x + cadre.width * 0.2f, cadre.y + cadre.height * 0.2f));
        }

        /// La mare pleine, à appeler juste avant le toucher : la boucle tourne en temps réel, et
        /// l'acquis qui monte la viderait d'une image à l'autre.
        void Remplir(double acquis)
        {
            var etat = partie.Etat with { Cycle = partie.Etat.Cycle with { AcquisDeSejour = acquis } };
            partie.Remplacer(etat with { Cycle = etat.Cycle with { ManaCourant = Economie.Contenance(etat) } });
        }

        [UnityTest]
        public IEnumerator Mare_pleine_le_toucher_dit_ce_qu_on_porte_de_plus()
        {
            VisualElement couche = null;
            yield return Charger(c => couche = c);
            Assert.That(couche, Is.Not.Null, "la couche #toucher manque");
            Remplir(0);
            yield return Toucher(couche);
            var envols = Envols(couche);
            Assert.That(envols, Has.Count.EqualTo(1), "rien ne dit ce que le toucher a fait porter");
            var suffixe = Noyau.Donnees.Textes.Ecran.GAIN_A_PORTER.Substring(Noyau.Donnees.Textes.Ecran.GAIN_A_PORTER.IndexOf('}') + 1);
            Assert.That(envols[0].text, Does.StartWith("+").And.EndWith(suffixe));
            Assert.That(envols[0].text, Does.Not.StartWith("+0.0").And.Not.StartWith("+0 "), "un zéro s'envole");
        }

        [UnityTest]
        public IEnumerator Mare_pleine_et_acquis_au_plafond_rien_ne_s_envole()
        {
            VisualElement couche = null;
            yield return Charger(c => couche = c);
            Assert.That(couche, Is.Not.Null, "la couche #toucher manque");
            Remplir(Constantes.ACQUIS_MAX);
            yield return Toucher(couche);
            Assert.That(Envols(couche), Is.Empty);
        }
    }
}
