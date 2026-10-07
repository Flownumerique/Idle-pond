using System.Collections;
using System.IO;
using IdlePond.Jeu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Les ateliers (spec du 2026-10-07, §2) : quand la sauvegarde est redirigée, la `Boucle`
    /// écrit là, et seulement là. La sauvegarde du joueur, dans `persistentDataPath`, n'est
    /// ni créée, ni touchée.
    /// </summary>
    public class SauvegardeRedirigeeTests
    {
        string dossier;
        GameObject objet;

        /// Une scène vide, seule chargée : une Mare restée d'un test précédent ferait tourner
        /// une autre `Boucle`, qui écrirait de son côté pendant les 10 s d'attente.
        [UnitySetUp]
        public IEnumerator Preparer()
        {
            var vide = SceneManager.CreateScene("Vide-" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(vide);
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene != vide) yield return SceneManager.UnloadSceneAsync(scene);
            }
            ServicesDePartie.Oublier();
            dossier = Path.Combine(Application.temporaryCachePath, "idlepond-atelier-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dossier);
        }

        [TearDown]
        public void Ranger()
        {
            if (objet != null) Object.Destroy(objet);
            ServicesDePartie.Oublier();
            if (Directory.Exists(dossier)) Directory.Delete(dossier, true);
        }

        [UnityTest]
        public IEnumerator La_boucle_redirigee_sauvegarde_dans_le_dossier_de_l_atelier()
        {
            var duJoueur = Persistance.CheminDeLaSauvegarde(Application.persistentDataPath);
            var avant = File.Exists(duJoueur) ? File.GetLastWriteTimeUtc(duJoueur) : (System.DateTime?)null;

            ServicesDePartie.RedirigerLaSauvegarde(dossier);
            ServicesDePartie.Installer(new Partie(new HorlogeFigee(1_700_000_000_000L)));
            objet = new GameObject("Boucle");
            objet.AddComponent<Boucle>();

            // La boucle sauvegarde toutes les 10 s.
            yield return new WaitForSecondsRealtime(10.5f);

            Assert.That(File.Exists(Persistance.CheminDeLaSauvegarde(dossier)), Is.True, "rien d'écrit dans le dossier de l'atelier");
            var apres = File.Exists(duJoueur) ? File.GetLastWriteTimeUtc(duJoueur) : (System.DateTime?)null;
            Assert.That(apres, Is.EqualTo(avant), "la sauvegarde du joueur a été touchée");
        }
    }
}
