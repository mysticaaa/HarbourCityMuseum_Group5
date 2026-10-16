using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using ZzxWorkspace.FirstDay;
using Object=UnityEngine.Object;

namespace ZzxWorkspace.FirstDayEditor
{
    [InitializeOnLoad]
    public static class FirstDayExhibitChecks
    {
        private const string Pending="ZZX.FirstDay.Smoke";
        private static FirstDayJourney task;
        private static int step;
        private static double started;
        [Serializable] private class SavedSetup { public SceneSetup[] scenes; }
        static FirstDayExhibitChecks()
        {
            EditorApplication.playModeStateChanged+=Changed;
        }

        [MenuItem("Harbour City Museum/ZZX/First Day/3 - Render exhibit overview %#F9")]
        public static void Render()
        {
            var scene=EditorSceneManager.NewPreviewScene();
            RenderTexture rt=null;
            Texture2D output=null;
            try
            {
                var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FirstDayExhibitBuilder.Prefab));
                SceneManager.MoveGameObjectToScene(go,scene);
                var cameraObject=new GameObject("Temporary QA camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var camera=cameraObject.AddComponent<Camera>();
                camera.transform.position=new Vector3(0,2.2f,-3.4f);camera.transform.LookAt(new Vector3(0,1.05f,0));
                camera.scene=scene;
                camera.fieldOfView=43;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.07f);
                var lightObject=new GameObject("Temporary QA light");SceneManager.MoveGameObjectToScene(lightObject,scene);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;
                light.transform.rotation=Quaternion.Euler(42,-25,0);
                foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate(true);
                rt=new RenderTexture(1800,1300,24);camera.targetTexture=rt;camera.Render();
                var previous=RenderTexture.active;
                try { RenderTexture.active=rt;output=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);output.Apply(); }
                finally {RenderTexture.active=previous;}
                File.WriteAllBytes(FirstDayExhibitBuilder.Folder+"/Validation/ExhibitOverview.png",output.EncodeToPNG());
                camera.targetTexture=null;
                AssetDatabase.Refresh();
            }
            finally {if(rt!=null)Object.DestroyImmediate(rt);if(output!=null)Object.DestroyImmediate(output);EditorSceneManager.ClosePreviewScene(scene);}
        }

        [MenuItem("Harbour City Museum/ZZX/First Day/5 - Keep one manager in ZZX scene %#F7")]
        public static void KeepOneManager()
        {
            var scene=EditorSceneManager.OpenScene(FirstDayExhibitBuilder.Preview,OpenSceneMode.Additive);
            try
            {
                var managers=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<XRInteractionManager>(true)).ToArray();
                var retained=managers.Single(x=>x.transform.parent==null);
                foreach(var manager in managers)
                {
                    if(manager==retained || !manager.gameObject.activeInHierarchy || !manager.enabled)continue;
                    manager.enabled=false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
                }
                EditorSceneManager.SaveScene(scene,FirstDayExhibitBuilder.Preview);
            }
            finally {EditorSceneManager.CloseScene(scene,true);}
            FirstDayExhibitBuilder.Validate();
        }

        [MenuItem("Harbour City Museum/ZZX/First Day/6 - Apply checked display layout %#F6")]
        public static void RefineLayout()
        {
            FirstDayPlacement.CompactPrefab();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Harbour City Museum/ZZX/First Day/7 - Build Windows prototype %#F5")]
        public static void BuildWindows()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            // Unity refuses builds inside Assets. Use temporary build staging, then copy into this workspace.
            string output=Path.Combine(Path.GetTempPath(),"ZZX_FirstDay_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"),"HarbourFirstDay.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes=new[]{FirstDayExhibitBuilder.Preview}.Concat(EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)).Distinct().ToArray();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=scenes,locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
            if(report==null)
            {
                File.WriteAllText(FirstDayExhibitBuilder.Folder+"/Validation/WindowsBuildReport.txt","FAILED: Unity did not return a build report. See Unity Console.\n");
                return;
            }
            string details=report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize+"\n"+string.Join("\n",scenes)+"\n"+
                string.Join("\n",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error || m.type==LogType.Exception).Select(m=>m.content));
            File.WriteAllText(FirstDayExhibitBuilder.Folder+"/Validation/WindowsBuildReport.txt",details);
            if(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                string source=Path.GetDirectoryName(output);
                string destination=FirstDayExhibitBuilder.Folder+"/Builds~/Windows";
                foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
                {
                    string target=Path.Combine(destination,file.Substring(source.Length+1));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target,true);
                }
            }
            Debug.Log("ZZX Windows build: "+details);
        }

        [MenuItem("Harbour City Museum/ZZX/First Day/4 - Run Play mode smoke checks %#F8")]
        public static void Smoke()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your scene changes before running checks.");
            SessionState.SetString(Pending+".Setup",JsonUtility.ToJson(new SavedSetup{scenes=EditorSceneManager.GetSceneManagerSetup()}));
            SessionState.SetBool(Pending,true);
            EditorSceneManager.OpenScene(FirstDayExhibitBuilder.Preview,OpenSceneMode.Single);
            EditorApplication.isPlaying=true;
        }
        private static void Changed(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Pending,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                started=EditorApplication.timeSinceStartup;step=0;
                EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);
                var saved=JsonUtility.FromJson<SavedSetup>(SessionState.GetString(Pending+".Setup",""));
                if(saved!=null)EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
            }
        }
        private static void Require(bool ok,string description) {if(!ok)throw new InvalidOperationException(description);}
        private static void Tick()
        {
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started<3)return;
            try
            {
                if(step==0)
                {
                    task=Object.FindObjectOfType<FirstDayJourney>();Require(task!=null,"Task missing in runtime scene.");
                    Require(!task.IsComplete && task.Attempts==0 && !task.ending.activeSelf,"Initial state incorrect.");
                    task.Choose(1);Require(!task.IsComplete && task.Attempts==1 && task.feedback.text.Contains("TRY AGAIN"),"Ferry error feedback failed.");
                }
                else if(step==1)
                {
                    task.Choose(2);Require(task.IsComplete && task.Attempts==2 && task.ending.activeSelf,"Correct choice failed.");
                }
                else if(step==2)
                {
                    task.Choose(3);Require(task.IsComplete && task.Attempts==2,"Completed task accepted another answer.");
                    task.ResetJourney();Require(!task.IsComplete && task.Attempts==0 && !task.ending.activeSelf,"Reset failed.");
                    task.Choose(3);Require(!task.IsComplete && task.Attempts==1 && task.feedback.text.Contains("repair"),"Repair yard feedback failed.");
                }
                else if(step==3)
                {
                    task.ResetJourney();task.Choose(2);Require(task.IsComplete,"Replay failed.");
                    var root=task.transform;
                    var restart=root.Find("Restart button");
                    var panel=root.Find("Journey ending/Ending panel");
                    var buttonBounds=restart.GetComponent<BoxCollider>().bounds;
                    var panelBounds=panel.GetComponent<Renderer>().bounds;
                    Require(buttonBounds.max.y < panelBounds.min.y,"Ending panel overlaps restart vertically.");
                    var manager=Object.FindObjectsOfType<XRInteractionManager>().Single(x=>x.isActiveAndEnabled);
                    var ray=Object.FindObjectsOfType<XRRayInteractor>().First(x=>x.isActiveAndEnabled);
                    var button=root.GetComponentsInChildren<FirstDayMapButton>().First().GetComponent<XRSimpleInteractable>();
                    var peers=ray.xrController.GetComponentsInChildren<XRBaseControllerInteractor>()
                        .Where(x=>x.isActiveAndEnabled && x.xrController==ray.xrController).Take(2).ToArray();
                    Require(peers.Length==2,"Need two existing interactors on one controller for hover regression.");
                    manager.HoverEnter((IXRHoverInteractor)peers[0],button);
                    manager.HoverEnter((IXRHoverInteractor)peers[1],button);
                    manager.HoverExit((IXRHoverInteractor)peers[0],button);
                    Require(button.interactorsHovering.Contains(peers[1]),"Remaining interactor lost hover.");
                    manager.HoverExit((IXRHoverInteractor)peers[1],button);
                    foreach(var grab in root.GetComponentsInChildren<XRGrabInteractable>())
                    {
                        var pose=grab.transform.position;
                        Require(!grab.throwOnDetach,"Returnable document must not throw on detach.");
                        manager.SelectEnter((IXRSelectInteractor)ray,grab);
                        Require(grab.isSelected,"Manager did not select document.");
                        grab.transform.position+=Vector3.up;
                        manager.SelectExit((IXRSelectInteractor)ray,grab);
                        grab.ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase.Late);
                        Require(!grab.isSelected,"Manager did not release document.");
                        Require(Vector3.Distance(grab.transform.position,pose)<.01f,"Original return script did not restore its Awake pose.");
                        Require(grab.GetComponent<Rigidbody>().isKinematic,"Return did not stop physics.");
                    }
                }
                else
                {
                    File.WriteAllText(FirstDayExhibitBuilder.Folder+"/Validation/PlayModeReport.txt",DateTime.UtcNow.ToString("O")+"\nPASS: initial state; both wrong answers; correct answer; completion lock; reset; replay; manager selection / release / late detach; shared-controller hover; restart clearance.\nThis uses programmatic task calls and XRInteractionManager transitions, not hardware input. Tracked-controller input, headset comfort and executable build are NOT verified.\n");
                    EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
                }
                step++;
            }
            catch(Exception ex)
            {
                File.WriteAllText(FirstDayExhibitBuilder.Folder+"/Validation/PlayModeReport.txt","FAIL at step "+step+"\n"+ex);
                Debug.LogException(ex);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            }
        }
    }
}
