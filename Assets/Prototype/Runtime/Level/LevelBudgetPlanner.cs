using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public sealed class LevelBudgetPlanner
    {
        readonly CorridorSolver corridor;
        readonly InfectionSolver infection;
        public LevelBudgetPlanner(CorridorSolver corridor, InfectionSolver infection)
        { this.corridor = corridor; this.infection = infection; }
        public bool TryPlan(LevelConfig config, ObstaclePlacement[] placements, out float initial, out float[] solution)
        {
            initial = Mathf.Max(0.7f, config.Length * 0.04f); solution = null;
            for (int iteration = 0; iteration < 10; iteration++)
            {
                var states = States(placements); var shots = new List<float>();
                float remaining = initial, z = 0; bool reached = false;
                for (int step = 0; step < 80; step++)
                {
                    z = corridor.ReachableZ(z, config.Length, config.Balance.PlayerRadius(remaining), config.Balance.Clearance, states);
                    if (z >= config.Length - 0.01f) { reached = true; break; }
                    float bestShot = 0, bestScore = 0;
                    List<InfectionHit> bestHits = null;
                    float maxShot = Mathf.Min(0.45f, remaining - config.Balance.CriticalVolume - 0.001f);
                    for (float shot = config.Balance.MinimumShotVolume * 1.05f; shot <= maxShot; shot *= 1.6f)
                    {
                        var end = corridor.ShotEnd(z, config, remaining - shot, shot, states);
                        int seed = corridor.FirstHit(new Vector2(0, z), end, ResourceBudget.Radius(shot), states, out var impact);
                        var hits = infection.Solve(seed, impact, config.Balance.BlastRadius(shot), config.Balance.InfectionLinkGap, states);
                        if (hits.Count == 0) continue;
                        foreach (var hit in hits) states[hit.Index].Alive = false;
                        float next = corridor.ReachableZ(z, config.Length, config.Balance.PlayerRadius(remaining - shot), config.Balance.Clearance, states);
                        foreach (var hit in hits) states[hit.Index].Alive = true;
                        float score = (next - z) / shot;
                        if (score > bestScore + 0.001f && next - z > 0.03f)
                        { bestScore = score; bestShot = shot; bestHits = hits; }
                    }
                    if (bestHits == null) break;
                    foreach (var hit in bestHits) states[hit.Index].Alive = false;
                    shots.Add(bestShot); remaining -= bestShot;
                }
                if (!reached) { initial *= 1.2f; if (initial > 3) break; continue; }
                float cost = initial - remaining;
                float calculated = config.Balance.CriticalVolume + cost * (1 + config.Balance.ReserveFraction);
                if (shots.Count > 0 && Replay(config, placements, calculated, shots))
                { initial = calculated; solution = shots.ToArray(); return true; }
                initial = calculated;
            }
            return false;
        }
        public bool Replay(LevelConfig config, ObstaclePlacement[] placements, float volume, IReadOnlyList<float> shots)
        {
            var states = States(placements); float z = 0;
            foreach (float shot in shots)
            {
                z = corridor.ReachableZ(z, config.Length, config.Balance.PlayerRadius(volume), config.Balance.Clearance, states);
                if (z >= config.Length - 0.01f) return false;
                volume -= shot;
                if (volume <= config.Balance.CriticalVolume) return false;
                var end = corridor.ShotEnd(z, config, volume, shot, states);
                int seed = corridor.FirstHit(new Vector2(0, z), end, ResourceBudget.Radius(shot), states, out var impact);
                foreach (var hit in infection.Solve(seed, impact, config.Balance.BlastRadius(shot), config.Balance.InfectionLinkGap, states)) states[hit.Index].Alive = false;
            }
            return corridor.ReachableZ(z, config.Length, config.Balance.PlayerRadius(volume), config.Balance.Clearance, states) >= config.Length - 0.01f;
        }
        static List<ObstacleState> States(ObstaclePlacement[] placements)
        {
            var result = new List<ObstacleState>();
            foreach (var p in placements) result.Add(new ObstacleState(p.Position, p.Radius));
            return result;
        }
    }
}
