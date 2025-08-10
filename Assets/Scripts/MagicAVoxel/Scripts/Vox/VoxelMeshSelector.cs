using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VoxelMeshSelector selects and displays specific frames and palettes from a VoxelDefinition.
/// This component handles mesh assignment and automatic synchronization.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelMeshSelector : MonoBehaviour
{
    //=========================================================================
    // Public variables

    [Header("Voxel Configuration")]
    [Tooltip("Reference to the VoxelDefinition that manages the voxel data")]
    public VoxelDefinition voxelDefinition;
    
    [Tooltip("Frame index to display")]
    [SerializeField] private int _frame = 0;
    
    [Tooltip("Name of the palette to use")]
    [SerializeField] private string _paletteName = "default";

    [Tooltip("Scale applied when generating the mesh (vertex positions scaled)")]
    [SerializeField] private float _scale = 1f;
    
    [Header("Collider Settings")]
    [Tooltip("Whether to automatically update the mesh collider when mesh changes")]
    [SerializeField] private bool _updateCollider = false;
    
    [Tooltip("Whether to make the collider convex (required for Rigidbody physics)")]
    [SerializeField] private bool _convexCollider = false;
    
    //=========================================================================
    // Internal variables

    // Mesh components
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    
    // Previous values for change detection
    private int _previousFrame = -1;
    private string _previousPaletteName = string.Empty;
    private float _previousScale = -1f;
    private bool _previousHasRigidbody = false;
    
    //=========================================================================
    // Public properties
    
    public int Frame
    {
        get => _frame;
        set => SelectFrame(value);
    }
    
    public string PaletteName
    {
        get => _paletteName;
        set => SelectPalette(value);
    }

    public float Scale
    {
        get => _scale;
        set
        {
            float clamped = Mathf.Max(0.0001f, value);
            if (!Mathf.Approximately(_scale, clamped))
            {
                _scale = clamped;
                UpdateMesh();
            }
        }
    }
    
    public bool UpdateCollider
    {
        get => _updateCollider;
        set => _updateCollider = value;
    }
    
    public bool ConvexCollider
    {
        get => _convexCollider;
        set => _convexCollider = value;
    }
    
    //=========================================================================
    // Unity lifecycle methods
    
    void Awake()
    {
        InitializeComponents();
    }
    
    void Start()
    {
        SubscribeToVoxelDefinitionEvents();
        UpdateMesh();
    }
    
    void Update()
    {
        // Check for external changes to frame or palette
        if (_frame != _previousFrame)
        {
            SelectFrame(_frame);
        }
        
        if (_paletteName != _previousPaletteName)
        {
            SelectPalette(_paletteName);
        }
        
        // Check for Rigidbody changes if collider updates are enabled
        if (_updateCollider && _meshCollider != null)
        {
            bool currentHasRigidbody = GetComponent<Rigidbody>() != null;
            if (currentHasRigidbody != _previousHasRigidbody)
            {
                UpdateMeshCollider();
                _previousHasRigidbody = currentHasRigidbody;
            }
        }

        // Detect scale changes
        if (!Mathf.Approximately(_scale, _previousScale))
        {
            UpdateMesh();
        }
    }
    
    void OnValidate()
    {
        InitializeComponents();
        SubscribeToVoxelDefinitionEvents();
        
        // Defer mesh update to avoid SendMessage restrictions during OnValidate
        if (Application.isPlaying)
        {
            UpdateMesh();
        }
        else
        {
            // In editor, schedule the update for the next editor update
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null) // Check if object still exists
                {
                    UpdateMesh();
                }
            };
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from VoxelDefinition events
        UnsubscribeFromVoxelDefinitionEvents();
    }
    
    //=========================================================================
    // Public methods
    
    //-------------------------------------------------------------------------
    // Frame & Palette Selection
    
    /// <summary>
    /// Selects a specific frame and updates the mesh.
    /// </summary>
    /// <param name="frame">Frame index to select</param>
    public void SelectFrame(int frame)
    {
        if (voxelDefinition != null)
        {
            int frameCount = voxelDefinition.GetFrameCount();
            _frame = Mathf.Clamp(frame, 0, Mathf.Max(0, frameCount - 1));
        }
        else
        {
            _frame = Mathf.Max(0, frame);
        }
        
        UpdateMesh();
    }
    
    /// <summary>
    /// Selects a specific palette and updates the mesh.
    /// </summary>
    /// <param name="paletteName">Name of the palette to select</param>
    public void SelectPalette(string paletteName)
    {
        _paletteName = string.IsNullOrEmpty(paletteName) ? "default" : paletteName;
        UpdateMesh();
    }

    // Custom palette creation removed. Use VoxelDefinition.RegisterPalette(Texture2D).

    //-------------------------------------------------------------------------
    // Information & Queries
    
    /// <summary>
    /// Gets the current frame index.
    /// </summary>
    public int GetCurrentFrame() => _frame;
    
    /// <summary>
    /// Gets the current palette name.
    /// </summary>
    public string GetCurrentPalette() => _paletteName;
    
    /// <summary>
    /// Gets the VoxelDefinition reference used by this selector.
    /// </summary>
    /// <returns>The VoxelDefinition component, or null if not assigned</returns>
    public VoxelDefinition GetVoxelDefinition()
    {
        return voxelDefinition;
    }
    
    //=========================================================================
    // Private methods
    
    //-------------------------------------------------------------------------
    // Initialization
    
    private void InitializeComponents()
    {
        _meshFilter = _meshFilter ?? GetComponent<MeshFilter>();
        _meshRenderer = _meshRenderer ?? GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>(); // Optional component
        
        // Assign default shader if no material is set
        AssignDefaultMaterial();
    }
    
    /// <summary>
    /// Updates the mesh based on current frame and palette settings.
    /// This is called internally when frame or palette changes.
    /// </summary>
    private void UpdateMesh()
    {
        if (voxelDefinition == null)
        {
            Debug.LogWarning("VoxelMeshSelector: No VoxelDefinition assigned");
            ClearMesh();
            return;
        }
        
        var mesh = voxelDefinition.GetMesh(_frame, _paletteName, _scale);
        
        if (mesh != null)
        {
            _meshFilter.sharedMesh = mesh;
            
            // Update collider if enabled
            if (_updateCollider)
            {
                UpdateMeshCollider();
            }
        }
        else
        {
            Debug.LogWarning($"VoxelMeshSelector: No mesh found for frame {_frame} and palette '{_paletteName}'");
            ClearMesh();
        }
        
        // Update tracking variables
        _previousFrame = _frame;
        _previousPaletteName = _paletteName;
        _previousScale = _scale;
    }
    
    private void ClearMesh()
    {
        if (_meshFilter != null)
            _meshFilter.sharedMesh = null;
        
        if (_updateCollider && _meshCollider != null)
            _meshCollider.sharedMesh = null;
    }
    
    private void UpdateMeshCollider()
    {
        if (_meshCollider == null)
        {
            _meshCollider = GetComponent<MeshCollider>();
            if (_meshCollider == null)
            {
                _meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _meshFilter.sharedMesh;

        // Set convex property based on settings and Rigidbody presence
        bool hasRigidbody = GetComponent<Rigidbody>() != null;
        _meshCollider.convex = _convexCollider || hasRigidbody;
        
        // Update tracking variable
        _previousHasRigidbody = hasRigidbody;
    }
    
    private void AssignDefaultMaterial()
    {
        if (_meshRenderer == null || _meshRenderer.sharedMaterial != null) return;
        
        // Use proper URP vertex color shader based on Unity documentation
        Shader voxelShader = Shader.Find("Custom/VoxelURPVertexColor");
        if (voxelShader != null)
        {
            Material material = new Material(voxelShader);
            material.name = $"VoxelMaterial_{gameObject.name}";
            _meshRenderer.sharedMaterial = material;
        }
        else
        {
            Debug.LogError($"VoxelMeshSelector: Could not find 'Custom/VoxelURPVertexColor' shader for {gameObject.name}. Make sure the shader file exists.");
        }
    }

    //-------------------------------------------------------------------------
    // Event Management
    
    private void SubscribeToVoxelDefinitionEvents()
    {
        // Unsubscribe first to avoid duplicate subscriptions
        UnsubscribeFromVoxelDefinitionEvents();
        
        if (voxelDefinition != null)
        {
            voxelDefinition.OnCacheReinitialized += OnVoxelDefinitionCacheReinitialized;
        }
    }
    
    private void UnsubscribeFromVoxelDefinitionEvents()
    {
        if (voxelDefinition != null)
        {
            voxelDefinition.OnCacheReinitialized -= OnVoxelDefinitionCacheReinitialized;
        }
    }
    
    private void OnVoxelDefinitionCacheReinitialized()
    {
        // VoxelDefinition has reinitialized its cache, refresh our mesh
        UpdateMesh();
    }
}
