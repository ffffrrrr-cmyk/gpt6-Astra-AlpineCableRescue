using UnityEngine;

namespace Frostline
{
    public sealed class CraneController : MonoBehaviour
    {
        public RescueGame Game { get; private set; }
        public Transform Turret { get; private set; }
        public Transform Boom { get; private set; }
        public Transform Tip { get; private set; }
        public Rigidbody Hook { get; private set; }
        public RescueCargo Attached { get; private set; }
        public float Yaw { get; private set; }
        public float Elevation { get; private set; }
        public float CableLength { get; private set; }
        public float Motion { get; private set; }
        public bool CableIntact { get { return rope != null; } }
        public Vector3 InputCommand;
        public bool ExternalControl;
        Rigidbody anchor;
        ConfigurableJoint rope;
        FixedJoint latch;
        LineRenderer cable;
        float yawVelocity;
        float elevationVelocity;
        float spoolVelocity;
        Transform hydraulic;
        Transform hydraulicRod;
        public float Swing
        {
            get { return Hook == null ? 0 : Vector3.Angle(Vector3.down, Hook.position - anchor.position); }
        }

        public void Initialize(RescueGame game, float yaw)
        {
            Game = game; Yaw = yaw; Elevation = 35; CableLength = 7.8f;
            Turret = transform.Find("Turret"); Boom = Turret.Find("Boom"); Tip = Boom.Find("CableTip");
            SetPose();
            var anchorObject = new GameObject("CableAnchor"); anchorObject.transform.SetParent(transform, false);
            anchor = anchorObject.AddComponent<Rigidbody>(); anchor.isKinematic = true; anchor.position = Tip.position;
            var hookObject = new GameObject("HookBlock"); hookObject.transform.SetParent(transform.parent, false);
            hookObject.transform.position = Tip.position + Vector3.down * CableLength;
            Hook = hookObject.AddComponent<Rigidbody>(); Hook.mass = 85; Hook.linearDamping = 0.7f; Hook.angularDamping = 3;
            Hook.interpolation = RigidbodyInterpolation.Interpolate; Hook.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Hook.solverIterations = 20; Hook.solverVelocityIterations = 12;
            var hookCollider = hookObject.AddComponent<SphereCollider>(); hookCollider.radius = 0.23f;
            MechanicalArt.Box("HookHousing", hookObject.transform, Vector3.zero, new Vector3(0.5f, 0.6f, 0.4f), MechanicalArt.Orange);
            MechanicalArt.Cylinder("MagneticLatch", hookObject.transform, new Vector3(0, -0.32f, 0), 0.24f, 0.17f, MechanicalArt.Teal);
            rope = hookObject.AddComponent<ConfigurableJoint>(); rope.connectedBody = anchor;
            rope.autoConfigureConnectedAnchor = false; rope.anchor = Vector3.zero; rope.connectedAnchor = Vector3.zero;
            rope.xMotion = ConfigurableJointMotion.Limited; rope.yMotion = ConfigurableJointMotion.Limited; rope.zMotion = ConfigurableJointMotion.Limited;
            rope.angularXMotion = ConfigurableJointMotion.Free; rope.angularYMotion = ConfigurableJointMotion.Free; rope.angularZMotion = ConfigurableJointMotion.Free;
            rope.linearLimit = new SoftJointLimit { limit = CableLength, bounciness = 0, contactDistance = 0.05f };
            rope.linearLimitSpring = new SoftJointLimitSpring { spring = 100000, damper = 9000 };
            rope.projectionMode = JointProjectionMode.PositionAndRotation; rope.projectionDistance = 0.15f;
            rope.breakForce = 150000; rope.breakTorque = Mathf.Infinity;
            var ropeObject = new GameObject("SteelCable"); ropeObject.transform.SetParent(transform, false);
            cable = ropeObject.AddComponent<LineRenderer>(); cable.positionCount = 2; cable.startWidth = 0.045f; cable.endWidth = 0.045f;
            cable.numCapVertices = 4; cable.sharedMaterial = MechanicalArt.Silver;
            hydraulic = MechanicalArt.Cylinder("LiftCylinder", Turret, Vector3.zero, 0.22f, 1, MechanicalArt.Orange).transform;
            hydraulicRod = MechanicalArt.Cylinder("PistonRod", Turret, Vector3.zero, 0.115f, 1, MechanicalArt.Silver).transform;
        }
        public void ReadInput()
        {
            if (ExternalControl) return;
            InputCommand = Game.Rules.Phase == MissionPhase.Running
                ? new Vector3((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0),
                    (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0)) : Vector3.zero;
        }
        public void Step(float delta)
        {
            if (Game.Rules.Phase != MissionPhase.Running) return;
            float sensitivity = Game.Save.sensitivity;
            yawVelocity = Mathf.MoveTowards(yawVelocity, InputCommand.x * 11 * sensitivity, delta * 12);
            elevationVelocity = Mathf.MoveTowards(elevationVelocity, InputCommand.y * 6 * sensitivity, delta * 9);
            spoolVelocity = Mathf.MoveTowards(spoolVelocity, InputCommand.z * 1.5f * sensitivity, delta * 2);
            Yaw = Mathf.Clamp(Yaw + yawVelocity * delta, -75, 75);
            Elevation = Mathf.Clamp(Elevation + elevationVelocity * delta, 22, 65);
            CableLength = Mathf.Clamp(CableLength + spoolVelocity * delta, 2.2f, 18);
            Motion = Mathf.Clamp01(Mathf.Abs(yawVelocity) / 11 + Mathf.Abs(elevationVelocity) / 6 + Mathf.Abs(spoolVelocity) / 1.5f);
            SetPose(); anchor.MovePosition(Tip.position);
            if (rope != null) rope.linearLimit = new SoftJointLimit { limit = CableLength, contactDistance = 0.05f };
            else Game.Rules.Fail("钢缆过载断裂。重试时降低回转速度并避开障碍。");
            if (Attached != null && latch == null)
            {
                Attached.Attached = false; Attached = null;
                Game.Rules.Fail("锁接机构因过载断开。");
            }
            if (Hook.position.y < -10) Game.Rules.Fail("吊钩已超出作业范围。");
        }
        void SetPose()
        {
            Turret.localRotation = Quaternion.Euler(0, Yaw, 0);
            Boom.localRotation = Quaternion.Euler(-Elevation, 0, 0);
        }
        public void VisualUpdate()
        {
            if (Hook == null) return;
            cable.SetPosition(0, Tip.position); cable.SetPosition(1, Hook.position + Vector3.up * 0.2f);
            var start = Turret.TransformPoint(new Vector3(0, 0.35f, 1.4f));
            var end = Boom.TransformPoint(new Vector3(0, -0.25f, 5));
            SetCylinder(hydraulic, start, Vector3.Lerp(start, end, 0.63f), 0.22f);
            SetCylinder(hydraulicRod, Vector3.Lerp(start, end, 0.55f), end, 0.115f);
            var drum = Turret.Find("WinchDrum");
            drum.localRotation = Quaternion.Euler(CableLength * 60, 0, 90);
        }
        static void SetCylinder(Transform cylinder, Vector3 start, Vector3 end, float radius)
        {
            cylinder.position = (start + end) / 2;
            cylinder.rotation = Quaternion.FromToRotation(Vector3.up, end - start);
            cylinder.localScale = new Vector3(radius * 2, Vector3.Distance(start, end) / 2, radius * 2);
        }
        public RescueCargo NearestCargo(out float distance)
        {
            distance = float.MaxValue; RescueCargo nearest = null;
            foreach (var cargo in Game.Cargo)
            {
                if (cargo.Delivered || cargo.Attached) continue;
                float candidate = Vector3.Distance(Hook.position + Vector3.down * 0.32f, cargo.LatchPosition);
                if (candidate < distance) { distance = candidate; nearest = cargo; }
            }
            return nearest;
        }
        public bool ToggleLatch()
        {
            if (Game.Rules.Phase != MissionPhase.Running) return false;
            if (Attached != null)
            {
                if (!Attached.CanRelease)
                {
                    Game.Notify("先将舱体缓慢降到当前目标平台，停稳后再解锁。"); return false;
                }
                var released = Attached; Destroy(latch); latch = null; Attached = null; released.Attached = false;
                released.ReleasedAtTarget(); Game.Sound.PlayCue(660); return true;
            }
            float distance; var target = NearestCargo(out distance);
            if (target == null || distance > 0.85f)
            {
                Game.Notify("吊钩尚未对准：将底部青色锁头移至舱顶吊环 0.85 m 内。"); return false;
            }
            if ((Hook.linearVelocity - target.Body.linearVelocity).magnitude > 1.1f)
            {
                Game.Notify("相对速度过高，松开控制，等待摆动减小。"); return false;
            }
            target.Body.isKinematic = false; target.Attached = true; Attached = target;
            latch = Hook.gameObject.AddComponent<FixedJoint>(); latch.connectedBody = target.Body;
            latch.breakForce = 110000; latch.breakTorque = 110000; latch.enableCollision = false;
            Game.Notify("锁接完成。按 Q 收缆，升高后再回转。"); Game.Sound.PlayCue(880); return true;
        }
        public void ClearMotion() { InputCommand = Vector3.zero; yawVelocity = 0; elevationVelocity = 0; spoolVelocity = 0; }
    }
}
