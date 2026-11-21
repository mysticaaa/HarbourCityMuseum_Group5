using System.Linq;
using CsyMuseum;
using UnityEditor;
using UnityEngine;

public static class CSYGuideValidation
{
    [MenuItem("CSY Tools/Phase 3/Check guide policies")]
    public static void Policies()
    {
        VisitPolicyTests.Main();
        var task = new CSYHelmetObservation();
        Require(!task.Complete && task.Attempts == 0, "New observation starts incomplete");
        task.Answer(false); Require(!task.Complete && task.Attempts == 1, "Incorrect choice allows retry");
        task.Answer(true); Require(task.Complete && task.Attempts == 2, "Correct choice completes observation");
        task.Answer(false); Require(task.Complete && task.Attempts == 2, "Completed observation ignores duplicate input");
        task.Reset(); Require(!task.Complete && task.Attempts == 0, "Reset clears only observation state");
        Debug.Log("[CSY E] Visit policy and helmet observation checks passed. These are software checks, not user evaluation.");
    }
    [MenuItem("CSY Tools/Phase 3/Check running guide references and text")]
    public static void RunningScene()
    {
        Require(EditorApplication.isPlaying, "Enter Play mode first");
        var gallery = Object.FindObjectsOfType<CSYGalleryGuides>();
        Require(gallery.Length == 1 && gallery[0].Coordinator, "One gallery adapter initialized");
        var coordinator = gallery[0].Coordinator;
        Require(coordinator.visitorCamera && coordinator.visitorCamera.isActiveAndEnabled, "Active visitor camera");
        Require(coordinator.Guides.Count == 3, "Three intended exhibit guides");
        foreach (var guide in coordinator.Guides)
        {
            Require(guide && guide.rangeAnchor && guide.narration, "Guide has fixed range and audio");
            var card = guide.GetComponent<CSYGuideCard>();
            Require(card && card.TextFits, "Card text fits at its readable font sizes");
            var anchor = guide.rangeAnchor;
            Require(!guide.inspectionObject || !anchor.IsChildOf(guide.inspectionObject.transform), "Range does not move with held helmet");
        }
        Require(Object.FindObjectsOfType<ExhibitInspectionHint>().All(h => !h.Source ||
            !coordinator.Guides.Any(g => g.inspectionObject && g.inspectionObject.transform == h.Source)), "Replaced helmet hint is not visible");
        Debug.Log("[CSY E] Running references and layout checks passed; human readability, mouse/XR activation and scene transitions still need task walkthroughs.");
    }
    private static void Require(bool result, string message)
    { if (!result) throw new System.InvalidOperationException("[CSY E] " + message); }
}
