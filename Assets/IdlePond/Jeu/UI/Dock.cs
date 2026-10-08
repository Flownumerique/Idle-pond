using System;
using System.Collections.Generic;
using System.Globalization;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// `Reglages` n'a pas de bouton dans le dock, qui reste au jeu : la roue de la barre l'ouvre.
    public enum Tiroir { Aucun, Toi, Especes, Oeuf, Journal, Reglages }

    /// <summary>
    /// Le dock, en bas de l'écran (spec du 2026-10-07, §1) : quatre boutons, un tiroir chacun.
    /// Les améliorations vivent dans le tiroir de l'œuf : elles se paient en Souffle, et le
    /// Souffle se gagne en y rentrant.
    /// Toucher le bouton du tiroir ouvert le referme — on revient à la mare sans viser une
    /// croix. Le contrôleur ne garde que le tiroir choisi ; c'est la racine qui montre et
    /// cache, comme elle le faisait des anciens onglets.
    ///
    /// La pastille du Journal compte les succès arrivés depuis sa dernière ouverture. Ceux
    /// d'avant l'ouverture de l'écran ne comptent pas : ils ont déjà été annoncés.
    /// </summary>
    public sealed class Dock
    {
        readonly Action surChangement;
        readonly Dictionary<Tiroir, VisualElement> boutons = new Dictionary<Tiroir, VisualElement>();
        readonly Label pastille;
        int succesVus = -1;
        int succesConnus;

        public Tiroir Actif { get; private set; } = Tiroir.Aucun;

        public Dock(VisualElement racine, Action surChangement)
        {
            this.surChangement = surChangement;
            Bouton(racine, Tiroir.Toi, Icones.TOI, E.DOCK_TOI, "dock-toi");
            Bouton(racine, Tiroir.Especes, Icones.ESPECES, E.DOCK_ESPECES, "dock-especes");
            Bouton(racine, Tiroir.Oeuf, Icones.OEUF, E.DOCK_OEUF, "dock-oeuf").AddToClassList("dock-dore");
            var journal = Bouton(racine, Tiroir.Journal, Icones.JOURNAL, E.DOCK_JOURNAL, "dock-journal");

            pastille = Elements.Texte("chiffre xs dock-pastille", "", "dock-pastille");
            journal.Add(pastille);
            Elements.Montrer(pastille, false);
        }

        VisualElement Bouton(VisualElement parent, Tiroir tiroir, string[] grille, string libelle, string nom)
        {
            var bouton = Elements.Conteneur("dock-bouton", nom);
            bouton.Add(new Icone(grille));
            bouton.Add(Elements.Texte("xs dock-libelle", libelle));
            bouton.AddManipulator(new Clickable(() => Choisir(Actif == tiroir ? Tiroir.Aucun : tiroir)));
            parent.Add(bouton);
            boutons[tiroir] = bouton;
            return bouton;
        }

        public void Fermer() => Choisir(Tiroir.Aucun);

        public void Choisir(Tiroir tiroir)
        {
            Actif = tiroir;
            foreach (var paire in boutons) Elements.Marquer(paire.Value, "actif", paire.Key == tiroir);
            if (tiroir == Tiroir.Journal) succesVus = succesConnus;
            MettreLaPastille();
            surChangement();
        }

        public void Rafraichir(EtatJeu etat)
        {
            succesConnus = etat.Permanent.Succes.Count;
            if (succesVus < 0) succesVus = succesConnus;
            if (Actif == Tiroir.Journal) succesVus = succesConnus;
            MettreLaPastille();
        }

        void MettreLaPastille()
        {
            var nouveaux = Math.Max(0, succesConnus - Math.Max(0, succesVus));
            Elements.Montrer(pastille, nouveaux > 0);
            if (nouveaux > 0) Elements.Poser(pastille, nouveaux.ToString(CultureInfo.InvariantCulture));
        }
    }
}
