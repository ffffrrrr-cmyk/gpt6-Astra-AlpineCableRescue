using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Frostline
{
    public sealed class RescueGame : MonoBehaviour
    {
        public GameObject cranePrefab;
        public GameObject cargoPrefab;
        public MissionDefinition[] missions;
        public RescueRules Rules { get; private set; } = new RescueRules();
        public RescueSave Save { get; private set; }
        public SaveStore Store { get; private set; }
        public MissionDefinition Mission { get; private set; }
        public CraneController Crane { get; private set; }
        public List<RescueCargo> Cargo { get; private set; } = new List<RescueCargo>();
        public RescueAudio Sound { get; private set; }
        public RescueCamera View { get; private set; }
        public Vector3 Wind { get; private set; }
        public bool GustWarning { get; private set; }
        public string Message { get; private set; }
        public float MessageUntil { get; private set; }
        public bool Help;
        public bool Testing { get; private set; }
        Transform missionRoot;
        bool initialized;
        bool resultSaved;
        public float TotalDamage
        {
            get { float total = 0; foreach (var cargo in Cargo) total += cargo.Damage; return total; }
        }
        void Start() { Initialize(); }
        public void Initialize()
        {
            if (initialized) return; initialized = true;
            bool capture = Array.IndexOf(Environment.GetCommandLineArgs(), "--capture") >= 0;
            Testing = Array.IndexOf(Environment.GetCommandLineArgs(), "--self-test") >= 0 || capture;
            string savePath = Testing ? Path.Combine(Application.dataPath, "../Logs/test-save.json") : Path.Combine(Application.persistentDataPath, "progress-v1.json");
            Store = new SaveStore(savePath); Save = Store.Load(); ApplySettings();
            Time.fixedDeltaTime = 1f / 60; Time.maximumDeltaTime = 0.1f;
            Physics.defaultSolverIterations = 16; Physics.defaultSolverVelocityIterations = 8;
            Application.targetFrameRate = 60;
            if (missions == null || missions.Length != 4)
            {
                missions = new MissionDefinition[4]; for (int mission = 0; mission < 4; mission++) missions[mission] = MissionDefinition.Create(mission);
            }
            AlpineWorld.ConfigureLighting(); AlpineWorld.BuildEnvironment(transform);
            Sound = gameObject.AddComponent<RescueAudio>(); Sound.Initialize();
            var cameraObject = new GameObject("RescueCamera", typeof(Camera), typeof(AudioListener));
            var camera = cameraObject.GetComponent<Camera>(); camera.tag = "MainCamera"; camera.fieldOfView = 43; camera.nearClipPlane = 0.2f; camera.farClipPlane = 300;
            camera.backgroundColor = new Color(0.42f, 0.62f, 0.73f); camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            View = cameraObject.AddComponent<RescueCamera>(); View.Game = this;
            gameObject.AddComponent<RescueHud>().Game = this;
            BuildMission(0); Rules.Menu(); Time.timeScale = 0; View.Snap();
            if (capture) gameObject.AddComponent<CaptureVerification>().Game = this;
            else if (Testing) gameObject.AddComponent<RuntimeVerification>().Game = this;
            if (!string.IsNullOrEmpty(Store.Notice)) Notify(Store.Notice);
        }
        void BuildMission(int index)
        {
            if (missionRoot != null) { missionRoot.gameObject.SetActive(false); Destroy(missionRoot.gameObject); }
            Mission = missions[Mathf.Clamp(index, 0, 3)]; Cargo.Clear();
            missionRoot = MechanicalArt.Group("Mission_" + index, transform, Vector3.zero);
            AlpineWorld.BuildMission(missionRoot, Mission);
            var craneObject = cranePrefab != null ? Instantiate(cranePrefab, missionRoot) : MechanicalArt.BuildCrane();
            craneObject.transform.SetParent(missionRoot, false);
            Crane = craneObject.GetComponent<CraneController>(); Crane.Initialize(this, Mission.pickupSites[0].yaw);
            for (int cargoIndex = 0; cargoIndex < Mission.cargoCount; cargoIndex++)
            {
                var pod = cargoPrefab != null ? Instantiate(cargoPrefab, missionRoot) : MechanicalArt.BuildPod();
                pod.transform.SetParent(missionRoot, false); pod.transform.position = Mission.pickupSites[cargoIndex].Center + Vector3.up * 1.06f;
                var cargo = pod.GetComponent<RescueCargo>(); cargo.Initialize(this, cargoIndex); Cargo.Add(cargo);
            }
            Wind = Vector3.zero; resultSaved = false;
        }
        public void Begin(int index, bool practice)
        {
            if (!Testing && index >= Save.unlocked) { Notify("先完成上一项任务解锁。"); return; }
            Time.timeScale = 1; Help = false; BuildMission(index); Rules.Begin(Mission.cargoCount, Mission.timeLimit, practice);
            View.Mode = 0; View.Snap(); Notify("先按 E 放缆，接近舱顶青色吊环。Space 锁接。");
            if (!Save.tutorialSeen && !Testing) { Save.tutorialSeen = true; Help = true; Pause(true); Store.Write(Save); }
        }
        void Update()
        {
            if (!initialized) return;
            if (!Testing)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (Rules.Phase == MissionPhase.Running) Pause(true);
                    else if (Rules.Phase == MissionPhase.Paused) { Help = false; Pause(false); }
                }
                if (Input.GetKeyDown(KeyCode.C)) View.Mode = (View.Mode + 1) % 3;
                if (Input.GetKeyDown(KeyCode.H) && Rules.Phase == MissionPhase.Running) { Help = true; Pause(true); }
                if (Input.GetKeyDown(KeyCode.R) && Rules.Phase != MissionPhase.Menu) Begin(Mission.index, Rules.Practice);
                if (Input.GetKeyDown(KeyCode.Space) && Rules.Phase == MissionPhase.Running) Crane.ToggleLatch();
            }
            Crane.ReadInput();
            if (Rules.Phase == MissionPhase.Won && !resultSaved)
            {
                resultSaved = true;
                if (!Rules.Practice)
                {
                    Save.unlocked = Mathf.Max(Save.unlocked, Mathf.Min(4, Mission.index + 2));
                    Save.best[Mission.index] = Mathf.Max(Save.best[Mission.index], Rules.Score(TotalDamage));
                    Store.Write(Save);
                }
                Sound.PlayCue(1100, 0.4f);
            }
            if (Rules.Phase == MissionPhase.Lost || Rules.Phase == MissionPhase.Won) Time.timeScale = 0;
            Sound.UpdateMotor(Crane.Motion, Wind.magnitude, Rules.Phase == MissionPhase.Running);
        }
        void FixedUpdate()
        {
            if (Rules.Phase != MissionPhase.Running) return;
            float delta = Time.fixedDeltaTime; Rules.Tick(delta);
            float phase = (Rules.Elapsed % Mission.gustPeriod) / Mission.gustPeriod;
            GustWarning = Mission.gustWind > 0 && phase > 0.48f && phase < 0.68f;
            float gust = phase > 0.68f && phase < 0.94f ? Mathf.Sin((phase - 0.68f) / 0.26f * Mathf.PI) * Mission.gustWind : 0;
            Wind = new Vector3(Mission.steadyWind + gust, 0, (Mission.steadyWind + gust) * Mathf.Sin(Rules.Elapsed * 0.18f) * 0.17f);
            Crane.Step(delta);
            foreach (var cargo in Cargo) cargo.Step(delta, Wind);
        }
        void LateUpdate() { if (Crane != null) Crane.VisualUpdate(); }
        public void Deliver(RescueCargo cargo)
        {
            if (!Rules.Deliver(cargo.Id)) return;
            Notify("接应完成 · " + Rules.Delivered + " / " + Rules.Required);
            if (Rules.Delivered < Rules.Required)
            {
                cargo.transform.position += new Vector3(3.5f, 0, 0);
                Notify("第一舱已转移到安全区。返回另一座橙色平台接应。");
            }
        }
        public void Pause(bool paused) { Rules.Pause(paused); Crane.ClearMotion(); Time.timeScale = paused ? 0 : 1; }
        public void Menu() { Help = false; Rules.Menu(); Crane.ClearMotion(); Time.timeScale = 0; }
        public void Notify(string message) { Message = message; MessageUntil = Time.unscaledTime + 6; }
        public void ApplySettings()
        {
            AudioListener.volume = Save.volume;
            QualitySettings.shadows = Save.quality == 0 ? UnityEngine.ShadowQuality.Disable : UnityEngine.ShadowQuality.All;
            QualitySettings.shadowResolution = Save.quality == 2 ? UnityEngine.ShadowResolution.VeryHigh : UnityEngine.ShadowResolution.High;
            QualitySettings.shadowDistance = Save.quality == 0 ? 0 : 100;
            QualitySettings.vSyncCount = 0;
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                pipeline.renderScale = Save.quality == 0 ? 0.8f : 1;
                pipeline.shadowDistance = Save.quality == 0 ? 0 : 110;
                pipeline.mainLightShadowmapResolution = Save.quality == 2 ? 4096 : 2048;
                pipeline.msaaSampleCount = Save.quality == 0 ? 1 : 4;
            }
        }
        public void ResetProgress() { Save.unlocked = 1; Save.best = new int[4]; Save.tutorialSeen = false; Store.Write(Save); }
        void OnApplicationFocus(bool focused) { if (!focused && initialized && !Testing && Rules.Phase == MissionPhase.Running) Pause(true); }
        void OnDestroy() { Time.timeScale = 1; }
    }
}
