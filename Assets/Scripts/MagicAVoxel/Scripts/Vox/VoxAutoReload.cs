using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class VoxAutoReload : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        string[] allChangedAssets = importedAssets.Concat(deletedAssets).Concat(movedAssets).ToArray();
        bool shouldReload = false;
        foreach (string assetPath in allChangedAssets)
        {
            if (Path.GetExtension(assetPath).Equals(".vox", System.StringComparison.OrdinalIgnoreCase))
            {
                // Find all VoxelDefinition components in the scene
                VoxelDefinition[] allDefinitions = FindObjectsOfType<VoxelDefinition>();
                foreach (VoxelDefinition voxelDefinition in allDefinitions)
                {
                    bool shouldReloadModel = voxelDefinition.voxAsset == null || 
                        AssetDatabase.GetAssetPath(voxelDefinition.voxAsset) == assetPath;
                    
                    if (shouldReloadModel)
                    {
                        // Clear the definition's cache and reinitialize
                        voxelDefinition.OnValidate();
                        EditorUtility.SetDirty(voxelDefinition);
                    }
                }
            }
        }
    }
}
