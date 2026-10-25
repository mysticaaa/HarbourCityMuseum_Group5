using System.Collections.Generic;
using UnityEngine;

namespace Wjy.Museum
{
    public enum WjyCatchKind { Fish, Turtle, Crab, Trash, Lobster, Seal, Whale }

    public sealed partial class WjyFishingCatchVisuals
    {
        private readonly List<Transform> catchModels = new List<Transform>();
        private static readonly string[] Labels = { "a fish", "a turtle", "a crab", "a Coca-Cola glass bottle", "a lobster", "a seal", "a miniature whale" };
        public WjyCatchKind CatchKind { get; private set; }
        public string CatchLabel => Labels[(int)CatchKind];
        public IReadOnlyList<Transform> CatchModels => catchModels;

        public void SelectCatch(WjyCatchKind kind)
        {
            CatchKind = kind;
            for (int i = 0; i < catchModels.Count; i++) catchModels[i].gameObject.SetActive(i == (int)kind);
        }

        private void CreateCatchCatalog(Transform originalFish)
        {
            // Keep the common hook anchor while allowing exactly one model to be shown.
            for (int i = Fish.childCount - 1; i >= 0; i--)
            {
                var child = Fish.GetChild(i);
                if (child != originalFish) child.SetParent(originalFish, false);
            }
            var names = new[] { "Fish", "Turtle", "Crab", "Trash", "Lobster", "Seal", "Whale" };
            var prefabs = new GameObject[names.Length];
            bool importedCatalogAvailable = true;
            for (int i = 0; i < names.Length; i++)
            {
                prefabs[i] = Resources.Load<GameObject>("WjyFishingCatches/" + names[i]);
                importedCatalogAvailable &= prefabs[i] != null;
            }
            if (importedCatalogAvailable)
            {
                originalFish.gameObject.SetActive(false);
                for (int i = 0; i < names.Length; i++)
                {
                    var model = Object.Instantiate(prefabs[i], Fish, false);
                    model.name = names[i];
                    catchModels.Add(model.transform);
                }
                SelectCatch(WjyCatchKind.Fish);
                return;
            }
            Debug.LogWarning("[WJY] Realistic catch prefabs are incomplete; using the procedural fallback catalog.");
            catchModels.Add(originalFish);
            var green = Material("Turtle skin", new Color(.29f,.49f,.19f));
            var shell = Material("Turtle shell", new Color(.22f,.29f,.09f));
            var scute = Material("Shell markings", new Color(.51f,.53f,.18f));
            var black = Material("Catch eyes", new Color(.012f,.02f,.025f));
            var white = Material("Catch ivory", new Color(.9f,.94f,.87f));
            var red = Material("Crab shell", new Color(.78f,.19f,.075f));
            var lobsterRed = Material("Lobster shell", new Color(.54f,.085f,.055f));
            var grey = Material("Seal grey", new Color(.45f,.51f,.54f));
            var blue = Material("Whale blue", new Color(.12f,.29f,.41f));
            var light = Material("Whale belly", new Color(.65f,.8f,.82f));

            var turtle = AddCatch("Turtle");
            Ellipsoid("Shell", turtle, new Vector3(0,-.13f,0), new Vector3(.19f,.09f,.24f), shell);
            Ellipsoid("Head", turtle, new Vector3(0,-.12f,-.145f), new Vector3(.07f,.055f,.085f), green);
            for (int i = -1; i <= 1; i++)
                Ellipsoid("Shell scute", turtle, new Vector3(0,-.083f,i*.063f), new Vector3(.075f,.012f,.052f), scute);
            foreach (float s in new[] {-1f,1f})
            {
                foreach (float z in new[] {-.07f,.075f})
                    Ellipsoid("Flipper", turtle, new Vector3(s*.105f,-.145f,z), new Vector3(.105f,.025f,.055f), green);
                Ellipsoid("Eye", turtle, new Vector3(s*.025f,-.105f,-.17f), Vector3.one*.014f, black);
            }
            Segment("Tail", turtle, new Vector3(0,-.14f,.1f), new Vector3(0,-.15f,.17f), .016f, green);
            Segment("Hook tether", turtle, Vector3.zero, new Vector3(0,-.08f,0), .003f, white);

            var crab = AddCatch("Crab");
            Ellipsoid("Body", crab, new Vector3(0,-.12f,0), new Vector3(.17f,.07f,.13f), red);
            foreach (float s in new[] {-1f,1f})
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = -.035f+i*.025f;
                    var knee = new Vector3(s*.13f,-.13f,z*1.8f);
                    Segment("Leg upper", crab, new Vector3(s*.05f,-.12f,z), knee,.012f,red);
                    Segment("Leg tip", crab,knee,new Vector3(s*.175f,-.17f,z*2.3f),.008f,red);
                }
                Pincer(crab,new Vector3(s*.115f,-.10f,-.105f),s,red);
                Segment("Claw arm",crab,new Vector3(s*.065f,-.12f,-.025f),new Vector3(s*.115f,-.10f,-.105f),.022f,red);
                Segment("Eye stalk",crab,new Vector3(s*.035f,-.10f,-.04f),new Vector3(s*.04f,-.065f,-.06f),.012f,red);
                Ellipsoid("Eye",crab,new Vector3(s*.04f,-.06f,-.06f),Vector3.one*.02f,black);
            }
            Segment("Hook tether", crab,Vector3.zero,new Vector3(0,-.085f,0),.003f,white);

