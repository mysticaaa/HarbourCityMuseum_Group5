using UnityEngine;

namespace ZzxWorkspace
{
    public class CabinLayoutView : MonoBehaviour
    {
        [System.Serializable]
        public class Zone
        {
            public string label;
            [TextArea] public string description;
            public Renderer[] renderers;
        }
        public Zone[] zones;
        private int selected = -1;
        private MaterialPropertyBlock[] previous;
        public void SelectZone(int index)
        {
            ClearHighlight();
            if (zones == null || index < 0 || index >= zones.Length) return;
            selected = index;
            var renderers = zones[index].renderers;
            if (renderers == null) return;
            previous = new MaterialPropertyBlock[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                previous[i] = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(previous[i]);
                var block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                block.SetColor("_Color", Color.yellow);
                block.SetColor("_BaseColor", Color.yellow);
                renderers[i].SetPropertyBlock(block);
            }
        }
        public void ClearHighlight()
        {
            if (selected >= 0 && zones != null && selected < zones.Length && previous != null)
                for (int i = 0; i < previous.Length; i++)
                    if (zones[selected].renderers[i] != null) zones[selected].renderers[i].SetPropertyBlock(previous[i]);
            selected = -1;
            previous = null;
        }
        public void DrawControls()
        {
            GUILayout.Label("Layout: conceptual zones, NOT a historical reconstruction.");
            if (zones != null)
                for (int i = 0; i < zones.Length; i++)
                    if (GUILayout.Button(zones[i].label)) SelectZone(i);
            if (selected >= 0) GUILayout.Label(zones[selected].description);
            if (GUILayout.Button("Clear zone highlight")) ClearHighlight();
        }
        private void OnDisable() { ClearHighlight(); }
    }
}
