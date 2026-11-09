using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Frostline
{
    public static class AlpineVegetation
    {
        public const int Seed = 6117;
        public static Vector3 DetailTarget { get; private set; }
        sealed class Geometry
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();
            public void Triangle(Vector3 first, Vector3 second, Vector3 third)
            {
                int offset = Vertices.Count;
                Vertices.Add(first); Vertices.Add(second); Vertices.Add(third);
                Triangles.Add(offset); Triangles.Add(offset + 1); Triangles.Add(offset + 2);
            }
            public void Tube(Vector3 start, Vector3 end, float firstRadius, float lastRadius)
            {
                Vector3 direction = (end - start).normalized;
                Vector3 across = Vector3.Cross(direction, Mathf.Abs(direction.y) > 0.95f ? Vector3.right : Vector3.up).normalized;
                Vector3 vertical = Vector3.Cross(direction, across);
                for (int side = 0; side < 6; side++)
                {
                    float angle = side * Mathf.PI / 3; float next = (side + 1) * Mathf.PI / 3;
                    Vector3 edge = across * Mathf.Cos(angle) + vertical * Mathf.Sin(angle);
                    Vector3 nextEdge = across * Mathf.Cos(next) + vertical * Mathf.Sin(next);
                    Triangle(start + edge * firstRadius, end + nextEdge * lastRadius, end + edge * lastRadius);
                    Triangle(start + edge * firstRadius, start + nextEdge * firstRadius, end + nextEdge * lastRadius);
                }
            }
            public void SnowMound(Vector3 center, Vector3 along, float length, float width, float thickness, float phase)
            {
                Vector3 across = Vector3.Cross(Vector3.up, along).normalized;
                int offset = Vertices.Count;
                for (int segment = 0; segment <= 10; segment++)
                for (int side = 0; side <= 12; side++) Vertices.Add(SnowPoint(segment, side));
                for (int segment = 0; segment < 10; segment++)
                for (int side = 0; side < 12; side++)
                {
                    int corner = offset + segment * 13 + side;
                    Triangles.Add(corner); Triangles.Add(corner + 14); Triangles.Add(corner + 13);
                    Triangles.Add(corner); Triangles.Add(corner + 1); Triangles.Add(corner + 14);
                }
                Vector3 SnowPoint(int segment, int side)
                {
                    float progress = segment / 10f; float angle = side * Mathf.PI / 6;
                    float radius = Mathf.Pow(Mathf.Max(0.001f, Mathf.Sin(progress * Mathf.PI)), 0.55f) * (0.95f + Mathf.Sin(progress * 13 + Mathf.Sin(angle) * 2 + phase) * 0.05f);
                    return center + along * ((progress - 0.5f) * length) + across * (Mathf.Cos(angle) * width * radius)
                        + Vector3.up * (Mathf.Sin(angle) * thickness * radius - Mathf.Pow(progress, 2) * thickness * 0.6f);
                }
            }
            public void Publish(Transform parent, string name, Material material)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(Vertices); mesh.SetTriangles(Triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(parent, false);
                part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<Renderer>().sharedMaterial = material;
            }
        }
        static float Range(System.Random random, float low, float high) { return Mathf.Lerp(low, high, (float)random.NextDouble()); }
        public static void Build(Transform parent)
        {
            var random = new System.Random(Seed);
            var forest = new GameObject("SnowLoadedSpruceForest_" + Seed).transform; forest.SetParent(parent, false);
            var bark = new Geometry(); var needles = new Geometry(); var snow = new Geometry(); var rime = new Geometry();
            var accepted = new List<Vector3>(); var heights = new List<float>(); var details = new List<int>();
            int candidates = 0;
            int leftAccepted = 0; int leftRejected = 0; int northeastAccepted = 0; int northeastRejected = 0;
            SeedWindow(-13, 13, -30, -12, 30, 2.6f, 3.8f, 7.2f, 0, ref leftAccepted, ref leftRejected);
            SeedWindow(-42, -10, -48, -12, 50, 2.8f, 3.2f, 6.0f, 1, ref northeastAccepted, ref northeastRejected);
            for (int tree = 0; tree < accepted.Count; tree++)
                BuildTree(random, accepted[tree], heights[tree], details[tree], bark, needles, snow, rime);

            void SeedWindow(float minX, float maxX, float minZ, float maxZ, int target, float spacing, float minHeight, float maxHeight, int detail, ref int count, ref int rejected)
            {
                while (candidates < 600 && count < target)
                {
                    candidates++;
                    float horizontal = Range(random, minX, maxX); float depth = Range(random, minZ, maxZ);
                    float height = Range(random, minHeight, maxHeight);
                    if (detail > 0) height = Mathf.Lerp(minHeight, maxHeight, Mathf.Sqrt(Mathf.InverseLerp(minHeight, maxHeight, height)));
                    float crownRadius = height * 0.29f + 0.7f;
                    float surfaceHeight = AlpineTerrain.SurfaceHeight(horizontal, depth);
                    float slope = Mathf.Abs(AlpineTerrain.SurfaceHeight(horizontal + 0.8f, depth) - surfaceHeight)
                        + Mathf.Abs(AlpineTerrain.SurfaceHeight(horizontal, depth + 0.8f) - surfaceHeight);
                    Vector3 position = new Vector3(horizontal, surfaceHeight - 0.12f, depth);
                    bool blocked = depth + crownRadius > -8 || surfaceHeight < -6 || slope > 1.6f;
                    foreach (Vector3 other in accepted)
                        if (Vector2.Distance(new Vector2(other.x, other.z), new Vector2(horizontal, depth)) < spacing) blocked = true;
                    if (blocked) { rejected++; continue; }
                    accepted.Add(position); heights.Add(height); details.Add(detail); count++;
                    if (accepted.Count == 1) DetailTarget = position + Vector3.up * height * 0.52f;
                }
            }
            var barkMaterial = MechanicalArt.Metal("SpruceBark", new Color(0.19f, 0.15f, 0.12f), 0, 0.12f);
            var needleMaterial = MechanicalArt.Metal("SpruceNeedles", new Color(0.095f, 0.18f, 0.135f), 0, 0.18f);
            needleMaterial.SetFloat("_Cull", 0);
            var snowMaterial = MechanicalArt.Metal("BranchSnow", new Color(0.68f, 0.77f, 0.82f), 0, 0.14f);
            var rimeMaterial = MechanicalArt.Metal("WindwardRime", new Color(0.54f, 0.69f, 0.76f), 0, 0.26f); rimeMaterial.SetFloat("_Cull", 0);
            bark.Publish(forest, "CurvedTrunksAndBoughs", barkMaterial); needles.Publish(forest, "IndividualNeedleSprays", needleMaterial);
            snow.Publish(forest, "DiscontinuousSnowLoads", snowMaterial); rime.Publish(forest, "WindwardIceNeedles", rimeMaterial);
            Debug.Log("VEGETATION_LAYOUT_PASS seed=" + Seed + " trees=" + accepted.Count + " left accepted=" + leftAccepted + " rejected=" + leftRejected
                + " visualRight accepted=" + northeastAccepted + " rejected=" + northeastRejected + " candidates=" + candidates + "; canopyZ<=-8; operational corridor z>=-7; no colliders");
            if (leftAccepted != 30 || northeastAccepted != 50) Debug.LogWarning("VEGETATION_LAYOUT_SHORTFALL expected left=30 northeast=50");
        }
        static void BuildTree(System.Random random, Vector3 origin, float height, int detail, Geometry bark, Geometry needles, Geometry snow, Geometry rime)
        {
            float lean = Range(random, -0.18f, 0.18f); float phase = Range(random, 0, 6.28f);
            Vector3 Trunk(float progress) { return origin + new Vector3(lean * progress * progress, height * progress, Mathf.Sin(progress * 2) * 0.09f); }
            for (int segment = 0; segment < 14; segment++)
                bark.Tube(Trunk(segment / 14f), Trunk((segment + 1) / 14f), Mathf.Lerp(height * 0.025f, 0.018f, segment / 14f), Mathf.Lerp(height * 0.025f, 0.018f, (segment + 1) / 14f));
            int layers = detail == 0 ? 16 : 11;
            int twigs = detail == 0 ? 8 : 5;
            int needleCount = detail == 0 ? 22 : 12;
            for (int layer = 0; layer < layers; layer++)
            {
                float rise = 0.10f + layer * (0.87f / (layers - 1));
                int branches = random.Next(4, 7);
                for (int branch = 0; branch < branches; branch++)
                {
                    if (random.NextDouble() < 0.09) continue;
                    float angle = phase + layer * 2.399f + branch * Mathf.PI * 2 / branches + Range(random, -0.25f, 0.25f);
                    Vector3 outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 sideways = Vector3.Cross(Vector3.up, outward);
                    float length = height * 0.32f * Mathf.Pow(1 - rise, 0.7f) * Range(random, 0.72f, 1.18f);
                    float load = Range(random, 0.55f, 1.1f);
                    Vector3 start = Trunk(rise) + Vector3.up * Range(random, -0.12f, 0.12f);
                    Vector3 Bough(float progress) { return start + outward * (progress * length) + Vector3.up * (length * (0.12f * progress - load * 0.48f * progress * progress)); }
                    for (int segment = 0; segment < 5; segment++) bark.Tube(Bough(segment / 5f), Bough((segment + 1) / 5f), 0.035f * (1 - segment / 6f), 0.035f * (1 - (segment + 1) / 6f));
                    for (int twig = 0; twig < twigs; twig++)
                    {
                        float progress = 0.20f + twig * (0.665f / (twigs - 1));
                        for (int side = -1; side <= 1; side += 2)
                        {
                            Vector3 root = Bough(progress);
                            Vector3 direction = (outward * 0.45f + sideways * side).normalized;
                            float twigLength = length * (1 - progress * 0.65f) * Range(random, 0.3f, 0.48f);
                            Vector3 tip = root + direction * twigLength + Vector3.down * (0.12f + load * 0.12f);
                            bark.Tube(root, tip, 0.009f, 0.003f);
                            Vector3 axis = (tip - root).normalized; Vector3 cross = Vector3.Cross(axis, Vector3.up).normalized;
                            for (int needle = 0; needle < needleCount; needle++)
                            {
                                float along = needle / (float)needleCount;
                                Vector3 center = Vector3.Lerp(root, tip, along);
                                float needleLength = Range(random, 0.04f, 0.095f) * (1 - along * 0.35f);
                                for (int spray = 0; spray < 5; spray++)
                                {
                                    float spin = spray * 1.257f + needle * 1.71f;
                                    Vector3 radial = cross * Mathf.Cos(spin) + Vector3.up * Mathf.Sin(spin);
                                    Vector3 end = center + radial * needleLength + axis * 0.045f;
                                    var target = outward.x < -0.25f && random.NextDouble() < 0.32 ? rime : needles;
                                    target.Triangle(center - cross * 0.0035f, end, center + cross * 0.0035f);
                                }
                            }
                            if (random.NextDouble() < 0.62)
                                snow.SnowMound(Vector3.Lerp(root, tip, 0.45f) + Vector3.up * 0.045f, axis, twigLength * 0.85f, twigLength * 0.21f, Range(random, 0.045f, 0.09f), phase + twig);
                        }
                    }
                }
            }
        }
    }
}
