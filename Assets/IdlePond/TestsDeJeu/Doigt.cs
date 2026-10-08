using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Un toucher au doigt : appuyer puis relâcher au centre de l'élément, envoyés au
    /// panneau, qui retrouve lui-même l'élément sous le doigt — comme pour un vrai doigt.
    /// Ce qui couvre l'élément (un voile, une couche) reçoit donc le toucher à sa place.
    /// Un tiroir qui glisse encore n'est pas à sa place : attendre qu'il soit arrivé.
    /// </summary>
    static class Doigt
    {
        public static IEnumerator Toucher(VisualElement element)
        {
            var centre = element.worldBound.center;
            var arbre = element.panel.visualTree;
            using (var e = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = centre, button = 0, clickCount = 1 }))
                arbre.SendEvent(e);
            yield return null;
            using (var e = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = centre, button = 0, clickCount = 1 }))
                arbre.SendEvent(e);
            yield return null;
        }
    }
}