            var trash = AddCatch("DiscardedPlasticBottle");
            var bottle = Material("Discarded green plastic",new Color(.17f,.43f,.28f),.1f);
            var label = Material("Bottle blue label",new Color(.13f,.49f,.73f));
            Ellipsoid("Bottle body",trash,new Vector3(0,-.15f,0),new Vector3(.095f,.20f,.095f),bottle);
            Shape("Label",PrimitiveType.Cylinder,trash,new Vector3(0,-.14f,0),new Vector3(.098f,.035f,.098f),label);
            Shape("Bottle neck",PrimitiveType.Cylinder,trash,new Vector3(0,-.035f,0),new Vector3(.035f,.028f,.035f),bottle);
            Shape("Bottle cap",PrimitiveType.Cylinder,trash,new Vector3(0,-.008f,0),new Vector3(.043f,.01f,.043f),white);
            for (int i=0;i<3;i++) Shape("Bottle ridges",PrimitiveType.Cylinder,trash,new Vector3(0,-.19f-i*.018f,0),new Vector3(.088f,.003f,.088f),bottle);

            var lobster = AddCatch("Lobster");
            Ellipsoid("Head and thorax",lobster,new Vector3(0,-.12f,-.03f),new Vector3(.085f,.075f,.13f),lobsterRed);
            for(int i=0;i<5;i++) Ellipsoid("Tail segment",lobster,new Vector3(0,-.13f,.04f+i*.027f),new Vector3(.075f-i*.006f,.055f,.04f),red);
            Ellipsoid("Tail fan",lobster,new Vector3(0,-.13f,.18f),new Vector3(.11f,.025f,.055f),red);
            foreach(float s in new[]{-1f,1f})
            {
                Segment("Large claw arm",lobster,new Vector3(s*.03f,-.12f,-.06f),new Vector3(s*.10f,-.12f,-.14f),.02f,lobsterRed);
                Pincer(lobster,new Vector3(s*.105f,-.12f,-.15f),s,red);
                Segment("Long antenna",lobster,new Vector3(s*.018f,-.085f,-.075f),new Vector3(s*.065f,-.06f,-.24f),.004f,lobsterRed);
                Ellipsoid("Eye",lobster,new Vector3(s*.025f,-.082f,-.08f),Vector3.one*.016f,black);
                for(int i=0;i<4;i++) Segment("Walking leg",lobster,new Vector3(s*.025f,-.14f,-.025f+i*.028f),new Vector3(s*.09f,-.17f,-.045f+i*.036f),.007f,red);
            }
            Segment("Hook tether",lobster,Vector3.zero,new Vector3(0,-.08f,0),.003f,white);

