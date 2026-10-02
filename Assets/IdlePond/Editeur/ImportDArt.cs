using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — l'import d'un dessin est imposé, jamais réglé à la main (spec DA §5) : un
    /// pixel par unité, filtrage net, sans compression ni mipmaps, pivot en bas à gauche pour
    /// que toute position entière tombe sur la grille. Un vrai dessin déposé dans `Art/` est
    /// donc juste du premier coup.
    /// </summary>
    public sealed class ImportDArt : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Chemins.ART + "/")) return;
            var importeur = (TextureImporter)assetImporter;
            importeur.textureType = TextureImporterType.Sprite;
            importeur.spriteImportMode = SpriteImportMode.Single;
            importeur.spritePixelsPerUnit = 1;
            importeur.filterMode = FilterMode.Point;
            importeur.textureCompression = TextureImporterCompression.Uncompressed;
            importeur.mipmapEnabled = false;
            importeur.alphaIsTransparency = true;
            importeur.wrapMode = TextureWrapMode.Clamp;
            importeur.isReadable = false;
            var reglages = new TextureImporterSettings();
            importeur.ReadTextureSettings(reglages);
            reglages.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            reglages.spriteMeshType = SpriteMeshType.FullRect;
            reglages.spriteGenerateFallbackPhysicsShape = false;
            importeur.SetTextureSettings(reglages);
        }
    }
}
