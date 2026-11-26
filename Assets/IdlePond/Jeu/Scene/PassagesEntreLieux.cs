using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — ce qui bouge au passage d'un lieu à l'autre (2026-11-25) : la membrane de mana
    /// qui ferme la vasque d'un lieu ouvert par une cassure (2026-11-26), et les lueurs de la
    /// magie du lieu suivant à travers la roche qu'on voit venir. L'eau d'au-dessus qui appuie
    /// sur la membrane est au décor (`Decor`) ; le QUAND et le OÙ sont dans `Lieux`, pur et testé.
    /// </summary>
    public sealed class PassagesEntreLieux
    {
        /// Le reflet d'écume qui parcourt la membrane.
        static readonly Color REFLET = Briques.Couleur(0xE6F8FF);

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<SpriteRenderer> pieces = new List<SpriteRenderer>();
        Material eclaire;
        static readonly (int, int)[] HALO = { (-2, 0), (2, 0), (0, -2), (0, 2) };
        readonly List<(int Palier, Color Couleur)> vasquesOuvertes = new List<(int, Color)>();
        readonly List<(int Palier, Color Couleur)> lieuxQuiViennent = new List<(int, Color)>();

        public PassagesEntreLieux(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Passages entre lieux").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public void Dessiner(VueDeScene vue)
        {
            vasquesOuvertes.Clear();
            lieuxQuiViennent.Clear();
            if (vue == null) return;
            foreach (var p in vue.Paliers)
                if (Lieux.ACassure(p.Index)) vasquesOuvertes.Add((p.Index, Briques.Couleur(Lieux.CouleurDeLaMagie(p.Assise))));
            foreach (var r in vue.Roches)
                if (Lieux.OuvreUnLieu(r.Index))
                    lieuxQuiViennent.Add((r.Index, Briques.Couleur(Lieux.CouleurDeLaMagie(r.Assise))));
        }

        public void Animer(double secondes, ChampDeCamera champ)
        {
            var utilisees = 0;
            if (catalogue != null && catalogue.Eclat != null)
            {
                foreach (var (palier, couleur) in vasquesOuvertes)
                {
                    if (!Lieux.BandeDansLeChamp(palier, champ)) continue;
                    // La membrane est du mana, donc de la lumière : sans éclairage, elle luit dans la
                    // pénombre du lieu. Les parois passent devant ses bouts.
                    var membrane = new Color(couleur.r, couleur.g, couleur.b, 0.9f);
                    var lueur = new Color(couleur.r, couleur.g, couleur.b, 0.35f);
                    foreach (var m in Lieux.Membrane(palier, secondes))
                        Placer(ref utilisees, m.X, m.Y, m.Couleur == 0 ? REFLET : m.Couleur == 1 ? membrane : lueur,
                            OrdreDeRendu.MEMBRANE, catalogue.SansLumiere);
                }
                foreach (var (palier, couleur) in lieuxQuiViennent)
                {
                    if (!Lieux.BandeDansLeChamp(palier, champ)) continue;
                    // Les lueurs sont de la lumière : sans éclairage, sinon la roche les éteint.
                    // Un halo en croix, plus pâle, autour de chaque lueur.
                    var halo = new Color(couleur.r, couleur.g, couleur.b, 0.4f);
                    foreach (var l in Lieux.Lueurs(palier, secondes))
                    {
                        Placer(ref utilisees, l.X, l.Y, couleur, OrdreDeRendu.LUEURS, catalogue.SansLumiere);
                        foreach (var (dx, dy) in HALO) Placer(ref utilisees, l.X + dx, l.Y + dy, halo, OrdreDeRendu.LUEURS, catalogue.SansLumiere);
                    }
                }
            }
            for (var i = utilisees; i < pieces.Count; i++)
                if (pieces[i] != null && pieces[i].gameObject.activeSelf) pieces[i].gameObject.SetActive(false);
        }

        /// `sansLumiere` : le matériau non éclairé, ou null pour rester dans la lumière de la scène.
        void Placer(ref int utilisees, int x, int y, Color couleur, int ordre, Material sansLumiere)
        {
            if (utilisees == pieces.Count)
            {
                var neuve = Briques.Poser(racine, "goutte", catalogue.Eclat, 0, 0, ordre);
                if (eclaire == null) eclaire = neuve.sharedMaterial;
                pieces.Add(neuve);
            }
            var piece = pieces[utilisees++];
            var materiau = sansLumiere != null ? sansLumiere : eclaire;
            if (piece.sharedMaterial != materiau) piece.sharedMaterial = materiau;
            if (!piece.gameObject.activeSelf) piece.gameObject.SetActive(true);
            piece.sortingOrder = ordre;
            piece.transform.localPosition = new Vector3(x, y, 0f);
            piece.color = couleur;
        }
    }
}
