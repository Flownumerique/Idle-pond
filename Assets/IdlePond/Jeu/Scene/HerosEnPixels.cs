using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le héros (spec DA §3) : son corps au stade de son niveau, et par-dessus une
    /// marque par assise traversée, dans l'ordre. Une marque peut teinter tout le corps et
    /// émettre une lumière. Quand le stade monte, il mue : un éclair et des écailles qui
    /// tombent. Il ne mue ni au chargement ni à la renaissance (`VueDeScene.EstUneMue`) :
    /// à la renaissance, il rentre dans son œuf et en ressort (le renouveau).
    /// </summary>
    public sealed class HerosEnPixels
    {
        const int ECLATS = 8;
        const float DUREE_DE_L_ECLAIR = 0.4f;
        const float DUREE_DES_ECLATS = 0.8f;
        const float INTENSITE_DE_L_ECLAIR = 3f;

        /// Le renouveau (bible v0.4, d'après da-1 ; Codex, acte III), joué à chaque renaissance, jamais au
        /// chargement : l'œuf tremble, se fend, éclate ; la larve nage ; l'alevin paraît.
        public enum PhaseDuRenouveau { Oeuf, OeufFendu, Larve, Finie }
        public const float FIN_DE_L_OEUF = 1.2f;
        public const float ECLATEMENT = 1.7f;
        public const float DUREE_DU_RENOUVEAU = 3.1f;

        public static PhaseDuRenouveau PhaseA(float age) =>
            age < FIN_DE_L_OEUF ? PhaseDuRenouveau.Oeuf
            : age < ECLATEMENT ? PhaseDuRenouveau.OeufFendu
            : age < DUREE_DU_RENOUVEAU ? PhaseDuRenouveau.Larve
            : PhaseDuRenouveau.Finie;

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
        readonly List<SpriteRenderer> eveil = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> etoiles = new List<SpriteRenderer>();
        SpriteRenderer corps;
        SpriteRenderer rubans;
        SpriteRenderer renouveau;
        bool renouveauPendant;
        double debutDuRenouveau = -10.0;
        bool larveSortie;
        Light2D eclair;
        Light2D sienne;
        bool muePendante;
        double debutDeLaMue = -10.0;
        int stade;
        int x, y;

        public int? StadeAffiche { get; private set; }
        public int MuesJouees { get; private set; }
        public int? RenaissancesAffichees { get; private set; }
        public int RenouveauxJoues { get; private set; }
        /// Pendant le renouveau, l'œuf puis la larve tiennent sa place : son corps, ses marques
        /// et son éveil sont cachés.
        public bool EnRenouveau { get; private set; }
        public int NombreDeMarques => marques.Count;
        /// Son éveil (bible v0.4) : 0 à 4, porté par ses renaissances.
        public int Eveil { get; private set; }
        public int NombreDEtoiles => etoiles.Count;
        /// Les calques d'éveil posés : runes, rubans, anneau, aura (les étoiles à part).
        public int NombreDeCalquesDEveil => eveil.Count;
        public bool EclairAllume => eclair != null && eclair.enabled;
        /// L'intensité de sa lumière propre ; 0 si elle est éteinte (près du jour).
        public float IntensiteDeSaLumiere => sienne != null && sienne.enabled ? sienne.intensity : 0f;

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
            if (RenaissancesAffichees.HasValue && heros.Renaissances > RenaissancesAffichees.Value)
            {
                RenouveauxJoues += 1;
                renouveauPendant = true;
                EnRenouveau = true;
            }
            RenaissancesAffichees = heros.Renaissances;
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

            PoserSaLumiere(vue, bandeDuBas, cadre);

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
            PoserSonEveil(heros.Renaissances);
            if (EnRenouveau) Montrer(false);
        }

        /// Son corps, ses marques et son éveil : cachés pendant le renouveau.
        void Montrer(bool visible)
        {
            if (corps != null) corps.enabled = visible;
            foreach (var m in marques) if (m != null) m.enabled = visible;
            foreach (var r in eveil) if (r != null) r.enabled = visible;
            foreach (var e in etoiles) if (e != null) e.enabled = visible;
        }

        void AnimerLeRenouveau(double horloge)
        {
            if (renouveauPendant)
            {
                renouveauPendant = false;
                debutDuRenouveau = horloge;
                larveSortie = false;
                if (renouveau == null) renouveau = Briques.Poser(racine, "Renouveau", null, 0, 0, OrdreDeRendu.CORPS);
            }
            if (!EnRenouveau) return;
            var age = (float)(horloge - debutDuRenouveau);
            var phase = PhaseA(age);
            if (phase == PhaseDuRenouveau.Finie)
            {
                EnRenouveau = false;
                renouveau.enabled = false;
                Montrer(true);
                // L'alevin paraît dans un éclair, sans écailles.
                Eclater(horloge, false);
                return;
            }
            Montrer(false);
            if (phase == PhaseDuRenouveau.Larve && !larveSortie)
            {
                larveSortie = true;
                Eclater(horloge, true); // la coquille éclate
            }
            Sprite sprite;
            var dx = 0;
            switch (phase)
            {
                case PhaseDuRenouveau.Oeuf:
                    sprite = catalogue != null ? catalogue.Oeuf : null;
                    // Il tremble d'un pixel, de plus en plus souvent.
                    if (age > 0.5f) dx = (int)(age * 6) % 2;
                    break;
                case PhaseDuRenouveau.OeufFendu:
                    sprite = catalogue != null ? catalogue.OeufFendu : null;
                    dx = (int)(age * 12) % 2;
                    break;
                default:
                    var larves = catalogue != null ? catalogue.Larve : null;
                    sprite = larves != null && larves.Length > 0 ? larves[(int)(age * 6) % larves.Length] : null;
                    // Elle nage de quatre pixels vers l'avant.
                    dx = Mathf.RoundToInt(4f * (age - ECLATEMENT) / (DUREE_DU_RENOUVEAU - ECLATEMENT)) - 4;
                    break;
            }
            renouveau.enabled = sprite != null;
            renouveau.sprite = sprite;
            if (sprite == null) return;
            var c = Gabarits.CadreDuCorps(stade);
            var w = Mathf.RoundToInt(sprite.rect.width);
            var h = Mathf.RoundToInt(sprite.rect.height);
            renouveau.transform.localPosition = new Vector3((c.Largeur - w) / 2 + dx, (c.Hauteur - h) / 2, 0f);
        }

        /// Son éveil (bible v0.4, d'après da-3) : I les runes sur lui ; II les rubans du voile
        /// qui sortent de sa queue ; III l'anneau et les étoiles qui y tournent ; IV l'aura. Les
        /// calques débordent du corps d'une marge : ils se posent à (-marge, -marge).
        void PoserSonEveil(int renaissances)
        {
            foreach (var r in eveil) if (r != null) Object.Destroy(r.gameObject);
            eveil.Clear();
            foreach (var e in etoiles) if (e != null) Object.Destroy(e.gameObject);
            etoiles.Clear();
            rubans = null;
            Eveil = Gabarits.EveilDe(renaissances);
            var art = catalogue != null ? catalogue.EveilDe(stade) : null;
            if (art == null) return;
            var m = Gabarits.MARGES_D_EVEIL[stade];
            if (Eveil >= 1 && art.Runes != null) eveil.Add(Briques.Poser(racine, "Runes", art.Runes, 0, 0, OrdreDeRendu.RUNES));
            if (Eveil >= 2)
            {
                rubans = Briques.Poser(racine, "Rubans", art.Voile.Length > 0 ? art.Voile[0] : null, -m, -m, OrdreDeRendu.RUBANS);
                eveil.Add(rubans);
            }
            if (Eveil >= 3 && art.Anneau != null) eveil.Add(Briques.Poser(racine, "Anneau", art.Anneau, -m, -m, OrdreDeRendu.ANNEAU));
            if (Eveil >= 4 && art.Aura != null) eveil.Add(Briques.Poser(racine, "Aura", art.Aura, -m, -m, OrdreDeRendu.AURA));
            for (var i = 0; i < Gabarits.EtoilesDe(renaissances); i++)
                etoiles.Add(Briques.Poser(racine, "Etoile", catalogue.Etoile, 0, 0, OrdreDeRendu.ETOILES));
            // Son halo s'élargit d'un cran par éveil.
            if (sienne != null) sienne.pointLightOuterRadius = Gabarits.CadreDuCorps(stade).Largeur * (1.5f + 0.25f * Eveil);
        }

        /// Les rubans ondulent (deux images) ; les étoiles tournent sur l'anneau, toujours au
        /// pixel entier.
        void AnimerLEveil(float secondes)
        {
            if (rubans != null)
            {
                var art = catalogue.EveilDe(stade);
                if (art != null && art.Voile.Length > 1) rubans.sprite = art.Voile[(int)(secondes * 3) % 2];
            }
            if (etoiles.Count == 0) return;
            var c = Gabarits.CadreDuCorps(stade);
            var m = Gabarits.MARGES_D_EVEIL[stade];
            var demi = Gabarits.COTE_D_ETOILE / 2;
            float cx = c.Largeur / 2f, cy = c.Hauteur / 2f;
            float rx = c.Largeur / 2f + m - 2, ry = c.Hauteur / 2f + m - 2;
            for (var i = 0; i < etoiles.Count; i++)
            {
                var a = secondes * 0.6f + i * Mathf.PI * 2f / etoiles.Count;
                etoiles[i].transform.localPosition = new Vector3(
                    Mathf.RoundToInt(cx + rx * Mathf.Cos(a)) - demi, Mathf.RoundToInt(cy + ry * Mathf.Sin(a)) - demi, 0f);
            }
        }

        /// Sa lumière propre : elle comble ce qui manque à sa bande pour qu'il reste d'or. Pleine
        /// sur tout le corps, elle déborde un peu sur l'eau autour de lui — un halo chaud, plus
        /// visible à mesure que la mare s'assombrit.
        void PoserSaLumiere(VueDeScene vue, int bandeDuBas, Cadre cadre)
        {
            if (sienne == null)
            {
                var go = new GameObject("Sa lumiere");
                go.transform.SetParent(racine, false);
                sienne = go.AddComponent<Light2D>();
                sienne.lightType = Light2D.LightType.Point;
                sienne.color = Briques.Couleur(RegistreDArt.LUMIERE_DU_HEROS);
            }
            var lumiereDeSaBande = vue.Paliers.Count > 0 ? RegistreDArt.LumiereDuPalier(vue.Paliers[bandeDuBas].Index) : 1.0;
            sienne.intensity = (float)RegistreDArt.IntensiteDuHeros(lumiereDeSaBande);
            sienne.enabled = sienne.intensity > 0f;
            sienne.transform.localPosition = new Vector3(cadre.Largeur / 2f, cadre.Hauteur / 2f, 0f);
            sienne.pointLightInnerRadius = cadre.Largeur * 0.8f;
            sienne.pointLightOuterRadius = cadre.Largeur * (1.5f + 0.25f * Eveil);
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

        public void Animer(double horloge)
        {
            if (corps == null) return;
            // Le calcul d'image et de respiration reste en float, mais sur une valeur bornée :
            // un float brut perd sa précision après des heures de jeu.
            var secondes = (float)(horloge % 3600.0);
            var image = (int)(secondes * 6) % Gabarits.IMAGES_DE_NAGE;
            corps.sprite = catalogue != null ? catalogue.CorpsDe(stade, image) : null;
            // Il respire d'un pixel, sur 2,2 s : jamais d'un demi-pixel.
            var monte = Mathf.Repeat(secondes, 2.2f) < 1.1f ? 1 : 0;
            racine.localPosition = new Vector3(x, y + monte, 0f);
            AnimerLEveil(secondes);
            AnimerLeRenouveau(horloge);
            AnimerLaMue(horloge);
        }

        // La mue se date sur l'horloge entière (double) : le retour à zéro de la valeur bornée
        // ne doit pas fausser son âge.
        void AnimerLaMue(double secondes)
        {
            if (muePendante)
            {
                muePendante = false;
                Eclater(secondes, true);
            }
            AnimerLEclair(secondes);
        }

        /// Un éclair blanc sur lui ; avec des éclats, des écailles (ou des bouts de coquille) qui
        /// tombent. La mue, l'œuf qui éclate et l'alevin qui paraît le partagent.
        void Eclater(double secondes, bool avecDesEclats)
        {
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
            eclair.enabled = true;
            eclair.transform.localPosition = new Vector3(cadre.Largeur / 2f, cadre.Hauteur / 2f, 0f);
            eclair.pointLightOuterRadius = cadre.Largeur;
            foreach (var e in eclats) if (e.Rendu != null) Object.Destroy(e.Rendu.gameObject);
            eclats.Clear();
            for (var i = 0; avecDesEclats && i < ECLATS; i++)
            {
                // Des écailles réparties sur le dos, sans hasard : (7i + 3) mod largeur.
                var depart = new Vector2Int((i * 7 + 3) % cadre.Largeur, cadre.Hauteur / 2 + (i * 5) % (cadre.Hauteur / 2));
                var rendu = Briques.Poser(racine, "Eclat", catalogue != null ? catalogue.Eclat : null, depart.x, depart.y, OrdreDeRendu.PARTICULES);
                eclats.Add(new Eclat { Rendu = rendu, Depart = depart, Sens = i % 2 == 0 ? -1 : 1 });
            }
        }

        void AnimerLEclair(double secondes)
        {
            var age = (float)(secondes - debutDeLaMue);
            // L'éclair éteint ne coûte plus rien au rendu ; le prochain réveille la lumière.
            if (eclair != null) eclair.enabled = age < DUREE_DE_L_ECLAIR;
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
