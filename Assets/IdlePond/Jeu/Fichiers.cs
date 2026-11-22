using System.IO;

namespace IdlePond.Jeu
{
    /// Les écritures par fichier temporaire, en un seul endroit.
    public static class Fichiers
    {
        /// <summary>
        /// Met `temporaire` à la place de `cible`. `File.Replace` n'est pas garanti sur le
        /// système de fichiers d'un navigateur (WebGL, Emscripten) : là, on efface puis on
        /// déplace — une coupure entre les deux laisse le `.tmp` complet, que rien ne perd.
        /// </summary>
        public static void Remplacer(string temporaire, string cible)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (File.Exists(cible)) File.Delete(cible);
            File.Move(temporaire, cible);
#else
            if (File.Exists(cible)) File.Replace(temporaire, cible, null);
            else File.Move(temporaire, cible);
#endif
        }
    }
}
