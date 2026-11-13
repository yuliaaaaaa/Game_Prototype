using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public sealed class LevelGenerator
    {
        readonly LevelBudgetPlanner planner;
        readonly Dictionary<string, GeneratedLevel> cache = new Dictionary<string, GeneratedLevel>();
        public LevelGenerator(LevelBudgetPlanner planner) => this.planner = planner;
        public GeneratedLevel Generate(LevelConfig config)
        {
            if (!config || !config.IsValid(out _)) throw new ArgumentException("Invalid level configuration.");
            string key = JsonUtility.ToJson(config) + JsonUtility.ToJson(config.Balance);
            if (cache.TryGetValue(key, out var cached)) return cached;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var placements = Scatter(config, unchecked(config.Seed + attempt * 7919));
                if (planner.TryPlan(config, placements, out float startingVolume, out var shots))
                {
                    var result = new GeneratedLevel(placements, startingVolume, shots);
                    if (cache.Count >= 8) cache.Clear();
                    cache.Add(key, result); return result;
                }
            }
            throw new InvalidOperationException("Could not generate a verified route. Change Seed or reduce Density; check the infection balance.");
        }
        static ObstaclePlacement[] Scatter(LevelConfig config, int seed)
        {
            var random = new System.Random(seed);
            var result = new List<ObstaclePlacement>();
            AddThickets(config, random, result);
            float halfWidth = Mathf.Max(12, config.Length * 0.55f), margin = 7;
            float meanRadius = (config.RadiusRange.x + config.RadiusRange.y) / 2;
            float area = halfWidth * 2 * (config.Length + margin * 2);
            int target = Mathf.Min(1800, Mathf.RoundToInt(area * config.Density * 0.5f / (Mathf.PI * meanRadius * meanRadius)));
            for (int attempt = 0; attempt < target * 35 && result.Count < target; attempt++)
            {
                float radius = Mathf.Lerp(config.RadiusRange.x, config.RadiusRange.y, (float)random.NextDouble());
                Vector2 point;
                if (result.Count > 0 && random.NextDouble() < config.Clustering)
                {
                    var neighbor = result[random.Next(result.Count)];
                    float angle = (float)random.NextDouble() * Mathf.PI * 2;
                    float distance = neighbor.Radius + radius + Mathf.Lerp(0.07f, 0.48f, (float)random.NextDouble());
                    point = neighbor.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                }
                else point = new Vector2(Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()),
                    Mathf.Lerp(-margin, config.Length + margin, (float)random.NextDouble()));
                if (Mathf.Abs(point.x) > halfWidth || point.y < -margin || point.y > config.Length + margin) continue;
                TryPlace(config, random, result, point, radius);
            }
            return result.ToArray();
        }

        static void AddThickets(LevelConfig config, System.Random random, List<ObstaclePlacement> result)
        {
            float spacing = config.RadiusRange.y * 2 + 0.12f;
            float playableLength = config.Length - 6;
            for (int group = 0; group < config.ThicketCount; group++)
            {
                float z = 3 + playableLength * (group + 0.5f) / config.ThicketCount;
                float x = (group % 2 == 0 ? -0.28f : 0.28f) * spacing;
                for (int row = -1; row <= 1; row++)
                    for (int column = -2; column <= 2; column++)
                    {
                        float jitterX = ((float)random.NextDouble() - 0.5f) * 0.08f;
                        float jitterZ = ((float)random.NextDouble() - 0.5f) * 0.08f;
                        float radius = Mathf.Lerp(config.RadiusRange.x, config.RadiusRange.y, (float)random.NextDouble());
                        var point = new Vector2(x + (column + 0.35f * row) * spacing + jitterX,
                            z + row * spacing + jitterZ);
                        TryPlace(config, random, result, point, radius);
                    }
            }
        }

        static void TryPlace(LevelConfig config, System.Random random, List<ObstaclePlacement> result, Vector2 point, float radius)
        {
            if (point.magnitude < 2.4f + radius || Vector2.Distance(point, new Vector2(0, config.Length)) < 2.4f + radius)
                return;
            foreach (var other in result)
            {
                float spacing = radius + other.Radius + 0.06f;
                if ((point - other.Position).sqrMagnitude < spacing * spacing) return;
            }
            result.Add(new ObstaclePlacement(point, radius, radius * Mathf.Lerp(2.8f, 3.8f, (float)random.NextDouble())));
        }
    }
}
