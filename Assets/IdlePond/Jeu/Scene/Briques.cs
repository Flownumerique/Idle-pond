using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// Les briques du dessin : poser un sprite à une position ENTIÈRE, paver une surface,
    /// vider un conteneur.
    public static class Briques
    {
        public static void Vider(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--) Object.Destroy(parent.GetChild(i).gameObject);
        }

        public static SpriteRenderer Poser(Transform parent, string nom, Sprite sprite, int x, int y, int ordre)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            var rendu = go.AddComponent<SpriteRenderer>();
            rendu.sprite = sprite;
            rendu.sortingOrder = ordre;
            return rendu;
        }

        /// Un sprite répété sur une surface : le pivot en bas à gauche fait partir la
        /// surface de (x, y).
        public static SpriteRenderer Paver(Transform parent, string nom, Sprite sprite, int x, int y, int largeur, int hauteur, int ordre)
        {
            var rendu = Poser(parent, nom, sprite, x, y, ordre);
            rendu.drawMode = SpriteDrawMode.Tiled;
            rendu.size = new Vector2(largeur, hauteur);
            return rendu;
        }

        public static Color Couleur(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}
