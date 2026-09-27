/*
 * IdlePond — de quoi construire l'écran en quelques lignes.
 *
 * L'écran est construit en code plutôt qu'en UXML : il n'a qu'une page, ses
 * listes se reconstruisent au gré de l'état, et un UXML n'apporterait qu'un
 * second endroit où chercher un nom de classe. Les styles, eux, sont tous dans
 * `IdlePond.uss`.
 */
using System;
using UnityEngine.UIElements;

namespace IdlePond.Interface
{
    internal static class Elements
    {
        public static VisualElement Boite(params string[] classes)
        {
            var element = new VisualElement();
            foreach (var classe in classes) element.AddToClassList(classe);
            return element;
        }

        public static Label Etiquette(string texte, params string[] classes)
        {
            var etiquette = new Label(texte ?? "");
            foreach (var classe in classes) etiquette.AddToClassList(classe);
            return etiquette;
        }

        public static Label Texte(string texte, params string[] classes) => Polices.Texte(Etiquette(texte, classes));

        public static Label Chiffre(string texte, params string[] classes) => Polices.Chiffre(Etiquette(texte, classes));

        public static Button Bouton(Action action, string texte, params string[] classes)
        {
            var bouton = new Button(action) { text = texte ?? "" };
            foreach (var classe in classes) bouton.AddToClassList(classe);
            return bouton;
        }

        /// <summary>Un bouton qui porte un libellé et un coût, comme ceux de la mare.</summary>
        public static Button BoutonAvecCout(Action action, out Label libelle, out Label cout, params string[] classes)
        {
            var bouton = new Button(action) { text = "" };
            foreach (var classe in classes) bouton.AddToClassList(classe);
            libelle = Etiquette("");
            cout = Chiffre("", "cout");
            bouton.Add(libelle);
            bouton.Add(cout);
            return bouton;
        }

        public static void Afficher(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>N'écrit que si le texte change : un Label réécrit relance sa mise en page.</summary>
        public static void Ecrire(Label etiquette, string texte)
        {
            texte = texte ?? "";
            if (etiquette.text != texte) etiquette.text = texte;
        }

        public static void Ecrire(Button bouton, string texte)
        {
            texte = texte ?? "";
            if (bouton.text != texte) bouton.text = texte;
        }

        public static void Largeur(VisualElement element, double part) =>
            element.style.width = Length.Percent((float)(Math.Max(0, Math.Min(1, part)) * 100));
    }
}
