/*
 * Les annonces de succès. Port de `Annonces.tsx`.
 *
 * §8.2 : « Notification discrète et non bloquante — une ligne qui apparaît et
 * s'efface. JAMAIS une fenêtre. » Rien ici ne prend le focus, rien n'attend un
 * geste, rien n'intercepte un clic : la couche entière ignore le pointeur.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueAnnonces
    {
        /// <summary>Le temps que met une ligne à s'effacer avant de quitter l'écran.</summary>
        private const long DureeDeSortieMs = 700;

        private readonly Action<string> _surExpiration;
        private readonly Dictionary<string, Label> _affichees = new Dictionary<string, Label>();
        private readonly HashSet<string> _programmees = new HashSet<string>();

        public VueAnnonces(Action<string> surExpiration)
        {
            _surExpiration = surExpiration;
            Racine = Boite("annonces");
            Racine.pickingMode = PickingMode.Ignore;
        }

        public VisualElement Racine { get; }

        public void Rafraichir(IReadOnlyList<string> aAnnoncer)
        {
            // Chaque annonce expire six secondes après son arrivée, qu'elle ait été
            // montrée ou non : dans une rafale, seules les trois dernières se voient,
            // et les autres ne doivent pas rester en file pour toujours. La minuterie
            // vit sur la couche, qui ne quitte jamais l'écran — jamais sur la ligne,
            // dont le retrait l'annulerait.
            foreach (var id in aAnnoncer)
            {
                if (!_programmees.Add(id)) continue;
                var annonce = id;
                Racine.schedule.Execute(() =>
                {
                    _programmees.Remove(annonce);
                    _surExpiration(annonce);
                }).StartingIn((long)Modeles.DureeDUneAnnonceMs);
            }

            var visibles = aAnnoncer.Distinct().ToList();
            visibles = visibles.Skip(Math.Max(0, visibles.Count - Modeles.AnnoncesVisibles)).ToList();

            foreach (var id in _affichees.Keys.Where(id => !visibles.Contains(id)).ToList())
            {
                _affichees[id].RemoveFromHierarchy();
                _affichees.Remove(id);
            }

            foreach (var id in visibles)
            {
                if (_affichees.ContainsKey(id)) continue;
                var ligne = Texte(Modeles.Annonce(id), "annonce");
                ligne.pickingMode = PickingMode.Ignore;
                _affichees[id] = ligne;
                Racine.Add(ligne);
                // La transition ne joue qu'entre deux états : la ligne entre invisible,
                // devient visible à la frame suivante, s'efface, puis s'en va.
                var dureeVisible = (long)Modeles.DureeDUneAnnonceMs - DureeDeSortieMs;
                ligne.schedule.Execute(() => ligne.AddToClassList("visible")).StartingIn(16);
                ligne.schedule.Execute(() => ligne.AddToClassList("sortante")).StartingIn(dureeVisible);
            }
        }
    }
}
