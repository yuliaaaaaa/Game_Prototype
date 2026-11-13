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
        public List<InfectionHit> Solve(int seed, Vector2 impact, float radius, float linkGap, IReadOnlyList<ObstacleState> obstacles)
        {
            var result = new List<InfectionHit>();
            if (seed < 0 || seed >= obstacles.Count || !obstacles[seed].Alive) return result;
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
                    if (visited[i] || !candidate.Alive) continue;
                    if (Vector2.Distance(candidate.Position, impact) > radius) continue;
                    if (Vector2.Distance(candidate.Position, source.Position) > source.Radius + candidate.Radius + linkGap) continue;
                    visited[i] = true; queue.Enqueue(new InfectionHit(i, current.Depth + 1));
                }
            }
            return result;
        }
    }
}
