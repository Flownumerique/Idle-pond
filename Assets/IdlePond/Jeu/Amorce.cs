using UnityEngine;
using UnityEngine.SceneManagement;

namespace IdlePond.Jeu
{
    /// <summary>
    /// IdlePond — l'amorce de `Demarrage.unity` : charger la sauvegarde, créditer l'absence
    /// (`Partie.Ouvrir` fait les deux), installer la partie, puis passer à la mare.
    ///
    /// Aucun objet `DontDestroyOnLoad` : `ServicesDePartie` est un singleton C# statique,
    /// qui survit de lui-même au changement de scène. Un objet persistant ne ferait que
    /// laisser une seconde boucle tourner après le retour à cette scène.
    /// </summary>
    public sealed class Amorce : MonoBehaviour
    {
        void Start()
        {
            var partie = Partie.Ouvrir(new HorlogeSysteme(), Application.persistentDataPath);
            ServicesDePartie.Installer(partie);
            SceneManager.LoadScene("Mare");
        }
    }
}
