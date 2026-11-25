using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — l'eau vivante dessinée (2026-11-25) : les caustiques de chaque bande, ses
    /// bulles, le pollen de la Noue et les filets du courant du Gour. Le QUAND et le OÙ sont
    /// dans `Eau`, pur et testé. Aucune lumière 2D de plus : tout est dans la lumière de la
    /// scène, comme l'eau — elle éteint les caustiques en descendant, sans marche.
    ///
    /// Seules les bandes visibles s'animent ; les particules sont prises dans une réserve et
    /// éteintes quand elles ne servent plus.
    /// </summary>
    public sealed class EauVivante
    {
        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<(int Palier, bool Courant, SpriteRenderer[] Couches)> caustiques = new List<(int, bool, SpriteRenderer[])>();
        readonly List<(int Palier, string Assise)> bandes = new List<(int, string)>();
        readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();
        int utilisees;

        public EauVivante(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Eau vivante").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        /// Les caustiques actuellement posées, pour les tests : une entrée par bande.
        public int BandesAvecCaustiques => caustiques.Count;

        public void Dessiner(VueDeScene vue)
        {
            foreach (var (_, _, couches) in caustiques)
                foreach (var c in couches) if (c != null) Object.Destroy(c.gameObject);
            caustiques.Clear();
            bandes.Clear();
            if (vue == null || catalogue == null) return;
            foreach (var p in vue.Paliers)
            {
                bandes.Add((p.Index, p.Assise));
                if (catalogue.Caustiques == null || catalogue.Caustiques.Length < Eau.COUCHES_DE_CAUSTIQUES) continue;
                var couleur = Briques.Couleur(Eau.CouleurDesCaustiques(p.Assise));
                var couches = new SpriteRenderer[Eau.COUCHES_DE_CAUSTIQUES];
                for (var c = 0; c < couches.Length; c++)
                {
                    // Une largeur de motif de plus que le décor : le glissement ne découvre jamais
                    // de bord.
                    var rendu = Briques.Paver(racine, $"Caustiques {p.Index}.{c}", catalogue.Caustiques[c],
                        Gabarits.GAUCHE_DU_DECOR - Eau.LARGEUR_DES_CAUSTIQUES, -(p.Index + 1) * Gabarits.HAUTEUR_DE_BANDE,
                        Gabarits.LARGEUR_DU_DECOR + Eau.LARGEUR_DES_CAUSTIQUES, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.CAUSTIQUES);

                    rendu.color = new Color(couleur.r, couleur.g, couleur.b, (float)Eau.ForceDesCaustiques(c, p.Index));
                    couches[c] = rendu;
                }
                caustiques.Add((p.Index, Eau.ACourant(p.Assise), couches));
            }
        }

        public void Animer(double secondes, int premiere, int derniere)
        {
            foreach (var (palier, courant, couches) in caustiques)
            {
                var visible = palier >= premiere && palier <= derniere;
                for (var c = 0; c < couches.Length; c++)
                {
                    var rendu = couches[c];
                    if (rendu == null) continue;
                    if (rendu.enabled != visible) rendu.enabled = visible;
                    if (!visible) continue;
                    var p = rendu.transform.localPosition;
                    rendu.transform.localPosition = new Vector3(
                        Gabarits.GAUCHE_DU_DECOR - Eau.LARGEUR_DES_CAUSTIQUES + Eau.DecalageDesCaustiques(c, secondes, courant), p.y, 0f);
                }
            }

            utilisees = 0;
            if (catalogue != null && catalogue.Eclat != null)
                foreach (var (palier, assise) in bandes)
                {
                    if (palier < premiere || palier > derniere) continue;
                    var bulle = Briques.Couleur(Eau.CouleurDesBulles(assise), 0.75f);
                    for (var n = 0; n < Eau.BULLES; n++)
                    {
                        var b = Eau.Bulle(palier, n, secondes);
                        if (b != null) Placer(b.X, b.Y, 1f, 1f, bulle);
                    }
                    if (Eau.APollen(assise))
                    {
                        var couleurs = Eau.CouleursDuPollen(assise);
                        for (var n = 0; n < Eau.GRAINS; n++)
                        {
                            var g = Eau.Grain(palier, n, secondes);
                            // Un pixel, pas deux : le pollen est plus fin que les bulles.
                            if (g != null) Placer(g.X, g.Y, 0.5f, 0.5f, Briques.Couleur(couleurs[n % couleurs.Count], 0.9f));
                        }
                    }
                    if (Eau.ACourant(assise))
                    {
                        var filet = Briques.Couleur(Eau.CouleurDesFilets(assise), 0.6f);
                        for (var n = 0; n < Eau.FILETS; n++)
                        {
                            var f = Eau.Filet(palier, n, secondes);
                            // L'éclat fait 2 × 2 : un trait d'un pixel de haut, de sa longueur.
                            if (f != null) Placer(f.X, f.Y, f.Longueur / 2f, 0.5f, filet);
                        }
                    }
                }
            for (var i = utilisees; i < pieces.Count; i++)
                if (pieces[i] != null && pieces[i].gameObject.activeSelf) pieces[i].gameObject.SetActive(false);
        }

        void Placer(int x, int y, float largeur, float hauteur, Color couleur)
        {
            if (utilisees == pieces.Count) pieces.Add(Briques.Poser(racine, "eau", catalogue.Eclat, 0, 0, OrdreDeRendu.EAU_VIVANTE));
            var piece = pieces[utilisees++];
            if (!piece.gameObject.activeSelf) piece.gameObject.SetActive(true);
            piece.transform.localPosition = new Vector3(x, y, 0f);
            piece.transform.localScale = new Vector3(largeur, hauteur, 1f);
            piece.color = couleur;
        }
    }
}
