using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public readonly struct InfectionHit
    {
        public readonly int Index;
        public readonly int Depth;
        public InfectionHit(int index, int depth) { Index = index; Depth = depth; }
    }

    public sealed class InfectionSolver
    {
        public List<InfectionHit> Solve(int seed, Vector2 impact, float shotVolume, BalanceConfig balance,
            float fromZ, float targetZ, float playerRadius, IReadOnlyList<ObstacleState> obstacles)
        {
            var result = new List<InfectionHit>();
            if (seed < 0 || seed >= obstacles.Count ||
                !CorridorSolver.TryGetContactZ(obstacles[seed], fromZ, targetZ, playerRadius, out _)) return result;
            float radius = balance.BlastRadius(shotVolume);
            float linkGap = balance.LinkGap(shotVolume);
            var visited = new bool[obstacles.Count];
            var queue = new Queue<InfectionHit>();
            queue.Enqueue(new InfectionHit(seed, 0)); visited[seed] = true;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue(); result.Add(current);
                var source = obstacles[current.Index];
                for (int i = 0; i < obstacles.Count; i++)
                {
                    var candidate = obstacles[i];
                    if (visited[i] || !CorridorSolver.TryGetContactZ(candidate, fromZ, targetZ, playerRadius, out _)) continue;
                    float reach = radius + candidate.Radius;
                    if ((candidate.Position - impact).sqrMagnitude > reach * reach) continue;
                    float link = source.Radius + candidate.Radius + linkGap;
                    if ((candidate.Position - source.Position).sqrMagnitude > link * link) continue;
                    visited[i] = true; queue.Enqueue(new InfectionHit(i, current.Depth + 1));
                }
            }
            return result;
        }
    }
}
