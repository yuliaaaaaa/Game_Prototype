using System;
using UnityEngine;
using Zenject;

namespace ClearPath
{
    public sealed class GameSession : IInitializable, ITickable, IDisposable
    {
        readonly LevelCatalog catalog;
        readonly LevelState world;
        readonly GameHud hud;
        readonly IChargeInput input;
        readonly ResourceBudget budget;
        readonly PlayerMovement movement;
        readonly ProjectileSystem projectiles;
        readonly InfectionSystem infection;
        readonly SessionStateMachine state;
        readonly LevelGenerator generator;
        readonly SessionPresentation presentation;
        bool charging, suspended, disposed;
        float timeScaleBeforePause;
        int inputFrame;
        public LevelConfig Level { get; private set; }
        public int LevelIndex { get; private set; }
        public SessionState State => state.State;
        public bool IsCharging => charging;
        public bool IsPaused { get; private set; }
        public bool IsBusy => projectiles.Active || infection.Active;
        public ResourceBudget Budget => budget;
        public PlayerMovement Movement => movement;
        public GameSession(LevelCatalog catalog, LevelState world, GameHud hud,
            IChargeInput input, ResourceBudget budget, PlayerMovement movement, ProjectileSystem projectiles,
            InfectionSystem infection, SessionStateMachine state, LevelGenerator generator, SessionPresentation presentation)
        {
            this.catalog = catalog; this.world = world; this.hud = hud; this.input = input;
            this.budget = budget; this.movement = movement; this.projectiles = projectiles; this.infection = infection; this.state = state;
            this.generator = generator;
            this.presentation = presentation;
        }
        public void Initialize()
        {
            hud.PrimaryClicked += Primary; hud.PreviousClicked += Previous; hud.NextClicked += Next;
            projectiles.Impact += OnImpact;
            LoadLevel(catalog.InitialLevelIndex);
            presentation.StartMusic();
        }
        public void LoadLevel(int index)
        {
            var candidate = catalog.GetLevel(index, out index);
            var layout = generator.Generate(candidate);
            Resume();
            CancelCharge(); projectiles.Clear(); infection.Clear(); movement.Reset();
            presentation.Build(candidate, layout);
            world.Reset(layout);
            LevelIndex = index; Level = candidate;
            budget.Reset(layout.StartingVolume, Level.Balance.CriticalVolume); state.Reset();
            inputFrame = Time.frameCount + 1;
            hud.ShowReady(Level.DisplayName, index, catalog.Count); Present();
        }
        public void StartGame()
        { if (state.Start()) { inputFrame = Time.frameCount + 1; hud.HideOverlay(); } }
        public void Restart() => LoadLevel(LevelIndex);
        public void ReturnToMainMenu() => LoadLevel(catalog.InitialLevelIndex);

        public bool Pause()
        {
            if (State != SessionState.Playing || IsPaused) return false;
            IsPaused = true;
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0;
            CancelCharge();
            movement.Stop();
            Present();
            return true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = timeScaleBeforePause;
            inputFrame = Time.frameCount + 1;
        }
        void Primary() { if (State == SessionState.Ready) StartGame(); else Restart(); }
        void Previous() { if (State == SessionState.Ready) LoadLevel(LevelIndex - 1); }
        void Next() { if (State == SessionState.Ready) LoadLevel(LevelIndex + 1); }
        public void BeginCharge()
        {
            if (State != SessionState.Playing || suspended || IsPaused || IsBusy || charging) return;
            if (!budget.CanAfford(Level.Balance.MinimumShotVolume)) return;
            charging = true;
            if (Level.Balance.PauseMovementWhileCharging) movement.Stop();
        }
        public void ReleaseCharge()
        {
            if (!charging) return;
            charging = false;
            if (budget.Charged < Level.Balance.MinimumShotVolume) { budget.Cancel(); return; }
            projectiles.Launch(movement.Z, budget.Remaining, budget.Release(), Level);
            presentation.Shot();
        }
        public void CancelCharge() { charging = false; budget.Cancel(); }
        public void SetSuspended(bool value) { suspended = value; if (value) CancelCharge(); }
        public void Tick()
        {
            if (disposed || IsPaused || Level == null) return;
            if (State == SessionState.Playing && !suspended && Time.frameCount > inputFrame)
            {
                if (input.Pressed && !input.IsOverUi) BeginCharge();
                if (charging && (input.Released || !input.Held)) ReleaseCharge();
            }
            Advance(Mathf.Min(Time.deltaTime, 0.05f));
        }
        public void Advance(float dt)
        {
            if (State != SessionState.Playing || suspended || IsPaused || Level == null) return;
            dt = Mathf.Max(0, dt);
            if (CheckCollision(movement.Z)) return;
            if (charging)
            {
                budget.Transfer(Level.Balance.ChargePerSecond * dt);
                if (budget.Exhausted) { Finish(false, "You transferred too much volume.\nRelease earlier and use smaller shots."); return; }
            }
            projectiles.Tick(dt, Level); infection.Tick(dt, Level.Balance);
            float previousZ = movement.Z;
            if (!(charging && Level.Balance.PauseMovementWhileCharging))
                movement.Tick(dt, Level, budget.Remaining + budget.Charged, charging || IsBusy);
            else movement.Stop();
            if (CheckCollision(previousZ)) return;
            presentation.UpdateGate(movement.Z);
            if (movement.Z >= Level.Length - 0.01f)
            { Finish(true, $"You reached the gate!\n\nVolume remaining: {budget.Remaining / budget.Initial:P0}"); return; }
            if (!charging && !IsBusy && movement.IsBlocked(0.01f) && !budget.CanAfford(Level.Balance.MinimumShotVolume))
            { Finish(false, "Not enough volume for another shot.\nUse small shots on isolated obstacles."); return; }
            Present();
        }
        bool CheckCollision(float fromZ)
        {
            if (!movement.TouchesObstacle(fromZ, Level.Balance.PlayerRadius(budget.Remaining))) return false;
            Finish(false, "You hit an obstacle.\nShoot to clear the path.");
            return true;
        }
        void OnImpact(ShotImpact impact) => infection.Begin(impact, Level.Balance);
        void Finish(bool won, string reason)
        {
            charging = false;
            if (won) budget.Cancel(); else budget.Release();
            projectiles.Clear(); infection.Clear(); movement.Stop();
            state.Finish(won);
            Present();
            presentation.Result(won, reason);
        }
        void Present() => presentation.Present(Level, budget, movement, State, charging, IsBusy);
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            Resume();
            hud.PrimaryClicked -= Primary; hud.PreviousClicked -= Previous; hud.NextClicked -= Next;
            projectiles.Impact -= OnImpact; CancelCharge(); projectiles.Clear(); infection.Clear();
            presentation.Dispose();
        }
    }
}
