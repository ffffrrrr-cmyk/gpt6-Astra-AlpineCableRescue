using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class VegetationBuild
{
    [MenuItem("Frostline/Build vegetation revision")]
    public static void Build()
    {
        Directory.CreateDirectory("Builds/VegetationRevision");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Frostline.unity" },
            locationPathName = "Builds/VegetationRevision/FrostlineRescue.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText("Logs/vegetation-build-summary.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Vegetation build failed");
        Debug.Log("VEGETATION_BUILD_PASS");
    }
}
