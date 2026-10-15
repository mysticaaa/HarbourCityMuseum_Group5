using System.Collections;
using UnityEngine;

namespace ZxhMuseum
{
    // Explicit test flag only; this does not run during normal user sessions.
    public sealed class RuntimeBuildSmokeTest : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableTest()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-zxh-smoke-test") >= 0)
            {
                Application.runInBackground = true;
                foreach(var legacy in FindObjectsOfType<Valve.VR.SteamVR_Render>())legacy.enabled=false;
                Time.timeScale = 1;
                Debug.Log("ZXH PLAYER SMOKE START: background enabled; hardware-free validation.");
                // Museum roots and session child panels are hidden on boarding; keep the runner on the session itself.
                var host = MuseumSession.Instance != null ? MuseumSession.Instance.gameObject : new GameObject("ZXH_PlayerSmokeTest");
                host.AddComponent<RuntimeBuildSmokeTest>();
            }
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2);
            Time.timeScale = 1;
            bool valid = MuseumSession.Instance != null &&
                FindObjectsOfType<WorldPanel>().Length >= 4 &&
                Application.CanStreamedLevelBeLoaded(MuseumSession.InteriorPath) &&
                Resources.Load<AudioClip>("ZxhNarration/Welcome") != null;
            if(valid)
            {
                var session = MuseumSession.Instance;
                session.RequestEnter();
                float deadline = Time.realtimeSinceStartup + 30;
                while((session.Busy || !session.InCabin) && Time.realtimeSinceStartup < deadline)yield return null;
                var deck = FindObjectOfType<ShipDeckEnvironment>();
                valid = !session.Busy && session.InCabin && deck != null && deck.UsesExhibitionGeometry && deck.HasOriginalHatch && deck.HasOriginalWinch;
                if(valid)
                {
                    deck.ToggleDoor(); deck.SetBoom(true,15); deck.ToggleHatch(); deck.TurnWinch();
                    yield return new WaitForSeconds(3.5f);
                    session.MoveTo(new Vector3(1.2f,.04f,-2),0);Physics.SyncTransforms();
                    var walk = session.GetComponent<BoatWalkController>();
                    for(int i=0;i<360;i++)walk.Advance(Vector2.up,1f/60);
                    valid = session.Origin.transform.position.z > 1.9f && session.Origin.transform.position.y > 1.8f && Mathf.Abs(deck.PortBoomAngle-15)<.1f;
                    session.RequestReturn();deadline = Time.realtimeSinceStartup + 30;
                    while(session.Busy && Time.realtimeSinceStartup < deadline)yield return null;
                    valid &= !session.Busy && !session.InCabin;
                }
            }
            Debug.Log(valid ? "ZXH PLAYER SMOKE PASS: original boat meshes readable in player, deck entry, fishing gear, physical walk into cabin and return." : "ZXH PLAYER SMOKE FAIL");
            Application.Quit(valid ? 0 : 1);
        }
    }
}
