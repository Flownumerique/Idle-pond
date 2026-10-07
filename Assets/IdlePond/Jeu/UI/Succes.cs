using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Le Journal : l'écran des succès (§8.3), en grille (maquette du 2026-10-07, révision 2).
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
        readonly string lieu = Assises.Toutes[0].Id;

        object signature;
        int filtre;
        EtatJeu dernier;

        public Succes(VisualElement racine)
        {
            racine.AddToClassList("panneau");

            var resume = Elements.Conteneur("carte carte-souffle journal-resume");
            var gauche = Elements.Conteneur("journal-resume-texte");
            gauche.Add(Elements.Texte("souffle-doux xs", Format.NomDeLAssiseCapitale(lieu)));
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

        void Reconstruire(EtatJeu etat)
        {
            grille.Clear();
            enCours.Clear();
            largeurs.Clear();

            var tous = RegleDesSucces.SuccesListables(etat, lieu);
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

        static VisualElement Arrive(SuccesAffichable entree)
        {
            var texte = Textes.DuSucces(entree.Succes.Id);
            var carte = Carte("arrive", Icones.JOURNAL);
            carte.Add(Elements.Texte("titre sm", texte.Nom));
            carte.Add(Elements.Texte("tu xs italique", texte.Rapport));
            carte.Add(Elements.Texte("souffle-doux xs", E.ARRIVE));
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
