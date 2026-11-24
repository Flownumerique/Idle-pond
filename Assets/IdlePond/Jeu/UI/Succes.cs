using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// La Bibliothèque (GDD §14.8 ; tiroir « Journal » jusqu'au 2026-11-24, renommé au choix de
    /// l'auteur — le journal qui raconte viendra avec le récit). Deux parties :
    ///
    /// - **Ceux qu'il a rencontrés** : les tribus des lieux atteints (Codex v1.4). Connue, une
    ///   tribu montre son représentant, son nom, ses mœurs et son texte ; seulement aperçue, sa
    ///   silhouette. Rien d'un lieu qu'on n'a pas atteint.
    /// - **Ce qu'il sait** : les succès (§8.3), en grille (maquette du 2026-10-07, révision 2).
    ///   Toucher un succès arrivé ou en chemin ouvre son détail : la condition, l'effet exact,
    ///   chiffré (§14.3, le lieu de l'audit), et la vie où il a été noté.
    ///
    ///
    /// | État    | Ce que le joueur voit                    |
    /// |---------|------------------------------------------|
    /// | Arrivé  | Nom + ce qui s'est passé, en or          |
    /// | Ouvert  | Nom + condition + barre de progression   |
    /// | Fermé   | Nom seul, condition masquée              |
    /// | Secret  | Emplacement vide, rien d'autre           |
    ///
    /// Écran seulement : un succès n'a ni rang ni geste à faire pour le toucher, son effet
    /// s'applique quand il tombe (choix de l'utilisateur, 2026-10-07). Le haut dit combien
    /// sont arrivés dans ce lieu ; les filtres trient par famille.
    ///
    /// Verrouillage par lieu : un succès n'est listé, quel que soit son état, que lorsque son
    /// lieu est atteint. Ce qui est ARRIVÉ passe devant : la narration est une récompense
    /// distribuée à la cadence idle (§4.2), pas une liste de courses.
    ///
    /// La grille ne se reconstruit que lorsqu'un succès tombe ou qu'on change de filtre ; entre
    /// deux, seules les barres de progression bougent.
    /// </summary>
    public sealed class Succes
    {
        static readonly (FamilleDeSucces? Famille, string Libelle)[] FILTRES =
        {
            (null, E.FAMILLE_TOUS),
            (FamilleDeSucces.Franchissement, E.FAMILLE_FRANCHISSEMENT),
            (FamilleDeSucces.Seuil, E.FAMILLE_SEUIL),
            (FamilleDeSucces.Acte, E.FAMILLE_ACTE),
        };

        readonly VisualElement grille;
        readonly Label compte;
        readonly VisualElement remplissageDuCompte;
        readonly List<VisualElement> puces = new List<VisualElement>();
        readonly List<(SuccesAffichable Entree, VisualElement Barre)> enCours =
            new List<(SuccesAffichable, VisualElement)>();
        readonly Dictionary<VisualElement, float> largeurs = new Dictionary<VisualElement, float>();
        readonly Label nomDuLieu;

        object signature;
        int filtre;
        EtatJeu dernier;

        readonly System.Func<string, UnityEngine.Sprite> portraitDe;
        readonly VisualElement tribus;
        string signatureDesTribus;
        /// Le succès dont le détail est ouvert : il le reste quand la grille se reconstruit.
        string ouvert;

        /// Une silhouette : le portrait du représentant, teint de l'ombre de l'eau.
        static readonly UnityEngine.Color SILHOUETTE = new UnityEngine.Color(0.06f, 0.11f, 0.12f, 1f);

        /// `portraitDe` : le portrait du représentant d'une tribu, pris au catalogue d'art ;
        /// null sans scène dessinée (la carte garde alors son icône).
        public Succes(VisualElement racine, System.Func<string, UnityEngine.Sprite> portraitDe = null)
        {
            racine.AddToClassList("panneau");
            this.portraitDe = portraitDe;

            racine.Add(Elements.Texte("titre base bibliotheque-titre", E.BIBLIOTHEQUE_TRIBUS));
            tribus = Elements.Conteneur("liste bibliotheque-tribus");
            racine.Add(tribus);
            racine.Add(Elements.Texte("titre base bibliotheque-titre", E.BIBLIOTHEQUE_SAVOIR));

            var resume = Elements.Conteneur("carte carte-souffle journal-resume");
            var gauche = Elements.Conteneur("journal-resume-texte");
            nomDuLieu = Elements.Texte("souffle-doux xs");
            gauche.Add(nomDuLieu);
            compte = Elements.Texte("chiffre souffle xl", "", "journal-compte");
            gauche.Add(compte);
            gauche.Add(Elements.Texte("souffle-doux xs", E.JOURNAL_ARRIVES));
            resume.Add(gauche);
            var rail = Elements.Conteneur("rail-de-seuil journal-rail");
            remplissageDuCompte = Elements.Conteneur("rail-de-seuil-remplissage journal-rail-remplissage");
            rail.Add(remplissageDuCompte);
            resume.Add(rail);
            racine.Add(resume);

            var filtres = Elements.Conteneur("filtres");
            for (var i = 0; i < FILTRES.Length; i++)
            {
                var indice = i;
                var puce = Elements.Conteneur("filtre");
                puce.Add(Elements.Texte("sm", FILTRES[i].Libelle));
                puce.AddManipulator(new Clickable(() => Filtrer(indice)));
                filtres.Add(puce);
                puces.Add(puce);
            }
            racine.Add(filtres);

            grille = Elements.Conteneur("grille-de-succes");
            racine.Add(grille);
            MarquerLeFiltre();
        }

        void Filtrer(int indice)
        {
            filtre = indice;
            MarquerLeFiltre();
            if (dernier != null) Reconstruire(dernier);
        }

        void MarquerLeFiltre()
        {
            for (var i = 0; i < puces.Count; i++) Elements.Marquer(puces[i], "actif", i == filtre);
        }

        public void Rafraichir(EtatJeu etat)
        {
            dernier = etat;
            RafraichirLesTribus(etat);
            if (!ReferenceEquals(signature, etat.Permanent.Succes))
            {
                signature = etat.Permanent.Succes;
                Reconstruire(etat);
            }
            foreach (var (entree, barre) in enCours)
            {
                var progression = RegleDesSucces.ProgressionVersLeSucces(etat, entree.Succes);
                if (progression == null) continue;
                // Un centième de pourcent ne se voit pas : on n'écrit pas à chaque tick.
                var part = (float)System.Math.Round(progression.Value * 100, 1);
                if (largeurs.TryGetValue(barre, out var avant) && avant == part) continue;
                largeurs[barre] = part;
                Elements.RegleLaLargeur(barre, part / 100.0);
            }
        }

        // Arrivé et en chemin se déplient : ils ne peuvent plus être statiques.
        void Reconstruire(EtatJeu etat)
        {
            grille.Clear();
            enCours.Clear();
            largeurs.Clear();

            // Tous les lieux atteints : `SuccesListables` ne rend rien d'un lieu qu'on n'a pas
            // atteint (§14.6). Le nom en tête est celui du lieu où l'on est.
            var tous = Assises.Toutes.SelectMany(a => RegleDesSucces.SuccesListables(etat, a.Id)).ToList();
            Elements.Poser(nomDuLieu, Format.NomDeLAssiseCapitale(Assises.DuPalier(System.Math.Max(0, etat.Cycle.PaliersOuverts - 1)).Id));
            var arrives = tous.Count(e => e.Acquis);
            Elements.Poser(compte, Format.Remplir(E.JOURNAL_COMPTE, arrives, tous.Count));
            Elements.RegleLaLargeur(remplissageDuCompte, tous.Count == 0 ? 0 : arrives / (double)tous.Count);

            var famille = FILTRES[filtre].Famille;
            var liste = tous.Where(e => famille == null || e.Succes.Famille == famille).ToList();
            if (liste.Count == 0)
            {
                grille.Add(Elements.Texte("tu sm", E.SUCCES_RIEN_ENCORE));
                return;
            }
            // Le plus récent des arrivés d'abord, puis ce qui est en chemin, puis le reste.
            foreach (var entree in liste.Where(e => e.Acquis).Reverse()) grille.Add(Arrive(entree));
            foreach (var entree in liste.Where(e => !e.Acquis && e.Visibilite == VisibiliteDeSucces.Ouvert)) grille.Add(EnChemin(etat, entree));
            foreach (var entree in liste.Where(e => !e.Acquis && e.Visibilite == VisibiliteDeSucces.Ferme)) grille.Add(Ferme(entree));
            // Un emplacement vide dit qu'il y a quelque chose là, pas quoi.
            foreach (var _ in liste.Where(e => !e.Acquis && e.Visibilite == VisibiliteDeSucces.Secret)) grille.Add(Secret());
        }

        static VisualElement Carte(string classe, string[] icone)
        {
            var carte = Elements.Conteneur("carte-de-succes " + classe);
            carte.Add(new Icone(icone));
            return carte;
        }

        /* ─── Ceux qu'il a rencontrés ─────────────────────────────────────────────── */

        /// Les tribus ne se redessinent que lorsque l'une change d'état ou qu'un lieu s'ouvre.
        void RafraichirLesTribus(EtatJeu etat)
        {
            var liste = Bibliotheque.Tribus(etat);
            var cle = string.Join(",", liste.Select(t => t.Espece.Id + ":" + (int)t.Etat));
            if (cle == signatureDesTribus) return;
            signatureDesTribus = cle;
            tribus.Clear();
            foreach (var entree in liste) tribus.Add(Tribu(entree));
        }

        VisualElement Tribu(EntreeDeTribu entree)
        {
            var connue = entree.Etat == EtatDeTribu.Connue;
            var carte = Elements.Conteneur("carte-de-tribu " + (connue ? "connue" : "apercue"), "tribu-" + entree.Espece.Id);
            var vignette = Elements.Conteneur("vignette vignette-portrait");
            var portrait = portraitDe?.Invoke(entree.Espece.Id);
            if (portrait != null)
            {
                vignette.style.backgroundImage = new StyleBackground(portrait);
                if (!connue) vignette.style.unityBackgroundImageTintColor = SILHOUETTE;
            }
            else vignette.Add(new Icone(connue ? Icones.ESPECES : Icones.CADENAS));
            carte.Add(vignette);

            var infos = Elements.Conteneur("carte-de-tribu-infos");
            var texte = Textes.DeLaTribu(entree.Espece.Id);
            if (connue)
            {
                infos.Add(Elements.Texte("titre sm", Format.Capitale(texte.Tribu)));
                infos.Add(Elements.Texte("souffle-doux xs", Format.Capitale(texte.Representant)));
                infos.Add(Elements.Texte("doux xs", texte.Moeurs));
                infos.Add(Elements.Texte("tu xs italique", "« " + texte.Citation + " »"));
            }
            else
            {
                infos.Add(Elements.Texte("titre tu sm", E.INCONNU));
                infos.Add(Elements.Texte("tu xs", E.TRIBU_APERCUE));
            }
            carte.Add(infos);
            return carte;
        }

        /* ─── Ce qu'il sait : le détail d'un succès ───────────────────────────────── */

        /// Toucher une carte ouvre son détail, toucher encore le referme ; un seul ouvert.
        void RendreDepliable(VisualElement carte, SuccesAffichable entree)
        {
            var detail = Elements.Conteneur("detail-de-succes");
            carte.Add(detail);
            carte.Add(Elements.Texte("tu xs indice-de-detail", E.DETAIL_TOUCHER));
            void Montrer(bool visible)
            {
                detail.Clear();
                Elements.Montrer(detail, visible);
                if (!visible || dernier == null) return;
                var d = Bibliotheque.Detail(dernier, entree.Succes);
                detail.Add(Elements.Texte("doux xs", Format.Remplir(E.DETAIL_QUAND, d.Condition)));
                detail.Add(Elements.Texte("souffle-doux xs", Format.Remplir(E.DETAIL_EFFET, d.Effet)));
                if (d.NoteDansLaVie.HasValue) detail.Add(Elements.Texte("tu xs", Format.Remplir(E.DETAIL_NOTE, d.NoteDansLaVie.Value)));
            }
            Montrer(ouvert == entree.Succes.Id);
            carte.AddManipulator(new Clickable(() =>
            {
                ouvert = ouvert == entree.Succes.Id ? null : entree.Succes.Id;
                if (dernier != null) Reconstruire(dernier);
            }));
        }

        VisualElement Arrive(SuccesAffichable entree)
        {
            var texte = Textes.DuSucces(entree.Succes.Id);
            var carte = Carte("arrive", Icones.JOURNAL);
            carte.Add(Elements.Texte("titre sm", texte.Nom));
            carte.Add(Elements.Texte("tu xs italique", texte.Rapport));
            carte.Add(Elements.Texte("souffle-doux xs", E.ARRIVE));
            RendreDepliable(carte, entree);
            return carte;
        }

        VisualElement EnChemin(EtatJeu etat, SuccesAffichable entree)
        {
            var texte = Textes.DuSucces(entree.Succes.Id);
            var carte = Carte("en-chemin", Icones.JOURNAL);
            carte.Add(Elements.Texte("titre sm", texte.Nom));
            carte.Add(Elements.Texte("tu xs", texte.Condition));
            // Sans seuil mesurable (creux au complet), pas de barre : une barre fixe à zéro
            // dirait une progression qui n'existe pas.
            if (RegleDesSucces.ProgressionVersLeSucces(etat, entree.Succes) != null)
            {
                var rail = Elements.Conteneur("succes-rail");
                var barre = Elements.Conteneur("succes-barre");
                rail.Add(barre);
                carte.Add(rail);
                enCours.Add((entree, barre));
            }
            RendreDepliable(carte, entree);
            return carte;
        }

        static VisualElement Ferme(SuccesAffichable entree)
        {
            var carte = Carte("ferme", Icones.CADENAS);
            carte.Add(Elements.Texte("titre tu sm", Textes.DuSucces(entree.Succes.Id).Nom));
            return carte;
        }

        static VisualElement Secret()
        {
            var carte = Carte("secret", Icones.CADENAS);
            carte.Add(Elements.Texte("tu sm", E.INCONNU));
            return carte;
        }
    }
}
