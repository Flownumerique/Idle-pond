using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la scène de la mare, en pixel art (spec DA). Elle ne connaît pas l'état du
    /// jeu : elle écoute `Partie.EtatChange`, en tire une `VueDeScene`, et ne redessine que si
    /// la vue a changé. Elle distribue le dessin à des pièces qui ont chacune un rôle, et
    /// affiche la texture du rendu pixel en fond de l'élément #scene de l'interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneDeLaMare : MonoBehaviour
    {
        [SerializeField] CatalogueDArt catalogue;

        public RenduPixel Rendu { get; private set; }
        public VueDeScene Vue { get; private set; }
        public Eclairage Eclairage { get; private set; }
        public Nageurs Nageurs { get; private set; }
        public Voile Voile { get; private set; }
        public HerosEnPixels Heros { get; private set; }

        Partie partie;
        bool vueAJour;
        Decor decor;

        UIDocument document;
        VisualElement cadre;
        float prochaineRecherche;
        (DimensionsDuRendu Dimensions, int Largeur, int Hauteur) affichee;
        // Le dernier cadrage appliqué : on ne le refait que si la taille ou le nombre de bandes
        // change, ou si un redessin a recréé les lumières.
        DimensionsDuRendu cadrageDe;
        int cadrageBandes = -1;
        int premiereVisible, derniereVisible = int.MaxValue;
        Vector2Int? tailleImposee;

        void Awake()
        {
            Rendu = new RenduPixel(transform);
            decor = new Decor(transform, catalogue);
            Eclairage = new Eclairage(transform);
            Nageurs = new Nageurs(transform, catalogue);
            Voile = new Voile(transform, catalogue);
            Heros = new HerosEnPixels(transform, catalogue);
        }

        void OnEnable()
        {
            partie = Boucle.ObtenirOuCreerLaPartie();
            partie.EtatChange += SurEtatChange;
            vueAJour = false;
        }

        void OnDisable()
        {
            if (partie != null) partie.EtatChange -= SurEtatChange;
        }

        void OnDestroy()
        {
            Rendu?.Dispose();
            Eclairage?.Dispose();
        }

        void Update()
        {
            var horloge = Time.timeAsDouble;
            Nageurs?.Animer(horloge, premiereVisible, derniereVisible);
            Voile?.Animer(Time.deltaTime);
            Heros?.Animer(horloge);
        }

        void SurEtatChange(EtatJeu _) => vueAJour = false;

        /// Pour les tests et les captures : une taille de cadre en pixels d'écran, à la place
        /// de celle de #scene.
        public void ImposerLaTaille(int largeurPx, int hauteurPx) => tailleImposee = new Vector2Int(largeurPx, hauteurPx);

        // Après tous les `Update` : la partie a fini d'avancer pour cette image, et
        // l'interface a eu sa mise en page.
        void LateUpdate()
        {
            if (!vueAJour && partie != null)
            {
                vueAJour = true;
                var nouvelle = VueDeScene.Depuis(partie.Etat);
                if (Vue == null || nouvelle.Clef != Vue.Clef)
                {
                    Vue = nouvelle;
                    Redessiner(nouvelle);
                }
            }
            Cadrer();
        }

        void Redessiner(VueDeScene v)
        {
            decor.Dessiner(v);
            Eclairage.Dessiner(v);
            Nageurs.Dessiner(v);
            Heros.Dessiner(v);
            var assiseDuBas = v.Paliers.Count > 0 ? v.Paliers[v.Paliers.Count - 1].Assise : RegistreDArt.ASSISES_DESSINEES[0];
            Voile.Viser(v.EauTroublee, RegistreDArt.DecorDe(assiseDuBas).Voile);
            // Le redessin remet les pièces au départ : on les replace avant que l'image soit rendue.
            Nageurs.Animer(Time.timeAsDouble, premiereVisible, derniereVisible);
            Heros.Animer(Time.timeAsDouble);
            // Les lumières viennent d'être recréées : le cadrage doit les rallumer.
            cadrageBandes = -1;
        }

        void Cadrer()
        {
            if (!TailleDuCadre(out var largeur, out var hauteur)) return;
            if (Rendu.Redimensionner(largeur, hauteur)) affichee = default;
            if (Rendu.Dimensions == null) return;
            var bandes = Vue != null ? Vue.Paliers.Count : 0;
            if (cadrageDe != Rendu.Dimensions || cadrageBandes != bandes)
            {
                cadrageDe = Rendu.Dimensions;
                cadrageBandes = bandes;
                Rendu.Cadrer(bandes);
                (premiereVisible, derniereVisible) = Cadrage.BandesVisibles(Rendu.Champ, bandes);
                Eclairage.Activer(premiereVisible, derniereVisible);
                Voile.Couvrir(Rendu.Champ);
            }
            if (Rendu.Texture != null && cadre != null && cadre.panel != null && affichee != (Rendu.Dimensions, largeur, hauteur)) Afficher(largeur, hauteur);
        }

        /// La taille de #scene en pixels d'écran. Le panneau couvre tout l'écran, quelle que
        /// soit son échelle : une fraction du panneau est une fraction de l'écran.
        bool TailleDuCadre(out int largeur, out int hauteur)
        {
            largeur = hauteur = 0;
            if (cadre == null || cadre.panel == null) TrouverLeCadre();
            if (tailleImposee.HasValue)
            {
                largeur = tailleImposee.Value.x;
                hauteur = tailleImposee.Value.y;
                return true;
            }
            if (cadre == null || cadre.panel == null) return false;
            var panneau = cadre.panel.visualTree.worldBound;
            var b = cadre.worldBound;
            if (!(panneau.width > 0f && b.width > 0f && b.height > 0f)) return false;
            var parUnite = Screen.width / panneau.width;
            largeur = Mathf.FloorToInt(b.width * parUnite);
            hauteur = Mathf.FloorToInt(b.height * parUnite);
            return true;
        }

        void TrouverLeCadre()
        {
            // Une recherche par demi-seconde : l'élément n'existe qu'après l'activation du
            // UIDocument, qui peut venir après la nôtre.
            if (Time.unscaledTime < prochaineRecherche) return;
            prochaineRecherche = Time.unscaledTime + 0.5f;
            if (document == null) document = FindFirstObjectByType<UIDocument>();
            var trouve = document != null && document.rootVisualElement != null
                ? document.rootVisualElement.Q<VisualElement>("scene")
                : null;
            // Un #scene reconstruit est un autre élément, sans fond : il faut le lui reposer.
            if (trouve != cadre) affichee = default;
            cadre = trouve;
        }

        /// La texture en fond de #scene, à sa taille exacte de k × texture : chaque pixel du
        /// dessin couvre k × k pixels d'écran. Centrée à un nombre ENTIER de pixels d'écran :
        /// `Center` pourrait tomber sur un demi-pixel et brouiller le pixel art.
        void Afficher(int largeurPx, int hauteurPx)
        {
            var d = Rendu.Dimensions;
            var parPixel = cadre.panel.visualTree.worldBound.width / Screen.width;
            cadre.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(Rendu.Texture));
            cadre.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(
                new Length(d.Largeur * d.Facteur * parPixel), new Length(d.Hauteur * d.Facteur * parPixel)));
            cadre.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            var decalageX = (largeurPx - d.Largeur * d.Facteur) / 2;
            var decalageY = (hauteurPx - d.Hauteur * d.Facteur) / 2;
            cadre.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Left, new Length(decalageX * parPixel)));
            cadre.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Top, new Length(decalageY * parPixel)));
            affichee = (d, largeurPx, hauteurPx);
        }
    }
}
