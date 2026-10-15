using UnityEngine;

namespace ZxhMuseum
{
    public sealed class InspectionReplica : MonoBehaviour
    {
        public static InspectionReplica Create(Transform source, Transform parent, Vector3 position, float diameter = 0.4f)
        {
            Bounds bounds = ExhibitInspectionHint.GetBounds(source);
            float factor = diameter / Mathf.Max(0.001f, bounds.size.magnitude);
            var root = new GameObject("ZXH_VisualInspectionReplica");
            root.transform.SetParent(parent, false); root.transform.position = position;
            foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (filter.sharedMesh == null || renderer == null) continue;
                var item = new GameObject(filter.name + "_visual");
                item.transform.SetParent(root.transform, false);
                item.transform.localPosition = (filter.transform.position - bounds.center) * factor;
                item.transform.localRotation = filter.transform.rotation;
                item.transform.localScale = filter.transform.lossyScale * factor;
                item.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                item.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            }
            return root.AddComponent<InspectionReplica>();
        }
        public void Turn(float degrees) => transform.Rotate(Vector3.up, degrees, Space.World);
    }
}
