using UnityEngine;

/// <summary>Changes only this scene's room shell; exhibits and plinths keep their size.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class MuseumRoomLength : MonoBehaviour
{
    [SerializeField] private Transform roomShell;
    [SerializeField, Min(1f)] private float referenceLength = 16.166f;
    [SerializeField] private Vector3 referenceScale;
    [SerializeField, Min(1f), Tooltip("Room length in metres. Does not resize the exhibits.")]
    private float lengthMetres = 20.2f;

    public void Configure(Transform shell, float originalLength, float desiredLength)
    {
        roomShell = shell;
        referenceScale = shell.localScale;
        referenceLength = originalLength;
        lengthMetres = desiredLength;
        ApplyLength();
    }

    private void OnEnable() => ApplyLength();
    private void OnValidate() => ApplyLength();

    private void ApplyLength()
    {
        if (!roomShell || referenceLength <= 0f || referenceScale.sqrMagnitude == 0f) return;
        // In the imported room, local Y is the room's horizontal length axis.
        Vector3 scale = referenceScale;
        scale.y *= Mathf.Max(1f, lengthMetres) / referenceLength;
        if ((roomShell.localScale - scale).sqrMagnitude > 0.000001f)
        {
            roomShell.localScale = scale;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(roomShell);
#endif
        }
    }
}
