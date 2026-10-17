using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;
using ZxhMuseum;

public static class SceneSixDoorValidation
{
    public const string Source="Assets/261016/Museum_261016_5.unity";
    public const string Target="Assets/261016/Museum_261016_6.unity";
    private const string Key="ZXH.SceneSixDoorValidation";
    private static int phase, trips;
    private static double deadline;
    [MenuItem("ZXH/Create scene 6 doorway fix")]
    public static void Create()
    {
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Target)!=null)throw new Exception("Scene 6 already exists; refusing to overwrite.");
        if(!AssetDatabase.CopyAsset(Source,Target))throw new Exception("Scene 5 could not be copied.");
        var scene=EditorSceneManager.OpenScene(Target);
        var rig=UnityEngine.Object.FindObjectsOfType<XROrigin>().Single(x=>x.gameObject.scene==scene);
        rig.gameObject.AddComponent<SceneSixDoorClearance>();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ZXH SCENE 6 CREATED: scene 5 copied; only rig doorway clearance component added.");
    }
    public static void Run()
    {
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Target)==null)Create();
        else EditorSceneManager.OpenScene(Target);
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if(!SessionState.GetBool(Key,false))return;
        deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Doorway validation timeout");
            var session=MuseumSession.Instance;
            if(phase==0)
            {
                if(session==null)return;
                session.RequestEnter();phase=1;return;
            }
            if(phase==1)
            {
                if(session==null || session.Busy || !session.InCabin)return;
                var cabin=UnityEngine.Object.FindObjectOfType<CabinEnvironment>();
                var fix=session.Origin.GetComponent<SceneSixDoorClearance>();
                if(fix==null || fix.CorrectedColliders==0)throw new Exception("Scene 6 correction not applied");
                cabin.Deck.OuterDoorButton.Actuate();cabin.Deck.AdvanceMotion(3);Physics.SyncTransforms();
                foreach(float height in new[]{1.8f,2.1f,2.4f})
                {
                    var foot=new Vector3(1.2f,2.14f,.3f);const float radius=.22f;
                    var hits=Physics.CapsuleCastAll(foot+Vector3.up*radius,foot+Vector3.up*(height-radius),radius,
                        Vector3.forward,2.3f,~0,QueryTriggerInteraction.Ignore)
                        .Where(h=>h.collider.gameObject.scene==cabin.gameObject.scene).ToArray();
                    if(hits.Length>0)throw new Exception("Standing height "+height+" blocked: "+string.Join(", ",hits.Select(h=>h.collider.name+" at "+h.point+" bounds="+h.collider.bounds)));
                    var inside=foot+Vector3.forward*2.3f;
                    var reverse=Physics.CapsuleCastAll(inside+Vector3.up*radius,inside+Vector3.up*(height-radius),radius,
                        Vector3.back,2.3f,~0,QueryTriggerInteraction.Ignore)
                        .Where(h=>h.collider.gameObject.scene==cabin.gameObject.scene).ToArray();
                    if(reverse.Length>0)throw new Exception("Standing exit blocked: "+string.Join(", ",reverse.Select(h=>h.collider.name)));
                }
                // Floor and working closed door must continue to stop the player.
                if(!Physics.Raycast(new Vector3(1.2f,2.7f,2.5f),Vector3.down,1))throw new Exception("Cabin floor collision lost");
                cabin.Deck.InnerDoorButton.Actuate();cabin.Deck.AdvanceMotion(3);Physics.SyncTransforms();
                if(!Physics.Raycast(new Vector3(1.2f,3.1f,.5f),Vector3.forward,out var hit,2) || hit.collider.name!="Sliding wheelhouse door")
                    throw new Exception("Closed door collision lost");
                Debug.Log("ZXH SCENE 6 DOOR PASS: standing capsules 1.8/2.1/2.4m pass open doorway; floor and closed door retained.");
                session.RequestReturn();phase=2;return;
            }
            if(phase==2 && session!=null && !session.Busy && !session.InCabin)
            {
                if(++trips<3){phase=0;return;}
                Debug.Log("ZXH SCENE 6 RELEASE PASS: three boarding/return cycles, standing entry/exit, floor and closed door collision verified.");
                Finish(0);
            }
        }
        catch(Exception exception){Debug.LogException(exception);Finish(1);}
    }
    private static void Finish(int result)
    {
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.Exit(result);
    }
}
