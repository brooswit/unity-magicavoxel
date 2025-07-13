using UnityEngine;

[System.Serializable]
public class VoxData
{
    public VoxPalette palette;
    public VoxModel[] models;
    public Mesh[] meshes;
    
    public VoxData()
    {
        palette = new VoxPalette();
        models = new VoxModel[0];
        meshes = new Mesh[0];
    }
    
    public VoxData(VoxPalette palette, VoxModel[] models, Mesh[] meshes)
    {
        this.palette = palette ?? new VoxPalette();
        this.models = models ?? new VoxModel[0];
        this.meshes = meshes ?? new Mesh[0];
    }
    
    // Constructor with Color32[] for backward compatibility
    public VoxData(Color32[] paletteColors, VoxModel[] models, Mesh[] meshes)
    {
        this.palette = new VoxPalette(paletteColors);
        this.models = models ?? new VoxModel[0];
        this.meshes = meshes ?? new Mesh[0];
    }
    
    // Constructor from raw .vox file data - handles entire pipeline
    public VoxData(byte[] rawVoxData)
    {
        if (rawVoxData == null || rawVoxData.Length == 0)
        {
            palette = new VoxPalette();
            models = new VoxModel[0];
            meshes = new Mesh[0];
            return;
        }
        
        // Parse the raw vox data
        var (parsedModels, parsedPalette) = VoxTools.ParseVoxData(rawVoxData);
        
        if (parsedModels == null || parsedModels.Length == 0 || parsedPalette == null)
        {
            // Parsing failed - create empty data
            palette = new VoxPalette();
            models = new VoxModel[0];
            meshes = new Mesh[0];
            return;
        }
        
        // Store parsed data
        palette = parsedPalette;
        models = parsedModels;
        
        // Generate meshes for all models
        meshes = new Mesh[models.Length];
        for (int i = 0; i < models.Length; i++)
        {
            meshes[i] = VoxTools.GenerateMesh(models[i], palette);
            meshes[i].name = $"VoxMesh_{i}";
        }
    }
    
    // Copy constructor - clones another VoxData
    public VoxData(VoxData other)
    {
        if (other == null)
        {
            palette = new VoxPalette();
            models = new VoxModel[0];
            meshes = new Mesh[0];
            return;
        }
        
        // Clone palette
        palette = new VoxPalette(other.palette);
        
        // Clone models array (shallow copy - VoxModel instances are shared)
        models = new VoxModel[other.models.Length];
        System.Array.Copy(other.models, models, other.models.Length);
        
        // Clone meshes array (shallow copy - Mesh instances are shared)
        meshes = new Mesh[other.meshes.Length];
        System.Array.Copy(other.meshes, meshes, other.meshes.Length);
    }
} 