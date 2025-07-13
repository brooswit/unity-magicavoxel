using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelController : MonoBehaviour
{
    // Reference to the imported .vox file data
    public VoxAsset voxAsset;
    
    [SerializeField]
    public int modelIndex = 0;
    [SerializeField]
    private bool updateCollider = false;
    [SerializeField]
    private bool convexCollider = false;
    
    [Header("Alternate Palette")]
    [Tooltip("Optional PNG texture to override the voxel file's palette. Leave empty to use original palette.")]
    public Texture2D alternatePaletteTexture;
    
    // Mesh components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    void OnEnable()
    {
        Initialize();
    }

    void OnValidate()
    {
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

    public void LoadVoxelModel()
    {
        if (voxAsset?.data?.models != null && voxAsset.data.models.Length > 0)
        {
            // Clamp modelIndex to valid range
            int clampedIndex = Mathf.Clamp(modelIndex, 0, voxAsset.data.models.Length - 1);
            
            if (clampedIndex != modelIndex)
            {
                modelIndex = clampedIndex;
            }
            
            // Use mesh cache for optimal performance and palette support
            var mesh = VoxelMeshCache.GetOrCreateMesh(voxAsset, alternatePaletteTexture, modelIndex);
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
} 