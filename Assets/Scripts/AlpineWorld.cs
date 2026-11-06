using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Frostline
{
    public static class AlpineWorld
    {
        public static Transform BuildEnvironment(Transform parent)
        {
            var root = MechanicalArt.Group("AlpineEnvironment", parent, Vector3.zero);
            var rock = MechanicalArt.Metal("Slate", new Color(0.19f, 0.27f, 0.30f), 0.05f, 0.15f);
            var darkRock = MechanicalArt.Metal("DeepSlate", new Color(0.10f, 0.17f, 0.21f), 0, 0.14f);
            var pine = MechanicalArt.Metal("Pine", new Color(0.06f, 0.18f, 0.18f), 0, 0.2f);
            var concrete = MechanicalArt.Metal("Concrete", new Color(0.37f, 0.43f, 0.43f), 0, 0.3f);
            AlpineTerrain.Build(root);
            MechanicalArt.Box("CraneFoundation", root, new Vector3(0, -0.45f, 0), new Vector3(8, 0.9f, 8), concrete, true);

            var asphalt = MechanicalArt.Metal("DeckCoating", new Color(0.15f, 0.23f, 0.26f), 0.25f, 0.32f);
            MechanicalArt.Box("DeckTop", root, new Vector3(0, 0.015f, 0), new Vector3(7.8f, 0.08f, 7.8f), asphalt);
            for (int stripe = 0; stripe < 9; stripe++)
            {
                var mark = MechanicalArt.Box("SafetyStripe", root, new Vector3(-3.6f + stripe * 0.9f, 0.07f, 3.65f), new Vector3(0.45f, 0.03f, 0.4f), MechanicalArt.Orange);
                mark.transform.localRotation = Quaternion.Euler(0, -35, 0);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                for (int post = 0; post < 5; post++) MechanicalArt.Bar("SafetyPost", root, new Vector3(side * 3.8f, 0, -3.6f + post * 1.6f), new Vector3(side * 3.8f, 1, -3.6f + post * 1.6f), 0.045f, MechanicalArt.Silver);
                MechanicalArt.Bar("Handrail", root, new Vector3(side * 3.8f, 1, -3.7f), new Vector3(side * 3.8f, 1, 2.8f), 0.045f, MechanicalArt.Orange);
            }
            var hut = MechanicalArt.Group("OperationsHut", root, new Vector3(-5.7f, 0.9f, -4.4f));
            MechanicalArt.Box("Walls", hut, Vector3.zero, new Vector3(3, 2.5f, 3.5f), asphalt);
            MechanicalArt.Box("SnowRoof", hut, new Vector3(0, 1.4f, 0), new Vector3(3.4f, 0.35f, 3.9f), MechanicalArt.Snow);
            MechanicalArt.Box("Window", hut, new Vector3(0, 0.25f, 1.78f), new Vector3(2.2f, 0.95f, 0.04f), MechanicalArt.Teal);
            MechanicalArt.Bar("Antenna", hut, new Vector3(0.8f, 1.5f, 0), new Vector3(0.8f, 4.5f, 0), 0.035f, MechanicalArt.Silver);
            AlpineVegetation.Build(root);
            var cableMaterial = MechanicalArt.Steel;
            for (int side = -1; side <= 1; side += 2)
            {
                var towerBase = new Vector3(side * 28, -2, 20);
                MechanicalArt.Bar("CablewayTower", root, towerBase, towerBase + Vector3.up * 15, 0.38f, concrete);
                MechanicalArt.Bar("CablewayCrossbar", root, towerBase + new Vector3(0, 15, -3), towerBase + new Vector3(0, 15, 3), 0.23f, MechanicalArt.Orange);
            }
            for (int track = 0; track < 2; track++)
            {
                var cableObject = new GameObject("SuspendedCableway"); cableObject.transform.SetParent(root, false);
                var line = cableObject.AddComponent<LineRenderer>(); line.sharedMaterial = cableMaterial; line.positionCount = 33; line.startWidth = 0.055f; line.endWidth = 0.055f;
                for (int point = 0; point < 33; point++)
                {
                    float progress = point / 32f;
                    line.SetPosition(point, new Vector3(Mathf.Lerp(-28, 28, progress), 13 - Mathf.Sin(progress * Mathf.PI) * 3, 18 + track * 4));
                }
            }
            StaticBatchingUtility.Combine(root.gameObject);
            return root;
        }
        public static void BuildMission(Transform root, MissionDefinition mission)
        {
            for (int pickup = 0; pickup < mission.pickupSites.Length; pickup++)
            {
                Platform(root, mission.pickupSites[pickup], MechanicalArt.Orange, "Pickup_" + pickup);
                CableCar(root, mission.pickupSites[pickup].Center + new Vector3(0, 1.5f, 4.6f));
            }
            Platform(root, mission.destination, MechanicalArt.Teal, "ReceivingDeck");
            if (mission.transfer) Platform(root, mission.relay, MechanicalArt.Orange, "RelayDeck");
            foreach (var obstacle in mission.obstacles)
            {
                var rock = MechanicalArt.Metal("ObstacleRock", new Color(0.23f, 0.31f, 0.32f), 0, 0.2f);
                var body = MechanicalArt.Box("CollisionRidge", root, obstacle, new Vector3(3.4f, 6, 3.2f), rock, true);
                body.transform.localRotation = Quaternion.Euler(0, 15, 0);
                body.GetComponent<Renderer>().enabled = false;
                AlpineTerrain.BuildRockSpur(root, obstacle);
            }
        }
        static void CableCar(Transform root, Vector3 position)
        {
            var car = MechanicalArt.Group("StrandedCableCar", root, position);
            var enamel = MechanicalArt.Metal("CableCarEnamel", new Color(0.65f, 0.13f, 0.08f), 0.45f, 0.5f);
            MechanicalArt.Box("LowerShell", car, new Vector3(0, -0.8f, 0), new Vector3(2.9f, 1, 3.5f), enamel);
            MechanicalArt.Box("Roof", car, new Vector3(0, 1.35f, 0), new Vector3(3.1f, 0.35f, 3.7f), MechanicalArt.Snow);
            MechanicalArt.Box("RearWindow", car, new Vector3(0, 0.5f, 1.73f), new Vector3(2.6f, 1.4f, 0.06f), MechanicalArt.Glass);
            for (int side = -1; side <= 1; side += 2)
            {
                MechanicalArt.Box("SideWindows", car, new Vector3(side * 1.45f, 0.5f, 0), new Vector3(0.05f, 1.4f, 3.2f), MechanicalArt.Glass);
                for (int pillar = 0; pillar < 3; pillar++) MechanicalArt.Box("WindowPillar", car, new Vector3(side * 1.48f, 0.45f, -1.6f + pillar * 1.6f), new Vector3(0.09f, 1.8f, 0.09f), MechanicalArt.Silver);
                MechanicalArt.Box("OpenDoorFrame", car, new Vector3(side * 1.15f, 0.25f, -1.73f), new Vector3(0.18f, 2.1f, 0.16f), enamel);
                MechanicalArt.Box("InteriorBench", car, new Vector3(side * 0.93f, -0.15f, 0.2f), new Vector3(0.45f, 0.2f, 2.4f), MechanicalArt.Steel);
                MechanicalArt.Bar("SuspensionFrame", car, new Vector3(side * 0.85f, 1.5f, 0), new Vector3(0, 4, 0), 0.12f, MechanicalArt.Silver);
                var roller = MechanicalArt.Cylinder("CableGripRoller", car, new Vector3(side * 0.4f, 4.2f, 0), 0.3f, 0.25f, MechanicalArt.Steel);
                roller.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            MechanicalArt.Box("ExtractionBridge", car, new Vector3(0, -1.25f, -2.9f), new Vector3(2.1f, 0.14f, 2.8f), MechanicalArt.Silver);
            MechanicalArt.Box("EmergencyStripe", car, new Vector3(0, -0.75f, -1.78f), new Vector3(2.65f, 0.22f, 0.04f), MechanicalArt.Orange);
            MechanicalArt.Cylinder("WarningBeacon", car, new Vector3(1.2f, 1.65f, -1.1f), 0.13f, 0.25f, MechanicalArt.Teal);
        }
        static void Platform(Transform root, LandingSite site, Material signal, string name)
        {
            AlpineTerrain.BuildFooting(root, site.Center);
            var deck = MechanicalArt.Group(name, root, site.Center);
            MechanicalArt.Box("Deck", deck, new Vector3(0, -0.3f, 0), new Vector3(5.6f, 0.6f, 5.6f), MechanicalArt.Steel, true);
            MechanicalArt.Box("LandingInset", deck, new Vector3(0, 0.025f, 0), new Vector3(3.8f, 0.04f, 3.8f), MechanicalArt.Metal("LandingPaint", new Color(0.25f, 0.35f, 0.37f), 0.2f, 0.25f));
            MechanicalArt.Ring("TargetRing", deck, new Vector3(0, 0.07f, 0), 1.85f, 0.055f, signal, 32);
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
            {
                MechanicalArt.Bar("DeckPile", deck, new Vector3(side * 2.25f, -14, end * 2.25f), new Vector3(side * 2.25f, -0.5f, end * 2.25f), 0.22f, MechanicalArt.Silver);
                MechanicalArt.Bar("DiagonalSupport", deck, new Vector3(side * 2.25f, -8, end * 2.25f), new Vector3(-side * 2.25f, -0.5f, end * 2.25f), 0.13f, MechanicalArt.Steel);
                MechanicalArt.Cylinder("BeaconBase", deck, new Vector3(side * 2.5f, 0.1f, end * 2.5f), 0.14f, 0.3f, MechanicalArt.Steel);
                MechanicalArt.Cylinder("Beacon", deck, new Vector3(side * 2.5f, 0.33f, end * 2.5f), 0.1f, 0.18f, signal);
            }
            MechanicalArt.Box("LandingCrossA", deck, new Vector3(0, 0.08f, 0), new Vector3(1, 0.02f, 0.12f), signal);
            MechanicalArt.Box("LandingCrossB", deck, new Vector3(0, 0.08f, 0), new Vector3(0.12f, 0.02f, 1), signal);
        }
        static void Mountain(Transform root, Vector3 position, Vector3 scale, int seed, Material rock)
        {
            const int rings = 12; const int sides = 36;
            var vertices = new List<Vector3>(); var stoneTriangles = new List<int>(); var snowTriangles = new List<int>();
            for (int ring = 0; ring <= rings; ring++)
            for (int side = 0; side <= sides; side++)
            {
                float height = ring / (float)rings; float angle = side * Mathf.PI * 2 / sides;
                float noise = Mathf.PerlinNoise(Mathf.Cos(angle) * 1.7f + seed, Mathf.Sin(angle) * 1.7f + height * 3);
                float radius = (1 - height) * (0.72f + noise * 0.65f);
                vertices.Add(Vector3.Scale(new Vector3(Mathf.Cos(angle) * radius + height * 0.12f, height, Mathf.Sin(angle) * radius), scale));
            }
            for (int ring = 0; ring < rings; ring++)
            for (int side = 0; side < sides; side++)
            {
                int corner = ring * (sides + 1) + side;
                var target = ring > 6 + Mathf.RoundToInt(Mathf.Sin(side * 1.3f + seed) * 2) ? snowTriangles : stoneTriangles;
                target.Add(corner); target.Add(corner + sides + 1); target.Add(corner + 1);
                target.Add(corner + 1); target.Add(corner + sides + 1); target.Add(corner + sides + 2);
            }
            var mesh = new Mesh { name = "AlpineRidge_" + seed }; mesh.SetVertices(vertices); mesh.subMeshCount = 2;
            mesh.SetTriangles(stoneTriangles, 0); mesh.SetTriangles(snowTriangles, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var mountain = new GameObject("Mountain_" + seed, typeof(MeshFilter), typeof(MeshRenderer)); mountain.transform.SetParent(root, false); mountain.transform.localPosition = position;
            mountain.GetComponent<MeshFilter>().sharedMesh = mesh; mountain.GetComponent<MeshRenderer>().sharedMaterials = new[] { rock, MechanicalArt.Snow };
        }
        public static void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.7f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.42f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.23f, 0.29f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.46f, 0.65f, 0.72f); RenderSettings.fogDensity = 0.0048f;
            var sunObject = new GameObject("AlpineSun"); var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1, 0.88f, 0.69f); sun.intensity = 1.8f;
            sun.shadows = LightShadows.Soft; sun.shadowBias = 0.035f; sun.shadowNormalBias = 0.15f; sunObject.transform.rotation = Quaternion.Euler(35, -35, 0);
            RenderSettings.sun = sun;
        }
    }
}

