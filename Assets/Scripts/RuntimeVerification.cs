using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Frostline
{
    public sealed class RuntimeVerification : MonoBehaviour
    {
        public RescueGame Game;
        readonly List<string> checks = new List<string>();
        string output;
        bool failed;
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs")); Directory.CreateDirectory(output);
            yield return null;
            Record(true, "Runtime started; graphics=" + SystemInfo.graphicsDeviceName + "; OS=" + SystemInfo.operatingSystem);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "menu.png"));
            yield return new WaitForSecondsRealtime(0.5f);
            Game.Save.unlocked = 4;
            Game.Begin(0, true); Game.Crane.ExternalControl = true;
            yield return new WaitForSeconds(1);
            float elapsed = Game.Rules.Elapsed; Vector3 hook = Game.Crane.Hook.position;
            Game.Pause(true); yield return new WaitForSecondsRealtime(0.3f);
            Record(Game.Rules.Elapsed == elapsed && Vector3.Distance(hook, Game.Crane.Hook.position) < 0.001f, "Pause freezes physical hook and mission clock");
            Game.Pause(false);
            for (int missionIndex = 0; missionIndex < 4; missionIndex++)
            {
                Game.Begin(missionIndex, true); Game.Crane.ExternalControl = true;
                Time.timeScale = 6;
                yield return new WaitForSeconds(1);
                Record(Game.Cargo.Count == Game.Mission.cargoCount && Game.Crane.Attached == null, "Mission " + missionIndex + " reset state");
                for (int cargoIndex = 0; cargoIndex < Game.Cargo.Count; cargoIndex++)
                {
                    var cargo = Game.Cargo[cargoIndex];
                    yield return MoveTo(Game.Mission.pickupSites[cargoIndex], 7, false);
                    yield return ApproachCargo(cargo);
                    bool attached = Game.Crane.ToggleLatch();
                    Record(attached && Game.Crane.Attached == cargo, "Mission " + missionIndex + " cargo " + cargoIndex + " low-speed physical latch");
                    if (!attached) { Finish(); yield break; }
                    if (Game.Mission.transfer)
                    {
                        yield return Transport(cargo, Game.Mission.relay);
                        Record(cargo.RelayDone && !cargo.Delivered, "Relay release preserves pending final delivery");
                        yield return new WaitForSeconds(1.5f);
                        Record(Game.Crane.ToggleLatch(), "Relay reattach");
                    }
                    yield return Transport(cargo, Game.Mission.destination);
                    Record(cargo.Delivered, "Mission " + missionIndex + " cargo " + cargoIndex + " stable delivery");
                    if (!cargo.Delivered) { Finish(); yield break; }
                }
                Record(Game.Rules.Phase == MissionPhase.Won, "Mission " + missionIndex + " complete physical route");
            }
            Time.timeScale = 1; Game.Begin(1, true); Game.Crane.ExternalControl = true;
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "gameplay.png"));
            yield return new WaitForSecondsRealtime(0.5f); Finish();
        }
        IEnumerator ApproachCargo(RescueCargo cargo)
        {
            float deadline = Time.unscaledTime + 25;
            while (Time.unscaledTime < deadline && Game.Rules.Phase == MissionPhase.Running)
            {
                Vector3 target = cargo.LatchPosition + Vector3.up * 0.32f;
                float radius = new Vector2(target.x, target.z).magnitude;
                float yaw = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;
                float elevation = Mathf.Acos(Mathf.Clamp(radius / 16.5f, -1, 1)) * Mathf.Rad2Deg;
                float length = Game.Crane.Tip.position.y - target.y;
                Drive(yaw, elevation, length);
                if (Vector3.Distance(Game.Crane.Hook.position, target) < 0.7f && Game.Crane.Hook.linearVelocity.magnitude < 0.8f) { Game.Crane.ClearMotion(); yield return new WaitForSeconds(0.6f); yield break; }
                yield return null;
            }
            Record(false, "Approach timeout; hook=" + Game.Crane.Hook.position + "; target=" + cargo.LatchPosition);
        }
        IEnumerator MoveTo(LandingSite site, float length, bool loaded)
        {
            float deadline = Time.unscaledTime + 25;
            float elevation = Mathf.Acos(Mathf.Clamp(site.radius / 16.5f, -1, 1)) * Mathf.Rad2Deg;
            while (Time.unscaledTime < deadline && Game.Rules.Phase == MissionPhase.Running)
            {
                Drive(site.yaw, elevation, length);
                if (Mathf.Abs(Game.Crane.Yaw - site.yaw) < 0.35f && Mathf.Abs(Game.Crane.Elevation - elevation) < 0.25f && Mathf.Abs(Game.Crane.CableLength - length) < 0.08f)
                { Game.Crane.ClearMotion(); yield return new WaitForSeconds(loaded ? 3 : 1); yield break; }
                yield return null;
            }
            Record(false, "Move timeout " + site.yaw);
        }
        IEnumerator Transport(RescueCargo cargo, LandingSite site)
        {
            yield return MoveTo(new LandingSite(Game.Crane.Yaw, 16.5f * Mathf.Cos(Game.Crane.Elevation * Mathf.Deg2Rad)), 3.6f, true);
            yield return MoveTo(site, 3.6f, true);
            float offset = Game.Crane.Hook.position.y - cargo.transform.position.y;
            float desiredLength = Game.Crane.Tip.position.y - (site.height + 1.04f + offset);
            float deadline = Time.unscaledTime + 30;
            while (Time.unscaledTime < deadline && Game.Rules.Phase == MissionPhase.Running && !cargo.CanRelease)
            {
                float compensation = Game.Wind.x * 0.08f;
                float yaw = site.yaw - compensation;
                Drive(yaw, Mathf.Acos(site.radius / 16.5f) * Mathf.Rad2Deg, desiredLength + 0.12f);
                yield return null;
            }
            Game.Crane.ClearMotion();
            Record(Game.Crane.ToggleLatch(), "Stable supported release; damage=" + cargo.Damage.ToString("0.0"));
            Time.timeScale = 6;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        void Drive(float yaw, float elevation, float length)
        {
            Game.Crane.InputCommand = new Vector3(Mathf.Clamp((yaw - Game.Crane.Yaw) * 0.18f, -0.65f, 0.65f), Mathf.Clamp((elevation - Game.Crane.Elevation) * 0.25f, -0.7f, 0.7f), Mathf.Clamp((length - Game.Crane.CableLength) * 0.75f, -0.8f, 0.8f));
        }
        void Record(bool passed, string description)
        {
            string entry = (passed ? "PASS " : "FAIL ") + description; checks.Add(entry); Debug.Log(entry);
            if (!passed) failed = true;
            File.WriteAllLines(Path.Combine(output, "runtime-verification.txt"), checks);
        }
        void Finish()
        {
            Time.timeScale = 1; File.WriteAllLines(Path.Combine(output, "runtime-verification.txt"), checks);
            Debug.Log(failed ? "FROSTLINE_RUNTIME_FAIL" : "FROSTLINE_RUNTIME_PASS"); Application.Quit(failed ? 1 : 0);
        }
    }
}