            var seal = AddCatch("Seal");
            Ellipsoid("Body",seal,new Vector3(0,-.18f,0),new Vector3(.12f,.24f,.105f),grey);
            Ellipsoid("Head",seal,new Vector3(0,-.065f,-.02f),new Vector3(.105f,.10f,.095f),grey);
            Ellipsoid("Muzzle",seal,new Vector3(0,-.08f,-.07f),new Vector3(.07f,.04f,.035f),white);
            Ellipsoid("Nose",seal,new Vector3(0,-.067f,-.09f),new Vector3(.025f,.018f,.016f),black);
            foreach(float s in new[]{-1f,1f})
            {
                Ellipsoid("Eye",seal,new Vector3(s*.031f,-.045f,-.06f),Vector3.one*.014f,black);
                var flipper=Shape("Front flipper",PrimitiveType.Sphere,seal,new Vector3(s*.075f,-.17f,0),new Vector3(.09f,.035f,.06f),grey);
                flipper.transform.localRotation=Quaternion.Euler(0,0,s*-28);
                Ellipsoid("Tail flipper",seal,new Vector3(s*.035f,-.30f,0),new Vector3(.075f,.06f,.025f),grey);
                for(int i=0;i<3;i++) Segment("Whisker",seal,new Vector3(s*.02f,-.08f,-.085f),new Vector3(s*.065f,-.072f-i*.008f,-.09f),.0018f,white);
            }

            var whale = AddCatch("MiniatureWhale");
            Ellipsoid("Whale body",whale,new Vector3(-.025f,-.14f,0),new Vector3(.33f,.15f,.14f),blue);
            Ellipsoid("Broad head",whale,new Vector3(-.14f,-.125f,0),new Vector3(.17f,.14f,.14f),blue);
            Ellipsoid("Pale belly",whale,new Vector3(-.06f,-.19f,0),new Vector3(.25f,.04f,.10f),light);
            Segment("Tail stock",whale,new Vector3(.095f,-.14f,0),new Vector3(.22f,-.14f,0),.045f,blue);
            foreach(float s in new[]{-1f,1f})
            {
                Ellipsoid("Tail fluke",whale,new Vector3(.235f,-.14f,s*.055f),new Vector3(.075f,.025f,.13f),blue);
                Ellipsoid("Pectoral flipper",whale,new Vector3(-.04f,-.19f,s*.085f),new Vector3(.12f,.025f,.09f),blue);
                Ellipsoid("Eye",whale,new Vector3(-.175f,-.13f,s*.064f),Vector3.one*.014f,black);
            }
            Triangle("Dorsal fin",whale,new Vector3(.035f,-.07f,0),new Vector3(.085f,-.01f,0),new Vector3(.11f,-.09f,0),blue);
            Segment("Blowhole",whale,new Vector3(-.12f,-.055f,-.015f),new Vector3(-.12f,-.055f,.015f),.007f,black);
            Segment("Hook tether",whale,Vector3.zero,new Vector3(-.025f,-.067f,0),.003f,white);
            SelectCatch(WjyCatchKind.Fish);
        }

        private Transform AddCatch(string name)
        { var model=Child(name,Fish,Vector3.zero);catchModels.Add(model);return model; }
        private static void Ellipsoid(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        { Shape(name,PrimitiveType.Sphere,parent,position,size,material); }
        private static GameObject Segment(string name,Transform parent,Vector3 start,Vector3 end,float diameter,Material material)
        {
            var obj=Shape(name,PrimitiveType.Cylinder,parent,(start+end)*.5f,new Vector3(diameter,(end-start).magnitude*.5f,diameter),material);
            obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(end-start).normalized);return obj;
        }
        private static void Pincer(Transform parent,Vector3 center,float side,Material material)
        {
            Ellipsoid("Claw palm",parent,center,new Vector3(.06f,.045f,.075f),material);
            Segment("Claw finger",parent,center+new Vector3(-.022f*side,0,-.02f),center+new Vector3(-.012f*side,0,-.075f),.015f,material);
            Segment("Claw thumb",parent,center+new Vector3(.022f*side,0,-.02f),center+new Vector3(.012f*side,0,-.065f),.015f,material);
        }
    }
}
