using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(VoxelModel))]
public class VoxScaleSync : MonoBehaviour
{
    // Reference to the source VoxelModel whose scale we want to sync
    public VoxelModel sourceVoxelModel;

    // Reference to the local VoxelModel component
    private VoxelModel localVoxelModel;

    // To keep track of the last known voxelsPerUnit value
    private float lastSourceVoxelsPerUnit = -1f;

    private void OnEnable()
    {
        // Get the local VoxelModel component
        localVoxelModel = GetComponent<VoxelModel>();

        if (localVoxelModel == null)
        {
            Debug.LogError("VoxScaleSync: No VoxelModel component found on this GameObject.");
        }

        // Initial sync
        SyncScale();
    }

    private void Update()
    {
        // Continuously sync during play mode
        SyncScale();
    }

    private void OnValidate()
    {
        // Sync in edit mode when values change in the Inspector
        SyncScale();
    }

    private void SyncScale()
    {
        if (sourceVoxelModel == null || localVoxelModel == null)
            return;

        // Check if the voxelsPerUnit value has changed
        if (lastSourceVoxelsPerUnit != sourceVoxelModel.voxelsPerUnit)
        {
            lastSourceVoxelsPerUnit = sourceVoxelModel.voxelsPerUnit;

            // Update the local VoxelModel's voxelsPerUnit
            localVoxelModel.voxelsPerUnit = sourceVoxelModel.voxelsPerUnit;

            // Rebuild the mesh to apply the new scale
            localVoxelModel.LoadVoxelModel();

            // Mark the local VoxelModel as dirty to ensure changes are saved (editor only)
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(localVoxelModel);
#endif
        }
    }
}
