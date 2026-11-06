using System;
using UnityEngine;

namespace Frostline
{
    [Serializable]
    public struct LandingSite
    {
        public float yaw;
        public float radius;
        public float height;
        public Vector3 Center { get { return Quaternion.Euler(0, yaw, 0) * Vector3.forward * radius + Vector3.up * height; } }
        public LandingSite(float yaw, float radius, float height = 1.8f)
        {
            this.yaw = yaw; this.radius = radius; this.height = height;
        }
    }
    [CreateAssetMenu(menuName = "Frostline/Mission")]
    public sealed class MissionDefinition : ScriptableObject
    {
        public int index;
        public string title;
        [TextArea] public string briefing;
        public string weather;
        public float timeLimit;
        public float steadyWind;
        public float gustWind;
        public float gustPeriod;
        public int cargoCount;
        public LandingSite[] pickupSites;
        public LandingSite destination;
        public bool transfer;
        public LandingSite relay;
        public Vector3[] obstacles;
        public static MissionDefinition Create(int index)
        {
            var mission = CreateInstance<MissionDefinition>();
            mission.index = index; mission.gustPeriod = 24; mission.cargoCount = 1;
            mission.pickupSites = new[] { new LandingSite(-38, 13.4f) };
            mission.destination = new LandingSite(45, 13.2f);
            mission.obstacles = new Vector3[0];
            switch (index)
            {
                case 0:
                    mission.title = "01  /  初次接应";
                    mission.briefing = "风停了。把吊钩降到橙色救援舱上方，锁接后升至安全高度，送入右侧青色接应区。";
                    mission.weather = "无风 · 视野清晰"; mission.timeLimit = 360; break;
                case 1:
                    mission.title = "02  /  岩隙横风";
                    mission.briefing = "侧风正在将载荷推向岩壁。先升高越过中部岩脊，再缓慢转向接应区；急停会加大摆幅。";
                    mission.weather = "西风 · 4.5 m/s"; mission.steadyWind = 4.5f; mission.timeLimit = 330;
                    mission.pickupSites[0] = new LandingSite(-48, 12.8f);
                    mission.obstacles = new[] { new Vector3(0, 3.4f, 12.5f) }; break;
                case 2:
                    mission.title = "03  /  中继转运";
                    mission.briefing = "先将救援舱停稳在琥珀色中继台并解锁。完成检查后重新锁接，再送往更高的接应平台。";
                    mission.weather = "轻风 · 双阶段作业"; mission.steadyWind = 2; mission.timeLimit = 480;
                    mission.transfer = true; mission.relay = new LandingSite(0, 13.5f, 2.6f);
                    mission.destination = new LandingSite(52, 12, 4.2f); break;
                default:
                    mission.title = "04  /  阵风救援";
                    mission.briefing = "两座故障缆车等待接应。阵风来临前仪表会预警，降低操作幅度；每个救援舱须单独落稳。";
                    mission.weather = "阵风 · 双舱救援"; mission.cargoCount = 2;
                    mission.pickupSites = new[] { new LandingSite(-52, 12.8f), new LandingSite(-18, 14.2f) };
                    mission.destination = new LandingSite(51, 12.9f);
                    mission.steadyWind = 2.8f; mission.gustWind = 5.2f; mission.timeLimit = 540;
                    mission.obstacles = new[] { new Vector3(3.4f, 2.1f, 14.8f) }; break;
            }
            return mission;
        }
    }
}
