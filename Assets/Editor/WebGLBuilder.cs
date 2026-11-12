using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 命令行 WebGL 打包：Unity.exe -quit -batchmode -projectPath <proj> -executeMethod WebGLBuilder.Build -logFile <log>
public static class WebGLBuilder
{
    public static void Build()
    {
        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(projectRoot, "Builds", "WebGL");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            // U1 场景体量大，默认几何增长会触发 wasm 堆 OOM；改为线性增长 + 增量 GC
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Linear;
            PlayerSettings.gcIncremental = true;
            // 默认 wasm 堆上限 2GB 不够用，放宽到 4GB
            PlayerSettings.WebGL.emscriptenArgs = "-s MAXIMUM_MEMORY=4GB";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Release);
            PlayerSettings.runInBackground = true;
            // 引擎代码剥离依据托管层保留结果裁剪；保留清单见 Assets/link.xml
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WebGLBuilder] 没有启用中的场景");
                EditorApplication.Exit(2);
                return;
            }
            Debug.Log("[WebGLBuilder] scenes: " + string.Join(", ", scenes));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log("[WebGLBuilder] result=" + summary.result + " size=" + summary.totalSize + " errors=" + summary.totalErrors + " time=" + summary.totalTime);

            if (summary.result == BuildResult.Succeeded)
            {
                foreach (var f in Directory.GetFiles(outDir, "*", SearchOption.AllDirectories))
                    Debug.Log("[WebGLBuilder] out " + new FileInfo(f).Length + " " + f);
                EditorApplication.Exit(0);
            }
            else
            {
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[WebGLBuilder] 异常: " + e);
            EditorApplication.Exit(3);
        }
    }
}
