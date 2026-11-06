using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class TerrainBuild
{
    [MenuItem("Frostline/Build terrain revision only")]
    public static void Build()
    {
        Directory.CreateDirectory("Builds/TerrainRevision");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Frostline.unity" },
            locationPathName = "Builds/TerrainRevision/FrostlineRescue.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText("Logs/terrain-build-summary.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Terrain revision build failed");
        Debug.Log("TERRAIN_BUILD_PASS");
    }
}
