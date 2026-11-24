using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — les retours visuels des événements de jeu (ROADMAP, chantier 6) : la roche qui
    /// cède quand on creuse, la couronne d'or d'un seuil, l'éclair d'une tribu ralliée, la vase
    /// qui monte quand l'eau se trouble. Des gerbes de points (l'éclat de deux pixels teinté),
    /// calculées par `Gerbes`, pur et testé. Un éclair, puis plus rien.
    /// </summary>
    public sealed class RetoursVisuels
    {
        /// Au-delà, une gerbe de plus ne se verrait pas : les verbes peuvent creuser ou monter
        /// des dizaines de fois d'un coup.
        public const int GERBES_AU_PLUS = 12;

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<Gerbe> enCours = new List<Gerbe>();
        readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();

        public RetoursVisuels(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Retours visuels").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public int EnCours => enCours.Count;

        public void Recevoir(IEnumerable<EvenementDeJeu> evenements, double instant, int bandesOuvertes, Func<string, Pixel> centreDe)
        {
            foreach (var e in evenements)
            {
                if (e.Genre == GenreDEvenement.Renaissance) { enCours.Clear(); continue; }
                var gerbe = Gerbes.De(e, instant, bandesOuvertes, centreDe);
                if (gerbe == null) continue;
                if (enCours.Count >= GERBES_AU_PLUS) enCours.RemoveAt(0);
                enCours.Add(gerbe);
            }
        }

        public void Animer(double instant)
        {
            enCours.RemoveAll(g => Gerbes.EstFinie(g, instant));
            var utilisees = 0;
            if (catalogue != null && catalogue.Eclat != null)
                foreach (var gerbe in enCours)
                {
                    var couleurs = Gerbes.CouleursDe(gerbe.Genre);
                    foreach (var p in Gerbes.Points(gerbe, instant))
                    {
                        if (utilisees == pieces.Count) pieces.Add(Briques.Poser(racine, "eclat", catalogue.Eclat, 0, 0, OrdreDeRendu.PARTICULES));
                        var piece = pieces[utilisees++];
                        if (!piece.gameObject.activeSelf) piece.gameObject.SetActive(true);
                        piece.transform.localPosition = new Vector3(p.X, p.Y, 0f);
                        piece.color = Briques.Couleur(couleurs[p.Couleur]);
                    }
                }
            for (var i = utilisees; i < pieces.Count; i++)
                if (pieces[i] != null && pieces[i].gameObject.activeSelf) pieces[i].gameObject.SetActive(false);
        }
    }
}
