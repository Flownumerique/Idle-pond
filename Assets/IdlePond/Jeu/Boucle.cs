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
        MagasinDeReglages reglages;
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
            ServicesDePartie.ObtenirOuCreer(() => Partie.Ouvrir(new HorlogeSysteme(), DossierDeSauvegarde()));

        /// <summary>
        /// Les réglages du joueur, ouverts au besoin, dans le même dossier que la partie. Un
        /// fichier illisible le dit une fois dans la console : les défauts sont en place, et
        /// il sera remplacé au premier changement.
        /// </summary>
        public static MagasinDeReglages ObtenirOuCreerLesReglages() =>
            ServicesDePartie.ObtenirOuCreerLesReglages(() =>
            {
                var magasin = MagasinDeReglages.Ouvrir(DossierDeSauvegarde());
                if (magasin.FichierIllisible)
                    Debug.LogWarning($"Réglages illisibles ({magasin.Chemin}) : les défauts sont en place.");
                return magasin;
            });

        /// Le dossier de la partie : celui du joueur, sauf si un atelier l'a redirigé.
        public static string DossierDeSauvegarde() => ServicesDePartie.DossierDeSauvegardeRedirige ?? Application.persistentDataPath;

        /// <summary>
        /// Le projet active les options d'entrée en Play Mode : si le rechargement du domaine
        /// est un jour coupé, la partie statique d'une session d'éditeur survivrait à la
        /// suivante, avec son horloge et son dernier instant. On l'oublie avant chaque
        /// lancement, pour que chaque Play reparte du disque.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OublierLaPartieDuLancementPrecedent() => ServicesDePartie.Oublier();

        void Awake()
        {
            // `persistentDataPath` ne se lit que sur le fil principal : on le garde ici.
            dossier = DossierDeSauvegarde();
            partie = ObtenirOuCreerLaPartie();
            reglages = ObtenirOuCreerLesReglages();
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
            Proteger(() => reglages.Avancer(dt));
        }

        /// <summary>
        /// Le jeu passe derrière une autre fenêtre, ou le téléphone change d'application : on
        /// se tait si le joueur l'a demandé. `AudioListener.pause` suspend tout, musique comme
        /// effets, et reprend où l'on en était.
        /// </summary>
        void OnApplicationFocus(bool aLeFocus) => Taire(!aLeFocus);

        void Taire(bool enArrierePlan) =>
            AudioListener.pause = enArrierePlan && reglages != null && reglages.Courants.MuetEnArrierePlan;

        /// Une écriture de réglages ratée ne doit pas arrêter la partie : on la journalise,
        /// et le magasin réessaiera.
        static void Proteger(Action ecrire)
        {
            try { ecrire(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>
        /// Sur mobile, `true` précède la mise en arrière-plan, et rien ne garantit qu'on
        /// revienne : c'est le dernier moment sûr pour écrire. `false` crédite l'absence.
        /// Dans l'éditeur, le bouton Pause déclenche les mêmes appels.
        /// </summary>
        void OnApplicationPause(bool enPause)
        {
            if (partie == null) return;
            Taire(enPause);
            if (enPause)
            {
                Sauvegarder();
                Proteger(() => reglages?.Enregistrer());
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
            Proteger(() => reglages?.Enregistrer());
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
