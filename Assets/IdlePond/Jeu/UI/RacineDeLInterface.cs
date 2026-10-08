using System;
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// IdlePond — le contrôleur racine de l'interface. Posé sur le GameObject de
    /// l'`UIDocument` de `Mare.unity` par le générateur de scènes.
    ///
    /// Il ne contient AUCUNE règle du jeu (§5.3) : il lit `Partie`, câble un contrôleur par
    /// panneau, et renvoie chaque geste du joueur à la partie. Tout ce que le joueur lit
    /// vient d'un état immuable, relu à chaque changement.
    ///
    /// Il possède trois choses, et trois seulement : l'abonnement aux événements de la
    /// partie (pris à l'activation, rendu à la désactivation, sans quoi une scène rechargée
    /// laisserait des fantômes qui écrivent dans un arbre détruit), l'adaptation au
    /// portrait et au paysage, et l'élément `#scene` que la scène dessinée emprunte.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RacineDeLInterface : MonoBehaviour
    {
        UIDocument document;
        Partie partie;
        VisualElement racine;
        VisualElement voile;
        VisualElement tiroir;
        ScrollView defilement;
        Label titreDuTiroir;
        Label sousTitreDuTiroir;
        readonly System.Collections.Generic.Dictionary<Tiroir, VisualElement> contenus =
            new System.Collections.Generic.Dictionary<Tiroir, VisualElement>();
        bool paysage;

        Barre barre;
        Lieu lieu;
        Creusement creusement;
        Dock dock;
        Retour retour;
        FicheDuHeros fiche;
        Heros heros;
        Captation captation;
        Mare mare;
        Renaissance renaissance;
        Succes succes;
        AmeliorationsDeRenaissance ameliorations;
        Annonces annonces;
        Action<EtatJeu>[] rafraichisseurs = Array.Empty<Action<EtatJeu>>();

        /// <summary>
        /// L'élément `#scene` : le rectangle que la scène dessinée remplit (elle le retrouve
        /// aussi seule, par son nom). Null avant
        /// `OnEnable` et après `OnDisable`. L'élément est transparent exprès : la caméra
        /// dessine DERRIÈRE l'interface, et tout fond opaque posé derrière lui la cacherait.
        /// </summary>
        public VisualElement CadreDeScene { get; private set; }

        void OnEnable() => Brancher();

        // `UIDocument` construit son arbre dans son propre `OnEnable`. L'ordre d'exécution
        // ci-dessus le place avant nous, mais si un autre ordre s'était glissé, la racine
        // serait vide ici : on réessaie une fois, au démarrage, au lieu de s'éteindre sans rien dire.
        void Start()
        {
            if (racine != null || !isActiveAndEnabled) return;
            Brancher();
            if (racine == null)
                Debug.LogError("RacineDeLInterface : l'UIDocument n'a pas d'arbre (Mare.uxml et PanelSettings.asset reliés ?).", this);
        }

        void OnDisable() => Debrancher();

        void Brancher()
        {
            if (racine != null) return;
            document = GetComponent<UIDocument>();
            var cible = document != null ? document.rootVisualElement : null;
            var trouvee = cible?.Q<VisualElement>("racine");
            if (trouvee == null) return;

            // Le seul chemin de création de la partie : Mare.unity lancée seule, sans
            // Amorce, la crée elle-même (§2) pour rester jouable.
            partie = Boucle.ObtenirOuCreerLaPartie();

            racine = trouvee;
            CadreDeScene = racine.Q<VisualElement>("scene");

            // Le `TemplateContainer` que l'UIDocument glisse entre le panneau et notre
            // racine ne remplit pas l'écran de lui-même : sans cela, `height: 100%` se
            // mesure sur un parent de hauteur nulle.
            for (var parent = racine.parent; parent != null; parent = parent.parent)
                parent.style.flexGrow = 1;

            ConstruirePanneaux();

            racine.RegisterCallback<GeometryChangedEvent>(SurGeometrie);
            partie.EtatChange += SurEtat;
            partie.SuccesDeclenches += SurSucces;
            partie.RetourAffiche += SurRetour;

            // Ce qui est arrivé AVANT que l'interface existe : le crédit hors ligne du
            // démarrage, les succès déjà déclenchés. Les événements, eux, sont passés.
            SurEtat(partie.Etat);
            retour.Afficher(partie.Retour);
            annonces.Afficher(partie.AAnnoncer);
            Adapter(racine.layout.width, racine.layout.height);
        }

        void Debrancher()
        {
            if (partie != null)
            {
                partie.EtatChange -= SurEtat;
                partie.SuccesDeclenches -= SurSucces;
                partie.RetourAffiche -= SurRetour;
            }
            racine?.UnregisterCallback<GeometryChangedEvent>(SurGeometrie);
            racine = null;
            CadreDeScene = null;
            partie = null;
            rafraichisseurs = Array.Empty<Action<EtatJeu>>();
        }

        void ConstruirePanneaux()
        {
            var p = partie;

            defilement = racine.Q<ScrollView>("defilement");
            ConfigurerLeDefilement(defilement);
            voile = racine.Q<VisualElement>("voile-du-tiroir");
            tiroir = racine.Q<VisualElement>("tiroir");
            contenus.Clear();
            contenus[Tiroir.Toi] = racine.Q<VisualElement>("tiroir-toi");
            contenus[Tiroir.Especes] = racine.Q<VisualElement>("tiroir-especes");
            contenus[Tiroir.Journal] = racine.Q<VisualElement>("tiroir-journal");
            contenus[Tiroir.Oeuf] = racine.Q<VisualElement>("tiroir-oeuf");

            barre = new Barre(racine.Q<VisualElement>("barre"));
            lieu = new Lieu(racine.Q<VisualElement>("lieu"));
            creusement = new Creusement(racine.Q<VisualElement>("creuser"), () => p.Creuser());
            retour = new Retour(racine.Q<VisualElement>("retour"), () => p.OublierRetour());
            fiche = new FicheDuHeros(racine.Q<VisualElement>("fiche-du-heros"));
            heros = new Heros(racine.Q<VisualElement>("heros"), () => p.Grandir());
            captation = new Captation(racine.Q<VisualElement>("captation"), () =>
            {
                captation.Fermer();
            });
            mare = new Mare(racine.Q<VisualElement>("mare"),
                id => p.Convaincre(id),
                id => p.Monter(id),
                id =>
                {
                    captation.Ouvrir(id);
                    captation.Rafraichir(p.Etat);
                    // La table s'ouvre en haut du tiroir : on y remonte pour la voir.
                    defilement.scrollOffset = Vector2.zero;
                });
            // Le gain prévu ne se calcule que tiroir de l'œuf ouvert : voir `Renaissance`.
            renaissance = new Renaissance(racine.Q<VisualElement>("renaissance"), () => p.Renaitre(),
                () => dock != null && dock.Actif == Tiroir.Oeuf);
            succes = new Succes(racine.Q<VisualElement>("succes"));
            ameliorations = new AmeliorationsDeRenaissance(racine.Q<VisualElement>("ameliorations"), id => p.AcheterUneAmelioration(id));
            annonces = new Annonces(racine.Q<VisualElement>("annonces"), id => p.OublierAnnonce(id));
            dock = new Dock(racine.Q<VisualElement>("dock"), AppliquerLeTiroir);
            ConstruireLaTeteDuTiroir(racine.Q<VisualElement>("tiroir-tete"));
            // Toucher la mare voilée referme le tiroir : on revient à la vue sans viser.
            voile.AddManipulator(new Clickable(() => dock.Fermer()));

            rafraichisseurs = new Action<EtatJeu>[]
            {
                barre.Rafraichir, lieu.Rafraichir, creusement.Rafraichir, fiche.Rafraichir,
                heros.Rafraichir, captation.Rafraichir, mare.Rafraichir, renaissance.Rafraichir,
                succes.Rafraichir, ameliorations.Rafraichir, dock.Rafraichir,
            };
            AppliquerLeTiroir();
        }

        /// La poignée, et un bouton fermer à droite : au doigt, la croix se touche sans viser.
        void ConstruireLaTeteDuTiroir(VisualElement tete)
        {
            tete.Add(Elements.Conteneur("tiroir-poignee"));
            var titres = Elements.Conteneur("tiroir-titres");
            titreDuTiroir = Elements.Texte("titre xl", "", "tiroir-titre");
            sousTitreDuTiroir = Elements.Texte("doux sm", "", "tiroir-sous-titre");
            titres.Add(titreDuTiroir);
            titres.Add(sousTitreDuTiroir);
            tete.Add(titres);
            var fermer = Elements.Conteneur("tiroir-fermer", "tiroir-fermer");
            fermer.Add(new Icone(Icones.CROIX));
            fermer.AddManipulator(new Clickable(() => dock.Fermer()));
            tete.Add(fermer);
        }

        static void ConfigurerLeDefilement(ScrollView defilement)
        {
            if (defilement == null) return;
            defilement.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            // Au doigt, on fait défiler sans viser une barre : elle ne mangerait que la largeur.
            defilement.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            // Au doigt, la liste glisse et revient ; à la souris, la molette suffit.
            defilement.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
        }

        /* ─── Ce que la partie raconte ─────────────────────────────────────────────── */

        // Une panne d'un panneau ne doit pas arrêter le jeu. Ces gestionnaires sont appelés
        // PAR la partie, au milieu de `Avancer` : une exception ici sauterait l'annonce des
        // succès qui suit, dans le tick même. On la journalise, panneau par panneau, et le
        // reste de l'écran continue de vivre. Les délégués sont créés une fois : dix ticks
        // par seconde ne doivent pas allouer dix fois huit fermetures.
        void SurEtat(EtatJeu etat)
        {
            foreach (var rafraichir in rafraichisseurs)
            {
                try { rafraichir(etat); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        void SurSucces(System.Collections.Generic.IReadOnlyList<string> _)
        {
            try { annonces.Afficher(partie.AAnnoncer); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        void SurRetour(AbsenceCreditee absence)
        {
            try { retour.Afficher(absence); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        /* ─── L'adaptation ─────────────────────────────────────────────────────────── */

        void SurGeometrie(GeometryChangedEvent evenement) => Adapter(evenement.newRect.width, evenement.newRect.height);

        /// <summary>
        /// Paysage si la largeur vaut au moins 1,2 fois la hauteur, sinon portrait. Le
        /// contrôleur ne pose que la classe : la mise en page est dans `Mare.uss`.
        /// </summary>
        void Adapter(float largeur, float hauteur)
        {
            if (!(largeur > 0 && hauteur > 0)) return;
            EviterLesBordsCaches();
            var estPaysage = Adaptation.EstPaysage(largeur, hauteur);
            if (estPaysage == paysage && racine.ClassListContains(estPaysage ? Adaptation.CLASSE_PAYSAGE : Adaptation.CLASSE_PORTRAIT))
                return;
            paysage = estPaysage;
            racine.EnableInClassList(Adaptation.CLASSE_PAYSAGE, estPaysage);
            racine.EnableInClassList(Adaptation.CLASSE_PORTRAIT, !estPaysage);
        }

        /// <summary>
        /// L'encoche, les coins arrondis, la barre de geste : `Screen.safeArea` dit où l'écran
        /// est vraiment libre. La racine s'en écarte d'autant ; son fond, lui, va jusqu'au bord.
        /// </summary>
        void EviterLesBordsCaches()
        {
            var largeurDuPanneau = racine.panel != null ? racine.panel.visualTree.worldBound.width : 0f;
            if (!(largeurDuPanneau > 0) || Screen.width <= 0) return;
            var parPixel = largeurDuPanneau / Screen.width;
            var sure = Screen.safeArea;
            racine.style.paddingTop = (Screen.height - sure.yMax) * parPixel;
            racine.style.paddingBottom = sure.yMin * parPixel;
            racine.style.paddingLeft = sure.xMin * parPixel;
            racine.style.paddingRight = (Screen.width - sure.xMax) * parPixel;
        }

        /// La durée du glissement, la même que celle du .uss (`.tiroir`, `.voile-du-tiroir`).
        const long DUREE_DU_TIROIR_MS = 260;

        /// <summary>
        /// Un seul tiroir à la fois. Fermé, la mare est entière ; ouvert, elle reste visible
        /// derrière le voile. Changer de tiroir remet le défilement en haut : on ouvre un
        /// panneau par son titre, pas au milieu de la liste du précédent.
        ///
        /// Le tiroir glisse : la classe `ouvert` porte la position, le .uss la transition. Il
        /// n'est montré qu'une image avant de recevoir la classe, sans quoi il apparaîtrait
        /// déjà arrivé ; et il n'est caché qu'une fois redescendu, sans quoi il disparaîtrait
        /// d'un coup.
        /// </summary>
        void AppliquerLeTiroir()
        {
            if (racine == null) return;
            var actif = dock.Actif;
            if (actif != Tiroir.Aucun)
            {
                var (titre, sousTitre) = TitresDu(actif);
                Elements.Poser(titreDuTiroir, titre);
                Elements.Poser(sousTitreDuTiroir, sousTitre);
                foreach (var paire in contenus) Elements.Montrer(paire.Value, paire.Key == actif);
                defilement.scrollOffset = Vector2.zero;
                Elements.Montrer(voile, true);
                Elements.Montrer(tiroir, true);
                voile.pickingMode = PickingMode.Position;
                tiroir.schedule.Execute(() =>
                {
                    if (dock.Actif == Tiroir.Aucun) return;
                    tiroir.AddToClassList("ouvert");
                    voile.AddToClassList("ouvert");
                });
                return;
            }

            var etaitOuvert = tiroir.ClassListContains("ouvert");
            tiroir.RemoveFromClassList("ouvert");
            voile.RemoveFromClassList("ouvert");
            voile.pickingMode = PickingMode.Ignore;
            if (!etaitOuvert)
            {
                Elements.Montrer(voile, false);
                Elements.Montrer(tiroir, false);
                return;
            }
            tiroir.schedule.Execute(() =>
            {
                if (dock.Actif != Tiroir.Aucun) return;
                Elements.Montrer(voile, false);
                Elements.Montrer(tiroir, false);
            }).StartingIn(DUREE_DU_TIROIR_MS);
        }

        static (string Titre, string SousTitre) TitresDu(Tiroir tiroir)
        {
            switch (tiroir)
            {
                case Tiroir.Toi: return (E.DOCK_TOI, E.SOUS_TITRE_TOI);
                case Tiroir.Especes: return (E.DOCK_ESPECES, E.SOUS_TITRE_ESPECES);
                case Tiroir.Oeuf: return (E.DOCK_OEUF, E.SOUS_TITRE_OEUF);
                case Tiroir.Journal: return (E.DOCK_JOURNAL, E.SOUS_TITRE_JOURNAL);
                default: return ("", "");
            }
        }

        /// Le tiroir ouvert, pour les tests et les ateliers.
        public Tiroir TiroirOuvert => dock != null ? dock.Actif : Tiroir.Aucun;

        public void Ouvrir(Tiroir quel) => dock?.Choisir(quel);
    }
}
