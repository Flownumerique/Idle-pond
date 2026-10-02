using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — l'eau qui se trouble (GDD §2.4) : un damier pavé sur tout le champ, dans la
    /// couleur de l'assise, qui monte en 0,9 s. Un voile, jamais un texte. Le damier ne
    /// couvre qu'un pixel sur deux : l'opacité est doublée pour retrouver la densité de
    /// l'ancien voile (0,28).
    /// </summary>
    public sealed class Voile
    {
        public const float OPACITE = 0.56f;
        public const float DUREE_S = 0.9f;

        readonly SpriteRenderer rendu;
        float visee;
        int teinte;

        public float Opacite { get; private set; }

        public Voile(Transform parent, CatalogueDArt catalogue)
        {
            rendu = Briques.Paver(parent, "Voile", catalogue != null ? catalogue.Voile : null, 0, 0, 1, 1, OrdreDeRendu.VOILE);
            rendu.enabled = false;
        }

        public void Viser(bool trouble, int teinteDeLAssise)
        {
            visee = trouble ? OPACITE : 0f;
            teinte = teinteDeLAssise;
        }

        public void Couvrir(ChampDeCamera champ)
        {
            rendu.transform.localPosition = new Vector3(champ.Gauche, champ.Bas, 0f);
            rendu.size = new Vector2(champ.Largeur, champ.Hauteur);
        }

        public void Animer(float dt)
        {
            Opacite = Mathf.MoveTowards(Opacite, visee, OPACITE / DUREE_S * dt);
            rendu.enabled = Opacite > 0.0001f && rendu.sprite != null;
            rendu.color = Briques.Couleur(teinte, Opacite);
        }
    }
}
