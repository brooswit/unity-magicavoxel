using UnityEngine;
using UnityEditor;
using System.IO;
public class VoxAssetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (Path.GetExtension(assetPath).Equals(".vox", System.StringComparison.OrdinalIgnoreCase))
            {
                foreach (VoxelModel voxelModel in VoxelModel.allVoxelModels)
                {
                    if (voxelModel != null && voxelModel.voxAsset != null)
                    {
                        string voxAssetPath = AssetDatabase.GetAssetPath(voxelModel.voxAsset);
                        if (voxAssetPath == assetPath)
                        {
                            voxelModel.LoadVoxelModel();
                            EditorUtility.SetDirty(voxelModel);
                        }
                    }
                }
            }
        }
    }
}
