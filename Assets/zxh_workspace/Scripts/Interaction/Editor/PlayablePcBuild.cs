using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class PlayablePcBuild
{
    public static void Run()
    {
        PlayableCabinBuilder.Build();
        QuestExhibitProjectSetup.Configure();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Museum.unity", ZxhMuseum.MuseumSession.InteriorPath },
            locationPathName = "Builds/ZxhMuseum-PCVR/HarbourCityMuseum.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("PCVR build failed: " + report.summary.result);
        UnityEngine.Debug.Log("ZXH PCVR BUILD PASS: " + report.summary.totalSize + " bytes; Quest Link runtime still needs hardware testing.");
    }
}
