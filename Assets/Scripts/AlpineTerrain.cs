using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Frostline
{
    public static class AlpineTerrain
    {
        public const int Seed = 6106;
        static Material surface;
        static float Noise(float horizontal, float depth, float frequency)
        {
            return Mathf.PerlinNoise(horizontal * frequency + 61.06f, depth * frequency + 13.7f);
        }
        static float Smooth(float low, float high, float value)
        {
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(low, high, value));
        }
        static float Ridge(float horizontal, float depth)
        {
            float ridge = 1 - Mathf.Abs(Noise(horizontal, depth, 0.018f) * 2 - 1);
            float secondary = 1 - Mathf.Abs(Noise(horizontal + 300, depth, 0.045f) * 2 - 1);
            return ridge * ridge * 35 + secondary * 12 + Noise(horizontal, depth, 0.11f) * 4;
        }
        public static float Height(float horizontal, float depth)
        {
            float grain = (Noise(horizontal, depth, 0.38f) - 0.5f) * 0.55f;
            float warp = (Noise(horizontal, depth, 0.08f) - 0.5f) * 3;
            float valley = -14 + Noise(horizontal, depth, 0.035f) * 2;
            float backShelf = (1 - Smooth(12, 20, Mathf.Abs(horizontal + warp))) * (1 - Smooth(-3, 7, depth + warp)) * Smooth(-47, -32, depth);
            float shelfHeight = -3.3f + Noise(horizontal, depth, 0.12f) * 1.5f;
            float ground = Mathf.Lerp(valley, shelfHeight, backShelf);
            float sideShelf = Smooth(17, 29, Mathf.Abs(horizontal + warp)) * (1 - Smooth(27, 47, depth));
            float sideHeight = -3 + Noise(horizontal, depth, 0.04f) * 6;
            ground = Mathf.Max(ground, Mathf.Lerp(valley, sideHeight, sideShelf));
            float cliff = Mathf.Max(4 * backShelf * (1 - backShelf), 4 * sideShelf * (1 - sideShelf));
            ground += cliff * ((Noise(horizontal, depth, 0.23f) - 0.5f) * 4 + (Noise(horizontal, depth, 0.71f) - 0.5f) * 0.8f);
            float mountainMask = Mathf.Max(Smooth(47, 90, Mathf.Abs(horizontal)), Smooth(36, 80, -depth));
            float rearMask = Smooth(58, 115, depth);
            mountainMask = Mathf.Max(mountainMask, rearMask);
            float alpine = -8 + Ridge(horizontal, depth) + Noise(horizontal, depth, 0.009f) * 43;
            float ridged = Mathf.Lerp(ground, alpine, mountainMask);
            float gully = Mathf.Pow(Mathf.Max(0, 1 - Mathf.Abs(Mathf.Sin(horizontal * 0.07f + depth * 0.035f + Noise(horizontal, depth, 0.025f) * 5))), 5);
            ridged -= gully * mountainMask * 11;
            float terrace = Mathf.Sin(ridged * 2.2f + horizontal * 0.17f + depth * 0.09f + Noise(horizontal, depth, 0.24f) * 3) * 0.22f;
            float platformClearance = 1 - Smooth(4.1f, 9 + Noise(horizontal, depth, 0.24f) * 3, Mathf.Max(Mathf.Abs(horizontal), Mathf.Abs(depth)));
            ridged = Mathf.Lerp(ridged, -0.65f, platformClearance);
            float foundationFlank = 4 * platformClearance * (1 - platformClearance);
            return ridged + grain * (0.2f + cliff + foundationFlank * 2) * (1 - platformClearance) + terrace * (cliff + foundationFlank) * (1 - platformClearance);
        }
        static float SnowDepth(float horizontal, float depth, float height, Vector3 normal)
        {
            float shelter = Noise(horizontal + 100, depth - 50, 0.09f);
            float slope = Smooth(0.4f, 0.72f, normal.y);
            float snowline = Smooth(-18, -3, height + shelter * 7);
            float cover = slope * snowline;
            float drift = Noise(horizontal, depth, 0.11f) * 0.26f + 0.32f;
            float wind = Mathf.Sin(horizontal * 3.9f + depth * 1.6f + Noise(horizontal, depth, 0.37f) * 4) * 0.055f;
            float pit = Mathf.Pow(Noise(horizontal + 40, depth, 0.8f), 7) * 0.35f;
            return cover * Mathf.Max(0.03f, drift + wind - pit);
        }
        public static void Build(Transform parent)
        {
            var terrain = new GameObject("ProceduralAlpineTerrain_" + Seed).transform;
            terrain.SetParent(parent, false);
            var shader = Resources.Load<Shader>("AlpineTerrainSurface");
            surface = new Material(shader) { name = "StratifiedRockAndWindPackedSnow" };
            MeshPatch(terrain, -52, -52, 104, 416, false);
            MeshPatch(terrain, -210, -210, 420, 420, true);
            BuildScree(terrain);
        }
        static void MeshPatch(Transform parent, float originX, float originZ, float extent, int divisions, bool omitCenter)
        {
            int stride = divisions + 1;
            var vertices = new Vector3[stride * stride];
            var colors = new Color[vertices.Length];
            var triangles = new List<int>();
            float step = extent / divisions;
            for (int row = 0; row <= divisions; row++)
            for (int column = 0; column <= divisions; column++)
            {
                float horizontal = originX + column * step; float depth = originZ + row * step;
                float elevation = Height(horizontal, depth);
                var normal = new Vector3(Height(horizontal - 0.35f, depth) - Height(horizontal + 0.35f, depth), 0.7f, Height(horizontal, depth - 0.35f) - Height(horizontal, depth + 0.35f)).normalized;
                float snow = SnowDepth(horizontal, depth, elevation, normal);
                float finalHeight = elevation + snow;
                if (!omitCenter && (row == 0 || row == divisions || column == 0 || column == divisions))
                {
                    float lowerX = Mathf.Floor(horizontal); float lowerZ = Mathf.Floor(depth);
                    finalHeight = Mathf.Lerp(Mathf.Lerp(SurfaceHeight(lowerX, lowerZ), SurfaceHeight(lowerX + 1, lowerZ), horizontal - lowerX), Mathf.Lerp(SurfaceHeight(lowerX, lowerZ + 1), SurfaceHeight(lowerX + 1, lowerZ + 1), horizontal - lowerX), depth - lowerZ);
                }
                vertices[row * stride + column] = new Vector3(horizontal, finalHeight, depth);
                colors[row * stride + column] = new Color(Mathf.Clamp01(snow / 0.18f), 0, 0, 1);
            }
            for (int row = 0; row < divisions; row++)
            for (int column = 0; column < divisions; column++)
            {
                float horizontal = originX + (column + 0.5f) * step; float depth = originZ + (row + 0.5f) * step;
                if (omitCenter && Mathf.Abs(horizontal) < 52 && Mathf.Abs(depth) < 52) continue;
                int corner = row * stride + column;
                triangles.Add(corner); triangles.Add(corner + stride); triangles.Add(corner + 1);
                triangles.Add(corner + 1); triangles.Add(corner + stride); triangles.Add(corner + stride + 1);
            }
            var mesh = new Mesh { name = omitCenter ? "DistantConnectedMassif" : "NearCliffSnowfield", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices; mesh.colors = colors; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var patch = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer)); patch.transform.SetParent(parent, false);
            patch.GetComponent<MeshFilter>().sharedMesh = mesh; patch.GetComponent<Renderer>().sharedMaterial = surface;
        }
        static void BuildScree(Transform parent)
        {
            var random = new System.Random(Seed);
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            Vector3[] corners = { new Vector3(-1, 0, -0.7f), new Vector3(0.7f, -0.1f, -1), new Vector3(1, 0, 0.5f), new Vector3(-0.6f, 0, 1), new Vector3(-0.2f, 0.85f, 0.1f), new Vector3(0, -0.3f, 0) };
            int[] faces = { 0,4,1,1,4,2,2,4,3,3,4,0,0,1,5,1,2,5,2,3,5,3,0,5 };
            for (int stone = 0; stone < 6200; stone++)
            {
                float horizontal = (float)random.NextDouble() * 92 - 46;
                float depth = (float)random.NextDouble() * 90 - 42;
                float ground = Height(horizontal, depth);
                float slope = Mathf.Abs(Height(horizontal + 0.8f, depth) - ground) + Mathf.Abs(Height(horizontal, depth + 0.8f) - ground);
                if (ground > -5 || ground < -25 || slope > 2.5f || slope < 0.12f) continue;
                float size = 0.12f + Mathf.Pow((float)random.NextDouble(), 3) * 1.2f;
                var rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
                int start = vertices.Count;
                foreach (var corner in corners)
                {
                    vertices.Add(new Vector3(horizontal, ground + size * 0.1f, depth) + rotation * Vector3.Scale(corner, new Vector3(size, size * 0.6f, size)));
                    colors.Add(new Color(0, 0, 0, 1));
                }
                foreach (int face in faces) triangles.Add(start + face);
            }
            var mesh = new Mesh { name = "AngularTalusFragments", indexFormat = IndexFormat.UInt32 }; mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            var scree = new GameObject("ScreeSlope", typeof(MeshFilter), typeof(MeshRenderer)); scree.transform.SetParent(parent, false);
            scree.GetComponent<MeshFilter>().sharedMesh = mesh; scree.GetComponent<Renderer>().sharedMaterial = surface;
        }
        public static float SurfaceHeight(float horizontal, float depth)
        {
            float elevation = Height(horizontal, depth);
            var normal = new Vector3(Height(horizontal - 0.35f, depth) - Height(horizontal + 0.35f, depth), 0.7f, Height(horizontal, depth - 0.35f) - Height(horizontal, depth + 0.35f)).normalized;
            return elevation + SnowDepth(horizontal, depth, elevation, normal);
        }
        public static void BuildRockSpur(Transform parent, Vector3 obstacle)
        {
            const int levels = 96; const int sides = 64;
            float bottom = Height(obstacle.x, obstacle.z) - 1;
            float top = obstacle.y + 3;
            var vertices = new Vector3[(levels + 1) * (sides + 1)];
            var colors = new Color[vertices.Length]; var triangles = new List<int>();
            for (int level = 0; level <= levels; level++)
            for (int side = 0; side <= sides; side++)
            {
                float rise = level / (float)levels; float angle = side * Mathf.PI * 2 / sides;
                float cosine = Mathf.Cos(angle); float sine = Mathf.Sin(angle);
                float block = 1 / Mathf.Max(Mathf.Abs(cosine), Mathf.Abs(sine));
                float ledge = 0.90f + Mathf.Sin(rise * 53 + sine * 0.5f) * 0.035f + Noise(cosine * 2 + obstacle.x, rise * 12, 1.3f) * 0.10f;
                float crown = 1 - Smooth(0.82f, 1, rise) * 0.32f;
                float baseFlare = 1 + (1 - Smooth(0, 0.45f, rise)) * 1.6f;
                var local = new Vector3(cosine * block * 1.7f, 0, sine * block * 1.6f) * ledge * crown * baseFlare;
                local = Quaternion.Euler(0, 15, 0) * local;
                local += new Vector3(obstacle.x, Mathf.Lerp(bottom, top, rise), obstacle.z);
                if (rise > 0.85f) local.y -= (1 - Noise(local.x, local.z, 0.9f)) * Smooth(0.85f, 1, rise) * 0.65f;
                vertices[level * (sides + 1) + side] = local;
                colors[level * (sides + 1) + side] = new Color(level > levels - 5 ? 1 : 0, 0, 0, 1);
            }
            for (int level = 0; level < levels; level++)
            for (int side = 0; side < sides; side++)
            {
                int corner = level * (sides + 1) + side;
                triangles.Add(corner); triangles.Add(corner + sides + 1); triangles.Add(corner + 1);
                triangles.Add(corner + 1); triangles.Add(corner + sides + 1); triangles.Add(corner + sides + 2);
            }
            for (int side = 1; side < sides - 1; side++)
            {
                triangles.Add(levels * (sides + 1)); triangles.Add(levels * (sides + 1) + side + 1); triangles.Add(levels * (sides + 1) + side);
            }
            var mesh = new Mesh { name = "WeatheredBedrockSpur" }; mesh.vertices = vertices; mesh.colors = colors; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            var spur = new GameObject("BedrockSpurVisual", typeof(MeshFilter), typeof(MeshRenderer)); spur.transform.SetParent(parent, false);
            spur.GetComponent<MeshFilter>().sharedMesh = mesh; spur.GetComponent<Renderer>().sharedMaterial = surface;
        }
        public static void BuildFooting(Transform parent, Vector3 center)
        {
            const int divisions = 48;
            var vertices = new Vector3[(divisions + 1) * (divisions + 1)];
            var colors = new Color[vertices.Length]; var triangles = new List<int>();
            for (int row = 0; row <= divisions; row++)
            for (int column = 0; column <= divisions; column++)
            {
                float horizontal = center.x - 5 + column * 10f / divisions;
                float depth = center.z - 5 + row * 10f / divisions;
                float radius = Mathf.Max(Mathf.Abs(horizontal - center.x), Mathf.Abs(depth - center.z));
                float blend = 1 - Smooth(2.9f, 5, radius);
                float ground = SurfaceHeight(horizontal, depth);
                float top = center.y - 13.85f + (Noise(horizontal, depth, 0.8f) - 0.5f) * 0.12f;
                int corner = row * (divisions + 1) + column;
                vertices[corner] = new Vector3(horizontal, Mathf.Lerp(ground - 0.06f, Mathf.Max(ground, top), blend), depth);
                colors[corner] = new Color(blend > 0.5f ? 0.2f : 0.8f, 0, 0, 1);
                if (row == divisions || column == divisions) continue;
                triangles.Add(corner); triangles.Add(corner + divisions + 1); triangles.Add(corner + 1);
                triangles.Add(corner + 1); triangles.Add(corner + divisions + 1); triangles.Add(corner + divisions + 2);
            }
            var mesh = new Mesh { name = "ExposedBedrockFooting" }; mesh.vertices = vertices; mesh.colors = colors; mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            var footing = new GameObject("TerrainFooting", typeof(MeshFilter), typeof(MeshRenderer)); footing.transform.SetParent(parent, false);
            footing.GetComponent<MeshFilter>().sharedMesh = mesh; footing.GetComponent<Renderer>().sharedMaterial = surface;
        }
    }
}
