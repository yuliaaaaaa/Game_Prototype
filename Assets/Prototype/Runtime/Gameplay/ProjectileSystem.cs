using System;
using UnityEngine;

namespace ClearPath
{
    public readonly struct ShotImpact
    {
        public readonly int Obstacle;
        public readonly Vector2 Position;
        public readonly float Volume;
        public ShotImpact(int obstacle, Vector2 position, float volume) { Obstacle = obstacle; Position = position; Volume = volume; }
    }
    public sealed class ProjectileSystem : IDisposable
    {
        readonly ShotVisual.Pool pool;
        readonly CorridorSolver corridor;
        readonly LevelState world;
        ShotVisual visual;
        Vector2 position, destination;
        int obstacle;
        float volume, height;
        public bool Active => visual != null;
        public event Action<ShotImpact> Impact;
        public ProjectileSystem(ShotVisual.Pool pool, CorridorSolver corridor, LevelState world)
        { this.pool = pool; this.corridor = corridor; this.world = world; }
        public void Launch(float playerZ, float remainingVolume, float shotVolume, LevelConfig level)
        {
            Clear();
            volume = shotVolume;
            height = level.Balance.PlayerRadius(remainingVolume);
            float radius = ResourceBudget.Radius(volume);
            var center = new Vector2(0, playerZ);
            var end = corridor.ShotEnd(playerZ, level, remainingVolume, volume, world.Obstacles);
            obstacle = corridor.FirstHit(center, end, radius, world.Obstacles, out destination);
            position = Vector2.MoveTowards(center, destination, height + radius * 0.65f);
            if (position == destination)
            {
                Impact?.Invoke(new ShotImpact(obstacle, destination, volume));
                return;
            }
            visual = pool.Spawn();
            visual.Launch(new Vector3(position.x, height, position.y), radius);
        }
        public void Tick(float dt, LevelConfig level)
        {
            if (!Active) return;
            position = Vector2.MoveTowards(position, destination, level.Balance.ShotSpeed * dt);
            if (position == destination)
            {
                Clear();
                Impact?.Invoke(new ShotImpact(obstacle, destination, volume));
                return;
            }
            visual.MoveTo(new Vector3(position.x, height, position.y));
        }
        public void Clear() { if (visual != null) { pool.Despawn(visual); visual = null; } }
        public void Dispose() => Clear();
    }
}
