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

        /// Le plein écran ne se règle que sur PC : un téléphone l'est toujours.
        public static bool PleinEcranReglable => !Application.isMobilePlatform && !Application.isEditor;

        public static void Appliquer(Reglages r)
        {
            if (PleinEcranReglable && r.PleinEcran.HasValue && r.PleinEcran.Value != Screen.fullScreen)
                Screen.fullScreenMode = r.PleinEcran.Value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            var limite = r.Images ?? (Mobile ? LimiteDImages.Trente : LimiteDImages.Soixante);
            // Une limite n'est tenue que sans synchronisation verticale ; « sans limite » rend
            // la main à l'écran (une image par rafraîchissement), pas à une boucle qui chauffe.
            // Pas dans l'éditeur : la qualité y est celle du projet, qu'on ne veut pas voir changer.
            if (!Application.isEditor) QualitySettings.vSyncCount = limite == LimiteDImages.Illimitee ? 1 : 0;
            Application.targetFrameRate = limite == LimiteDImages.Trente ? 30 : limite == LimiteDImages.Soixante ? 60 : -1;
        }
    }
}
