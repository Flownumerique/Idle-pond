using System.Collections;
using System.IO;
using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Le critère de réussite de l'étape 6 (spec §3, « PlayMode ») : la mare se joue.
    /// Une partie à horloge figée, une minute simulée, puis on vérifie qu'un premier achat
    /// a eu lieu, que l'interface en rend compte et que la sauvegarde s'écrit.
    ///
    /// PRÉREQUIS : `Mare` doit figurer dans les Build Settings. Le générateur de scènes
    /// (menu « IdlePond ▸ Générer les scènes ») l'y inscrit, après `Demarrage` ; sans lui,
    /// `SceneManager` refuse de la charger, même en PlayMode dans l'éditeur.
    ///
    /// Le test charge `Mare` plutôt que `Demarrage` : `Demarrage` ouvrirait la partie du
    /// disque, alors que celui-ci veut la sienne — une partie neuve, à horloge figée,
    /// installée dans `ServicesDePartie` avant le chargement. `Mare` la reprend telle
    /// quelle (c'est le chemin « scène lancée seule » du §2).
    ///
    /// La `Boucle` de la scène tourne pendant ce temps, sur le temps réel : à quelques
    /// millisecondes près, elle ajoute au plus un tick à la partie. Le test ne compare donc
    /// jamais un chiffre exact de la partie, seulement la concordance de l'écran avec elle.
    /// </summary>
    public class MareJouableTests
    {
        const long DEPART_MS = 1_700_000_000_000L;

        string dossier;
        HorlogeFigee horloge;
        Partie partie;

        [SetUp]
        public void Preparer()
        {
            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            dossier = Path.Combine(Application.temporaryCachePath, "idlepond-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dossier);
            horloge = new HorlogeFigee(DEPART_MS);
            partie = new Partie(horloge);
            ServicesDePartie.Installer(partie);
        }

        [TearDown]
        public void Ranger()
        {
            ServicesDePartie.Oublier();
            if (Directory.Exists(dossier)) Directory.Delete(dossier, true);
        }

        [UnityTest]
        public IEnumerator Une_minute_de_jeu_donne_un_achat_un_Souffle_a_l_ecran_et_une_sauvegarde()
        {
            Assert.That(Application.CanStreamedLevelBeLoaded("Mare"), Is.True,
                "Mare n'est pas dans les Build Settings : lancer « IdlePond ▸ Générer les scènes ».");
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;

            // L'interface se branche à l'activation de son contrôleur, une image après le
            // chargement au plus. On attend ses éléments plutôt qu'un nombre d'images.
            UIDocument document = null;
            Label souffle = null;
            for (var i = 0; i < 120 && souffle == null; i++)
            {
                yield return null;
                document = Object.FindAnyObjectByType<UIDocument>();
                if (document != null && document.rootVisualElement != null)
                    souffle = document.rootVisualElement.Q<Label>("souffle-valeur");
            }
            Assert.That(souffle, Is.Not.Null, "l'interface ne s'est jamais branchée sur l'UIDocument de Mare");
            Assert.That(document.GetComponent<RacineDeLInterface>(), Is.Not.Null);
            Assert.That(document.GetComponent<RacineDeLInterface>().CadreDeScene, Is.Not.Null, "l'élément #scene manque");

            // Le premier achat : le héros sort de l'œuf avec exactement de quoi convaincre
            // la première espèce, et l'achat se fait par l'API de la partie, comme le bouton.
            var premiere = Especes.Toutes[0].Id;
            partie.Convaincre(premiere);
            Assert.That(partie.Etat.Cycle.Especes[premiere].Debloquee, Is.True, "le premier achat n'a pas eu lieu");

            // Soixante secondes simulées : un pas d'une seconde, soixante fois. L'horloge
            // figée avance avec elles, pour que la sauvegarde date de la fin de la minute.
            for (var seconde = 0; seconde < 60; seconde++)
            {
                partie.Avancer(1.0);
                horloge.AvancerMs(1000);
            }
            yield return null;

            Assert.That(partie.Etat.TempsJeuSecondes, Is.GreaterThanOrEqualTo(60.0));

            // L'écran affiche un Souffle (zéro avant la première renaissance) ET suit la partie.
            var racine = document.rootVisualElement;
            Assert.That(souffle.text, Is.EqualTo(Format.Montant(partie.Etat.Permanent.Souffle)));
            Assert.That(racine.Q<Label>("mana-valeur").text, Is.EqualTo(Format.Montant(partie.Etat.Cycle.ManaCourant)),
                "le mana affiché ne suit pas la partie");
            Assert.That(racine.Q<VisualElement>("espece-" + premiere), Is.Not.Null, "la carte de la première espèce manque");
            Assert.That(racine.Q<VisualElement>("annonces").childCount, Is.GreaterThan(0),
                "le premier succès n'a pas été annoncé");

            // La sauvegarde est écrite, relisible, et porte l'achat.
            partie.Sauvegarder(dossier);
            Assert.That(File.Exists(Persistance.CheminDeLaSauvegarde(dossier)), Is.True, "la sauvegarde n'a pas été écrite");
            var relue = Persistance.Charger(dossier, horloge);
            Assert.That(relue.NouvellePartie, Is.False);
            Assert.That(relue.Etat.Cycle.Especes[premiere].Debloquee, Is.True);
            Assert.That(relue.Etat.TempsJeuSecondes, Is.GreaterThanOrEqualTo(60.0));
        }
    }
}
