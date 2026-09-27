/*
 * Le détail de la captation (§8.2, et GDD §14.3). Port de `Captation.tsx`.
 *
 * Contrepartie OBLIGATOIRE d'un effet appliqué silencieusement : chaque terme
 * actif y est attribuable à sa source. Les DEUX canaux du GDD §3 y figurent, et
 * séparément.
 */
using System;
using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueCaptation
    {
        private sealed class Ligne
        {
            public VisualElement Racine;
            public Label Terme;
            public Label Valeur;
            public Label Source;
        }

        private readonly Label _nom;
        private readonly Label _profondeur;
        private readonly Button _fermer;
        private readonly VisualElement _lignes;
        private readonly Label _libelleNatif;
        private readonly Label _natif;
        private readonly Label _parSecondeNatif;
        private readonly Label _libelleEau;
        private readonly VisualElement _lignesAcclimatees;
        private readonly Label _libelleAcclimate;
        private readonly Label _acclimate;
        private readonly Label _parSecondeAcclimate;
        private readonly List<Ligne> _vuesNatives = new List<Ligne>();
        private readonly List<Ligne> _vuesAcclimatees = new List<Ligne>();

        public VueCaptation(Action surFermeture)
        {
            Racine = Boite("panneau", "panneau-clair", "pile-3");

            var entete = Boite("ligne");
            var titre = Boite("ligne-gauche");
            _nom = Texte("", "titre-3");
            _profondeur = Chiffre("", "jour-tu", "tres-petit");
            titre.Add(_nom);
            titre.Add(_profondeur);
            _fermer = Bouton(surFermeture, "", "bouton-lien", "petit");
            _fermer.AddToClassList("jour-tu");
            entete.Add(titre);
            entete.Add(_fermer);
            Racine.Add(entete);

            var table = Boite();
            _lignes = Boite();
            table.Add(_lignes);
            table.Add(Total(out _libelleNatif, out _natif, out _parSecondeNatif, "mana"));

            _libelleEau = Etiquette("", "majuscules", "intertitre-captation");
            table.Add(_libelleEau);
            _lignesAcclimatees = Boite();
            table.Add(_lignesAcclimatees);
            table.Add(Total(out _libelleAcclimate, out _acclimate, out _parSecondeAcclimate, "densite"));
            Racine.Add(table);
        }

        public VisualElement Racine { get; }

        private static VisualElement Total(out Label libelle, out Label valeur, out Label parSeconde, string couleur)
        {
            var ligne = Boite("ligne-de-captation", "total-de-captation");
            libelle = Chiffre("", "terme", couleur);
            valeur = Chiffre("", "valeur", couleur);
            parSeconde = Etiquette("", "source");
            ligne.Add(libelle);
            ligne.Add(valeur);
            ligne.Add(parSeconde);
            return ligne;
        }

        private static void Remplir(VisualElement conteneur, List<Ligne> vues, IReadOnlyList<LigneAffichee> lignes)
        {
            while (vues.Count < lignes.Count)
            {
                var vue = new Ligne { Racine = Boite("ligne-de-captation") };
                vue.Terme = Chiffre("", "terme");
                vue.Valeur = Chiffre("", "valeur");
                vue.Source = Etiquette("", "source");
                vue.Racine.Add(vue.Terme);
                vue.Racine.Add(vue.Valeur);
                vue.Racine.Add(vue.Source);
                conteneur.Add(vue.Racine);
                vues.Add(vue);
            }
            for (var i = 0; i < vues.Count; i += 1)
            {
                var visible = i < lignes.Count;
                Afficher(vues[i].Racine, visible);
                if (!visible) continue;
                Ecrire(vues[i].Terme, lignes[i].Terme);
                Ecrire(vues[i].Valeur, lignes[i].Valeur);
                Ecrire(vues[i].Source, lignes[i].Source);
            }
        }

        /// <summary>Rend faux si le banc n'existe pas : le panneau n'a alors rien à montrer.</summary>
        public bool Rafraichir(EtatJeu etat, string bancId)
        {
            var modele = bancId == null ? null : Modeles.Captation(etat, bancId);
            Afficher(Racine, modele != null);
            if (modele == null) return false;

            Ecrire(_nom, modele.Nom);
            Ecrire(_profondeur, modele.Profondeur);
            Ecrire(_fermer, modele.Fermer);
            Remplir(_lignes, _vuesNatives, modele.Lignes);
            Ecrire(_libelleNatif, modele.LibelleNatif);
            Ecrire(_natif, modele.Natif);
            Ecrire(_parSecondeNatif, modele.ParSeconde);
            Ecrire(_libelleEau, modele.LibelleEau);
            Remplir(_lignesAcclimatees, _vuesAcclimatees, modele.LignesAcclimatees);
            Ecrire(_libelleAcclimate, modele.LibelleAcclimate);
            Ecrire(_acclimate, modele.Acclimate);
            Ecrire(_parSecondeAcclimate, modele.ParSeconde);
            return true;
        }
    }
}
