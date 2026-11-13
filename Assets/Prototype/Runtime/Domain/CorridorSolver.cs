using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public sealed class CorridorSolver
    {
        public float ReachableZ(float fromZ, float targetZ, float radius, float clearance, IReadOnlyList<ObstacleState> obstacles)
            => ReachableZ(fromZ, targetZ, radius, clearance, obstacles, out _);

        float ReachableZ(float fromZ, float targetZ, float radius, float clearance, IReadOnlyList<ObstacleState> obstacles, out int blocker)
        {
            float result = targetZ;
            blocker = -1;
            for (int i = 0; i < obstacles.Count; i++)
            {
                var obstacle = obstacles[i];
                if (!obstacle.Alive) continue;
                float combined = radius + obstacle.Radius;
                float x = Mathf.Abs(obstacle.Position.x);
                if (x > combined) continue;
                float half = Mathf.Sqrt(combined * combined - x * x);
                if (obstacle.Position.y + half < fromZ) continue;
                float stop = Mathf.Max(fromZ, obstacle.Position.y - half - clearance);
                if (stop >= result) continue;
                result = stop;
                blocker = i;
            }
            return result;
        }

        public Vector2 ShotEnd(float fromZ, LevelConfig level, float remainingVolume, float shotVolume, IReadOnlyList<ObstacleState> obstacles)
        {
            float endZ = level.Length + 2;
            ReachableZ(fromZ, level.Length, level.Balance.PlayerRadius(remainingVolume), level.Balance.Clearance, obstacles, out int blocker);
            if (blocker < 0) return new Vector2(0, endZ);

            var obstacle = obstacles[blocker];
            float hitRadius = obstacle.Radius + ResourceBudget.Radius(shotVolume);
            if (Mathf.Abs(obstacle.Position.x) < hitRadius || obstacle.Position.y <= fromZ)
                return new Vector2(0, endZ);

            float contactX = obstacle.Position.x - Mathf.Sign(obstacle.Position.x) * hitRadius * 0.9f;
            float endX = contactX * (endZ - fromZ) / (obstacle.Position.y - fromZ);
            return new Vector2(endX, endZ);
        }

        public int FirstHit(Vector2 from, Vector2 to, float radius, IReadOnlyList<ObstacleState> obstacles, out Vector2 hit)
        {
            float best = float.PositiveInfinity;
            int index = -1;
            Vector2 delta = to - from;
            float length = delta.magnitude;
            Vector2 direction = length > 0 ? delta / length : Vector2.up;
            for (int i = 0; i < obstacles.Count; i++)
            {
                var o = obstacles[i];
                if (!o.Alive) continue;
                Vector2 offset = o.Position - from;
                float combined = o.Radius + radius;
                float combinedSq = combined * combined;
                if (offset.sqrMagnitude <= combinedSq + 0.000001f)
                {
                    hit = from;
                    return i;
                }
                if (length == 0) continue;

                float projected = Vector2.Dot(offset, direction);
                float perpendicular = offset.x * direction.y - offset.y * direction.x;
                float perpendicularSq = perpendicular * perpendicular;
                if (perpendicularSq > combinedSq) continue;
                float root = Mathf.Sqrt(Mathf.Max(0, combinedSq - perpendicularSq));
                if (projected + root < 0) continue;
                float distance = Mathf.Max(0, projected - root);
                if (distance <= length + 0.000001f && distance < best)
                {
                    best = Mathf.Min(distance, length);
                    index = i;
                }
            }
            hit = index < 0 ? to : from + direction * best;
            return index;
        }
    }
}
