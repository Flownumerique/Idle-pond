using System.IO;
using IdlePond.Jeu;
using UnityEngine;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Tout test qui charge la Mare y fait vivre une `Boucle`, et une `Boucle` sauvegarde
    /// toutes les 10 s — dans `persistentDataPath`, la partie du joueur, si rien ne la
    /// redirige. Une scène restée chargée d'un test au suivant l'a fait (2026-10-07).
    ///
    /// Le dossier est fixe et jamais supprimé : une boucle encore vivante après le
    /// `TearDown` doit pouvoir y écrire sans lever d'exception dans le test d'après.
    /// À appeler APRÈS `ServicesDePartie.Oublier`, qui efface la redirection.
    /// </summary>
    static class SauvegardeDeTest
    {
        public static void Rediriger()
        {
            var dossier = Path.Combine(Application.temporaryCachePath, "idlepond-tests");
            Directory.CreateDirectory(dossier);
            // Les réglages vivent dans le même dossier : un format forcé par un test ne doit
            // pas se retrouver dans le suivant.
            var reglages = Path.Combine(dossier, MagasinDeReglages.NOM_DU_FICHIER);
            if (File.Exists(reglages)) File.Delete(reglages);
            ServicesDePartie.RedirigerLaSauvegarde(dossier);
        }
    }
}
