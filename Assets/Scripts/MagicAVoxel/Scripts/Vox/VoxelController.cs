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
        if (voxAsset?.data?.meshes != null && voxAsset.data.meshes.Length > 0)
        {
            // Clamp modelIndex to valid range
            int clampedIndex = Mathf.Clamp(modelIndex, 0, voxAsset.data.meshes.Length - 1);
            
            if (clampedIndex != modelIndex)
            {
                modelIndex = clampedIndex;
            }
            
            // Use the selected mesh from the asset
            meshFilter.sharedMesh = voxAsset.data.meshes[modelIndex];

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
        meshCollider.sharedMesh = voxAsset.data.meshes[modelIndex];

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