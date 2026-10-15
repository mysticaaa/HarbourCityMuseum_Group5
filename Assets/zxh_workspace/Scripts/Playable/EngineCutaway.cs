using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZxhMuseum
{
    // Generic piston/crank teaching diagram, not a reconstruction or repair procedure.
    public sealed class EngineCutaway : MonoBehaviour
    {
        private GameObject cover;
        private Transform piston, rod, crank;
        private WorldPanel panel;
        private Material metal, casing;
        private bool running;
        private float angle;
        public bool IsOpen => cover != null && !cover.activeSelf;
        public bool Running => running;
        private GameObject Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name;
            part.transform.SetParent(transform, false); part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }
        private void Start()
        {
            metal = new Material(Shader.Find("Standard")) { color = new Color(0.65f, 0.68f, 0.7f) };
            casing = new Material(Shader.Find("Standard")) { color = new Color(0.15f, 0.3f, 0.3f) };
            Part("Engine base", PrimitiveType.Cube, new Vector3(0, -0.25f, 0), new Vector3(0.7f, 0.12f, 0.6f), casing);
            cover = Part("Removable diagram cover", PrimitiveType.Cube, Vector3.zero, new Vector3(0.6f, 0.55f, 0.5f), casing);
            piston = Part("Piston", PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(0.2f, 0.045f, 0.2f), metal).transform;
            rod = Part("Connecting rod", PrimitiveType.Cube, Vector3.zero, new Vector3(0.035f, 0.25f, 0.04f), metal).transform;
            crank = Part("Crankshaft", PrimitiveType.Cylinder, new Vector3(0, -0.18f, 0), new Vector3(0.1f, 0.2f, 0.1f), metal).transform;
            crank.localRotation = Quaternion.Euler(90, 0, 0);
            panel = WorldPanel.Create(transform, "Engine teaching cutaway", new Vector3(-0.55f, 0.8f, 0), Quaternion.Euler(0, 90, 0), 650);
            panel.Status.text = "A generic piston/rod/crank diagram.\nNot this vessel's real engine or a maintenance procedure.";
            panel.Button("Show / hide cover", ToggleCover);
            panel.Button("Play / pause mechanism", () => { if (!IsOpen) ToggleCover(); running = !running; });
            panel.Button("Reset engine diagram", ResetDiagram);
            foreach (var part in new[] { cover.transform, piston, rod, crank })
            {
                string partName = part.name;
                System.Action inspect = () =>
                {
                    panel.Status.text = partName + "\nPiston motion is linked to the crank by the connecting rod.\nAnimation is a simplified mechanical relationship.";
                    if (part == cover.transform) ToggleCover();
                };
                BoatTriggerAction.Bind(part.gameObject, inspect);
                part.gameObject.AddComponent<DesktopObjectClick>().Selected = inspect;
                var record = part.gameObject.AddComponent<InteractionRecord>();
                record.assetId = "zxh.engine." + partName.Replace(" ", "-").ToLowerInvariant(); record.category = "Teaching cutaway";
                part.gameObject.AddComponent<ExhibitHighlight>();
            }
        }
        public void ToggleCover() { if (cover != null) cover.SetActive(!cover.activeSelf); }
        public void ResetDiagram() { angle = 0; running = false; if (cover != null) cover.SetActive(true); }
        private void Update()
        {
            if (piston == null) return;
            if (running) angle = Mathf.Repeat(angle + Time.deltaTime * 80, 360);
            float y = 0.1f + Mathf.Cos(angle * Mathf.Deg2Rad) * 0.07f;
            piston.localPosition = Vector3.up * y;
            rod.localPosition = Vector3.up * (y - 0.1f);
            rod.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(angle * Mathf.Deg2Rad) * 15);
            crank.localRotation = Quaternion.Euler(90, angle, 0);
        }
        private void OnDestroy() { if (metal != null) Destroy(metal); if (casing != null) Destroy(casing); }
    }
}
