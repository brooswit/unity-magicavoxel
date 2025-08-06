using UnityEngine;

[System.Serializable]
public class VoxelDebugger : MonoBehaviour
{
    [Header("Debug Info")]
    public VoxAsset voxAsset;
    
    [Space]
    [Header("Debug Output")]
    [TextArea(10, 20)]
    public string debugInfo = "Click 'Analyze Voxel Data' to see details";

    [ContextMenu("Analyze Voxel Data")]
    public void AnalyzeVoxelData()
    {
        if (voxAsset?.rawData == null)
        {
            debugInfo = "No voxAsset assigned or no rawData found.";
            return;
        }

        var voxData = new VoxData(voxAsset.rawData);
        
        if (voxData?.models == null || voxData.models.Length == 0)
        {
            debugInfo = "Failed to parse vox data or no models found.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== VOX FILE ANALYSIS ===");
        sb.AppendLine($"File: {voxAsset.name}");
        sb.AppendLine($"Models found: {voxData.models.Length}");
        sb.AppendLine();

        for (int modelIdx = 0; modelIdx < voxData.models.Length; modelIdx++)
        {
            var model = voxData.models[modelIdx];
            sb.AppendLine($"--- Model {modelIdx} ---");
            sb.AppendLine($"Size: {model.sizeX} x {model.sizeY} x {model.sizeZ}");
            
            // Sample some voxels to see colors
            int voxelCount = 0;
            var colorCounts = new System.Collections.Generic.Dictionary<byte, int>();
            
            for (int x = 0; x < model.sizeX; x++)
            {
                for (int y = 0; y < model.sizeY; y++)
                {
                    for (int z = 0; z < model.sizeZ; z++)
                    {
                        Vector3 pos = new Vector3(x, z, -y);
                        byte paletteIndex = model.GetVoxel(pos);
                        
                        if (paletteIndex != 0)
                        {
                            voxelCount++;
                            if (colorCounts.ContainsKey(paletteIndex))
                                colorCounts[paletteIndex]++;
                            else
                                colorCounts[paletteIndex] = 1;
                        }
                    }
                }
            }
            
            sb.AppendLine($"Total voxels: {voxelCount}");
            sb.AppendLine($"Unique colors: {colorCounts.Count}");
            
            // Show color breakdown
            foreach (var kvp in colorCounts)
            {
                Color32 color = voxData.palette[kvp.Key - 1];
                sb.AppendLine($"  Palette {kvp.Key}: {kvp.Value} voxels - RGB({color.r},{color.g},{color.b})");
            }
            sb.AppendLine();
        }

        debugInfo = sb.ToString();
        Debug.Log(debugInfo);
    }
}