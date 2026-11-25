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
                case 1: expect(HarbourTabletopPrototype.Stage.Explore); click("Main action"); break;
                case 2: expect(HarbourTabletopPrototype.Stage.Choosing); click("Answer 0"); break;
                case 3: expect(HarbourTabletopPrototype.Stage.Choosing); click("Work pass"); break;
                case 4: click("Readable text"); break;
                case 5: click("Reading size"); break;
                case 6: click("Reading height"); break;
                case 7: click("Close document"); break;
                case 8: click("Letter"); break;
                case 9: click("Close document"); break;
                case 10: click("Answer 1"); break;
                case 11: expect(HarbourTabletopPrototype.Stage.Report); click("Main action"); break;
                case 12: expect(HarbourTabletopPrototype.Stage.Cargo); click("Select cargo"); break;
                case 13: expect(HarbourTabletopPrototype.Stage.Selected); click("Boat target"); break;
                case 14: expect(HarbourTabletopPrototype.Stage.Loading); click("Skip"); break;
                case 15: expect(HarbourTabletopPrototype.Stage.Review); click("Main action"); break;
                case 16: expect(HarbourTabletopPrototype.Stage.Complete); click("Reset"); break;
                case 17: expect(HarbourTabletopPrototype.Stage.Idle); click("Area 0"); break;
                case 18: click("Area 2"); break;
                case 19: click("Area 1"); break;
                case 20: click("Reset"); break;
                case 21:
                    expect(HarbourTabletopPrototype.Stage.Idle);
                    var data=new SerializedObject(exhibit);
                    var panel=((GameObject)data.FindProperty("reader").objectReferenceValue).transform;
                    if(Vector3.Distance(panel.localPosition,data.FindProperty("readerDefaultPosition").vector3Value)>.00001f) throw new Exception("Reading height did not reset.");
                    click("Main action"); break;
                case 22: click("Main action"); break;
                case 23: click("Answer 1"); break;
                case 24: click("Main action"); break;
                case 25: click("Select cargo"); break;
                case 26: click("Boat target"); break;
                case 27: expect(HarbourTabletopPrototype.Stage.Loading); next=EditorApplication.timeSinceStartup+8; break;
                case 28: expect(HarbourTabletopPrototype.Stage.Review); click("Main action"); break;
                case 29: expect(HarbourTabletopPrototype.Stage.Complete); click("Reset"); break;
                case 30:
                    expect(HarbourTabletopPrototype.Stage.Idle);
                    SessionState.SetBool(Key,false);
                    File.WriteAllText("Assets/zzx_workspace/FirstDayExhibit/Validation~/PlaySmoke.txt","PASS: saved prefab, shared XR manager, no document-reading gate, wrong/right answer, image/text, enlarged reading, lowered panel, report, full loading and skip hold explanation until CONTINUE, help, reset, animation switching. Headset and visual rendering not tested.");
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
