using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZxhMuseum
{
    // Mesh controls, not Canvas menus. Each cap is an ordinary Museum-style XRI target.
    public sealed class PhysicalBoatControls : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Material> labelMaterials = new List<Material>();
        private void OnEnable() => Font.textureRebuilt += RefreshFont;
        private void OnDisable() => Font.textureRebuilt -= RefreshFont;
        private void RefreshFont(Font font) { foreach(var material in labelMaterials) if(material!=null)material.mainTexture=font.material.mainTexture; }
        public GameObject Part(string name, PrimitiveType shape, Vector3 position, Vector3 size, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(shape); go.name = name;
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            var material = new Material(Shader.Find("Standard")) { color = color };
            materials.Add(material); go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
        public void Label(Transform parent, string value, Vector3 position, float size = .025f, float maxWidth=.26f, float maxHeight=.11f)
        {
            var go = new GameObject("Label " + value); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var text = go.AddComponent<TextMesh>(); text.text = value; text.fontSize = 48; text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font.RequestCharactersInTexture(value,text.fontSize);
            var material=new Material(Resources.Load<Shader>("Shaders/PhysicalLabel")){mainTexture=text.font.material.mainTexture};
            materials.Add(material);labelMaterials.Add(material);
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            var bounds=renderer.localBounds.size;
            go.transform.localScale=Vector3.one*Mathf.Min(1,maxWidth/Mathf.Max(.001f,bounds.x),maxHeight/Mathf.Max(.001f,bounds.y));
        }
        public PhysicalPushButton Button(string name, Vector3 position, Quaternion rotation, string label, Action action)
        {
            var root = new GameObject(name); root.transform.SetParent(transform, false);
            root.transform.localPosition = position; root.transform.localRotation = rotation;
            Part("Metal button housing", PrimitiveType.Cube, Vector3.zero, new Vector3(.30f, .32f, .06f), new Color(.18f,.22f,.24f), root.transform);
            var cap = Part(name + " cap", PrimitiveType.Cylinder, new Vector3(0,-.055f,-.06f), new Vector3(.13f,.025f,.13f), new Color(.65f,.2f,.12f), root.transform);
            cap.transform.localRotation = Quaternion.Euler(90,0,0);
            Destroy(cap.GetComponent<Collider>());
            cap.AddComponent<BoxCollider>().size = new Vector3(1,2,1);
            Label(root.transform, label, new Vector3(0,.09f,-.035f), .022f);
            var button = cap.AddComponent<PhysicalPushButton>(); button.Initialize(action);
            BoatTriggerAction.Bind(cap, button.Actuate);
            cap.AddComponent<DesktopObjectClick>().Selected = button.Actuate;
            return button;
        }
        private void OnDestroy() { foreach (var material in materials) if (material != null) Destroy(material); }
    }

    public sealed class PhysicalPushButton : MonoBehaviour
    {
        private Action action;
        private Vector3 home;
        private float pressedUntil;
        public void Initialize(Action callback) { action=callback; home=transform.localPosition; }
        public void Actuate() { pressedUntil=Time.unscaledTime+.15f; action?.Invoke(); }
        public void SetIndicator(bool on)
        { GetComponent<Renderer>().sharedMaterial.color = on ? new Color(.12f,.65f,.27f) : new Color(.65f,.2f,.12f); }
        private void Update() => transform.localPosition = Vector3.MoveTowards(transform.localPosition,
            home+(Time.unscaledTime<pressedUntil?Vector3.forward*.014f:Vector3.zero),Time.unscaledDeltaTime*.16f);
    }
}
