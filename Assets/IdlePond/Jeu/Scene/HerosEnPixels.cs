using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le héros (spec DA §3) : son corps au stade de son niveau, et par-dessus une
    /// marque par assise traversée, dans l'ordre. Une marque peut teinter tout le corps et
    /// émettre une lumière. Quand le stade monte, il mue : un éclair et des écailles qui
    /// tombent. Il ne mue ni au chargement ni à la renaissance (`VueDeScene.EstUneMue`).
    /// </summary>
    public sealed class HerosEnPixels
    {
        const int ECLATS = 8;
        const float DUREE_DE_L_ECLAIR = 0.4f;
        const float DUREE_DES_ECLATS = 0.8f;
        const float INTENSITE_DE_L_ECLAIR = 3f;

        sealed class Eclat
        {
            public SpriteRenderer Rendu;
            public Vector2Int Depart;
            public int Sens;
        }

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<SpriteRenderer> marques = new List<SpriteRenderer>();
        readonly List<Light2D> lueurs = new List<Light2D>();
        readonly List<Eclat> eclats = new List<Eclat>();
        SpriteRenderer corps;
        Light2D eclair;
        bool muePendante;
        float debutDeLaMue = -10f;
        int stade;
        int x, y;

        public int? StadeAffiche { get; private set; }
        public int MuesJouees { get; private set; }
        public int NombreDeMarques => marques.Count;

        public HerosEnPixels(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Heros").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public void Dessiner(VueDeScene vue)
        {
            var heros = vue.Heros;
            if (VueDeScene.EstUneMue(StadeAffiche, heros.Stade))
            {
                MuesJouees += 1;
                muePendante = true;
            }
            StadeAffiche = heros.Stade;
            stade = heros.Stade;

            foreach (var m in marques) if (m != null) Object.Destroy(m.gameObject);
            marques.Clear();
            foreach (var l in lueurs) if (l != null) Object.Destroy(l.gameObject);
            lueurs.Clear();
            if (corps == null) corps = Briques.Poser(racine, "Corps", null, 0, 0, OrdreDeRendu.CORPS);

            // Dans la bande la plus basse, centré verticalement : c'est là qu'il vit.
            var cadre = Gabarits.CadreDuCorps(stade);
            var bandeDuBas = Mathf.Max(0, vue.Paliers.Count - 1);
            x = Gabarits.X_DU_HEROS - cadre.Largeur / 2;
            y = -(bandeDuBas + 1) * Gabarits.HAUTEUR_DE_BANDE + (Gabarits.HAUTEUR_DE_BANDE - cadre.Hauteur) / 2;

            var teinte = Color.white;
            var rang = 0;
            foreach (var assise in heros.Couches ?? new string[0])
            {
                var art = RegistreDArt.MarqueDe(assise);
                if (art != null)
                {
                    if (art.Teinte.HasValue) teinte *= Briques.Couleur(art.Teinte.Value);
                    var sprite = catalogue != null ? catalogue.MarqueDe(assise, stade) : null;
                    if (sprite != null) marques.Add(Briques.Poser(racine, "Marque " + assise, sprite, 0, 0, OrdreDeRendu.MARQUES + rang));
                    if (art.Lumiere != null) lueurs.Add(Lueur(assise, art));
                }
                rang += 1;
            }
            corps.color = teinte;
        }

        Light2D Lueur(string assise, MarqueDArt art)
        {
            var a = Gabarits.Ancrage(stade, art.Ancrage);
            var go = new GameObject("Lueur " + assise);
            go.transform.SetParent(racine, false);
            go.transform.localPosition = new Vector3(a.X + 0.5f, a.Y + 0.5f, 0f);
            var lueur = go.AddComponent<Light2D>();
            lueur.lightType = Light2D.LightType.Point;
            lueur.color = Briques.Couleur(art.Lumiere.Couleur);
            lueur.intensity = (float)art.Lumiere.Intensite;
            lueur.pointLightOuterRadius = art.Lumiere.Rayon;
            return lueur;
        }

        public void Animer(float secondes)
        {
            if (corps == null) return;
            var image = (int)(secondes * 6) % Gabarits.IMAGES_DE_NAGE;
            corps.sprite = catalogue != null ? catalogue.CorpsDe(stade, image) : null;
            // Il respire d'un pixel, sur 2,2 s : jamais d'un demi-pixel.
            var monte = Mathf.Repeat(secondes, 2.2f) < 1.1f ? 1 : 0;
            racine.localPosition = new Vector3(x, y + monte, 0f);
            AnimerLaMue(secondes);
        }

        void AnimerLaMue(float secondes)
        {
            if (muePendante)
            {
                muePendante = false;
                debutDeLaMue = secondes;
                var cadre = Gabarits.CadreDuCorps(stade);
                if (eclair == null)
                {
                    var go = new GameObject("Eclair");
                    go.transform.SetParent(racine, false);
                    eclair = go.AddComponent<Light2D>();
                    eclair.lightType = Light2D.LightType.Point;
                    eclair.color = Color.white;
                }
                eclair.transform.localPosition = new Vector3(cadre.Largeur / 2f, cadre.Hauteur / 2f, 0f);
                eclair.pointLightOuterRadius = cadre.Largeur;
                foreach (var e in eclats) if (e.Rendu != null) Object.Destroy(e.Rendu.gameObject);
                eclats.Clear();
                for (var i = 0; i < ECLATS; i++)
                {
                    // Des écailles réparties sur le dos, sans hasard : (7i + 3) mod largeur.
                    var depart = new Vector2Int((i * 7 + 3) % cadre.Largeur, cadre.Hauteur / 2 + (i * 5) % (cadre.Hauteur / 2));
                    var rendu = Briques.Poser(racine, "Eclat", catalogue != null ? catalogue.Eclat : null, depart.x, depart.y, OrdreDeRendu.PARTICULES);
                    eclats.Add(new Eclat { Rendu = rendu, Depart = depart, Sens = i % 2 == 0 ? -1 : 1 });
                }
            }
            var age = secondes - debutDeLaMue;
            if (eclair != null) eclair.intensity = Mathf.Max(0f, INTENSITE_DE_L_ECLAIR * (1f - age / DUREE_DE_L_ECLAIR));
            for (var i = eclats.Count - 1; i >= 0; i--)
            {
                var e = eclats[i];
                if (e.Rendu == null) { eclats.RemoveAt(i); continue; }
                if (age > DUREE_DES_ECLATS)
                {
                    Object.Destroy(e.Rendu.gameObject);
                    eclats.RemoveAt(i);
                    continue;
                }
                var t = age / DUREE_DES_ECLATS;
                e.Rendu.transform.localPosition = new Vector3(e.Depart.x + e.Sens * Mathf.RoundToInt(t * 4), e.Depart.y - Mathf.RoundToInt(t * 14), 0f);
                e.Rendu.color = new Color(1f, 1f, 1f, 1f - t);
            }
        }
    }
}
