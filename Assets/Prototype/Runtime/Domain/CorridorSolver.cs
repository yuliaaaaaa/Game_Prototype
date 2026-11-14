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
                if (!TryGetContactZ(obstacle, fromZ, targetZ, radius, out float contactZ)) continue;
                float stop = Mathf.Max(fromZ, contactZ - clearance);
                if (stop > result || (stop == result && blocker >= 0)) continue;
                result = stop;
                blocker = i;
            }
            return result;
        }

        public static bool TryGetContactZ(ObstacleState obstacle, float fromZ, float targetZ, float radius, out float contactZ)
        {
            contactZ = 0;
            if (!obstacle.Alive) return false;
            float combined = radius + obstacle.Radius;
            float x = obstacle.Position.x;
            float longitudinalSq = combined * combined - x * x;
            if (longitudinalSq < 0) return false;
            float half = Mathf.Sqrt(longitudinalSq);
            contactZ = obstacle.Position.y - half;
            return contactZ <= targetZ && obstacle.Position.y + half >= fromZ;
        }

        public int ShotTarget(float fromZ, float targetZ, float playerRadius, float shotVolume,
            IReadOnlyList<ObstacleState> obstacles, out Vector2 impact)
        {
            ReachableZ(fromZ, targetZ, playerRadius, 0, obstacles, out int blocker);
            var origin = new Vector2(0, fromZ);
            if (blocker < 0)
            {
                impact = new Vector2(0, targetZ);
                return -1;
            }
            var obstacle = obstacles[blocker];
            float contactDistance = Mathf.Max(0, Vector2.Distance(origin, obstacle.Position)
                - obstacle.Radius - ResourceBudget.Radius(shotVolume));
            impact = Vector2.MoveTowards(origin, obstacle.Position, contactDistance);
            return blocker;
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
