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
        
        foreach (string assetPath in allChangedAssets)
        {
            if (Path.GetExtension(assetPath).Equals(".vox", System.StringComparison.OrdinalIgnoreCase))
            {
                // Find all VoxelDefinition components in the scene
                VoxelDefinition[] allDefinitions = Object.FindObjectsByType<VoxelDefinition>(FindObjectsSortMode.None);
                foreach (VoxelDefinition voxelDefinition in allDefinitions)
                {
                    bool shouldReloadModel = voxelDefinition.voxAsset == null || 
                        AssetDatabase.GetAssetPath(voxelDefinition.voxAsset) == assetPath;
                    
                    if (shouldReloadModel)
                    {
                        // Force cache clearing and reinitialize using reflection since OnValidate is protected
                        var clearMethod = typeof(VoxelDefinition).GetMethod("ClearAllCaches", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var initMethod = typeof(VoxelDefinition).GetMethod("InitializeCache", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        
                        if (clearMethod != null && initMethod != null)
                        {
                            clearMethod.Invoke(voxelDefinition, null);
                            initMethod.Invoke(voxelDefinition, null);
                        }
                        
                        EditorUtility.SetDirty(voxelDefinition);
                    }
                }
            }
        }
    }
}
