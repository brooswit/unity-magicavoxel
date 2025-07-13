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

        // Create the VoxAsset
        VoxAsset voxAsset = ScriptableObject.CreateInstance<VoxAsset>();
        voxAsset.rawData = rawData;
        
        // Create VoxData from raw data (handles parsing and mesh generation)
        voxAsset.data = new VoxData(rawData);
        
        // Set mesh names to include the asset name
        for (int i = 0; i < voxAsset.data.meshes.Length; i++)
        {
            voxAsset.data.meshes[i].name = Path.GetFileNameWithoutExtension(ctx.assetPath) + $"_Mesh_{i}";
        }

        // Name the asset and set it as the main object in the import context
        voxAsset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
        ctx.AddObjectToAsset("main obj", voxAsset);
        ctx.SetMainObject(voxAsset);
    }
}
