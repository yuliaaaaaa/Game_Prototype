using UnityEngine;

namespace ClearPath
{
    public sealed class ObstacleState
    {
        public readonly Vector2 Position;
        public readonly float Radius;
        public bool Alive = true;
        public bool Infected;
        public ObstacleState(Vector2 position, float radius) { Position = position; Radius = radius; }
    }
}
