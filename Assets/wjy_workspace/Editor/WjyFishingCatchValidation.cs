#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using Wjy.Museum;

// Runs only in the disposable validation project; never edits the museum scenes.
public static class WjyFishingCatchValidation
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static int checks;
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Private).Invoke(obj, args);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj, value);
    private static object Get(object obj, string name) => obj.GetType().GetField(name, Private).GetValue(obj);
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception(label);
        checks++;
        Debug.Log("PASS " + label);
    }

    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Contains("/.work/CatchValidation/"))
            throw new InvalidOperationException("Use the isolated CatchValidation project.");
        try
        {
            var station = new GameObject("Fishing station");
            var rod = new GameObject("Rod", typeof(BoxCollider), typeof(Rigidbody));
            rod.transform.SetParent(station.transform, false);
            var tip = new GameObject("Tip").transform;
            tip.SetParent(rod.transform, false); tip.localPosition = Vector3.up * 2.1f;
            var hook = new GameObject("Hook").transform; hook.SetParent(rod.transform, false);
            var line = rod.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.widthMultiplier = .002f;
            var water = new GameObject("Water", typeof(BoxCollider)).GetComponent<BoxCollider>();
            water.transform.position = new Vector3(0, .5f, -3); water.size = new Vector3(2.3f, .02f, 1.5f);
            var rig = new GameObject("Shared rig").AddComponent<XROrigin>();
            rig.Camera = new GameObject("Camera").AddComponent<Camera>(); rig.Camera.transform.SetParent(rig.transform);
            var controller = new GameObject("Left Controller"); controller.transform.SetParent(rig.transform);
            var ray = controller.AddComponent<XRRayInteractor>();
            rod.AddComponent<WjyFishingRodGrab>();
            var interaction = rod.AddComponent<WjyFishingRodCatchInteraction>();
            Set(interaction, "sharedRig", rig); Set(interaction, "rodTip", tip); Set(interaction, "hook", hook);
            Set(interaction, "lineGuides", new[] { tip }); Set(interaction, "fishingLine", line); Set(interaction, "waterSurface", water);
            Call(interaction, "Awake"); Call(interaction, "Start");
            interaction.EditorSkipDevicePolling = true;
            Check(!interaction.FloatVisible && !interaction.FishVisible, "No float or fish on display");
            var hands = (IList)Get(interaction, "hands");
            Check(hands.Count == 1, "Uses the shared controller rig");
            Set(interaction, "holdingHand", hands[0]); Set(interaction, "triggerReady", true);
            Call(interaction, "SetState", WjyFishingRodCatchInteraction.RodState.Held, "test");

            for (int attempt = 0; attempt < 2; attempt++)
            {
                Call(interaction, "ProcessTrigger", true, false);
                Check(!interaction.FloatVisible && !interaction.FishVisible && string.IsNullOrEmpty(interaction.CatchMessage), "New cast resets previous visuals");
                interaction.LaunchHook(new Vector3(0, 1, -2));
                Check(interaction.State == WjyFishingRodCatchInteraction.RodState.Flying && !interaction.FloatVisible, "Float absent during ballistic cast");
                for (int i = 0; i < 250 && interaction.State == WjyFishingRodCatchInteraction.RodState.Flying; i++) Call(interaction, "AdvanceHook", .02f);
                Check(interaction.State == WjyFishingRodCatchInteraction.RodState.InWater && interaction.FloatVisible, "Landing activates float");
                float wait = (float)Get(interaction,"resultDelay");
                Check(wait >= 1f && wait <= 3f, "Each water entry samples a delay within one to three seconds");
                Call(interaction, "AdvanceFishing", wait-.01f);
                Check(interaction.State == WjyFishingRodCatchInteraction.RodState.InWater, "No outcome before the sampled delay");
                bool expected = attempt == 0;
                int seed;
                for (seed = 0; ; seed++)
                {
                    UnityEngine.Random.InitState(seed); float roll = UnityEngine.Random.value;
                    if (expected ? roll >= .5f && roll < .8f : roll >= .8f) break;
                }
                UnityEngine.Random.InitState(seed);
                Call(interaction, "AdvanceFishing", .02f);
                Check(interaction.HasFish == expected, "80% threshold accepts 50–80% rolls and rejects 80–100% rolls");
                var randomState = UnityEngine.Random.state;
                Call(interaction, "AdvanceFishing", 1f);
                Check(UnityEngine.Random.state.Equals(randomState), "No repeated rolls after outcome");
                var visuals = (WjyFishingCatchVisuals)Get(interaction, "visuals");
                Check(Quaternion.Angle(visuals.Float.rotation, Quaternion.identity) < .001f, "Float remains upright");
                var oldPosition = visuals.Float.position;
                Call(interaction, "AdvanceFishing", .07f);
                Check(Vector3.Distance(oldPosition, visuals.Float.position) > .00001f, "Float bobs on water");
                Call(interaction, "DrawLine");
                Check(Vector3.Distance(line.GetPosition(line.positionCount - 2), visuals.Float.position) < .0001f &&
                    Vector3.Distance(line.GetPosition(line.positionCount - 1), hook.position) < .0001f, "Line joins float and submerged hook");
                if (expected)
                {
                    visuals.Pose(.123f, true, water.bounds.center, rig.Camera);
                    Check(Quaternion.Angle(visuals.RodMotion.localRotation, Quaternion.identity) > .01f, "Bite visibly shakes rod model");
                    Check(!interaction.FishVisible, "Fish stays hidden underwater before reeling");
                    Preview(visuals.Float.position + Vector3.up * .04f, "Float", .23f);
                }
                else Check(interaction.CatchMessage == "Fish don\u2019t seem to like you. Try again." && visuals.Panel.activeSelf, "Exact failure text shown on panel");
                Call(interaction, "ProcessTrigger", true, false);
                Check(interaction.State == WjyFishingRodCatchInteraction.RodState.Reeling && !interaction.FloatVisible, "Trigger reels in and hides float");
                for (int i = 0; i < 50; i++) Call(interaction, "AdvanceReel", .02f);
                Check(interaction.State == WjyFishingRodCatchInteraction.RodState.Held && interaction.FishVisible == expected, "Only a successful catch brings back a fish");
                Check(visuals.Fish.parent == hook && visuals.Fish.localPosition == Vector3.zero, "Caught fish mouth attached to hook");
                if (expected) Preview(hook.position + Vector3.down * .15f, "Fish", .23f);
            }
            Call(interaction, "ProcessTrigger", true, false); interaction.LaunchHook(new Vector3(0, 1, -2));
            for (int i = 0; i < 250 && interaction.State == WjyFishingRodCatchInteraction.RodState.Flying; i++) Call(interaction, "AdvanceHook", .02f);
            Call(interaction, "AdvanceFishing", .5f); Call(interaction, "ProcessTrigger", true, false);
            for (int i = 0; i < 50; i++) Call(interaction, "AdvanceReel", .02f);
            Check(!interaction.HasFish && !interaction.FishVisible, "Reeling early cancels the bite attempt");
            var catalog = (WjyFishingCatchVisuals)Get(interaction, "visuals");
            Check(catalog.CatchModels.Count == 7, "Seven catch models including all six additions");
            catalog.Fish.gameObject.SetActive(true);
            for (int kind = 0; kind < 7; kind++)
            {
                catalog.SelectCatch((WjyCatchKind)kind);
                int visible = 0;
                foreach (var model in catalog.CatchModels) if (model.gameObject.activeSelf) visible++;
                Check(visible == 1 && catalog.CatchModels[kind].gameObject.activeSelf, "Exclusive model selection: " + (WjyCatchKind)kind);
                Check(catalog.CatchModels[kind].GetComponentsInChildren<Renderer>().Length > 0, "Catch has rendered geometry: " + (WjyCatchKind)kind);
                Check(catalog.CatchModels[kind].Find("ImportedGeometry") != null, "Downloaded realistic prefab loaded: " + (WjyCatchKind)kind);
                Preview(hook.position + Vector3.down * .13f, "Catch_" + (WjyCatchKind)kind, .29f);
            }
            Call(interaction, "ProcessTrigger", true, false); interaction.LaunchHook(new Vector3(0, 1, -2));
            for (int i = 0; i < 250 && interaction.State == WjyFishingRodCatchInteraction.RodState.Flying; i++) Call(interaction, "AdvanceHook", .02f);
            int successSeed;
            for (successSeed = 0; ; successSeed++) { UnityEngine.Random.InitState(successSeed); if (UnityEngine.Random.value < .8f) break; }
            int beforeEscapeCount = ((System.Collections.Generic.List<WjyCatchKind>)Get(interaction,"availableCatches")).Count;
            UnityEngine.Random.InitState(successSeed); Call(interaction, "AdvanceFishing", 3.01f);
            Check(interaction.State == WjyFishingRodCatchInteraction.RodState.FishHooked, "Successful catch starts a fresh three-second retrieval window");
            Call(interaction, "AdvanceFishing", 2.99f);
            Check(interaction.HasFish, "Catch retained just before deadline");
            Call(interaction, "AdvanceFishing", .02f);
            Check(interaction.State == WjyFishingRodCatchInteraction.RodState.Escaped && !interaction.HasFish && !interaction.FishVisible, "Catch escapes after three seconds without retrieval");
            Check(((System.Collections.Generic.List<WjyCatchKind>)Get(interaction,"availableCatches")).Count == beforeEscapeCount, "An escaped catch stays eligible in the current round");
            var escapeText = catalog.Panel.GetComponentInChildren<UnityEngine.UI.Text>();
            Check(interaction.CatchMessage == "The fish is gone. Try again!" && escapeText.color.r > .9f && escapeText.color.g < .2f && escapeText.fontStyle == FontStyle.Bold && escapeText.fontSize >= 40, "Escape warning is exact, large, bold and red");
            var min = catalog.Float.position; var max = min; bool inBounds = true;
            var driftRandomState = UnityEngine.Random.state;
            for (int i = 0; i < 2000; i++)
            {
                Call(interaction, "AdvanceFishing", .02f);
                var p = catalog.Float.position; min = Vector3.Min(min,p); max = Vector3.Max(max,p);
                var b = water.bounds;
                inBounds &= p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
            }
            Check(inBounds && max.x-min.x > .15f && max.z-min.z > .15f, "Random drift explores the pool and stays within its edges");
            Check(UnityEngine.Random.state.Equals(driftRandomState), "Random drift does not consume the catch random stream");
            Call(interaction, "ProcessTrigger", true, false);
            for (int i = 0; i < 50; i++) Call(interaction, "AdvanceReel", .02f);
            Check(!interaction.FishVisible && !interaction.HasFish, "Reeling after escape cannot recover the lost catch");
            Call(interaction, "ProcessTrigger", true, false);
            Check(string.IsNullOrEmpty(interaction.CatchMessage) && escapeText.color == Color.white && escapeText.fontStyle == FontStyle.Normal, "Next cast clears the red warning and resets message style");
            var bag = (System.Collections.Generic.List<WjyCatchKind>)Get(interaction,"availableCatches");
            bag.Clear(); bag.AddRange((WjyCatchKind[])Enum.GetValues(typeof(WjyCatchKind)));
            var sampledWaits = new System.Collections.Generic.HashSet<int>();
            for (int round = 0; round < 2; round++)
            {
                var caught = new System.Collections.Generic.HashSet<WjyCatchKind>();
                for (int item = 0; item < 7; item++)
                {
                    if (interaction.State == WjyFishingRodCatchInteraction.RodState.Held) Call(interaction,"ProcessTrigger",true,false);
                    interaction.LaunchHook(new Vector3(0,1,-2));
                    for(int i=0;i<250 && interaction.State==WjyFishingRodCatchInteraction.RodState.Flying;i++) Call(interaction,"AdvanceHook",.02f);
                    float wait = (float)Get(interaction,"resultDelay");
                    Check(wait >= 1f && wait <= 3f,"Repeated casts keep the delay inside the requested range");
                    sampledWaits.Add(Mathf.RoundToInt(wait*10000));
                    int seed=round*101+item*17;
                    for(;;seed++){UnityEngine.Random.InitState(seed);if(UnityEngine.Random.value<.8f)break;}
                    UnityEngine.Random.InitState(seed);Call(interaction,"AdvanceFishing",3.01f);
                    Check(interaction.HasFish && caught.Add(catalog.CatchKind),"Round "+round+": each successful catch selects a new model");
                    Check(bag.Count==7-item,"Biting reserves but does not consume a model");
                    Call(interaction,"ProcessTrigger",true,false);Call(interaction,"AdvanceReel",1f);
                    Check(bag.Count==6-item,"Successful retrieval consumes exactly one model");
                    Call(interaction,"CommitCatch");
                    Check(bag.Count==6-item,"Returning an already collected reward does not consume another slot");
                }
                Check(caught.Count==7 && bag.Count==0,"Round "+round+": all seven collected before refill");
            }
            Check(sampledWaits.Count > 1,"Wait time changes between casts rather than staying fixed");
            Call(interaction,"ProcessTrigger",true,false);
            Call(interaction, "OnDisable");
            Check(!interaction.FloatVisible && !interaction.FishVisible && string.IsNullOrEmpty(interaction.CatchMessage), "Disable cleans up visible feedback");
            File.WriteAllText(Path.Combine(Application.dataPath, "../result.txt"), "PASS: " + checks + " fishing interaction checks.\nNo headset connected; device comfort and haptics require Quest validation.");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            File.WriteAllText(Path.Combine(Application.dataPath, "../result.txt"), "FAIL: " + e);
            EditorApplication.Exit(1);
        }
    }

    private static void Preview(Vector3 center, string name, float halfHeight)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var lines = UnityEngine.Object.FindObjectsOfType<LineRenderer>();
        foreach (var line in lines) line.enabled = false;
        RenderSettings.ambientLight = new Color(.65f, .65f, .65f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        var light = new GameObject("Preview light").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(35, -35, 0);
        var camera = new GameObject("Preview camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .17f, .2f);
        camera.orthographic = true; camera.orthographicSize = halfHeight; camera.nearClipPlane = .01f; camera.farClipPlane = 10;
        camera.transform.position = center + new Vector3(.3f, .48f, -.8f); camera.transform.LookAt(center);
        var render = new RenderTexture(720, 720, 24); camera.targetTexture = render; camera.Render();
        RenderTexture.active = render; var texture = new Texture2D(720, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 720, 720), 0, 0); texture.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "../../" + name + ".png"), texture.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(light.gameObject);
        UnityEngine.Object.DestroyImmediate(render); UnityEngine.Object.DestroyImmediate(texture);
        foreach (var line in lines) line.enabled = true;
    }
}
#endif
