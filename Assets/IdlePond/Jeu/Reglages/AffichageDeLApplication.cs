using UnityEngine;

namespace IdlePond.Jeu
{
    /// <summary>
    /// Ce que les réglages changent à l'application entière, hors de l'interface : le plein
    /// écran et le nombre d'images par seconde. Appelé par la racine de l'interface à chaque
    /// changement de réglages.
    /// </summary>
    public static class AffichageDeLApplication
    {
        public static bool Mobile => Application.isMobilePlatform;

        /// Dans un navigateur, la page décide de la fenêtre ; le jeu ne la ferme pas, et un plein
        /// écran demandé au démarrage tomberait au premier toucher, sans qu'on l'ait voulu.
        static bool Navigateur => Application.platform == RuntimePlatform.WebGLPlayer;

        /// Le plein écran ne se règle que sur PC : un téléphone l'est toujours.
        public static bool PleinEcranReglable => !Application.isMobilePlatform && !Application.isEditor && !Navigateur;

        /// « Quitter » n'a de sens que pour une application de bureau.
        public static bool QuitterPossible => !Application.isMobilePlatform && !Navigateur;

        /// <summary>
        /// Le plein écran à imposer, ou null pour laisser la fenêtre telle qu'elle est. Il ne
        /// s'impose qu'au démarrage (`avant` null) et quand le joueur change CE réglage : un
        /// Alt+Entrée ne doit pas être défait parce qu'on a touché au volume.
        /// </summary>
        public static bool? PleinEcranAImposer(Reglages avant, Reglages apres)
        {
            if (avant != null && avant.PleinEcran == apres.PleinEcran) return null;
            return apres.PleinEcran;
        }

        /// `avant` : les réglages appliqués la fois précédente, null au premier appel.
        public static void Appliquer(Reglages avant, Reglages r)
        {
            var plein = PleinEcranAImposer(avant, r);
            if (PleinEcranReglable && plein.HasValue && plein.Value != Screen.fullScreen)
                Screen.fullScreenMode = plein.Value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            var limite = r.Images ?? (Mobile ? LimiteDImages.Trente : LimiteDImages.Soixante);
            // Une limite n'est tenue que sans synchronisation verticale ; « sans limite » rend
            // la main à l'écran (une image par rafraîchissement), pas à une boucle qui chauffe.
            // Pas dans l'éditeur : la qualité y est celle du projet, qu'on ne veut pas voir changer.
            if (!Application.isEditor) QualitySettings.vSyncCount = limite == LimiteDImages.Illimitee ? 1 : 0;
            Application.targetFrameRate = limite == LimiteDImages.Trente ? 30 : limite == LimiteDImages.Soixante ? 60 : -1;
        }
    }
}
