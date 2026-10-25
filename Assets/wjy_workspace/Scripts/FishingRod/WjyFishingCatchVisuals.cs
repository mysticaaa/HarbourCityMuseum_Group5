using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wjy.Museum
{
    // All new model geometry is generated locally; there are no external asset dependencies.
    public sealed partial class WjyFishingCatchVisuals
    {
        public Transform Float { get; private set; }
        public Transform Fish { get; private set; }
        public Transform RodMotion { get; private set; }
        public GameObject Panel { get; private set; }
        public string Message => message ? message.text : "";
        private Text message;
        private readonly List<Object> owned = new List<Object>();
        private Vector3 rodHomePosition;
        private Quaternion rodHomeRotation;

        public void Create(Transform rod, Transform hook, Transform station, Transform tip, Transform[] guides)
        {
            var red = Material("Float red", new Color(.82f,.07f,.045f));
            var ivory = Material("Float ivory", new Color(.96f,.91f,.75f));
            var dark = Material("Float stem", new Color(.08f,.10f,.08f));
            Float = Child("WJY_WaterOnlyFloat", station, Vector3.zero);
            Shape("Cork body", PrimitiveType.Sphere, Float, new Vector3(0,.012f,0), new Vector3(.054f,.10f,.054f), ivory);
            Shape("Red cap", PrimitiveType.Sphere, Float, new Vector3(0,.047f,0), new Vector3(.048f,.045f,.048f), red);
            Shape("Upright stem", PrimitiveType.Cylinder, Float, new Vector3(0,.11f,0), new Vector3(.008f,.065f,.008f), dark);
            Shape("Red tip", PrimitiveType.Cylinder, Float, new Vector3(0,.165f,0), new Vector3(.011f,.025f,.011f), red);
            Shape("Lower pin", PrimitiveType.Cylinder, Float, new Vector3(0,-.075f,0), new Vector3(.007f,.04f,.007f), dark);
            Float.gameObject.SetActive(false);

            var silver = Material("Fish silver", new Color(.42f,.68f,.67f), .25f);
            var back = Material("Fish darker back", new Color(.10f,.32f,.32f), .15f);
            var fin = Material("Fish fins", new Color(.18f,.44f,.43f));
            var eye = Material("Fish eyes", new Color(.015f,.02f,.02f));
            Fish = Child("WJY_CaughtFish_MouthAtHook", hook, Vector3.zero);
            var fishModel = Child("Fish", Fish, Vector3.zero);
            // The mouth is at the hook; the fish hangs vertically below it when reeled in.
            Shape("Fish body", PrimitiveType.Sphere, Fish, new Vector3(0,-.125f,0), new Vector3(.092f,.235f,.052f), silver);
            Shape("Fish back", PrimitiveType.Sphere, Fish, new Vector3(.022f,-.13f,0), new Vector3(.058f,.20f,.045f), back);
            Shape("Mouth", PrimitiveType.Sphere, Fish, new Vector3(0,-.012f,-.007f), new Vector3(.024f,.018f,.022f), ivory);
            foreach (float side in new[] {-1f,1f})
            {
                Shape("Eye white", PrimitiveType.Sphere, Fish, new Vector3(-.014f,-.055f,side*.024f), Vector3.one*.017f, ivory);
                Shape("Eye pupil", PrimitiveType.Sphere, Fish, new Vector3(-.014f,-.055f,side*.031f), Vector3.one*.009f, eye);
            }
            Triangle("Forked tail upper", Fish, new Vector3(0,-.225f,0), new Vector3(-.065f,-.315f,0), new Vector3(0,-.285f,0), fin);
            Triangle("Forked tail lower", Fish, new Vector3(0,-.225f,0), new Vector3(0,-.285f,0), new Vector3(.065f,-.315f,0), fin);
            Triangle("Dorsal fin", Fish, new Vector3(.035f,-.075f,0), new Vector3(.095f,-.15f,0), new Vector3(.025f,-.20f,0), fin);
            Triangle("Side fin", Fish, new Vector3(-.025f,-.11f,-.02f), new Vector3(-.08f,-.18f,-.045f), new Vector3(-.02f,-.165f,-.02f), fin);
            CreateCatchCatalog(fishModel);
            Fish.gameObject.SetActive(false);

            RodMotion = Child("WJY_BiteRodMotion", rod, Vector3.zero);
            var model = rod.Find("BambooRod_Reel_Guides");
            if (model) model.SetParent(RodMotion, true);
            if (tip) tip.SetParent(RodMotion, true);
            foreach (var guide in guides) if (guide) guide.SetParent(RodMotion, true);
            rodHomePosition = RodMotion.localPosition; rodHomeRotation = RodMotion.localRotation;

            Panel = new GameObject("WJY_FishingResultPanel", typeof(RectTransform), typeof(Canvas), typeof(Image));
            Panel.transform.SetParent(station, false); Panel.transform.localScale = Vector3.one*.0012f;
            Panel.layer = Physics.IgnoreRaycastLayer;
            var rect = (RectTransform)Panel.transform; rect.sizeDelta = new Vector2(780,190);
            var canvas = Panel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var image = Panel.GetComponent<Image>(); image.color = new Color(.025f,.075f,.085f,.94f); image.raycastTarget = false;
            var label = new GameObject("Message", typeof(RectTransform), typeof(Text)); label.transform.SetParent(Panel.transform,false);
            var labelRect = (RectTransform)label.transform; labelRect.anchorMin=Vector2.zero; labelRect.anchorMax=Vector2.one;
            labelRect.offsetMin=new Vector2(24,16); labelRect.offsetMax=new Vector2(-24,-16);
            message = label.GetComponent<Text>(); message.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            message.fontSize=34; message.alignment=TextAnchor.MiddleCenter; message.color=Color.white;
            message.horizontalOverflow=HorizontalWrapMode.Wrap; message.verticalOverflow=VerticalWrapMode.Truncate; message.raycastTarget=false;
            Panel.SetActive(false);
        }

        public void ShowMessage(string value, bool urgent = false)
        {
            if (!Panel) return;
            message.text=value;
            message.color = urgent ? new Color(1f,.12f,.12f) : Color.white;
            message.fontStyle = urgent ? FontStyle.Bold : FontStyle.Normal;
            message.fontSize = urgent ? 40 : 34;
            Panel.SetActive(!string.IsNullOrEmpty(value));
        }

        public void Pose(float time, bool biting, Vector3 waterPosition, Camera camera)
        {
            if (RodMotion)
            {
                RodMotion.localPosition=rodHomePosition+(biting?new Vector3(Mathf.Sin(time*39)*.003f,Mathf.Sin(time*47)*.004f,0):Vector3.zero);
                RodMotion.localRotation=rodHomeRotation*(biting?Quaternion.Euler(Mathf.Sin(time*36)*.8f,0,Mathf.Sin(time*43)*.9f):Quaternion.identity);
            }
            if (Fish && Fish.gameObject.activeSelf) Fish.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*7)*5);
            if (Panel)
            {
                Panel.transform.position=waterPosition+Vector3.up*.65f;
                if (camera)
                {
                    Vector3 away=Panel.transform.position-camera.transform.position;
                    if (away.sqrMagnitude>.001f) Panel.transform.rotation=Quaternion.LookRotation(away,Vector3.up);
                }
            }
        }

        private Material Material(string name, Color color, float metallic=0)
        {
            var shader=Shader.Find("Standard")??Shader.Find("Unlit/Color");
            var mat=new Material(shader){name="WJY "+name,color=color}; mat.SetFloat("_Metallic",metallic); owned.Add(mat); return mat;
        }
        private static Transform Child(string name, Transform parent, Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
        private static GameObject Shape(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var obj=GameObject.CreatePrimitive(primitive);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;
            var collider=obj.GetComponent<Collider>();collider.enabled=false;
            if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            obj.GetComponent<Renderer>().sharedMaterial=material;
            return obj;
        }
        private void Triangle(string name, Transform parent, Vector3 a, Vector3 b, Vector3 c, Material material)
        {
            var obj=Child(name,parent,Vector3.zero).gameObject;
            // Separate vertices keep front/back normals from cancelling each other.
            var mesh=new Mesh{name="WJY "+name};mesh.vertices=new[]{a,b,c,a,c,b};mesh.triangles=new[]{0,1,2,3,4,5};mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        public void Dispose()
        {
            foreach(var value in owned)if(value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
            owned.Clear();
        }
    }
}
