using UnityEngine;

/// <summary>
/// Zero-setup entry point kept on SampleScene's Main Camera.
/// In Play Mode it creates the complete venue when a baked venue is not present.
/// Use Tools/Las Vegas Sphere/Build Editable Scene to create the editable scene.
/// </summary>
public sealed class NewBehaviourScript : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectOfType<AudioVisualSyncExperiment>() == null)
        {
            SphereVenueFactory.Build(transform.GetComponent<Camera>());
        }
    }
}
