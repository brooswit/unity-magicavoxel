using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "vox")]
public class VoxImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        // Read the voxel file rawData
        byte[] rawData = File.ReadAllBytes(ctx.assetPath);

        // Create the VoxAsset with only raw data
        VoxAsset voxAsset = ScriptableObject.CreateInstance<VoxAsset>();
        voxAsset.rawData = rawData;

        // Name the asset and set it as the main object in the import context
        voxAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        ctx.AddObjectToAsset("main obj", voxAsset);
        ctx.SetMainObject(voxAsset);
    }
}
