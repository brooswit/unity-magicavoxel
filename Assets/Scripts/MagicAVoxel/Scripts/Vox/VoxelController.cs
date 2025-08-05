using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelController : MonoBehaviour
{
    // Reference to the imported .vox file data
    public VoxAsset voxAsset;
    
    [SerializeField]
    public int modelIndex = 0;
    
    [Header("Alternate Palette")]
    [Tooltip("Optional PNG texture to override the voxel file's palette. Leave empty to use original palette.")]
    public Texture2D alternatePaletteTexture;
    
    [SerializeField]
    private bool updateCollider = false;
    [SerializeField]
    private bool convexCollider = false;
    
    // Mesh components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    // Cached parsed data to avoid repeated parsing
    private VoxData _cachedVoxData;
    private bool _dataCached = false;

    void OnEnable()
    {
        Initialize();
    }

    void OnValidate()
    {
        // Clear cache when asset changes
        _dataCached = false;
        _cachedVoxData = null;
        Initialize();
    }

    private void Initialize()
    {
        InitializeComponents();
        AssignMaterial();
        LoadVoxelModel();
    }

    private void InitializeComponents()
    {
        meshFilter = meshFilter ?? GetComponent<MeshFilter>();
        meshRenderer = meshRenderer ?? GetComponent<MeshRenderer>();
        meshCollider = meshCollider ?? GetComponent<MeshCollider>();
    }

    private void AssignMaterial()
    {
        if (meshRenderer.sharedMaterial == null)
        {
            // Assign your shader here if needed
            // For example:
            Shader shader = Shader.Find("Custom/VertexColorShader");
            if (shader != null)
            {
                Material material = new Material(shader);
                meshRenderer.sharedMaterial = material;
            }
        }
    }

    private VoxData GetParsedData()
    {
        if (voxAsset?.rawData == null) return null;
        
        if (!_dataCached || _cachedVoxData == null)
        {
            _cachedVoxData = new VoxData(voxAsset.rawData);
            _dataCached = true;
        }
        
        return _cachedVoxData;
    }

    public void LoadVoxelModel()
    {
        var voxData = GetParsedData();
        if (voxData?.models != null && voxData.models.Length > 0)
        {
            // Clamp modelIndex to valid range
            int clampedIndex = Mathf.Clamp(modelIndex, 0, voxData.models.Length - 1);
            
            if (clampedIndex != modelIndex)
            {
                modelIndex = clampedIndex;
            }
            
            // Use mesh cache for optimal performance and palette support
            var mesh = VoxMeshCache.GetOrCreateMesh(voxAsset, alternatePaletteTexture, modelIndex);
            meshFilter.sharedMesh = mesh;

            // Update collider if needed
            if (updateCollider)
            {
                UpdateCollider();
            }
        }
        else
        {
            ClearVoxelData();
        }
    }

    private void ClearVoxelData()
    {
        meshFilter.sharedMesh = null;
        if (updateCollider)
        {
            UpdateCollider();
        }
    }

    private void UpdateCollider()
    {
        if (meshCollider == null)
        {
            meshCollider = gameObject.GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshFilter.sharedMesh;

        // Set the convex property based on the presence of a Rigidbody
        if (GetComponent<Rigidbody>() != null && convexCollider)
        {
            meshCollider.convex = true;
        }
        else
        {
            meshCollider.convex = false;
        }
    }
    
    /// <summary>
    /// Creates or retrieves a cached runtime palette with color overrides.
    /// Automatically sets alternatePaletteTexture to the generated palette.
    /// </summary>
    /// <param name="paletteName">Unique name for the palette (e.g., "player_123")</param>
    /// <param name="colorOverrides">Dictionary mapping palette index to Color32 override</param>
    /// <returns>Generated or cached Texture2D palette</returns>
    public Texture2D SetColorOverrides(string paletteName, Dictionary<int, Color32> colorOverrides)
    {
        var voxData = GetParsedData();
        if (voxData?.palette == null)
        {
            Debug.LogError("VoxelController: Cannot create color overrides - voxAsset has no base palette");
            return null;
        }
        
        // Get or create the palette texture
        var paletteTexture = VoxPaletteCache.GetOrCreatePalette(paletteName, voxData.palette, colorOverrides);
        
        // Automatically apply it to this controller
        alternatePaletteTexture = paletteTexture;
        
        // Trigger mesh refresh
        LoadVoxelModel();
        
        return paletteTexture;
    }
    
    /// <summary>
    /// Removes color overrides and returns to the original vox file palette.
    /// </summary>
    public void ClearColorOverrides()
    {
        alternatePaletteTexture = null;
        LoadVoxelModel();
    }
} 