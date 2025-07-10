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
                // Find all VoxelModel components in the scene instead of using static list
                VoxelModel[] allModels = FindObjectsOfType<VoxelModel>();
                foreach (VoxelModel voxelModel in allModels)
                {
                    bool shouldReloadModel = voxelModel.voxAsset == null || 
                        AssetDatabase.GetAssetPath(voxelModel.voxAsset) == assetPath;
                    
                    if (shouldReloadModel)
                    {
                        voxelModel.LoadVoxelModel();
                        EditorUtility.SetDirty(voxelModel);
                    }
                }
            }
        }
    }
}
