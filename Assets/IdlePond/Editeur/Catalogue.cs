using System.Linq;
using IdlePond.Jeu.Scene;
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — remplit `Art/Catalogue.asset` à partir des chemins imposés. Un vrai dessin
    /// déposé sous le même nom garde le GUID du provisoire : le catalogue n'a pas à changer.
    /// Il se reconstruit quand la LISTE change (une assise, une espèce de plus).
    /// </summary>
    public static class Catalogue
    {
        [MenuItem("IdlePond/Reconstruire le catalogue d'art")]
        public static CatalogueDArt Reconstruire()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<CatalogueDArt>(Chemins.CATALOGUE);
            if (catalogue == null)
            {
                catalogue = ScriptableObject.CreateInstance<CatalogueDArt>();
                AssetDatabase.CreateAsset(catalogue, Chemins.CATALOGUE);
            }
            Sprite S(string relatif)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Chemins.ART + "/" + relatif);
                // Un chemin attendu qui ne charge rien : on le dit, sinon la scène reste muette à cet endroit.
                if (sprite == null) Debug.LogWarning("Catalogue d'art : aucun sprite à " + Chemins.ART + "/" + relatif);
                return sprite;
            }
            var stades = Enumerable.Range(0, Gabarits.LONGUEURS_DU_CORPS.Count).ToList();

            catalogue.Corps = stades.Select(s => new CatalogueDArt.Images
            {
                Sprites = Enumerable.Range(0, Gabarits.IMAGES_DE_NAGE).Select(i => S(Chemins.CorpsDuHeros(s, i))).ToArray(),
            }).ToArray();
            catalogue.Marques = RegistreDArt.ASSISES_DESSINEES.Select(a => new CatalogueDArt.MarqueDAssise
            {
                Assise = a,
                ParStade = stades.Select(s => S(Chemins.MarqueDAssise(a, s))).ToArray(),
            }).ToArray();
            catalogue.Especes = Chemins.EspecesLivrees().Select(e => new CatalogueDArt.ImagesDEspece
            {
                Espece = e.Id,
                Images = Enumerable.Range(0, Gabarits.IMAGES_D_ESPECE).Select(i => S(Chemins.ImageDEspece(e.Id, i))).ToArray(),
            }).ToArray();
            catalogue.Decors = RegistreDArt.ASSISES_DESSINEES.Select(a => new CatalogueDArt.DecorDAssise
            {
                Assise = a,
                Fond = S(Chemins.Fond(a)),
                Roche = S(Chemins.Roche(a)),
                Berge = S(Chemins.Berge(a)),
                Rayons = S(Chemins.Rayons(a)),
            }).ToArray();
            catalogue.Voile = S(Chemins.VOILE);
            catalogue.Eclat = S(Chemins.ECLAT);

            EditorUtility.SetDirty(catalogue);
            AssetDatabase.SaveAssets();
            return catalogue;
        }
    }
}
