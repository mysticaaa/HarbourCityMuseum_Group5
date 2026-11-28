using CsyMuseum;
using UnityEditor;
using UnityEngine;

public static class CSYP4GuideValidation
{
    [MenuItem("CSY Tools/Phase 4/Check E audio and control policies")]
    public static void Policies()
    {
        int audio = CSYGuideAudioChecks.Run();
        int controls = CSYGuidePresentationChecks.Run();
        Debug.Log("[CSY P4] Audio ownership and control-state checks passed: " + audio + " and " + controls
            + " assertions. Software checks do not certify headset presentation or video evidence.");
    }

    [MenuItem("CSY Tools/Phase 4/Check running E presentation")]
    public static void RunningScene()
    {
        CSYGuideValidation.RunningScene();
        foreach (var guide in Object.FindObjectsOfType<CSYExhibitGuide>())
        {
            var card = guide.GetComponent<CSYGuideCard>();
            if (!card || !card.TextFits) throw new System.InvalidOperationException("[CSY P4] E card text needs inspection.");
            if (guide.Coordinator.Speaking == guide && guide.AudioState != CSYGuideAudioState.Playing)
                throw new System.InvalidOperationException("[CSY P4] Audio status does not match the current speaker.");
        }
        Debug.Log("[CSY P4] References and text checks passed. Check actual visual occlusion, click feedback and stereo legibility in the demonstration.");
    }
}
