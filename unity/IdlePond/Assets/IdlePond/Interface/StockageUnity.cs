/*
 * IdlePond — où la save se range, selon la plateforme.
 *
 * Le seul endroit du jeu qui touche au disque. Le magasin ne connaît que
 * `IStockage` ; ce fichier choisit :
 *
 *   - natif (Windows, macOS, Linux, Android, iOS) : un fichier JSON dans
 *     `Application.persistentDataPath`, écrit à côté puis substitué, avec une
 *     copie de secours — une coupure pendant l'écriture ne coûte jamais la
 *     partie ;
 *   - WebGL : PlayerPrefs, que Unity range dans l'IndexedDB du navigateur. Le
 *     système de fichiers virtuel de WebGL ne se synchronise pas tout seul, et
 *     une save perdue au rechargement serait pire qu'aucune.
 *
 * Le contenu est le même partout : la chaîne que le web range sous la clef
 * `idlepond` de son localStorage.
 */
using System;
using System.IO;
using System.Text;
using IdlePond.Etat;
using UnityEngine;

namespace IdlePond.Interface
{
    public static class StockageUnity
    {
        public static IStockage ParDefaut()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new StockagePlayerPrefs();
#else
            return new StockageFichier(Application.persistentDataPath);
#endif
        }
    }

    public sealed class StockageFichier : IStockage
    {
        private static readonly Encoding Utf8SansBom = new UTF8Encoding(false);

        private readonly string _dossier;

        public StockageFichier(string dossier) => _dossier = dossier;

        public string Chemin(string clef) => Path.Combine(_dossier, clef + ".json");

        private string Secours(string clef) => Path.Combine(_dossier, clef + ".secours.json");

        public string Lire(string clef)
        {
            foreach (var chemin in new[] { Chemin(clef), Secours(clef) })
            {
                try
                {
                    if (File.Exists(chemin)) return File.ReadAllText(chemin, Utf8SansBom);
                }
                catch (IOException erreur)
                {
                    Debug.LogWarning("IdlePond : save illisible (" + chemin + ") — " + erreur.Message);
                }
            }
            return null;
        }

        public void Ecrire(string clef, string valeur)
        {
            Directory.CreateDirectory(_dossier);
            var chemin = Chemin(clef);
            var provisoire = chemin + ".tmp";
            File.WriteAllText(provisoire, valeur, Utf8SansBom);
            try
            {
                if (File.Exists(chemin)) File.Replace(provisoire, chemin, Secours(clef));
                else File.Move(provisoire, chemin);
            }
            catch (Exception)
            {
                // Certains systèmes de fichiers refusent `Replace` : on retombe sur une
                // copie, moins atomique mais qui ne perd rien.
                File.Copy(provisoire, chemin, true);
                File.Delete(provisoire);
            }
        }

        public void Effacer(string clef)
        {
            foreach (var chemin in new[] { Chemin(clef), Secours(clef), Chemin(clef) + ".tmp" })
            {
                if (File.Exists(chemin)) File.Delete(chemin);
            }
        }
    }

    public sealed class StockagePlayerPrefs : IStockage
    {
        public string Lire(string clef) => PlayerPrefs.HasKey(clef) ? PlayerPrefs.GetString(clef) : null;

        public void Ecrire(string clef, string valeur)
        {
            PlayerPrefs.SetString(clef, valeur);
            PlayerPrefs.Save();
        }

        public void Effacer(string clef)
        {
            PlayerPrefs.DeleteKey(clef);
            PlayerPrefs.Save();
        }
    }
}
