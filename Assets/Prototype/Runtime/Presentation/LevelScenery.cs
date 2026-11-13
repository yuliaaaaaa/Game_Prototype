using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ClearPath
{
    public sealed class LevelScenery
    {
        readonly Dictionary<(GameObject model, float yaw, bool fitFootprint), ModelFit> modelFits =
            new Dictionary<(GameObject, float, bool), ModelFit>();

        readonly struct ModelFit
        {
            public readonly Bounds Bounds;
            public readonly float UnitRadius;

            public ModelFit(Bounds bounds, float unitRadius)
            {
                Bounds = bounds;
                UnitRadius = unitRadius;
            }
        }

        public ObstacleView CreateObstacle(Transform parent, VisualPalette palette, LevelConfig level,
            ObstaclePlacement placement, int index, DebrisEffect debris)
        {
            bool rock = index % Mathf.Max(1, level.RockInterval) == 0;
            var models = level.ObstacleModels?.Length > 0 ? level.ObstacleModels : palette.BushModels;
            var rockModel = level.RockModel ? level.RockModel : palette.RockModel;
            var model = rock ? rockModel : models[index % models.Length];
            var material = level.ObstacleMaterial ? level.ObstacleMaterial : palette.Foliage;
            float diameter = placement.Radius * 2;
            var size = new Vector3(diameter, placement.Height * (rock ? 0.68f : 0.78f), diameter);
            var position = new Vector3(placement.Position.x, 0, placement.Position.y);
            var obstacle = Fit(model, parent, position, size, index * 137.508f, rock ? palette.Stone : material,
                footprintRadius: placement.Radius);
            obstacle.name = $"Obstacle {index}";

            var view = obstacle.AddComponent<ObstacleView>();
            view.Configure(debris, placement.Radius);
            return view;
        }

        public DoorView CreateGate(Transform parent, VisualPalette palette, float distance)
        {
            var root = new GameObject("Goal Gate");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.forward * distance;
            Fit(palette.GateFrame, root.transform, Vector3.zero, new Vector3(4, 3.5f, 1.1f), 90, palette.Door);
            var panel = Fit(palette.GatePanel, root.transform, new Vector3(0, 0, -0.18f),
                new Vector3(2.36f, 2.65f, 0.26f), 90, palette.Door);
            var door = root.AddComponent<DoorView>();
            var effect = new GameObject("Finish Markers").AddComponent<FinishEffect>();
            effect.transform.SetParent(root.transform, false);
            effect.transform.localPosition = new Vector3(0, 0, -0.57f);
            effect.Configure(palette);
            door.Configure(panel.transform, effect);
            return door;
        }

        public void AddForestBackdrop(Transform parent, VisualPalette palette, LevelConfig level, GeneratedLevel data)
        {
            if (!palette.BackdropTree) return;
            var root = new GameObject("Forest Beyond Gate").transform;
            root.SetParent(parent, false);
            var random = new System.Random(unchecked(level.Seed + 731));
            for (int row = 0; row < 16; row++)
                for (int column = -11; column <= 13; column++)
                {
                    if (row > 6 && Mathf.Abs(column) > 10) continue;
                    float x = (column + (row % 2) * 0.5f) * 1.18f;
                    var point = new Vector2(x + (float)random.NextDouble() * 0.4f - 0.2f,
                        level.Length + 1.15f + row * 0.93f + (float)random.NextDouble() * 0.36f);
                    if (NearObstacle(point, data, 0.08f)) continue;
                    float width = Mathf.Lerp(1.12f, 1.58f, (float)random.NextDouble());
                    float height = Mathf.Lerp(1.9f, 3.05f, (float)random.NextDouble());
                    Fit(palette.BackdropTree, root, new Vector3(point.x, 0, point.y),
                        new Vector3(width, height, width), random.Next(360), palette.Foliage, false);
                }
        }

        public void AddUndergrowth(Transform parent, VisualPalette palette, LevelConfig level, GeneratedLevel data)
        {
            if (!palette.Undergrowth) return;
            var root = new GameObject("Undergrowth").transform;
            root.SetParent(parent, false);
            var random = new System.Random(level.Seed);
            for (int z = -2; z < level.Length + 3; z++)
                for (int x = -7; x <= 7; x++)
                {
                    if (Mathf.Abs(x) < 2 || random.NextDouble() > 0.88) continue;
                    var point = new Vector2(x + (float)random.NextDouble() * 0.85f - 0.425f,
                        z + (float)random.NextDouble() * 0.85f - 0.425f);
                    if (NearObstacle(point, data, 0.04f)) continue;
                    float scale = Mathf.Lerp(0.34f, 0.60f, (float)random.NextDouble());
                    Fit(palette.Undergrowth, root, new Vector3(point.x, 0, point.y),
                        new Vector3(scale, scale * 0.45f, scale), random.Next(360), palette.Foliage, false);
                }
        }

        static bool NearObstacle(Vector2 point, GeneratedLevel data, float padding = 0.18f)
        {
            foreach (var obstacle in data.Obstacles)
            {
                float minimum = obstacle.Radius + padding;
                if ((point - obstacle.Position).sqrMagnitude < minimum * minimum) return true;
            }
            return false;
        }

        GameObject Fit(GameObject model, Transform parent, Vector3 position, Vector3 size, float yaw,
            Material material, bool castShadows = true, float footprintRadius = 0)
        {
            var root = new GameObject(model.name);
            var instance = Object.Instantiate(model, root.transform, false);
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0) * instance.transform.localRotation;
            var renderers = instance.GetComponentsInChildren<MeshRenderer>();
            bool fitFootprint = footprintRadius > 0;
            var key = (model, Mathf.Repeat(yaw, 360), fitFootprint);
            if (!modelFits.TryGetValue(key, out var fit))
            {
                if (modelFits.Count >= 16384) modelFits.Clear();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                float unitRadius = fitFootprint ? MeasureUnitRadius(instance, bounds, pivot) : 1;
                fit = new ModelFit(bounds, unitRadius);
                modelFits.Add(key, fit);
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }

            var modelBounds = fit.Bounds;
            instance.transform.localPosition -= new Vector3(modelBounds.center.x, modelBounds.min.y, modelBounds.center.z);
            float width = fitFootprint ? footprintRadius / fit.UnitRadius : size.x;
            float depth = fitFootprint ? width : size.z;
            Vector3 scale = new Vector3(width / modelBounds.size.x, size.y / modelBounds.size.y, depth / modelBounds.size.z);
            root.transform.SetParent(parent, false);
            root.transform.localScale = scale;
            root.transform.localPosition = position;
            return root;
        }

        static float MeasureUnitRadius(GameObject model, Bounds bounds, Vector3 pivot)
        {
            const float boundsRadius = 0.70710678f;
            float maxRadiusSq = 0;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (!mesh || !mesh.isReadable)
                    return boundsRadius;
                var toWorld = filter.transform.localToWorldMatrix;
                foreach (var vertex in mesh.vertices)
                {
                    var point = toWorld.MultiplyPoint3x4(vertex) - pivot;
                    var normalized = new Vector2(point.x / bounds.size.x, point.z / bounds.size.z);
                    maxRadiusSq = Mathf.Max(maxRadiusSq, normalized.sqrMagnitude);
                }
            }
            return maxRadiusSq > 0 ? Mathf.Sqrt(maxRadiusSq) : boundsRadius;
        }
    }
}
