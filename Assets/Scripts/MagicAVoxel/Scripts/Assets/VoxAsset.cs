using UnityEngine;
using System;

[System.Serializable]
public class VoxAsset : ScriptableObject
{
    public byte[] rawData;

    // Fired whenever a .vox asset is (re)imported
    public static event Action<VoxAsset> OnAnyReimported;

    internal static void RaiseReimported(VoxAsset asset)
    {
        OnAnyReimported?.Invoke(asset);
    }
}
