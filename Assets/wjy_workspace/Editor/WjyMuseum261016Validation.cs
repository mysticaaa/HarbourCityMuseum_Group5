using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Wjy.Museum;

// Run only against an isolated project copy. Does not configure or save XR settings.
public static class WjyMuseum261016Validation
{
    const string Key="WJY.261016Validation";
    const string Report="Assets/wjy_workspace/Documentation/Museum261016Validation.txt";
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly StringBuilder Results=new StringBuilder();
    static WjyFishingRodInteraction rod;
    static WjyFishingRodGrab grab;
    static XRRayInteractor[] rays;
    static Transform tip,hook;
    static LineRenderer line;
    static Vector3 home;
    static Quaternion beforeRotation;
    static object selectAction;
    static int phase,hand,frames;
    static float waitUntil;

    [InitializeOnLoadMethod]
    static void Resume(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/261016/Museum_261016_2.unity");
        SessionState.SetBool(Key,true);SessionState.SetString(Key+"Result","");
        SessionState.SetFloat(Key+"Start",(float)EditorApplication.timeSinceStartup);
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        EditorApplication.EnterPlaymode();
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);Results.AppendLine("PASS: "+message);}
    static void Trigger(bool down,bool up)=>typeof(WjyFishingRodInteraction).GetMethod("ProcessTrigger",Private).Invoke(rod,new object[]{down,up});
    static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying)
            {
                var result=SessionState.GetString(Key+"Result","");
                if(result!=""){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(int.Parse(result));}
                return;
            }
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"Start",0)>75)throw new Exception("Runtime validation timed out at phase "+phase);
            if(EditorApplication.isCompiling)return;
            if(phase==0)
            {
                if(++frames<10)return;
                rod=UnityEngine.Object.FindObjectOfType<WjyFishingRodInteraction>();Check(rod!=null,"New fishing rod exists in running scene");
                rod.EditorSkipDevicePolling=true;grab=rod.GetComponent<WjyFishingRodGrab>();
                tip=(Transform)typeof(WjyFishingRodInteraction).GetField("rodTip",Private).GetValue(rod);
                hook=(Transform)typeof(WjyFishingRodInteraction).GetField("hook",Private).GetValue(rod);
                line=(LineRenderer)typeof(WjyFishingRodInteraction).GetField("fishingLine",Private).GetValue(rod);
                home=rod.transform.localPosition;
                Check(UnityEngine.Object.FindObjectsOfType<Unity.XR.CoreUtils.XROrigin>().Length==1,"Exactly one active XR Rig");
                Check(UnityEngine.Object.FindObjectsOfType<XRInteractionManager>().Count(m=>m.isActiveAndEnabled)==1,"Exactly one effective interaction manager");
                Check(rod.GetComponentInParent<ExhibitAutoRotation>()==null,"Rod rotation is independent of helmet and boat scripts");
                Check(hook.GetComponent<MeshFilter>().sharedMesh.vertexCount>0 && line.positionCount>=8,"Visible hook mesh and guided fishing line");
                foreach(var behaviour in rod.SharedRig.GetComponentsInChildren<MonoBehaviour>(true)) if(behaviour && (behaviour.GetType().Name=="XRInputModalityManager" || behaviour.GetType().Name=="ActionBasedControllerManager"))behaviour.enabled=false;
                foreach(var controller in rod.SharedRig.GetComponentsInChildren<ActionBasedController>(true)) if(controller.name.Contains("Controller")){controller.gameObject.SetActive(true);controller.enabled=false;}
                rays=rod.SharedRig.GetComponentsInChildren<XRRayInteractor>(true).Where(r=>r.name=="Ray Interactor" && r.GetComponentsInParent<Transform>(true).Any(t=>t.name.Contains("Controller"))).OrderBy(r=>r.transform.parent.name).ToArray(); foreach(var ray in rays){ray.gameObject.SetActive(true);ray.enabled=true;ray.interactionManager=grab.interactionManager;}Check(rays.Length==2,"Uses existing left and right rays");
                foreach(var controller in rod.SharedRig.GetComponentsInChildren<ActionBasedController>())controller.enabled=false;
                PrepareHand();phase=1;frames=0;waitUntil=Time.time+.25f;
            }
            else if(phase==1)
            {
                if(++frames<4 || Time.time<waitUntil)return;
                var ray=rays[hand];Check(!grab.IsSelectableBy((IXRSelectInteractor)ray),"Trigger alone cannot select rod (hand "+hand+")");
                bool picked=rod.TryGrip(ray);
                if(!picked)
                {
                    bool hit=ray.TryGetCurrent3DRaycastHit(out var rayHit);
                    Results.AppendLine($"Grip diagnostics: state={rod.State}, active={ray.isActiveAndEnabled}, hasSelection={ray.hasSelection}, hit={hit}/{rayHit.collider?.name}, ray={ray.transform.position}, origin={ray.rayOriginTransform.position}, direction={ray.rayOriginTransform.forward}, manager={ray.interactionManager?.name}, rodManager={grab.interactionManager?.name}");
                    grab.AuthorizedRay=ray;
                    Results.AppendLine($"Select diagnostics: rayCanSelect={ray.CanSelect((IXRSelectInteractable)grab)}, rodSelectable={grab.IsSelectableBy((IXRSelectInteractor)ray)}, managerPossible={ray.interactionManager.IsSelectPossible(ray,grab)}");
                    grab.AuthorizedRay=null;
                }
                Check(picked,"Aimed Grip starts manual selection (hand "+hand+")");
                Check(rod.State==WjyFishingRodInteraction.RodState.Held && grab.IsSelected((IXRSelectInteractor)ray),"Rod is held through shared XR manager");
                Check(grab.interactionManager==ray.interactionManager,"Rod and shared ray use same manager");
                waitUntil=Time.time+.4f;phase=2;
            }
            else if(phase==2 && Time.time>=waitUntil)
            {
                beforeRotation=rod.transform.rotation;rays[hand].transform.Rotate(Vector3.forward,25,Space.Self);waitUntil=Time.time+.4f;phase=3;
            }
            else if(phase==3 && Time.time>=waitUntil)
            {
                Check(Quaternion.Angle(beforeRotation,rod.transform.rotation)>10,"Held rod follows controller rotation");
                typeof(WjyFishingRodInteraction).GetField("triggerReady",Private).SetValue(rod,true);
                Trigger(true,false);Check(rod.State==WjyFishingRodInteraction.RodState.Charging,"Trigger press arms cast");
                Trigger(false,true);Check(rod.State==WjyFishingRodInteraction.RodState.Held,"No cast without a swing");
                Trigger(true,false);rays[hand].transform.position+=new Vector3(0,.3f,-.3f);waitUntil=Time.time+.07f;phase=4;
            }
            else if(phase==4 && Time.time>=waitUntil)
            {
                Trigger(false,true);Check(rod.State==WjyFishingRodInteraction.RodState.Flying,"Swing followed by Trigger release launches hook");
                Check(!hook.IsChildOf(rod.transform),"Flying hook is independent of subsequent hand movement");
                rays[hand].transform.position+=Vector3.right*.2f;waitUntil=Time.time+4;phase=5;
            }
            else if(phase==5)
            {
                if(rod.State!=WjyFishingRodInteraction.RodState.InWater){if(Time.time>waitUntil)throw new Exception("Hook did not land in water");return;}
                Check(Vector3.Distance(line.GetPosition(line.positionCount-1),hook.position)<.01f,"Fishing line remains connected to flying/landed hook");
                var controller=(rays[hand].xrController as ActionBasedController);Check(ReferenceEquals(selectAction,controller.selectAction.action),"Shared controller select binding unchanged");
                rod.ReleaseGrip();Check(!rays[hand].useForceGrab,"Ray force-grab setting restored after releasing fish exhibit");Check(!rays[hand].isPerformingManualInteraction && !grab.isSelected,"Grip release ends selection");
                waitUntil=Time.time+.6f;phase=6;
            }
            else if(phase==6 && Time.time>=waitUntil)
            {
                Check(rod.State==WjyFishingRodInteraction.RodState.Display && Vector3.Distance(home,rod.transform.localPosition)<.001f,"Release returns rod to its display");
                Check(hook.IsChildOf(rod.transform),"Hook returns with rod");
                if(hand==0){hand=1;PrepareHand();phase=1;frames=0;waitUntil=Time.time+.25f;}
                else {Results.AppendLine("ALL CHECKS PASSED (simulated controllers; physical Quest test still required).");Finish(0);}
            }
        }
        catch(Exception e){Results.AppendLine("FAIL: "+e);Debug.LogException(e);Finish(1);}
    }
    static void PrepareHand()
    {
        var ray=rays[hand];ray.useForceGrab=false;ray.rayOriginTransform=ray.transform;ray.attachTransform=ray.transform;
        ray.transform.position=new Vector3(3.2f,1.65f,1.3f);ray.transform.rotation=Quaternion.LookRotation(new Vector3(3.2f,1.65f,-.75f)-ray.transform.position);
        selectAction=(ray.xrController as ActionBasedController).selectAction.action;Physics.SyncTransforms();
    }
    static void Finish(int code)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,Results.ToString());
        SessionState.SetString(Key+"Result",code.ToString());
        if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
        else {SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
    }
}


