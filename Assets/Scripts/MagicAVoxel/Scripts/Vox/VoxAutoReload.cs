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
                // Find all VoxelController components in the scene instead of using static list
                VoxelController[] allModels = FindObjectsOfType<VoxelController>();
                foreach (VoxelController voxelController in allModels)
                {
                    bool shouldReloadModel = voxelController.voxAsset == null || 
                        AssetDatabase.GetAssetPath(voxelController.voxAsset) == assetPath;
                    
                    if (shouldReloadModel)
                    {
                        voxelController.LoadVoxelModel();
                        EditorUtility.SetDirty(voxelController);
                    }
                }
            }
        }
    }
}
