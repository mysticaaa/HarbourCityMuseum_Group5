using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using ZzxWorkspace.FirstDay;

[InitializeOnLoad]
public static class HarbourSavedSmoke
{
    private const string Key="ZZX.SavedHarbourSmoke";
    private static int step;
    private static double next;
    static HarbourSavedSmoke() { EditorApplication.update+=Tick; }
    public static void Batch()
    {
        EditorSceneManager.OpenScene(HarbourTabletopPrototype.TargetScene);
        SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if(EditorApplication.timeSinceStartup<next) return;
        next=EditorApplication.timeSinceStartup+.20;
        try
        {
            var exhibit=UnityEngine.Object.FindObjectsOfType<HarbourTabletopPrototype>().Single(x=>x.gameObject.scene.path==HarbourTabletopPrototype.TargetScene);
            if(!exhibit.IsSavedExhibit) throw new Exception("Runtime regenerated the exhibit instead of using the saved prefab.");
            var buttons=exhibit.GetComponentsInChildren<HarbourPrototypeButton>(true);
            Action<string> click=name=>buttons.Single(x=>x.name==name).Invoke();
            Action<HarbourTabletopPrototype.Stage> expect=stage=> { if(exhibit.CurrentStage!=stage) throw new Exception("Stage "+exhibit.CurrentStage+" != "+stage+" at step "+step); };
            switch(step++)
            {
                case 0:
                    expect(HarbourTabletopPrototype.Stage.Idle);
                    if(exhibit.GetComponentsInChildren<Camera>(true).Length!=0 || exhibit.GetComponentsInChildren<XRInteractionManager>(true).Length!=0) throw new Exception("Private XR camera or manager exists.");
                    if(buttons.Any(x=>x.action==null)) throw new Exception("Button callback missing.");
                    if(exhibit.GetComponentsInChildren<XRBaseInteractable>(true).Any(x=>!x.interactionManager)) throw new Exception("Shared interaction manager missing.");
                    click("Main action"); break;
                case 1: expect(HarbourTabletopPrototype.Stage.Explore); click("Work pass"); break;
                case 2: click("Close document"); break;
                case 3: click("Letter"); break;
                case 4: click("Close document"); break;
                case 5: click("Main action"); break;
                case 6: expect(HarbourTabletopPrototype.Stage.Choosing); click("Answer 0"); break;
                case 7: expect(HarbourTabletopPrototype.Stage.Choosing); click("Answer 1"); break;
                case 8: expect(HarbourTabletopPrototype.Stage.Report); click("Main action"); break;
                case 9: expect(HarbourTabletopPrototype.Stage.Cargo); click("Select cargo"); break;
                case 10: expect(HarbourTabletopPrototype.Stage.Selected); click("Boat target"); break;
                case 11: expect(HarbourTabletopPrototype.Stage.Loading); click("Skip"); break;
                case 12: expect(HarbourTabletopPrototype.Stage.Letter); click("Letter"); break;
                case 13: expect(HarbourTabletopPrototype.Stage.Complete); click("Reset"); break;
                case 14: expect(HarbourTabletopPrototype.Stage.Idle); click("Area 0"); break;
                case 15: click("Area 2"); break;
                case 16: click("Area 1"); break;
                case 17: click("Reset"); break;
                case 18:
                    expect(HarbourTabletopPrototype.Stage.Idle);
                    SessionState.SetBool(Key,false);
                    File.WriteAllText("Assets/zzx_workspace/FirstDayExhibit/Validation~/PlaySmoke.txt","PASS: saved prefab, callback rebinding, shared manager, documents, wrong/correct answer, report, loading/skip, ending/reset, animation switching/reset. Headset and visual rendering not tested.");
                    EditorApplication.Exit(0); break;
            }
        }
        catch(Exception e)
        {
            SessionState.SetBool(Key,false); Debug.LogException(e);
            File.WriteAllText("Assets/zzx_workspace/FirstDayExhibit/Validation~/PlaySmoke.txt","FAIL: "+e);
            EditorApplication.Exit(1);
        }
    }
}
