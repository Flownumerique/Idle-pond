/*
 * IdlePond — la sauvegarde, depuis l'éditeur.
 *
 * Le chemin de migration des joueurs du web passe par ici : leur save est la
 * valeur `idlepond` du localStorage de la version web (outils de développement
 * du navigateur ▸ Application ▸ Local Storage). Copiée, puis collée par
 * `IdlePond ▸ Sauvegarde ▸ Importer depuis le presse-papiers`, elle se relit à
 * l'identique — migrations comprises, puisque le format est le même.
 */
using System.IO;
using IdlePond.Adaptateurs;
using IdlePond.Etat;
using IdlePond.Interface;
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    public static class OutilsDeSauvegarde
    {
        private static string CheminDuFichier() => new StockageFichier(Application.persistentDataPath).Chemin(Magasin.ClefDeSauvegarde);

        [MenuItem("IdlePond/Sauvegarde/Importer depuis le presse-papiers", priority = 20)]
        public static void Importer()
        {
            var texte = EditorGUIUtility.systemCopyBuffer;
            // Validée sur un magasin jetable avant de toucher à la vraie save.
            var essai = new Magasin(new StockageEnMemoire(), HorlogeSysteme.Instance);
            if (string.IsNullOrWhiteSpace(texte) || !essai.EssayerDImporter(texte))
            {
                EditorUtility.DisplayDialog("IdlePond", "Le presse-papiers ne contient pas une sauvegarde lisible.", "Fermer");
                return;
            }

            if (Application.isPlaying && Jeu.Courant != null)
            {
                Jeu.Courant.Importer(texte);
            }
            else
            {
                StockageUnity.ParDefaut().Ecrire(Magasin.ClefDeSauvegarde, texte);
            }
            EditorUtility.DisplayDialog("IdlePond",
                "Sauvegarde importée : " + essai.Etat.Permanent.NombreEclosions + " retours dans l’œuf, "
                + essai.Etat.Permanent.Succes.Count + " succès.", "Fermer");
        }

        [MenuItem("IdlePond/Sauvegarde/Copier dans le presse-papiers", priority = 21)]
        public static void Exporter()
        {
            string texte;
            if (Application.isPlaying && Jeu.Courant != null) texte = Jeu.Courant.Magasin.Exporter();
            else texte = StockageUnity.ParDefaut().Lire(Magasin.ClefDeSauvegarde);
            if (texte == null)
            {
                EditorUtility.DisplayDialog("IdlePond", "Aucune sauvegarde pour l'instant.", "Fermer");
                return;
            }
            EditorGUIUtility.systemCopyBuffer = texte;
            Debug.Log("IdlePond : sauvegarde copiée (" + texte.Length + " caractères). Elle se colle telle quelle dans le localStorage du web.");
        }

        [MenuItem("IdlePond/Sauvegarde/Ouvrir le dossier", priority = 22)]
        public static void OuvrirLeDossier()
        {
            var chemin = CheminDuFichier();
            EditorUtility.RevealInFinder(File.Exists(chemin) ? chemin : Application.persistentDataPath);
        }

        [MenuItem("IdlePond/Sauvegarde/Effacer…", priority = 40)]
        public static void Effacer()
        {
            if (!EditorUtility.DisplayDialog("IdlePond", "Effacer la sauvegarde et repartir de l’œuf ?", "Effacer", "Annuler")) return;
            if (Application.isPlaying && Jeu.Courant != null) Jeu.Courant.Reinitialiser();
            StockageUnity.ParDefaut().Effacer(Magasin.ClefDeSauvegarde);
            new StockagePlayerPrefs().Effacer(Magasin.ClefDeSauvegarde);
        }
    }
}
