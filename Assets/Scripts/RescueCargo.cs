using UnityEngine;

namespace Frostline
{
    public sealed class RescueCargo : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public RescueGame Game { get; private set; }
        public int Id { get; private set; }
        public bool Attached;
        public bool Delivered { get; private set; }
        public bool RelayDone { get; private set; }
        public float Damage { get; private set; }
        public float StableSeconds { get; private set; }
        public Vector3 LatchPosition { get { return transform.TransformPoint(new Vector3(0, 1.6f, 0)); } }
        public LandingSite Target { get { return Game.Mission.transfer && !RelayDone ? Game.Mission.relay : Game.Mission.destination; } }
        public bool CanRelease { get { return StableSeconds >= 1.2f; } }
        public void Initialize(RescueGame game, int id)
        {
            Game = game; Id = id; Body = GetComponent<Rigidbody>(); Body.isKinematic = true;
        }
        public void Step(float delta, Vector3 wind)
        {
            if (Delivered || Game.Rules.Phase != MissionPhase.Running) return;
            if (!Body.isKinematic) Body.AddForce(wind * 52, ForceMode.Force);
            Vector3 target = Target.Center;
            Vector3 local = transform.position - target;
            bool inArea = new Vector2(local.x, local.z).magnitude < 1.55f;
            bool onDeck = Mathf.Abs(local.y - 1.06f) < 0.24f;
            bool upright = Vector3.Angle(transform.up, Vector3.up) < 13;
            bool stopped = Body.linearVelocity.magnitude < 0.48f && Body.angularVelocity.magnitude < 0.4f;
            StableSeconds = inArea && onDeck && upright && stopped ? StableSeconds + delta : 0;
            if (transform.position.y < -8 || transform.position.sqrMagnitude > 3000) Game.Rules.Fail("救援舱离开安全作业范围。");
            if (Damage >= 100) Game.Rules.Fail("救援舱损伤过重。尝试避开岩壁并减小落地速度。");
        }
        public void ReleasedAtTarget()
        {
            if (!CanRelease || Delivered) return;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero; Body.isKinematic = true;
            if (Game.Mission.transfer && !RelayDone)
            {
                RelayDone = true; StableSeconds = 0;
                Game.Notify("中继检查完成。再次锁接，送往右侧高台。"); return;
            }
            Delivered = true; Game.Deliver(this);
        }
        void OnCollisionEnter(Collision collision)
        {
            if (Game == null || Game.Rules.Phase != MissionPhase.Running || Delivered || Body.isKinematic) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed <= 2.4f) return;
            Damage = Mathf.Clamp(Damage + (speed - 2.4f) * 9, 0, 100);
            Game.Sound.PlayCue(130, 0.2f); Game.Notify("碰撞警告 · 舱体损伤 " + Mathf.RoundToInt(Damage) + "%");
        }
    }
}
