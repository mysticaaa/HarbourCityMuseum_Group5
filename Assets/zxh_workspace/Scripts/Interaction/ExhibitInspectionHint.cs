using UnityEngine;

public class ExhibitInspectionHint : MonoBehaviour
{
    public static Bounds GetBounds(Transform model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(model.position, Vector3.zero);
        bool first = true;
        foreach (Renderer renderer in renderers)
        {
            if (renderer is MeshRenderer && renderer.GetComponent<TextMesh>() == null)
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
        }
        return bounds;
    }

    public static void Create(Transform model, string message)
    {
        Bounds bounds = GetBounds(model);
        var label = new GameObject("ZXH_ExhibitHint");
        // The label stays with the cabinet, not the grabbed or enlarged model.
        label.transform.position = bounds.center + Vector3.down * (bounds.extents.y + 0.06f);
        if (model.parent != null) label.transform.SetParent(model.parent.parent, true);
        var text = label.AddComponent<TextMesh>();
        text.text = message;
        text.fontSize = 48;
        text.characterSize = 0.003f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = Color.white;
        label.AddComponent<ExhibitInspectionHint>();
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}
