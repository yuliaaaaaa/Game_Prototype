using System;
using UnityEngine;

namespace ClearPath
{
    public sealed class SessionPresentation : IDisposable
    {
        readonly LevelConfigurator world;
        readonly GameHud hud;
        readonly AimPreview aim;
        readonly GameAudio audio;
        readonly InfectionSystem infection;
        readonly PulseVisual.Pool pulses;
        PulseVisual pulse;

        public SessionPresentation(LevelConfigurator world, GameHud hud, AimPreview aim, GameAudio audio,
            InfectionSystem infection, PulseVisual.Pool pulses)
        {
            this.world = world;
            this.hud = hud;
            this.aim = aim;
            this.audio = audio;
            this.infection = infection;
            this.pulses = pulses;
            infection.ImpactStarted += ShowImpact;
            infection.ObstacleInfected += world.ShowInfection;
            infection.ObstacleDestroyed += ShowDestruction;
            infection.Cleared += ClearImpact;
        }

        public void StartMusic() => audio.StartMusic();
        public void Build(LevelConfig level, GeneratedLevel layout)
        {
            audio.Reset();
            world.Build(level, layout);
        }

        public void UpdateGate(float z) => world.Door.UpdateDistance(z);

        public void Shot()
        {
            world.Player.FireFeedback();
            audio.Shot();
        }

        public void Result(bool won, string reason)
        {
            hud.ShowResult(won, reason);
            if (won) world.Door.Celebrate();
            audio.Result(won);
        }

        public void Present(LevelConfig level, ResourceBudget budget, PlayerMovement movement, SessionState state,
            bool charging, bool busy)
        {
            var balance = level.Balance;
            float radius = balance.PlayerRadius(budget.Remaining);
            world.Player.Present(movement.Z, radius, budget.Charged, movement.IsMoving);
            world.UpdatePath(radius);
            aim.Present(movement.Z, budget.Remaining, budget.Charged, level);

            string hint = "HOLD TO CHARGE  /  RELEASE TO FIRE";
            if (state == SessionState.Ready) hint = "SMALL SHOTS. SMART CHOICES.";
            else if (charging) hint = $"RELEASE TO FIRE  /  RADIUS {balance.BlastRadius(budget.Charged):0.0}m";
            else if (busy) hint = "CLEARING THE WAY";
            else if (state == SessionState.Playing && movement.IsBlocked(0.001f))
                hint = $"HOLD TO SHOOT  /  {movement.ReactionRemaining(level):0.0}s";

            hud.Present(budget, Mathf.Clamp01(movement.Z / level.Length), hint);
        }

        void ShowImpact(Vector2 position, float radius)
        {
            pulse = pulses.Spawn();
            pulse.Play(new Vector3(position.x, 0, position.y), radius);
        }

        void ShowDestruction(int id)
        {
            world.ShowDestruction(id);
            audio.ObstacleBroken();
        }

        void ClearImpact()
        {
            if (pulse == null) return;
            pulses.Despawn(pulse);
            pulse = null;
        }

        public void Dispose()
        {
            infection.ImpactStarted -= ShowImpact;
            infection.ObstacleInfected -= world.ShowInfection;
            infection.ObstacleDestroyed -= ShowDestruction;
            infection.Cleared -= ClearImpact;
            ClearImpact();
        }
    }
}
