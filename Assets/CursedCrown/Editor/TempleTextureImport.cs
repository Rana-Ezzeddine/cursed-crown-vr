using UnityEditor;
using UnityEngine;
namespace CursedCrown.Editor
{
    // Keep the scanned detail while fitting standalone Quest memory/bandwidth.
    public sealed class TempleTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/TempleTextures/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=assetPath.Contains("_normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=assetPath.Contains("_albedo");
            importer.mipmapEnabled=true;importer.anisoLevel=4;importer.maxTextureSize=1024;
            importer.wrapMode=TextureWrapMode.Repeat;importer.isReadable=false;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name="Android", overridden=true, maxTextureSize=1024, format=TextureImporterFormat.ASTC_6x6 });
        }
    }
}
