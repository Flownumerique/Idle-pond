using System;
using IdlePond.Noyau;
using UnityEngine;

namespace IdlePond.Jeu
{
    /// <summary>
    /// IdlePond — la boucle de `Mare.unity` : l'unique pont entre l'horloge d'Unity et la
    /// `Partie`, qui n'en sait rien.
    ///
    /// Elle cumule le temps des images, avance la partie par pas FIXES de
    /// `PERIODE_DE_TICK_MS` — le pas du noyau doit rester homogène (§11), jamais « le
    /// temps de l'image » — et sauvegarde toutes les 10 s, à la mise en pause et à la
    /// sortie. Au retour de pause, `Partie.Reprendre` crédite l'absence : la même
    /// fonction qu'au démarrage.
    ///
    /// Si personne n'a installé de partie (on a lancé `Mare.unity` seule dans l'éditeur),
    /// elle en crée une : la scène reste jouable sans passer par `Demarrage.unity`.
    /// </summary>
    // Avant tout autre script : leur `OnEnable` lit `ServicesDePartie`, et la partie doit
    // exister quand ils le font — y compris quand c'est cette boucle qui la crée.
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class Boucle : MonoBehaviour
    {
        const double PERIODE_DE_TICK_S = Constantes.PERIODE_DE_TICK_MS / 1000.0;
        const double PERIODE_DE_SAUVEGARDE_S = 10.0;
        /// Une image plus longue qu'une seconde (point d'arrêt, fenêtre déplacée) ne se
        /// rattrape pas pas à pas : le temps réellement absent est l'affaire du crédit hors ligne.
        const double IMAGE_MAXIMALE_S = 1.0;

        Partie partie;
        string dossier;
        double cumul;
        double depuisLaSauvegarde;
        /// Vrai tant qu'aucun pas n'a eu lieu depuis la dernière écriture : évite d'écrire
        /// deux fois de suite (pause puis sortie) la même partie.
        bool sauvegardeAJour;

        /// <summary>
        /// La partie courante, créée au besoin. Un seul chemin de création pour toute la
        /// scène : la boucle, la scène dessinée et l'interface la demandent ici.
        /// </summary>
        public static Partie ObtenirOuCreerLaPartie() =>
            ServicesDePartie.ObtenirOuCreer(() => Partie.Ouvrir(new HorlogeSysteme(), Application.persistentDataPath));

        void Awake()
        {
            // `persistentDataPath` ne se lit que sur le fil principal : on le garde ici.
            dossier = Application.persistentDataPath;
            partie = ObtenirOuCreerLaPartie();
        }

        void Update()
        {
            if (partie == null) return;
            var dt = Math.Min(Time.unscaledDeltaTime, IMAGE_MAXIMALE_S);
            cumul += dt;
            while (cumul >= PERIODE_DE_TICK_S)
            {
                partie.Avancer(PERIODE_DE_TICK_S);
                cumul -= PERIODE_DE_TICK_S;
                sauvegardeAJour = false;
            }
            depuisLaSauvegarde += dt;
            if (depuisLaSauvegarde >= PERIODE_DE_SAUVEGARDE_S) Sauvegarder();
        }

        /// <summary>
        /// Sur mobile, `true` précède la mise en arrière-plan, et rien ne garantit qu'on
        /// revienne : c'est le dernier moment sûr pour écrire. `false` crédite l'absence.
        /// Dans l'éditeur, le bouton Pause déclenche les mêmes appels.
        /// </summary>
        void OnApplicationPause(bool enPause)
        {
            if (partie == null) return;
            if (enPause)
            {
                Sauvegarder();
                return;
            }
            partie.Reprendre();
            // Le temps d'avant la pause n'est pas du temps à jouer : il vient d'être crédité.
            cumul = 0;
            depuisLaSauvegarde = 0;
            sauvegardeAJour = false;
        }

        void OnApplicationQuit()
        {
            if (partie != null && !sauvegardeAJour) Sauvegarder();
        }

        void Sauvegarder()
        {
            depuisLaSauvegarde = 0;
            try
            {
                partie.Sauvegarder(dossier);
                sauvegardeAJour = true;
            }
            catch (Exception e)
            {
                // Une écriture ratée (disque plein) ne doit pas arrêter la partie : on
                // réessaiera dans dix secondes, et la sauvegarde précédente est intacte
                // (écriture par fichier temporaire).
                Debug.LogException(e);
            }
        }
    }
}
