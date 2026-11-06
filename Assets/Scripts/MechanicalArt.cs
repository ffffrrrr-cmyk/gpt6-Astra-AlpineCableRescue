using System.Collections.Generic;
using UnityEngine;

namespace Frostline
{
    public static class MechanicalArt
    {
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static Mesh bevelMesh;
        public static Material Metal(string name, Color color, float metallic = 0.4f, float smoothness = 0.5f, bool emission = false)
        {
            if (materials.TryGetValue(name, out var existing) && existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2);
            }
            materials[name] = material;
            return material;
        }
        public static Material Orange { get { return Metal("SafetyOchre", new Color(0.94f, 0.37f, 0.09f), 0.55f, 0.44f); } }
        public static Material Steel { get { return Metal("GraphiteSteel", new Color(0.105f, 0.15f, 0.18f), 0.8f, 0.56f); } }
        public static Material Silver { get { return Metal("BrushedAlloy", new Color(0.55f, 0.64f, 0.67f), 0.85f, 0.68f); } }
        public static Material Snow { get { return Metal("FreshSnow", new Color(0.79f, 0.88f, 0.89f), 0, 0.18f); } }
        public static Material Teal { get { return Metal("SignalCyan", new Color(0.1f, 0.8f, 0.73f), 0.3f, 0.6f, true); } }
        public static Material Glass { get { return Metal("SmokedBlueGlass", new Color(0.035f, 0.13f, 0.18f), 0.72f, 0.95f); } }
        public static Transform Group(string name, Transform parent, Vector3 position)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false); group.localPosition = position; return group;
        }
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = false)
        {
            if (bevelMesh == null) bevelMesh = BuildBevelCube();
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = size;
            part.GetComponent<MeshFilter>().sharedMesh = bevelMesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (collider) part.AddComponent<BoxCollider>();
            return part;
        }
        public static GameObject Cylinder(string name, Transform parent, Vector3 position, float radius, float length, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.name = name; part.transform.SetParent(parent, false); part.transform.localPosition = position;
            part.transform.localScale = new Vector3(radius * 2, length * 0.5f, radius * 2);
            part.GetComponent<Renderer>().sharedMaterial = material; return part;
        }
        public static GameObject Bar(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            var part = Cylinder(name, parent, (start + end) * 0.5f, radius, Vector3.Distance(start, end), material);
            part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start); return part;
        }
        public static void Ring(string name, Transform parent, Vector3 center, float radius, float tube, Material material, int count = 24)
        {
            var root = Group(name, parent, center);
            for (int segment = 0; segment < count; segment++)
            {
                float angle = segment * Mathf.PI * 2 / count;
                float next = (segment + 1) * Mathf.PI * 2 / count;
                Bar("Arc", root, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius,
                    new Vector3(Mathf.Cos(next), 0, Mathf.Sin(next)) * radius, tube, material);
            }
        }
        public static Mesh BuildBevelCube()
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            Vector3[] directions = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            float[] divisions = { -0.5f, -0.47f, -0.43f, 0.43f, 0.47f, 0.5f };
            foreach (var normal in directions)
            {
                var across = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                var vertical = Vector3.Cross(normal, across);
                int offset = vertices.Count;
                for (int row = 0; row < 6; row++)
                for (int column = 0; column < 6; column++)
                {
                    var point = normal * 0.5f + across * divisions[column] + vertical * divisions[row];
                    var inner = new Vector3(Mathf.Clamp(point.x, -0.43f, 0.43f), Mathf.Clamp(point.y, -0.43f, 0.43f), Mathf.Clamp(point.z, -0.43f, 0.43f));
                    var outward = (point - inner).normalized;
                    vertices.Add(inner + outward * 0.07f); normals.Add(outward); uv.Add(new Vector2(column / 5f, row / 5f));
                }
                for (int row = 0; row < 5; row++)
                for (int column = 0; column < 5; column++)
                {
                    int corner = offset + row * 6 + column;
                    triangles.Add(corner); triangles.Add(corner + 1); triangles.Add(corner + 6);
                    triangles.Add(corner + 1); triangles.Add(corner + 7); triangles.Add(corner + 6);
                }
            }
            var mesh = new Mesh { name = "MachinedBevelCube" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); return mesh;
        }
        public static GameObject BuildCrane()
        {
            var root = new GameObject("SR7_AlpineCrane");
            Box("Chassis", root.transform, new Vector3(0, 0.75f, 0), new Vector3(3.4f, 1.2f, 3.8f), Steel);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("OutriggerBeam", root.transform, new Vector3(side * 2, 0.6f, 0), new Vector3(3.4f, 0.35f, 0.55f), Orange);
                for (int end = -1; end <= 1; end += 2)
                {
                    Bar("Stabilizer", root.transform, new Vector3(side * 2.8f, 0.4f, end * 1.4f), new Vector3(side * 1.2f, 1, end * 1), 0.15f, Silver);
                    Cylinder("Foot", root.transform, new Vector3(side * 2.8f, 0.05f, end * 1.4f), 0.5f, 0.18f, Steel);
                }
            }
            Cylinder("SlewBearing", root.transform, new Vector3(0, 1.55f, 0), 1.45f, 0.4f, Silver);
            var turret = Group("Turret", root.transform, new Vector3(0, 1.8f, 0));
            Box("EngineHousing", turret, new Vector3(0.5f, 0.8f, -1.3f), new Vector3(2.2f, 1.4f, 2.4f), Orange);
            Box("Counterweight", turret, new Vector3(0, 0.6f, -3), new Vector3(3.1f, 1.1f, 1.2f), Steel);
            var cabin = Group("OperatorCab", turret, new Vector3(-1.5f, 1.2f, 0.2f));
            Box("CabShell", cabin, Vector3.zero, new Vector3(1.5f, 2.1f, 2.2f), Orange);
            Box("FrontGlass", cabin, new Vector3(0, 0.25f, 1.09f), new Vector3(1.25f, 1.15f, 0.055f), Glass);
            Box("SideGlass", cabin, new Vector3(-0.75f, 0.25f, 0.25f), new Vector3(0.055f, 1.15f, 1.5f), Glass);
            Box("Roof", cabin, new Vector3(0, 1.13f, 0), new Vector3(1.7f, 0.2f, 2.35f), Snow);
            for (int vent = 0; vent < 8; vent++) Box("RadiatorFin", turret, new Vector3(1.62f, 0.55f + vent * 0.11f, -1.3f), new Vector3(0.07f, 0.045f, 1.6f), Steel);
            for (int step = 0; step < 5; step++) Box("AccessStep", turret, new Vector3(-2.2f, -1 + step * 0.35f, -0.5f), new Vector3(0.55f, 0.09f, 0.4f), Silver);
            Bar("LeftTrunnion", turret, new Vector3(-0.65f, 0, 0), new Vector3(-0.65f, 3.2f, 0), 0.32f, Orange);
            Bar("RightTrunnion", turret, new Vector3(0.65f, 0, 0), new Vector3(0.65f, 3.2f, 0), 0.32f, Orange);
            var boom = Group("Boom", turret, new Vector3(0, 3.2f, 0));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int edge = -1; edge <= 1; edge += 2) Bar("Chord", boom, new Vector3(side * 0.5f, edge * 0.4f, 0), new Vector3(side * 0.22f, edge * 0.2f, 16.5f), 0.095f, Orange);
                for (int bay = 0; bay < 11; bay++)
                {
                    float start = bay * 1.5f; float finish = start + 1.5f;
                    float widthStart = Mathf.Lerp(0.5f, 0.22f, start / 16.5f);
                    float widthEnd = Mathf.Lerp(0.5f, 0.22f, finish / 16.5f);
                    Bar("LatticeDiagonal", boom, new Vector3(side * widthStart, bay % 2 == 0 ? -0.36f : 0.36f, start), new Vector3(side * widthEnd, bay % 2 == 0 ? 0.36f : -0.36f, finish), 0.055f, Silver);
                    Bar("CrossTie", boom, new Vector3(-widthStart, 0.36f, start), new Vector3(widthStart, 0.36f, start), 0.065f, Orange);
                }
            }
            var wheel = Cylinder("TipSheave", boom, new Vector3(0, 0, 16.5f), 0.4f, 0.5f, Steel);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            Group("CableTip", boom, new Vector3(0, -0.15f, 16.5f));
            var drum = Cylinder("WinchDrum", turret, new Vector3(0, 1.3f, -0.7f), 0.55f, 1.2f, Silver);
            drum.transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int bolt = 0; bolt < 12; bolt++)
            {
                float angle = bolt * Mathf.PI / 6;
                Cylinder("BearingBolt", root.transform, new Vector3(Mathf.Cos(angle) * 1.2f, 1.79f, Mathf.Sin(angle) * 1.2f), 0.075f, 0.1f, Steel);
            }
            Box("SR7Badge", cabin, new Vector3(0, -0.65f, 1.13f), new Vector3(0.9f, 0.2f, 0.03f), Snow);
            root.AddComponent<CraneController>(); return root;
        }
        public static GameObject BuildPod()
        {
            var root = new GameObject("RescuePod");
            Box("InsulatedHull", root.transform, Vector3.zero, new Vector3(1.75f, 1.65f, 2.3f), Orange);
            Box("Roof", root.transform, new Vector3(0, 0.88f, 0), new Vector3(1.9f, 0.22f, 2.4f), Snow);
            Box("Undercarriage", root.transform, new Vector3(0, -0.87f, 0), new Vector3(1.6f, 0.23f, 2.1f), Steel);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Skid", root.transform, new Vector3(side * 0.64f, -1, 0), new Vector3(0.16f, 0.13f, 2.6f), Silver);
                Box("SideWindow", root.transform, new Vector3(side * 0.88f, 0.22f, 0), new Vector3(0.04f, 0.7f, 1.65f), Glass);
                Bar("GrabRail", root.transform, new Vector3(side * 0.95f, -0.48f, -0.8f), new Vector3(side * 0.95f, -0.48f, 0.8f), 0.035f, Silver);
                for (int end = -1; end <= 1; end += 2)
                {
                    Bar("LiftingBridle", root.transform, new Vector3(side * 0.65f, 0.95f, end * 0.7f), new Vector3(0, 1.6f, 0), 0.04f, Silver);
                    Cylinder("RoofBolt", root.transform, new Vector3(side * 0.65f, 1, end * 0.85f), 0.07f, 0.08f, Steel);
                }
            }
            Box("DoorWindow", root.transform, new Vector3(0, 0.25f, 1.16f), new Vector3(1.25f, 0.65f, 0.04f), Glass);
            Box("DoorSeam", root.transform, new Vector3(0, 0, 1.2f), new Vector3(0.035f, 1.4f, 0.025f), Silver);
            Box("RescueCrossH", root.transform, new Vector3(0, -0.43f, 1.2f), new Vector3(0.55f, 0.13f, 0.04f), Snow);
            Box("RescueCrossV", root.transform, new Vector3(0, -0.43f, 1.2f), new Vector3(0.13f, 0.45f, 0.04f), Snow);
            Cylinder("LatchEye", root.transform, new Vector3(0, 1.6f, 0), 0.17f, 0.14f, Teal);
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(1.78f, 2.05f, 2.4f); collider.center = new Vector3(0, -0.03f, 0);
            var body = root.AddComponent<Rigidbody>(); body.mass = 320; body.linearDamping = 0.4f; body.angularDamping = 2.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.solverIterations = 16; body.solverVelocityIterations = 8;
            root.AddComponent<RescueCargo>(); return root;
        }
    }
}
