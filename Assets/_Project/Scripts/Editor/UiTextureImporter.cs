#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class UiTextureImporter : AssetPostprocessor
{
    private static readonly string[] UiAssetRoots =
    {
        "Assets/_Project/Resources/UI/",
        "Assets/_Project/Art/UI/"
    };

    private void OnPreprocessTexture()
    {
        var path = assetPath.Replace("\\", "/");
        if (!IsUiAsset(path))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Compressed;
    }

    private static bool IsUiAsset(string path)
    {
        for (var i = 0; i < UiAssetRoots.Length; i++)
        {
            if (path.StartsWith(UiAssetRoots[i]))
            {
                return true;
            }
        }

        return false;
    }
}
#endif
