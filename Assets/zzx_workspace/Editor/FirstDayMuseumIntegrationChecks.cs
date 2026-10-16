using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using ZzxWorkspace.FirstDay;

[InitializeOnLoad]
public static class FirstDayMuseumIntegrationChecks
{
    const string ScenePath = "Assets/261016/Museum_261016_3.unity";
    const string Output = "Assets/zzx_workspace/Integration~/";
    public static void Survey()
    {
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var report = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects())
        {
            report.AppendLine(root.name + " active=" + root.activeInHierarchy + " position=" + root.transform.position);
            foreach (var r in root.GetComponentsInChildren<Renderer>().Where(r=>r.gameObject.activeInHierarchy))
                report.AppendLine("  " + r.name + " bounds=" + r.bounds);
        }
        foreach(var t in all)
        {
            var rig=t.GetComponent<XROrigin>();var manager=t.GetComponent<XRInteractionManager>();var camera=t.GetComponent<Camera>();
            if(rig || manager || camera)report.AppendLine("XR " + t.name + " active="+t.gameObject.activeInHierarchy+" rig="+!!rig+" manager="+!!manager+" camera="+!!camera);
        }
        File.WriteAllText(Output+"survey.txt",report.ToString());
    }
    static void Require(bool ok,string message) { if(!ok)throw new InvalidOperationException(message); }
    static int step;
    static double started;
    static Vector3 documentPose;
    static XRGrabInteractable held;
    static XRRayInteractor hand;
    static FirstDayMuseumIntegrationChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ZZX.IntegrationSmoke",false))
            { started=EditorApplication.timeSinceStartup;step=0;EditorApplication.update+=Tick; }
        };
    }
    public static void Smoke()
    {
        Require(Application.isBatchMode,"Run only in a disposable batch-mode validation copy.");
        Validate();
        SessionState.SetBool("ZZX.IntegrationSmoke",true);
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup-started<3)return;
        try
        {
            var task=UnityEngine.Object.FindObjectOfType<FirstDayJourney>();
            var manager=UnityEngine.Object.FindObjectsOfType<XRInteractionManager>().Single(x=>x.isActiveAndEnabled);
            var rig=UnityEngine.Object.FindObjectsOfType<XROrigin>().Single(x=>x.isActiveAndEnabled);
            if(step==0)
            {
                // No tracked devices exist in batch mode. Enable only the existing controller branches in this disposable runtime.
                foreach(var behaviour in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                    if(behaviour.GetType().Name=="XRInputModalityManager")behaviour.enabled=false;
                foreach(var t in rig.GetComponentsInChildren<Transform>(true))
                    if(t.name=="Left Controller" || t.name=="Right Controller")t.gameObject.SetActive(true);
                task.ResetJourney();task.Choose(1);Require(!task.IsComplete && task.Attempts==1,"Wrong answer feedback");
            }
            else if(step==1) {task.Choose(2);Require(task.IsComplete && task.ending.activeSelf,"Completion");}
            else if(step==2) {task.Choose(3);Require(task.Attempts==2,"Completion lock");task.ResetJourney();Require(!task.IsComplete && !task.ending.activeSelf,"Restart");task.Choose(3);Require(task.feedback.text.Contains("repair"),"Second wrong answer");}
            else if(step>=3 && step<15)
            {
                int index=(step-3)/3;
                var rays=rig.GetComponentsInChildren<XRRayInteractor>(true).Where(r=>r.name=="Ray Interactor" &&
                    r.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Left Controller" || t.name=="Right Controller")).ToArray();
                Require(rays.Length==2,"Expected left and right controller rays");
                if((step-3)%3==0)
                {
                    held=task.GetComponentsInChildren<XRGrabInteractable>()[index%2];hand=rays[index/2];
                    documentPose=held.transform.position;
                    manager.SelectEnter((IXRSelectInteractor)hand,held);
                    Require(held.isSelected,"Document select");
                    held.transform.position+=Vector3.up*.2f;
                }
                else if((step-3)%3==1)
                {manager.SelectExit((IXRSelectInteractor)hand,held);held.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Late);}
                else
                {Require(!held.isSelected && Vector3.Distance(held.transform.position,documentPose)<.01f,"Document return");Require(held.GetComponent<Rigidbody>().isKinematic,"Document physics reset");}
            }
            else
            {
                File.AppendAllText(Output+"validation.txt","PASS Play mode: both wrong answers, correct answer, completion lock, restart; left and right XR controller ray selection/release for both documents; return position and kinematic reset. Programmatic XR transitions, not hardware input.\n");
                EditorApplication.update-=Tick;EditorApplication.Exit(0);return;
            }
            step++;
        }
        catch(Exception ex)
        {File.AppendAllText(Output+"validation.txt","FAIL step="+step+" "+ex);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
    }
    public static void Validate()
    {
        Survey();
        var task=UnityEngine.Object.FindObjectOfType<FirstDayJourney>();
        Require(task,"Missing exhibit");
        var manager=UnityEngine.Object.FindObjectsOfType<XRInteractionManager>().Single(x=>x.isActiveAndEnabled);
        var rig=UnityEngine.Object.FindObjectsOfType<XROrigin>().Single(x=>x.isActiveAndEnabled);
        Require(rig.Camera && rig.Camera.isActiveAndEnabled,"Shared camera missing");
        Require(task.GetComponentsInChildren<Camera>(true).Length==0,"Exhibit contains camera");
        Require(task.GetComponentsInChildren<XROrigin>(true).Length==0,"Exhibit contains rig");
        Require(task.GetComponentsInChildren<XRInteractionManager>(true).Length==0,"Exhibit contains manager");
        var interactions=task.GetComponentsInChildren<XRBaseInteractable>(true);
        Require(interactions.Length==6,"Expected 2 documents and 4 buttons");
        Require(interactions.All(x=>x.interactionManager==manager),"Manager reference mismatch");
        Require(task.GetComponentsInChildren<FirstDayInspectionHint>(true).Length==2,"Local inspection hints missing");
        foreach(var t in task.GetComponentsInChildren<Transform>(true))
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
        var rays=rig.GetComponentsInChildren<XRRayInteractor>(true).Where(x=>x.gameObject.name.Contains("Ray")).ToArray();
        Require(rays.Length>=2,"Shared controller rays missing");
        Require(rays.All(r=>r.interactionManager==manager),"Ray manager mismatch");
        File.WriteAllText(Output+"validation.txt","PASS: one active XR rig, shared camera, one active manager; existing controller rays share manager; six added interactables explicitly bound; two local hint scripts; no missing scripts in new exhibit.\n");
    }
}
