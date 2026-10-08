using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdlePond.Jeu
{
    /// <summary>
    /// IdlePond — l'amorce de `Demarrage.unity` : charger la sauvegarde, créditer l'absence
    /// (`Partie.Ouvrir` fait les deux), installer la partie, demander l'écran d'accueil, puis
    /// passer à la mare. « Première fois » : aucune sauvegarde n'existait avant d'ouvrir.
    ///
    /// Aucun objet `DontDestroyOnLoad` : `ServicesDePartie` est un singleton C# statique,
    /// qui survit de lui-même au changement de scène. Un objet persistant ne ferait que
    /// laisser une seconde boucle tourner après le retour à cette scène.
    /// </summary>
    public sealed class Amorce : MonoBehaviour
    {
        void Start()
        {
            var dossier = Application.persistentDataPath;
            var premiereFois = !File.Exists(Persistance.CheminDeLaSauvegarde(dossier));
            var partie = Partie.Ouvrir(new HorlogeSysteme(), dossier);
            ServicesDePartie.Installer(partie);
            ServicesDePartie.DemanderLAccueil(premiereFois);
            SceneManager.LoadScene("Mare");
        }
    }
}
