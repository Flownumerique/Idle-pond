/*
 * IdlePond — l'amorce.
 *
 * N'importe quelle scène démarre le jeu : s'il n'y a pas déjà un `Jeu` dans la
 * scène chargée, l'amorce en crée un. Le projet n'a donc besoin d'aucune scène
 * préparée à la main pour être jouable dans l'éditeur, et la scène de build
 * (`IdlePond ▸ Préparer la scène de jeu`) n'est qu'une coquille.
 *
 * Pour s'en passer — une scène de test, un autre prototype —, poser un objet
 * portant `SansAmorce` dans la scène.
 */
using UnityEngine;

namespace IdlePond.Interface
{
    public static class Amorce
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Demarrer()
        {
            if (Object.FindAnyObjectByType<SansAmorce>() != null) return;
            if (Object.FindAnyObjectByType<Jeu>() != null) return;
            new GameObject("IdlePond").AddComponent<Jeu>();
        }
    }

    /// <summary>Présent dans une scène, empêche l'amorce de créer le jeu.</summary>
    [DisallowMultipleComponent]
    public sealed class SansAmorce : MonoBehaviour
    {
    }
}
