using UnityEngine;

namespace ClearPath
{
    public sealed class AimPreview
    {
        readonly LevelConfigurator world;
        readonly CorridorSolver corridor;
        readonly LevelState levelState;
        readonly Vector3[] points = new Vector3[64];
        public AimPreview(LevelConfigurator world, CorridorSolver corridor, LevelState levelState)
        { this.world = world; this.corridor = corridor; this.levelState = levelState; }
        public void Present(float playerZ, float remainingVolume, float charge, LevelConfig level)
        {
            var ring = world.AimRing;
            ring.enabled = false;
            if (charge <= 0) return;
            var origin = new Vector2(0, playerZ);
            var end = corridor.ShotEnd(playerZ, level, remainingVolume, charge, levelState.Obstacles);
            world.Player.AimCharge(end - origin);
            ring.enabled = charge >= level.Balance.MinimumShotVolume;
            if (!ring.enabled) return;
            ring.widthMultiplier = Mathf.Lerp(0.045f, 0.085f, Mathf.Clamp01(charge / 0.25f));
            ring.startColor = ring.endColor = new Color(1, 0.75f, 0.29f, 0.8f);
            int seed = corridor.FirstHit(origin, end, ResourceBudget.Radius(charge), levelState.Obstacles, out var hit);
            ring.enabled = seed >= 0;
            if (!ring.enabled) return;
            float radius = level.Balance.BlastRadius(charge);
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / points.Length;
                points[i] = new Vector3(hit.x + Mathf.Cos(angle) * radius, 0.09f, hit.y + Mathf.Sin(angle) * radius);
            }
            ring.SetPositions(points);
        }
    }
}
