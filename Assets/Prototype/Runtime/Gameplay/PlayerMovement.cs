using UnityEngine;

namespace ClearPath
{
    public sealed class PlayerMovement
    {
        readonly CorridorSolver corridor;
        readonly LevelState world;
        float blockedTime;
        float warningStop = float.PositiveInfinity;
        public float Z { get; private set; }
        public bool IsMoving { get; private set; }
        public PlayerMovement(CorridorSolver corridor, LevelState world) { this.corridor = corridor; this.world = world; }
        public bool IsBlocked(float tolerance) => warningStop <= Z + tolerance;
        public float ReactionRemaining(LevelConfig level) => Mathf.Max(0, level.Balance.ReactionTime - blockedTime);
        public void Reset() { Z = 0; IsMoving = false; blockedTime = 0; warningStop = float.PositiveInfinity; }

        public bool TouchesObstacle(float fromZ, float radius)
            => corridor.FirstHit(new Vector2(0, fromZ), new Vector2(0, Z), radius,
                world.Obstacles, out _) >= 0;
        public void Tick(float dt, LevelConfig level, float volume, bool shotInProgress)
        {
            float radius = level.Balance.PlayerRadius(volume);
            warningStop = corridor.ReachableZ(Z, level.Length, radius, level.Balance.Clearance, world.Obstacles);
            float target = warningStop;
            if (target > Z + 0.001f || shotInProgress)
                blockedTime = 0;
            else if (blockedTime < level.Balance.ReactionTime)
                blockedTime += dt;

            if (blockedTime >= level.Balance.ReactionTime)
            {
                var from = new Vector2(0, Z);
                var next = new Vector2(0, Mathf.Min(level.Length, Z + level.Balance.MoveSpeed * dt));
                corridor.FirstHit(from, next, radius, world.Obstacles, out var contact);
                target = contact.y;
            }

            IsMoving = target - Z > 0.005f;
            Z = Mathf.MoveTowards(Z, target, level.Balance.MoveSpeed * dt);
        }
        public void Stop() => IsMoving = false;
    }
}
