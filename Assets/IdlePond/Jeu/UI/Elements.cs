using System;
using UnityEngine.UIElements;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Les briques que les panneaux assemblent. Les panneaux construisent leur arbre UNE
    /// fois, dans leur constructeur, puis ne font plus que changer des textes et des
    /// classes : recréer des éléments à chaque tick (dix par seconde) ferait travailler
    /// le ramasse-miettes pour dessiner exactement la même chose.
    ///
    /// Aucun style ici : tout vit dans `Theme.uss` et `Mare.uss`, par classes. Le code dit
    /// QUOI est cet élément (une carte, un coût), le .uss dit à quoi il ressemble.
    /// </summary>
    static class Elements
    {
        public static VisualElement Conteneur(string classes, string nom = null)
        {
            var element = new VisualElement();
            if (nom != null) element.name = nom;
            Classer(element, classes);
            return element;
        }

        /// Un texte. Jamais une cible de clic : qu'un clic sur le libellé d'un bouton
        /// atteigne le bouton, pas l'étiquette.
        public static Label Texte(string classes, string texte = "", string nom = null)
        {
            var etiquette = new Label(texte) { pickingMode = PickingMode.Ignore };
            if (nom != null) etiquette.name = nom;
            etiquette.AddToClassList("texte");
            Classer(etiquette, classes);
            return etiquette;
        }

        /// L'écriture ne se fait que si le texte change : un `Label` qui reçoit la même
        /// chaîne dix fois par seconde invalide quand même sa mise en page.
        public static void Poser(Label etiquette, string texte)
        {
            if (etiquette.text != texte) etiquette.text = texte;
        }

        public static void Montrer(VisualElement element, bool visible)
        {
            // Réaffecter la même valeur ne relance aucun calcul de style : UI Toolkit compare.
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void Marquer(VisualElement element, string classe, bool actif) =>
            element.EnableInClassList(classe, actif);

        public static void Classer(VisualElement element, string classes)
        {
            if (string.IsNullOrEmpty(classes)) return;
            foreach (var classe in classes.Split(' '))
                if (classe.Length > 0) element.AddToClassList(classe);
        }

        /// La part d'une jauge, de 0 à 1, posée en pourcentage de la largeur du rail.
        public static void RegleLaLargeur(VisualElement element, double part)
        {
            var pourcent = (float)(Math.Max(0, Math.Min(1, part)) * 100);
            element.style.width = new Length(pourcent, LengthUnit.Percent);
        }
    }

    /// <summary>
    /// La part d'un prix déjà réunie, de 0 à 1. Le rapport se calcule en `Decimal` : une
    /// réserve et un prix de 10⁴⁰⁰ ne tiennent pas dans un double, leur rapport si.
    /// </summary>
    public static class Progression
    {
        public static double Part(Decimal reserve, Decimal prix)
        {
            if (prix.Lte(0)) return 1;
            var part = reserve.Div(prix).ToNumber();
            return double.IsNaN(part) ? 0 : Math.Max(0, Math.Min(1, part));
        }
    }

    /// <summary>
    /// Un bouton d'achat : un libellé et, à droite, ce qu'il coûte. C'est un simple
    /// `VisualElement` à `Clickable`, pas un `Button` : le `Button` d'Unity arrive avec le
    /// style du thème par défaut (fond gris, survol, marges), qu'il faudrait défaire
    /// propriété par propriété pour retrouver l'écran sobre du jalon v0.2.
    ///
    /// Un achat impossible n'est pas un bouton qu'on cache : il reste là, éteint, parce
    /// que voir ce qu'on pourra acheter est ce qui donne envie d'attendre.
    /// </summary>
    sealed class BoutonDAchat
    {
        public readonly VisualElement Racine;
        public readonly Label Libelle;
        public readonly Label Cout;
        readonly VisualElement remplissage;

        bool actif = true;

        public BoutonDAchat(string classes, string libelle, Action surClic)
        {
            Racine = Elements.Conteneur("bouton " + classes);
            // Derrière le libellé : la part du prix déjà réunie, tant qu'on ne peut pas payer.
            remplissage = Elements.Conteneur("bouton-remplissage");
            remplissage.pickingMode = PickingMode.Ignore;
            Racine.Add(remplissage);
            Libelle = Elements.Texte("base", libelle);
            Cout = Elements.Texte("chiffre tu sm bouton-cout");
            Racine.Add(Libelle);
            Racine.Add(Cout);
            Racine.AddManipulator(new Clickable(() => { if (actif) surClic(); }));
        }

        /// `SetEnabled(false)` coupe aussi les événements de pointeur ; la garde du clic
        /// reste là pour qu'un clic déjà parti dans le même tick ne passe pas.
        /// L'achat approche : le fond se remplit à mesure que la réserve rejoint le prix.
        public void Progresser(Decimal reserve, Decimal prix) =>
            Elements.RegleLaLargeur(remplissage, Progression.Part(reserve, prix));

        public void Regler(bool payable)
        {
            if (actif == payable) return;
            actif = payable;
            Racine.SetEnabled(payable);
            Racine.EnableInClassList("inactif", !payable);
        }
    }
}
