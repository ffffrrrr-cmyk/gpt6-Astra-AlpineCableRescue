using System;
using System.Collections.Generic;
using System.IO;
using Frostline;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProjectBuilder
{
    [MenuItem("Frostline/Rebuild generated assets and scene")]
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Generated"); Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Logs");
        AssetDatabase.Refresh();
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Generated/AlpineRenderer.asset");
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, "Assets/Generated/AlpineRenderer.asset");
        }
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Generated/AlpineURP.asset");
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, "Assets/Generated/AlpineURP.asset");
        }
        pipeline.msaaSampleCount = 4; pipeline.shadowDistance = 110; pipeline.supportsHDR = true;
        pipeline.mainLightShadowmapResolution = 2048; pipeline.shadowCascadeCount = 2;
        var pipelineSettings = new SerializedObject(pipeline);
        pipelineSettings.FindProperty("m_MainLightRenderingMode").intValue = (int)LightRenderingMode.PerPixel;
        pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue = true;
        pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue = true;
        pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        PlayerSettings.companyName = "FrostlineStudio"; PlayerSettings.productName = "Frostline Rescue";
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true; PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var inputProperty = playerSettings.FindProperty("activeInputHandler");
        if (inputProperty != null) { inputProperty.intValue = 0; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var crane = MechanicalArt.BuildCrane(); PersistGeometry(crane);
        var cranePrefab = PrefabUtility.SaveAsPrefabAsset(crane, "Assets/Generated/SR7_Crane.prefab"); UnityEngine.Object.DestroyImmediate(crane);
        var pod = MechanicalArt.BuildPod(); PersistGeometry(pod);
        var podPrefab = PrefabUtility.SaveAsPrefabAsset(pod, "Assets/Generated/RescuePod.prefab"); UnityEngine.Object.DestroyImmediate(pod);
        var game = new GameObject("FrostlineGame").AddComponent<RescueGame>();
        game.cranePrefab = cranePrefab; game.cargoPrefab = podPrefab; game.missions = new MissionDefinition[4];
        for (int missionIndex = 0; missionIndex < 4; missionIndex++)
        {
            string assetPath = "Assets/Generated/Mission" + missionIndex + ".asset";
            var source = MissionDefinition.Create(missionIndex);
            var asset = AssetDatabase.LoadAssetAtPath<MissionDefinition>(assetPath);
            if (asset == null) { asset = source; AssetDatabase.CreateAsset(asset, assetPath); }
            else { EditorUtility.CopySerialized(source, asset); UnityEngine.Object.DestroyImmediate(source); }
            game.missions[missionIndex] = asset;
        }
        var shaderCollection = new ShaderVariantCollection();
        shaderCollection.Add(new ShaderVariantCollection.ShaderVariant(Shader.Find("Universal Render Pipeline/Lit"), PassType.ScriptableRenderPipeline));
        string variantsPath = "Assets/Generated/RequiredShaders.shadervariants";
        var existingVariants = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(variantsPath);
        if (existingVariants == null) AssetDatabase.CreateAsset(shaderCollection, variantsPath);
        else UnityEngine.Object.DestroyImmediate(shaderCollection);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Frostline.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Frostline.unity", true) };
        EditorUtility.SetDirty(pipeline); AssetDatabase.SaveAssets();
        Debug.Log("FROSTLINE_GENERATION_PASS");
    }
    static void PersistGeometry(GameObject root)
    {
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || AssetDatabase.Contains(mesh)) continue;
            string path = "Assets/Generated/" + mesh.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path); else filter.sharedMesh = existing;
        }
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            var material = renderer.sharedMaterial;
            if (material == null || AssetDatabase.Contains(material)) continue;
            string path = "Assets/Generated/" + material.name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null) AssetDatabase.CreateAsset(material, path); else renderer.sharedMaterial = existing;
        }
    }
    [MenuItem("Frostline/Validate rules and save system")]
    public static void Verify()
    {
        Directory.CreateDirectory("Logs"); var checks = new List<string>();
        var rules = new RescueRules();
        rules.Begin(2, 100, false); rules.Tick(2); rules.Pause(true); rules.Tick(20);
        Assert(rules.Elapsed == 2 && rules.Phase == MissionPhase.Paused, "Pause freezes clock", checks);
        Assert(!rules.Deliver(1), "Paused delivery rejected", checks);
        rules.Pause(false); Assert(rules.Deliver(1) && !rules.Deliver(1) && rules.Delivered == 1, "Delivery idempotence", checks);
        rules.Deliver(2); Assert(rules.Phase == MissionPhase.Won, "Win after all unique cargo", checks);
        rules.Fail("late failure"); Assert(rules.Phase == MissionPhase.Won, "Terminal win immutable", checks);
        rules.Begin(1, 2, false); rules.Tick(3); Assert(rules.Phase == MissionPhase.Lost, "Timed failure", checks);
        rules.Begin(1, 2, true); rules.Tick(100); Assert(rules.Phase == MissionPhase.Running && rules.Delivered == 0, "Practice and reset", checks);
        string path = Path.GetFullPath("Logs/save-check.json"); var store = new SaveStore(path);
        var data = new RescueSave { unlocked = 3, sensitivity = 1.2f }; data.best[2] = 400;
        Assert(store.Write(data), "Save write", checks);
        var loaded = store.Load(); Assert(loaded.unlocked == 3 && loaded.best[2] == 400, "Save round trip", checks);
        File.WriteAllText(path, "{broken"); loaded = store.Load(); Assert(loaded.unlocked == 1 && !string.IsNullOrEmpty(store.Notice), "Corrupt save recovery", checks);
        for (int missionIndex = 0; missionIndex < 4; missionIndex++)
        {
            var mission = MissionDefinition.Create(missionIndex);
            Assert(mission.pickupSites.Length == mission.cargoCount && mission.timeLimit > 0, "Mission " + missionIndex + " data valid", checks);
            foreach (var site in mission.pickupSites) Assert(site.radius >= 7 && site.radius <= 15.3f && Mathf.Abs(site.yaw) < 75, "Pickup reachable " + missionIndex, checks);
            Assert(mission.destination.radius <= 15.3f && mission.destination.height < 8, "Destination reachable " + missionIndex, checks);
            UnityEngine.Object.DestroyImmediate(mission);
        }
        File.WriteAllLines("Logs/rules-verification.txt", checks);
        Debug.Log("FROSTLINE_RULES_PASS " + checks.Count);
    }
    static void Assert(bool condition, string description, List<string> checks)
    {
        if (!condition) throw new Exception("FAIL: " + description);
        checks.Add("PASS " + description);
    }
    public static void Build()
    {
        Generate(); Verify(); Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Frostline.unity" }, locationPathName = "Builds/Windows/FrostlineRescue.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        File.WriteAllText("Logs/build-summary.txt", report.summary.result + "\nBytes: " + report.summary.totalSize + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nDuration: " + report.summary.totalTime);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        Debug.Log("FROSTLINE_BUILD_PASS");
    }
}
