using System;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    public enum OngletActif { Aucun, Succes, Insufflations }

    /// <summary>
    /// Les deux onglets du portrait. Succès et insufflations ne tiennent pas à côté de la
    /// colonne unique, alors ils passent derrière deux onglets en bas de l'écran. Toucher
    /// l'onglet ouvert le referme : on revient à la mare sans bouton de plus.
    ///
    /// En paysage la barre est cachée par le .uss, et la colonne de droite montre tout : ce
    /// contrôleur ne fait que garder l'onglet choisi, pour que la rotation n'en perde pas le fil.
    /// </summary>
    public sealed class Onglets
    {
        readonly Action surChangement;
        readonly VisualElement succes;
        readonly VisualElement insufflations;

        public OngletActif Actif { get; private set; } = OngletActif.Aucun;

        public Onglets(VisualElement racine, Action surChangement)
        {
            this.surChangement = surChangement;
            racine.AddToClassList("onglets");
            succes = Onglet(racine, E.ONGLET_SUCCES, OngletActif.Succes, "onglet-succes");
            insufflations = Onglet(racine, E.ONGLET_INSUFFLATIONS, OngletActif.Insufflations, "onglet-insufflations");
            Marquer();
        }

        VisualElement Onglet(VisualElement parent, string libelle, OngletActif quand, string nom)
        {
            var onglet = Elements.Conteneur("onglet", nom);
            onglet.Add(Elements.Texte("doux base", libelle));
            onglet.AddManipulator(new Clickable(() =>
            {
                Actif = Actif == quand ? OngletActif.Aucun : quand;
                Marquer();
                surChangement();
            }));
            parent.Add(onglet);
            return onglet;
        }

        void Marquer()
        {
            Elements.Marquer(succes, "actif", Actif == OngletActif.Succes);
            Elements.Marquer(insufflations, "actif", Actif == OngletActif.Insufflations);
        }
    }
}
