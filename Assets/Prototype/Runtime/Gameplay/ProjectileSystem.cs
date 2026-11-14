using System;
using UnityEngine;

namespace ClearPath
{
    public readonly struct ShotImpact
    {
        public readonly int Obstacle;
        public readonly Vector2 Position;
        public readonly float Volume;
        public readonly float FromZ;
        public readonly float TargetZ;
        public readonly float PlayerRadius;
        public ShotImpact(int obstacle, Vector2 position, float volume, float fromZ, float targetZ, float playerRadius)
        {
            Obstacle = obstacle;
            Position = position;
            Volume = volume;
            FromZ = fromZ;
            TargetZ = targetZ;
            PlayerRadius = playerRadius;
        }
    }
    public sealed class ProjectileSystem : IDisposable
    {
        readonly ShotVisual.Pool pool;
        readonly CorridorSolver corridor;
        readonly LevelState world;
        ShotVisual visual;
        Vector2 position, destination;
        ShotImpact pendingImpact;
        float height;
        public bool Active => visual != null;
        public event Action<ShotImpact> Impact;
        public ProjectileSystem(ShotVisual.Pool pool, CorridorSolver corridor, LevelState world)
        { this.pool = pool; this.corridor = corridor; this.world = world; }
        public bool Launch(float playerZ, float remainingVolume, float shotVolume, LevelConfig level)
        {
            Clear();
            height = level.Balance.PlayerRadius(remainingVolume);
            float radius = ResourceBudget.Radius(shotVolume);
            var center = new Vector2(0, playerZ);
            int obstacle = corridor.ShotTarget(playerZ, level.Length, height, shotVolume, world.Obstacles, out destination);
            if (obstacle < 0) return false;
            pendingImpact = new ShotImpact(obstacle, destination, shotVolume, playerZ, level.Length, height);
            position = Vector2.MoveTowards(center, destination, height + radius * 0.65f);
            if (position == destination)
            {
                Impact?.Invoke(pendingImpact);
                return true;
            }
            visual = pool.Spawn();
            visual.Launch(new Vector3(position.x, height, position.y), radius);
            return true;
        }
        public void Tick(float dt, LevelConfig level)
        {
            if (!Active) return;
            position = Vector2.MoveTowards(position, destination, level.Balance.ShotSpeed * dt);
            if (position == destination)
            {
                Clear();
                Impact?.Invoke(pendingImpact);
                return;
            }
            visual.MoveTo(new Vector3(position.x, height, position.y));
        }
        public void Clear() { if (visual != null) { pool.Despawn(visual); visual = null; } }
        public void Dispose() => Clear();
    }
}
