using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Frostline
{
    public sealed class CaptureVerification : MonoBehaviour
    {
        public RescueGame Game;
        bool captureContentValid = true;
        IEnumerator Start()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
            Directory.CreateDirectory(output);
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(3);
            yield return new WaitForEndOfFrame();
            yield return Capture(output, "menu");
            yield return new WaitForSecondsRealtime(1);
            Game.Save.unlocked = 4; Game.Save.tutorialSeen = true;
            for (int mission = 0; mission < 4; mission++)
            {
                Game.Begin(mission, true); Game.Crane.ExternalControl = true;
                yield return new WaitForSecondsRealtime(2);
                yield return new WaitForEndOfFrame();
                yield return Capture(output, "mission-" + mission);
                yield return new WaitForSecondsRealtime(0.5f);
            }
            var frames = new List<float>(); float end = Time.realtimeSinceStartup + 12;
            while (Time.realtimeSinceStartup < end)
            {
                frames.Add(Time.unscaledDeltaTime); yield return null;
            }
            frames.Sort(); float total = 0; foreach (float frame in frames) total += frame;
            string report = "Capture validity: " + (captureContentValid ? "nonblack frames observed; visual review required" : "INVALID: hidden/black frames; FPS is not a rendering-performance result")
                + "\nGPU: " + SystemInfo.graphicsDeviceName + "\nOS: " + SystemInfo.operatingSystem + "\nResolution: " + Screen.width + "x" + Screen.height
                + "\nQuality: standard; target 60 FPS\nScenario: Mission 4 idle, two cargo bodies on pickup decks, static overview\nSamples: " + frames.Count
                + "\nAverage FPS: " + (frames.Count / total).ToString("0.0") + "\n95th-percentile frame ms: " + (frames[Mathf.Min(frames.Count - 1, Mathf.FloorToInt(frames.Count * 0.95f))] * 1000).ToString("0.00")
                + "\nThis is an automated idle-scene sample, not a full gameplay performance guarantee.\n";
            File.WriteAllText(Path.Combine(output, "performance.txt"), report);
            Debug.Log((captureContentValid ? "FROSTLINE_CAPTURE_PASS\n" : "FROSTLINE_CAPTURE_ENVIRONMENT_LIMITED\n") + report);
            Application.Quit(captureContentValid ? 0 : 2);
        }
        IEnumerator Capture(string output, string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null) { Debug.LogError("CAPTURE_EMPTY " + name); yield break; }
            var pixels = texture.GetPixels32(); int nonblack = 0;
            for (int pixel = 0; pixel < pixels.Length; pixel += 41)
                if (pixels[pixel].r > 12 || pixels[pixel].g > 12 || pixels[pixel].b > 12) nonblack++;
            bool valid = nonblack > pixels.Length / 410;
            captureContentValid &= valid;
            Debug.Log((valid ? "CAPTURE_CONTENT_PASS " : "CAPTURE_BLACK_FAIL ") + name);
            File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            Destroy(texture);
        }
    }
}
