using System.Collections.Generic;
using IdlePond.Noyau.Donnees;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la faune d'ambiance dans la mare (Codex v1.3, §2) : elle vit, elle ne produit
    /// rien. Les bêtes de surface — libellules-lanternes, gerris-scribes et leurs runes,
    /// grenouilles-chantres, l'hydre — dans la première bande ; les jardins de la Sève au fond
    /// des bandes de la Noue ; la procession des écrevisses au fond de la bande d'une tribu qui
    /// vient d'être ralliée. Le QUAND et le OÙ sont dans `Faune`, pur et testé.
    ///
    /// Les pièces sont prises dans une réserve et éteintes quand elles ne servent plus : la
    /// procession va et vient sans rien créer ni détruire à chaque image.
    /// </summary>
    public sealed class FauneDAmbiance
    {
        /// La rune d'un gerris : un point pâle sur l'eau.
        static readonly Color RUNE = Briques.Couleur(0xFFF0C0);

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();
        int utilisees;

        // Ce que la dernière vue a dit : la surface est-elle à la Noue, quelles bandes sont de
        // la Noue, combien de renaissances.
        bool surfaceDeLaNoue;
        readonly List<int> bandesDeLaNoue = new List<int>();
        int renaissances;

        public FauneDAmbiance(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Faune d'ambiance").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public void Dessiner(VueDeScene vue)
        {
            bandesDeLaNoue.Clear();
            surfaceDeLaNoue = false;
            if (vue == null) return;
            renaissances = vue.Heros != null ? vue.Heros.Renaissances : 0;
            foreach (var p in vue.Paliers)
            {
                // Les jardins de la Sève ne poussent que dans la Noue.
                if (p.Assise != "noue") continue;
                bandesDeLaNoue.Add(p.Index);
                if (p.Index == 0) surfaceDeLaNoue = true;
            }
        }

        /// À chaque image : seules les bandes visibles sont peuplées.
        public void Animer(double secondes, int premiere, int derniere, IReadOnlyList<Reaction> reactions)
        {
            utilisees = 0;
            if (catalogue != null)
            {
                if (surfaceDeLaNoue && premiere <= 0)
                {
                    for (var n = 0; n < Faune.LIBELLULES; n++) Poser(Faune.Libellule(n, secondes), OrdreDeRendu.SURFACE);
                    for (var n = 0; n < Faune.GERRIS_A_LA_SURFACE; n++)
                    {
                        Poser(Faune.Gerris(n, secondes), OrdreDeRendu.SURFACE);
                        var rune = Faune.RuneDuGerris(n, secondes);
                        if (rune != null && catalogue.Eclat != null)
                        {
                            var piece = Piece(catalogue.Eclat, OrdreDeRendu.SURFACE);
                            piece.transform.localPosition = new Vector3(rune.X, rune.Y, 0f);
                            piece.color = RUNE;
                        }
                    }
                    for (var n = 0; n < Faune.Grenouilles(renaissances); n++)
                        Poser(Faune.Grenouille(n, secondes, renaissances), OrdreDeRendu.SURFACE);
                    Poser(Faune.Hydre(secondes), OrdreDeRendu.HYDRE);
                }
                foreach (var bande in bandesDeLaNoue)
                {
                    if (bande < premiere || bande > derniere) continue;
                    foreach (var fleur in Faune.Jardin(bande, renaissances)) Poser(fleur, OrdreDeRendu.JARDINS);
                }
                if (reactions != null)
                    foreach (var r in reactions)
                    {
                        if (r.Genre != GenreDeReaction.Procession || r.EstFinie(secondes)) continue;
                        var espece = Especes.ParId(r.Source);
                        if (espece == null || espece.Palier < premiere || espece.Palier > derniere) continue;
                        foreach (var p in Faune.Procession(espece.Palier, r.Avancement(secondes))) Poser(p, OrdreDeRendu.PROCESSION);
                    }
            }
            for (var i = utilisees; i < pieces.Count; i++)
                if (pieces[i] != null && pieces[i].gameObject.activeSelf) pieces[i].gameObject.SetActive(false);
        }

        void Poser(PoseDeFaune pose, int ordre)
        {
            var images = catalogue.ImagesDeLaFaune(pose.Nom);
            var sprite = images != null && pose.Image < images.Length ? images[pose.Image] : null;
            if (sprite == null) return;
            var piece = Piece(sprite, ordre);
            var largeur = Faune.DESSINS[pose.Nom].Cadre.Largeur;
            // Un sprite retourné garde son pied : on le décale de la largeur de son cadre.
            piece.flipX = !pose.VersLaDroite;
            piece.transform.localPosition = new Vector3(pose.VersLaDroite ? pose.X : pose.X + largeur, pose.Y, 0f);
            piece.color = Color.white;
        }

        SpriteRenderer Piece(Sprite sprite, int ordre)
        {
            if (utilisees == pieces.Count) pieces.Add(Briques.Poser(racine, "faune", sprite, 0, 0, ordre));
            var piece = pieces[utilisees++];
            piece.sprite = sprite;
            piece.sortingOrder = ordre;
            piece.flipX = false;
            if (!piece.gameObject.activeSelf) piece.gameObject.SetActive(true);
            return piece;
        }
    }
}
