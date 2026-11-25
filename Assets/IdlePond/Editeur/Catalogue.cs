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
            catalogue.Eveils = stades.Select(s => new CatalogueDArt.EveilDUnStade
            {
                Runes = S(Chemins.Runes(s)),
                Voile = Enumerable.Range(0, 2).Select(i => S(Chemins.Voile(s, i))).ToArray(),
                Anneau = S(Chemins.Anneau(s)),
                Aura = S(Chemins.Aura(s)),
            }).ToArray();
            catalogue.Etoile = S(Chemins.ETOILE);
            catalogue.Oeuf = S(Chemins.OEUF);
            catalogue.OeufFendu = S(Chemins.OEUF_FENDU);
            catalogue.Larve = Enumerable.Range(0, 2).Select(i => S(Chemins.Larve(i))).ToArray();
            catalogue.Marques =RegistreDArt.ASSISES_DESSINEES.Select(a => new CatalogueDArt.MarqueDAssise
            {
                Assise = a,
                ParStade = stades.Select(s => S(Chemins.MarqueDAssise(a, s))).ToArray(),
            }).ToArray();
            catalogue.Especes = Chemins.EspecesLivrees().Select(e => new CatalogueDArt.ImagesDEspece
            {
                Espece = e.Id,
                Images = Enumerable.Range(0, Gabarits.IMAGES_D_ESPECE).Select(i => S(Chemins.ImageDEspece(e.Id, i))).ToArray(),
            }).ToArray();
            catalogue.Representants = Chemins.EspecesLivrees().Select(e => new CatalogueDArt.ImagesDEspece
            {
                Espece = e.Id,
                Images = Enumerable.Range(0, Gabarits.IMAGES_DE_NAGE).Select(i => S(Chemins.ImageDuRepresentant(e.Id, i))).ToArray(),
            }).ToArray();
            catalogue.Faune = IdlePond.Jeu.Scene.Faune.DESSINS.Select(f => new CatalogueDArt.ImagesDeFaune
            {
                Nom = f.Key,
                Images = Enumerable.Range(0, f.Value.Images).Select(i => S(Chemins.ImageDeFaune(f.Key, i))).ToArray(),
            }).ToArray();
            catalogue.Decors = RegistreDArt.ASSISES_DESSINEES.Select(a => new CatalogueDArt.DecorDAssise
            {
                Assise = a,
                Eaux = Chemins.Rangs(a).Select(r => S(Chemins.Eau(a, r))).ToArray(),
                Parois = Chemins.Rangs(a).Select(r => S(Chemins.Parois(a, r))).ToArray(),
                Corniches = Chemins.Rangs(a).Select(r => S(Chemins.Corniche(a, r))).ToArray(),
                Lit = S(Chemins.Lit(a)),
                Roche = S(Chemins.Roche(a)),
                // Seulement là où il y en a une : sous terre, pas de berge.
                Berge = RegistreDArt.DecorDe(a).Berge ? S(Chemins.Berge(a)) : null,
                Rayons = S(Chemins.Rayons(a)),
                // Ce qui flotte dans chaque bande : seulement là où il y a quelque chose.
                Ambiance = RegistreDArt.DecorDe(a).AUneAmbiance ? S(Chemins.Ambiance(a)) : null,
                // La lèvre de la vasque : pour un lieu qu'on atteint en perçant (2026-11-25).
                Seuil = a != RegistreDArt.ASSISES_DESSINEES[0] ? S(Chemins.Seuil(a)) : null,
            }).ToArray();
            catalogue.Voile = S(Chemins.VOILE);
            catalogue.Eclat = S(Chemins.ECLAT);
            catalogue.Caustiques = Enumerable.Range(0, Eau.COUCHES_DE_CAUSTIQUES).Select(c => S(Chemins.Caustiques(c))).ToArray();
            catalogue.SansLumiere = AssetDatabase.LoadAssetAtPath<Material>(Chemins.SANS_LUMIERE);
            if (catalogue.SansLumiere == null) Debug.LogWarning("Catalogue d'art : aucun matériau à " + Chemins.SANS_LUMIERE);

            EditorUtility.SetDirty(catalogue);
            AssetDatabase.SaveAssets();
            return catalogue;
        }
    }
}
