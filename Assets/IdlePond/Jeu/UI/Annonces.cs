using System;
using System.Collections.Generic;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Les annonces de succès (`Annonces.tsx`).
    ///
    /// §8.2 : « Notification discrète et non bloquante — une ligne qui apparaît et
    /// s'efface. JAMAIS une fenêtre. » Rien ici ne prend le focus, rien n'attend un clic,
    /// rien n'interrompt : le conteneur ignore les touchers, et un joueur absent ne rate
    /// rien — le détail reste dans l'écran des succès.
    ///
    /// Chaque annonce a sa minuterie, posée une seule fois quand elle apparaît ; au bout de
    /// six secondes elle s'efface, puis la partie l'oublie. Seules les trois dernières se
    /// voient, mais toutes s'éteignent : une rafale de succès ne laisse pas de file d'attente
    /// qui s'afficherait plus tard comme du bruit.
    /// </summary>
    public sealed class Annonces
    {
        public const long DUREE_MS = 6000;
        const long DUREE_DE_L_EFFACEMENT_MS = 600;
        const int NOMBRE_VISIBLE = 3;

        readonly VisualElement racine;
        readonly Action<string> surExpiration;
        readonly Dictionary<string, Label> lignes = new Dictionary<string, Label>();
        readonly HashSet<string> minuteries = new HashSet<string>();
        string derniereAffichee = "";
        IReadOnlyList<string> source;

        public Annonces(VisualElement racine, Action<string> surExpiration)
        {
            this.racine = racine;
            this.surExpiration = surExpiration;
            racine.AddToClassList("annonces");
            // Rien de ce qui est ici n'attrape un toucher : le bouton dessous reste le bouton.
            racine.pickingMode = PickingMode.Ignore;
        }

        /// `ids` est la liste vivante de la partie : on la garde pour la relire quand une
        /// annonce s'éteint et qu'une autre, jusque-là cachée derrière les trois dernières,
        /// peut enfin se voir.
        public void Afficher(IReadOnlyList<string> ids)
        {
            source = ids;
            foreach (var id in ids)
                if (minuteries.Add(id)) Armer(id);

            var montrees = AMontrer(ids, TexteDe, NOMBRE_VISIBLE);
            var cle = string.Join("|", montrees);
            if (cle == derniereAffichee) return;
            derniereAffichee = cle;

            racine.Clear();
            foreach (var id in montrees)
            {
                if (lignes.TryGetValue(id, out var existante))
                {
                    racine.Add(existante);
                    continue;
                }
                var ligne = Elements.Texte("annonce base titre", TexteDe(id));
                if (id.StartsWith(Partie.ANNONCE_DE_LIEU)) ligne.AddToClassList("annonce-de-lieu");
                lignes[id] = ligne;
                racine.Add(ligne);
                // Elle se lève au tour suivant : une classe posée avant le premier calcul de
                // style ne déclenche aucune transition, et la ligne surgirait au lieu de
                // s'élever.
                ligne.schedule.Execute(() => ligne.AddToClassList("annonce-visible")).StartingIn(30);
            }
        }

        static string TexteDe(string id) =>
            id.StartsWith(Partie.ANNONCE_DE_LIEU)
                ? Textes.ArriveeDans(id.Substring(Partie.ANNONCE_DE_LIEU.Length))
                : Textes.DuSucces(id).Rapport;

        /// <summary>
        /// Les annonces à montrer, dans l'ordre : une seule ligne par texte — plusieurs succès
        /// partagent la même phrase (« Celui-là ne prendra plus personne. » pour chaque creux
        /// saturé), et deux lignes identiques empilées se lisent comme un bug —, puis les
        /// `visibles` dernières. Les doublons cachés gardent leur minuterie et s'éteignent
        /// avec les autres.
        /// </summary>
        public static IReadOnlyList<string> AMontrer(IReadOnlyList<string> ids, Func<string, string> texteDe, int visibles)
        {
            var vus = new HashSet<string>();
            var distinctes = new List<string>();
            foreach (var id in ids)
                if (vus.Add(texteDe(id))) distinctes.Add(id);
            return distinctes.Count > visibles ? distinctes.GetRange(distinctes.Count - visibles, visibles) : distinctes;
        }

        void Armer(string id)
        {
            racine.schedule.Execute(() =>
            {
                if (lignes.TryGetValue(id, out var ligne)) ligne.RemoveFromClassList("annonce-visible");
            }).StartingIn(DUREE_MS - DUREE_DE_L_EFFACEMENT_MS);

            racine.schedule.Execute(() =>
            {
                surExpiration(id);
                minuteries.Remove(id);
                if (lignes.TryGetValue(id, out var ligne)) ligne.RemoveFromHierarchy();
                lignes.Remove(id);
                // La liste a changé sous nos yeux : on la relit, pour qu'une annonce restée
                // cachée derrière les trois dernières prenne la place libérée.
                derniereAffichee = "";
                if (source != null) Afficher(source);
            }).StartingIn(DUREE_MS);
        }
    }
}
