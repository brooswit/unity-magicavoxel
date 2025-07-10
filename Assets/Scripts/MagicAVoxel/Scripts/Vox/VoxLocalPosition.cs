using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class VoxLocalPosition : MonoBehaviour
{
    // Public attributes for position in voxel coordinates
    public float x = 0f;
    public float y = 0f;
    public float z = 0f;

    // Reference to the VoxelModel used for scaling
    public VoxelModel referenceVoxelModel;

    // Keep track of the last known values to detect changes
    private float lastX;
    private float lastY;
    private float lastZ;
    private float lastVoxelsPerUnit;

    // Keep track of the last known reference VoxelModel
    private VoxelModel lastReferenceVoxelModel;

    // Flag to check if the initial position has been set
    private bool initialPositionSet = false;

    private void OnEnable()
    {
        // If referenceVoxelModel is not set, try to find a VoxelModel
        if (referenceVoxelModel == null)
        {
            FindReferenceVoxelModel();
        }

        // Initialize the voxel position after finding the reference model
        if (!initialPositionSet)
        {
            if (referenceVoxelModel != null)
            {
                InitializeVoxelPosition();
                initialPositionSet = true;
            }
            else
            {
                Debug.LogError("VoxLocalPosition: No reference VoxelModel found on self, siblings, parents, or children.");
            }
        }
    }

    private void OnValidate()
    {
        // If referenceVoxelModel is not set, try to find a VoxelModel
        if (referenceVoxelModel == null)
        {
            FindReferenceVoxelModel();
        }

        // Reinitialize if the referenceVoxelModel has changed
        if (referenceVoxelModel != lastReferenceVoxelModel)
        {
            initialPositionSet = false;
            lastReferenceVoxelModel = referenceVoxelModel;
        }

        // Initialize the voxel position after finding the reference model
        if (!initialPositionSet)
        {
            if (referenceVoxelModel != null)
            {
                InitializeVoxelPosition();
                initialPositionSet = true;
            }
            else
            {
                Debug.LogError("VoxLocalPosition: No reference VoxelModel found on self, siblings, parents, or children.");
            }
        }
        else
        {
            UpdatePosition();
        }
    }

    private void Update()
    {
        // In play mode, check for changes
        if (x != lastX || y != lastY || z != lastZ ||
            (referenceVoxelModel != null && referenceVoxelModel.voxelsPerUnit != lastVoxelsPerUnit))
        {
            UpdatePosition();
        }
    }

    private void InitializeVoxelPosition()
    {
        if (referenceVoxelModel == null)
        {
            Debug.LogError("VoxLocalPosition: No reference VoxelModel assigned.");
            return;
        }

        // Get the scale factor from the voxelsPerUnit value
        float scale = 1f / referenceVoxelModel.voxelsPerUnit;

        // Convert current local position to voxel coordinates
        Vector3 localPos = transform.localPosition;
        x = localPos.x / scale;
        y = localPos.y / scale;
        z = localPos.z / scale;

        // Update last known values
        lastX = x;
        lastY = y;
        lastZ = z;
        lastVoxelsPerUnit = referenceVoxelModel.voxelsPerUnit;
    }

    private void UpdatePosition()
    {
        if (referenceVoxelModel == null)
        {
            Debug.LogError("VoxLocalPosition: No reference VoxelModel found on self, siblings, parents, or children.");
            return;
        }

        // Get the scale factor from the voxelsPerUnit value
        float scale = 1f / referenceVoxelModel.voxelsPerUnit;

        // Calculate the new local position
        Vector3 newPosition = new Vector3(x * scale, y * scale, z * scale);

        // Update the local position of the GameObject
        transform.localPosition = newPosition;

        // Update last known values
        lastX = x;
        lastY = y;
        lastZ = z;
        lastVoxelsPerUnit = referenceVoxelModel.voxelsPerUnit;
    }

    private void FindReferenceVoxelModel()
    {
        // First, check if this GameObject has a VoxelModel
        VoxelModel selfVoxelModel = GetComponent<VoxelModel>();
        if (selfVoxelModel != null)
        {
            referenceVoxelModel = selfVoxelModel;
            return;
        }

        // Then, try to find a sibling VoxelModel
        if (FindSiblingVoxelModel()) return;

        // If not found, try to find a parent VoxelModel
        if (FindParentVoxelModel()) return;

        // If still not found, try to find the closest child VoxelModel
        if (FindChildVoxelModel()) return;
    }

    private bool FindSiblingVoxelModel()
    {
        if (transform.parent != null)
        {
            // Get the parent transform
            Transform parent = transform.parent;

            // Loop through all the children of the parent (siblings)
            foreach (Transform sibling in parent)
            {
                if (sibling == transform) continue; // Skip self

                VoxelModel siblingVoxelModel = sibling.GetComponent<VoxelModel>();
                if (siblingVoxelModel != null)
                {
                    referenceVoxelModel = siblingVoxelModel;
                    return true;
                }
            }
        }
        return false;
    }

    private bool FindParentVoxelModel()
    {
        Transform currentParent = transform.parent;
        while (currentParent != null)
        {
            VoxelModel parentVoxelModel = currentParent.GetComponent<VoxelModel>();
            if (parentVoxelModel != null)
            {
                referenceVoxelModel = parentVoxelModel;
                return true;
            }
            currentParent = currentParent.parent;
        }
        return false;
    }

    private bool FindChildVoxelModel()
    {
        VoxelModel childVoxelModel = FindClosestChildVoxelModel(transform);
        if (childVoxelModel != null)
        {
            referenceVoxelModel = childVoxelModel;
            return true;
        }
        return false;
    }

    private VoxelModel FindClosestChildVoxelModel(Transform parent)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            if (current != parent)
            {
                VoxelModel voxelModel = current.GetComponent<VoxelModel>();
                if (voxelModel != null)
                {
                    return voxelModel;
                }
            }

            foreach (Transform child in current)
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }
}
