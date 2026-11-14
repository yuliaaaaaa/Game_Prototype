using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public sealed class InfectionSystem
    {
        readonly InfectionSolver solver;
        readonly LevelState world;
        List<InfectionHit> hits;
        float elapsed;
        public bool Active => hits != null;
        public event System.Action<Vector2, float> ImpactStarted;
        public event System.Action<int> ObstacleInfected;
        public event System.Action<int> ObstacleDestroyed;
        public event System.Action Cleared;

        public InfectionSystem(InfectionSolver solver, LevelState world)
        { this.solver = solver; this.world = world; }
        public void Begin(ShotImpact impact, BalanceConfig balance)
        {
            Clear(); if (impact.Obstacle < 0) return;
            float radius = balance.BlastRadius(impact.Volume);
            hits = solver.Solve(impact.Obstacle, impact.Position, impact.Volume, balance,
                impact.FromZ, impact.TargetZ, impact.PlayerRadius, world.Obstacles);
            ImpactStarted?.Invoke(impact.Position, radius);
        }
        public void Tick(float dt, BalanceConfig balance)
        {
            if (!Active) return;
            elapsed += dt; bool pending = false;
            foreach (var hit in hits)
            {
                var state = world.Obstacles[hit.Index];
                float time = hit.Depth * balance.InfectionStepDelay;
                if (!state.Infected && elapsed >= time)
                { state.Infected = true; ObstacleInfected?.Invoke(hit.Index); }
                if (state.Alive && elapsed >= time + balance.InfectionFuse)
                { state.Alive = false; ObstacleDestroyed?.Invoke(hit.Index); }
                if (state.Alive) pending = true;
            }
            if (!pending && elapsed > 0.45f) Clear();
        }
        public void Clear()
        {
            if (!Active) return;
            hits = null;
            elapsed = 0;
            Cleared?.Invoke();
        }
    }
}
