using System;
using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Le détail de la captation (§8.2, et GDD §14.3), `Captation.tsx`.
    ///
    /// Contrepartie OBLIGATOIRE d'un effet appliqué silencieusement : chaque terme actif y
    /// est attribuable à sa source. C'est aussi ce qui rend tenable la règle du §7.5.3 — un
    /// effet qui donne « +12 % » sans dire à quel terme n'a pas sa place dans cette table,
    /// donc pas sa place dans le jeu.
    ///
    /// Un seul canal de revenu — noyau v1.0 §10 : les espèces.
    ///
    /// La table est construite à l'ouverture, puis seulement relue : les termes d'une
    /// espèce ne changent pas pendant qu'on la regarde, leurs valeurs si.
    /// </summary>
    public sealed class Captation
    {
        sealed class Ligne
        {
            public Label Terme;
            public Label Valeur;
            public Label Source;
        }

        readonly VisualElement racine;
        readonly Label nom;
        readonly Label profondeur;
        readonly VisualElement table;
        readonly Label total;
        readonly List<Ligne> lignes = new List<Ligne>();

        string espece;

        public Captation(VisualElement racine, Action surFermeture)
        {
            this.racine = racine;
            racine.AddToClassList("carte");
            racine.AddToClassList("carte-claire");

            var haut = Elements.Conteneur("rangee entre");
            var titre = Elements.Conteneur("rangee");
            nom = Elements.Texte("titre lg", "", "captation-nom");
            profondeur = Elements.Texte("chiffre tu sm captation-profondeur");
            titre.Add(nom);
            titre.Add(profondeur);
            haut.Add(titre);

            var fermer = Elements.Conteneur("lien");
            fermer.Add(Elements.Texte("tu base", E.FERMER));
            fermer.AddManipulator(new Clickable(surFermeture));
            haut.Add(fermer);
            racine.Add(haut);

            table = Elements.Conteneur("table");
            racine.Add(table);

            var pied = Elements.Conteneur("table-ligne table-pied");
            pied.Add(Elements.Texte("chiffre mana sm table-terme", E.CE_QU_ILS_TE_DONNENT));
            total = Elements.Texte("chiffre mana base table-valeur");
            pied.Add(total);
            pied.Add(Elements.Texte("tu sm table-source", E.PAR_SECONDE));
            racine.Add(pied);

            Elements.Montrer(racine, false);
        }

        public bool EstOuverte => espece != null;

        public void Ouvrir(string id)
        {
            if (Especes.ParId(id) == null) return;
            espece = id;
            Elements.Montrer(racine, true);
        }

        public void Fermer()
        {
            espece = null;
            Elements.Montrer(racine, false);
        }

        public void Rafraichir(EtatJeu etat)
        {
            if (espece == null) return;
            var cible = Especes.ParId(espece);
            var detail = Economie.DetailDeCaptation(etat, cible);

            Elements.Poser(nom, Format.NomDeLEspece(cible.Id));
            Elements.Poser(profondeur, Format.Profondeur(cible.Palier));

            if (lignes.Count != detail.Count) Reconstruire(detail.Count);
            for (var i = 0; i < detail.Count; i++)
            {
                Elements.Poser(lignes[i].Terme, Termes.Identifiant(detail[i].Terme));
                Elements.Poser(lignes[i].Valeur, Format.Terme(detail[i].Valeur));
                Elements.Poser(lignes[i].Source, Format.SourceDuTerme(detail[i].Source));
            }
            Elements.Poser(total, Format.Montant(Economie.ProductionDeLEspece(etat, cible)));
        }

        void Reconstruire(int nombre)
        {
            table.Clear();
            lignes.Clear();
            for (var i = 0; i < nombre; i++)
            {
                var rangee = Elements.Conteneur("table-ligne");
                var ligne = new Ligne
                {
                    Terme = Elements.Texte("chiffre tu sm table-terme"),
                    Valeur = Elements.Texte("chiffre base table-valeur"),
                    Source = Elements.Texte("tu sm table-source"),
                };
                rangee.Add(ligne.Terme);
                rangee.Add(ligne.Valeur);
                rangee.Add(ligne.Source);
                table.Add(rangee);
                lignes.Add(ligne);
            }
        }
    }
}
