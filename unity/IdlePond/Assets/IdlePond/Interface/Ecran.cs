/*
 * IdlePond — l'écran du jalon v0.2 : l'assise I est jouable. Port de `App.tsx`.
 *
 * La boucle du genre, sans exception : débloquer → améliorer → réinitialiser.
 * Pas de clic obligatoire, pas de mini-jeu, pas de fenêtre de temps — aucune
 * présence active n'est requise (§4.2). Tout ce qui se passe ici se passerait
 * aussi bien sans personne devant l'écran.
 *
 * L'écran ne décide de rien : il dessine les modèles de `Presentation/` et
 * transmet les gestes au magasin. Deux colonnes sur un grand écran, une seule
 * sous 768 points de large — le `md:` de Tailwind.
 */
using IdlePond.Etat;
using IdlePond.Interface.Vues;
using UnityEngine;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface
{
    public sealed class Ecran
    {
        /// <summary>La largeur, en points de panneau, sous laquelle l'écran passe à une colonne.</summary>
        private const float LargeurDUneSeuleColonne = 768;

        private readonly Magasin _magasin;
        private readonly VisualElement _racine;
        private readonly VueEnTete _enTete;
        private readonly VueRetour _retour;
        private readonly VueContenance _contenance;
        private readonly VueCaptation _captation;
        private readonly VueMare _mare;
        private readonly VueEclosion _eclosion;
        private readonly VueSucces _succes;
        private readonly VueAnnonces _annonces;
        private readonly VisualElement _page;
        private string _captationOuverte;
        private Rect _aireSure;

        public Ecran(VisualElement racine, Magasin magasin)
        {
            _magasin = magasin;
            _racine = racine;

            var feuille = Resources.Load<StyleSheet>("IdlePond/IdlePond");
            if (feuille != null) racine.styleSheets.Add(feuille);
            else Debug.LogWarning("IdlePond : feuille de style introuvable (Resources/IdlePond/IdlePond.uss).");
            racine.AddToClassList("ecran");

            _enTete = new VueEnTete();
            _retour = new VueRetour(() => _magasin.OublierRetour());
            _contenance = new VueContenance();
            _captation = new VueCaptation(() => OuvrirCaptation(null));
            _mare = new VueMare(
                banc => _magasin.Convaincre(banc),
                banc => _magasin.AcheterPlace(banc),
                () => _magasin.Creuser(),
                OuvrirCaptation);
            _eclosion = new VueEclosion(() => _magasin.Eclore());
            _succes = new VueSucces();
            _annonces = new VueAnnonces(id => _magasin.OublierAnnonce(id));

            var defilement = new ScrollView(ScrollViewMode.Vertical);
            defilement.AddToClassList("defilement");
            _page = Boite("page", "pile-6");
            _page.Add(_enTete.Racine);
            _page.Add(_retour.Racine);

            var colonnes = Boite("colonnes");
            var principal = Boite("principal", "pile-4");
            principal.Add(_contenance.Racine);
            principal.Add(_captation.Racine);
            principal.Add(_mare.Racine);
            principal.Add(_eclosion.Racine);
            var cote = Boite("cote");
            cote.Add(_succes.Racine);
            colonnes.Add(principal);
            colonnes.Add(cote);
            _page.Add(colonnes);

            defilement.Add(_page);
            racine.Add(defilement);
            racine.Add(_annonces.Racine);

            racine.RegisterCallback<GeometryChangedEvent>(evenement =>
            {
                racine.EnableInClassList("etroit", evenement.newRect.width < LargeurDUneSeuleColonne);
                AppliquerLAireSure(true);
            });

            Rafraichir();
        }

        private void OuvrirCaptation(string banc)
        {
            _captationOuverte = banc;
            Rafraichir();
        }

        public void Rafraichir()
        {
            var etat = _magasin.Etat;
            _enTete.Rafraichir(etat);
            _retour.Rafraichir(_magasin.Retour);
            _contenance.Rafraichir(etat);
            // Une éclosion emporte le banc dont on lisait le détail : le panneau se
            // referme de lui-même plutôt que de décrire ce qui n'est plus là.
            if (!_captation.Rafraichir(etat, _captationOuverte)) _captationOuverte = null;
            if (_captationOuverte != null && !etat.Cycle.Bancs.Contient(_captationOuverte))
            {
                _captationOuverte = null;
                _captation.Rafraichir(etat, null);
            }
            _mare.Rafraichir(etat);
            _eclosion.Rafraichir(etat);
            _succes.Rafraichir(etat);
            _annonces.Rafraichir(_magasin.AAnnoncer);
            AppliquerLAireSure(false);
        }

        /// <summary>
        /// Encoches et barres système : la page se tient dans `Screen.safeArea`. Les
        /// marges sont converties des pixels d'écran vers les points du panneau.
        /// </summary>
        private void AppliquerLAireSure(bool forcer)
        {
            var aire = Screen.safeArea;
            if (!forcer && aire == _aireSure) return;
            _aireSure = aire;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var largeur = _racine.layout.width;
            var hauteur = _racine.layout.height;
            if (float.IsNaN(largeur) || largeur <= 0 || float.IsNaN(hauteur) || hauteur <= 0) return;
            var echelleX = largeur / Screen.width;
            var echelleY = hauteur / Screen.height;
            _racine.style.paddingLeft = aire.xMin * echelleX;
            _racine.style.paddingRight = (Screen.width - aire.xMax) * echelleX;
            _racine.style.paddingTop = (Screen.height - aire.yMax) * echelleY;
            _racine.style.paddingBottom = aire.yMin * echelleY;
        }
    }
}
