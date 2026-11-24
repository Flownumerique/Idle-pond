using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — ce que les tribus posent dans la mare, à côté des nageurs (Codex v1.4) : les
    /// nids-forteresses de l'épinoche, le fil de mana qui va jusqu'aux lamproies, le mirage
    /// qu'une ombre laisse du nouveau venu.
    ///
    /// Provisoire, comme le reste de l'art : tout est fait de l'éclat (2 × 2, blanc) teinté et
    /// des images des espèces. Le QUAND et le OÙ sont dans `Tribus`, qui est pur et testé.
    /// </summary>
    public sealed class ComportementsDesTribus
    {
        static readonly Color ACCENT_DE_LA_LAMPROIE = Briques.Couleur(RegistreDArt.CouleursDEspece(4).Accent ?? 0x9FF0FF);
        static readonly Color ACCENT_DE_L_EPINOCHE = Briques.Couleur(RegistreDArt.CouleursDEspece(2).Accent ?? 0xFF8FA0);

        readonly Transform nids;
        readonly Transform eclairs;
        readonly CatalogueDArt catalogue;
        readonly List<SpriteRenderer> points = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> mirages = new List<SpriteRenderer>();
        // Ce que les nids dessinés représentent : la bande de l'épinoche et leur nombre.
        (int Bande, int Nombre) nidsDessines = (-1, 0);

        public ComportementsDesTribus(Transform parent, CatalogueDArt catalogue)
        {
            nids = new GameObject("Nids").transform;
            nids.SetParent(parent, false);
            eclairs = new GameObject("Eclairs des tribus").transform;
            eclairs.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        /// Les nids d'une vie : un par seuil franchi par l'épinoche, posés sur le fond de sa
        /// bande. Redessinés seulement quand leur nombre ou leur bande change.
        public void DessinerLesNids(VueDeScene vue, EtatDesTribus tribus)
        {
            var bande = -1;
            if (vue != null)
                foreach (var p in vue.Paliers)
                    if (p.Espece != null && p.Espece.Id == Tribus.EPINOCHE) bande = p.Index;
            var nombre = bande < 0 || tribus == null ? 0 : tribus.NidsDEpinoche;
            if (nidsDessines == (bande, nombre)) return;
            nidsDessines = (bande, nombre);
            Briques.Vider(nids);
            if (catalogue == null || catalogue.Eclat == null || nombre == 0) return;
            var fond = -(bande + 1) * Gabarits.HAUTEUR_DE_BANDE + 3;
            for (var numero = 0; numero < nombre; numero++)
            {
                var x0 = Tribus.XDuNid(numero);
                foreach (var point in Tribus.PointsDUnNid(numero))
                {
                    var rendu = Briques.Poser(nids, "nid", catalogue.Eclat, x0 + point.X, fond + point.Y, OrdreDeRendu.NIDS);
                    rendu.color = point.Couleur < 0 ? ACCENT_DE_L_EPINOCHE : Briques.Couleur(Tribus.HERBES_DU_NID[point.Couleur]);
                }
            }
        }

        /// Les réactions en cours : les fils et les mirages. Un éclair, puis plus rien — les
        /// pièces inutiles sont éteintes, pas détruites, pour la prochaine fois.
        public void Animer(double secondes, IReadOnlyList<Reaction> reactions, Nageurs nageurs)
        {
            var point = 0;
            var mirage = 0;
            if (catalogue != null && reactions != null && nageurs != null)
            {
                foreach (var r in reactions)
                {
                    if (r.EstFinie(secondes)) continue;
                    var avancement = r.Avancement(secondes);
                    if (r.Genre == GenreDeReaction.FilDeMana && catalogue.Eclat != null)
                    {
                        var source = nageurs.CentreDuPremier(r.Source);
                        var cible = nageurs.CentreDuPremier(Tribus.LAMPROIE);
                        if (source == null || cible == null) continue;
                        foreach (var p in Tribus.PointsDuFil(source, cible, avancement))
                        {
                            var rendu = Piece(points, point++, catalogue.Eclat, OrdreDeRendu.FILS);
                            rendu.transform.localPosition = new Vector3(p.X, p.Y, 0f);
                            rendu.color = ACCENT_DE_LA_LAMPROIE;
                        }
                    }
                    else if (r.Genre == GenreDeReaction.Mirage)
                    {
                        var ombre = nageurs.CentreDuPremier(Tribus.OMBRE);
                        var image = catalogue.EspeceDe(r.Source, 0);
                        if (ombre == null || image == null) continue;
                        // Le mirage reste derrière l'ombre, là d'où elle vient.
                        var versLaDroite = nageurs.PremierVersLaDroite(Tribus.OMBRE);
                        var cadre = Gabarits.CADRE_D_ESPECE;
                        var x = versLaDroite ? ombre.X - cadre.Largeur - 6 : ombre.X + 6;
                        var rendu = Piece(mirages, mirage++, image, OrdreDeRendu.MIRAGES);
                        rendu.flipX = !versLaDroite;
                        rendu.enabled = Tribus.MirageVisible(avancement);
                        rendu.transform.localPosition = new Vector3(versLaDroite ? x : x + cadre.Largeur, ombre.Y - cadre.Hauteur / 2 - 4, 0f);
                    }
                }
            }
            Eteindre(points, point);
            Eteindre(mirages, mirage);
        }

        SpriteRenderer Piece(List<SpriteRenderer> reserve, int i, Sprite sprite, int ordre)
        {
            while (reserve.Count <= i) reserve.Add(Briques.Poser(eclairs, "eclair", sprite, 0, 0, ordre));
            var rendu = reserve[i];
            rendu.sprite = sprite;
            rendu.enabled = true;
            if (!rendu.gameObject.activeSelf) rendu.gameObject.SetActive(true);
            return rendu;
        }

        static void Eteindre(List<SpriteRenderer> reserve, int aPartirDe)
        {
            for (var i = aPartirDe; i < reserve.Count; i++)
                if (reserve[i] != null && reserve[i].gameObject.activeSelf) reserve[i].gameObject.SetActive(false);
        }
    }
}
