/*
 * IdlePond — les polices, si le projet en fournit.
 *
 * Voir `Resources/IdlePond/Polices/LISEZMOI.md`. Sans fichier, rien n'est
 * appliqué et l'écran garde la police du thème ; sans thème non plus, la police
 * intégrée d'Unity, pour qu'aucun texte ne disparaisse.
 */
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Interface
{
    public static class Polices
    {
        private static bool _chargees;
        private static Font _texte;
        private static Font _chiffres;

        private static void Charger()
        {
            if (_chargees) return;
            _chargees = true;
            _texte = Resources.Load<Font>("IdlePond/Polices/Texte");
            _chiffres = Resources.Load<Font>("IdlePond/Polices/Chiffres");
        }

        /// <summary>La police de secours, posée sur la racine quand aucun thème n'a pu être chargé.</summary>
        public static void AppliquerLaPoliceIntegree(VisualElement racine)
        {
            var integree = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (integree != null) racine.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(integree));
        }

        public static T Texte<T>(T element) where T : VisualElement
        {
            Charger();
            element.AddToClassList("texte");
            if (_texte != null) element.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(_texte));
            return element;
        }

        public static T Chiffre<T>(T element) where T : VisualElement
        {
            Charger();
            element.AddToClassList("chiffre");
            if (_chiffres != null) element.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(_chiffres));
            return element;
        }
    }
}
