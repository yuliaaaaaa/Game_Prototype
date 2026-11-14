using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace ClearPath.Editor
{
    public static class PrototypeVerification
    {
        static readonly List<string> results = new List<string>();
        static void Check(bool condition, string message)
        { if (!condition) throw new Exception("FAIL: " + message); results.Add("PASS: " + message); }
        static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.0001f;

        [MenuItem("Clear Path/Verify domain and reference solutions")]
        public static void VerifyDomain()
        {
            results.Clear();
            try
            {
                var budget = new ResourceBudget(); budget.Reset(1, 0.1f); budget.Transfer(0.25f);
                Check(Near(budget.Remaining + budget.Charged, 1), "Volume is conserved during charging");
                budget.Cancel(); Check(Near(budget.Remaining, 1) && Near(budget.Charged, 0), "Cancelled charge refunds volume");
                budget.Transfer(10); Check(budget.Exhausted && Near(budget.Remaining, 0.1f), "Overcharge stops at critical volume");
                Check(Near(ResourceBudget.Radius(0.125f), 0.5f), "Radius is derived from volume, not linear scale");
                var corridor = new CorridorSolver();
                var obstacles = new List<ObstacleState> { new ObstacleState(new Vector2(0.7f, 5), 0.4f) };
                Check(corridor.ReachableZ(0, 10, 0.5f, 0.05f, obstacles) < 5, "Player footprint catches off-center obstacles");
                Check(Near(corridor.ReachableZ(0, 10, 0.2f, 0.05f, obstacles), 10), "A smaller player fits through the same gap");
                var roadside = new[] { new ObstacleState(new Vector2(0.95f, 5), 0.4f) };
                Check(Near(corridor.ReachableZ(0, 10, 0.5f, 0.24f, roadside), 10),
                    "Forward clearance never makes a roadside obstacle block movement");
                obstacles = new List<ObstacleState> { new ObstacleState(new Vector2(0, 5), 0.4f), new ObstacleState(new Vector2(0, 9), 0.4f) };
                Check(corridor.FirstHit(Vector2.zero, new Vector2(0, 15), 0.2f, obstacles, out _) == 0, "Swept projectile hits the first obstacle even in a long step");
                var infection = new InfectionSolver();
                var state = new SessionStateMachine(); state.Finish(true); Check(state.State == SessionState.Ready, "Cannot win before starting");
                state.Start(); state.Finish(false); Check(!state.Start() && state.State == SessionState.Lost, "Terminal state requires reset");
                var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(PrototypeBuilder.Root + "/Configs/LevelCatalog.asset");
                var balance = catalog.Levels[0].Balance;
                var sideBlocker = new[] { new ObstacleState(new Vector2(0.78f, 5), 0.4f) };
                var origin = new Vector2(0, corridor.ReachableZ(0, 10, balance.PlayerRadius(0.343f), balance.Clearance, sideBlocker));
                const float smallShot = 0.013f;
                float shotRadius = ResourceBudget.Radius(smallShot);
                Check(corridor.FirstHit(origin, new Vector2(0, 10), shotRadius, sideBlocker, out _) < 0,
                    "Straight small shot misses the obstacle blocking the player's edge");
                Check(corridor.ShotTarget(origin.y, 10, balance.PlayerRadius(0.343f), smallShot, sideBlocker, out _) == 0,
                    "The same small shot can clear the side blocker with automatic aim");
                VerifyShotChain(corridor, infection, balance);
                foreach (var level in catalog.Levels) VerifyReference(level, corridor, infection);
                Write("DomainResults.txt");
                Debug.Log("Clear Path domain verification passed: " + results.Count + " assertions.");
            }
            catch (Exception error) { results.Add(error.ToString()); Write("DomainResults.txt"); throw; }
        }
        static void VerifyReference(LevelConfig level, CorridorSolver corridor, InfectionSolver infection)
        {
            Check(level.IsValid(out _), level.name + " configuration is valid");
            var generator = new LevelGenerator(new LevelBudgetPlanner(corridor, infection));
            var layout = generator.Generate(level);
            var duplicate = new LevelGenerator(new LevelBudgetPlanner(corridor, infection)).Generate(level);
            bool identical = layout.Obstacles.Length == duplicate.Obstacles.Length;
            for (int i = 0; identical && i < layout.Obstacles.Length; i++)
                identical &= layout.Obstacles[i].Position == duplicate.Obstacles[i].Position && Near(layout.Obstacles[i].Radius, duplicate.Obstacles[i].Radius);
            Check(identical && Near(layout.StartingVolume, duplicate.StartingVolume), level.name + " seed deterministically reproduces geometry and resource budget");
            var variant = UnityEngine.Object.Instantiate(level);
            try
            {
                variant.Seed++;
                var different = generator.Generate(variant);
                Check(different.Obstacles[0].Position != layout.Obstacles[0].Position, level.name + " changing seed changes placement");
                variant.Density *= 0.7f;
                var sparse = generator.Generate(variant);
                Check(sparse.Obstacles.Length < different.Obstacles.Length, level.name + " lower density produces fewer obstacles");
            }
            finally { UnityEngine.Object.DestroyImmediate(variant); }
            Check(layout.Obstacles.Length > 200, level.name + " generates a dense field instead of authored rows");
            var obstacles = layout.CreateStates();
            float volume = layout.StartingVolume, z = 0, total = 0;
            foreach (float shot in layout.SolutionShots)
            {
                z = corridor.ReachableZ(z, level.Length, level.Balance.PlayerRadius(volume), level.Balance.Clearance, obstacles);
                volume -= shot; total += shot;
                float radius = level.Balance.PlayerRadius(volume);
                int seed = corridor.ShotTarget(z, level.Length, radius, shot, obstacles, out var impact);
                foreach (var hit in infection.Solve(seed, impact, shot, level.Balance, z, level.Length, radius, obstacles))
                    obstacles[hit.Index].Alive = false;
            }
            z = corridor.ReachableZ(z, level.Length, level.Balance.PlayerRadius(volume), level.Balance.Clearance, obstacles);
            Check(Near(z, level.Length), level.name + " automatically planned shots clear the whole corridor");
            float reserve = Mathf.Max(total * level.Balance.ReserveFraction, level.Balance.MinimumReserveVolume);
            Check(Near(volume - level.Balance.CriticalVolume, reserve),
                $"{level.name} retains its configured spending reserve above critical");
        }

        static void VerifyShotChain(CorridorSolver corridor, InfectionSolver infection, BalanceConfig balance)
        {
            const float playerRadius = 0.4f;
            var states = new[]
            {
                new ObstacleState(new Vector2(0.85f, 1), 0.4f),
                new ObstacleState(new Vector2(0, 2), 0.4f),
                new ObstacleState(new Vector2(0, 3.5f), 0.4f),
                new ObstacleState(new Vector2(0, 5), 0.4f),
                new ObstacleState(new Vector2(0.85f, 3), 0.4f)
            };
            float small = balance.MinimumShotVolume;
            float large = Mathf.Max(small * 16, 0.03f);
            int seed = corridor.ShotTarget(0, 8, playerRadius, small, states, out var impact);
            Check(seed == 1, "Auto aim skips the closer roadside obstacle");
            Check(infection.Solve(seed, impact, small, balance, 0, 8, playerRadius, states).Count == 1,
                "A weak shot clears just the first blocker");
            seed = corridor.ShotTarget(0, 8, playerRadius, large, states, out impact);
            var hits = infection.Solve(seed, impact, large, balance, 0, 8, playerRadius, states);
            Check(hits.Select(hit => hit.Index).SequenceEqual(new[] { 1, 2, 3 }) && hits[2].Depth > hits[1].Depth,
                "A charged shot spreads behind the first blocker and spares roadside obstacles");
            Check(corridor.ShotTarget(0, 8, playerRadius, large, new[] { states[0], states[4] }, out _) < 0,
                "No shot target is selected when the route is clear");
        }
        [MenuItem("Clear Path/Verify session in Play Mode")]
        public static void VerifySession()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
            results.Clear();
            var context = UnityEngine.Object.FindFirstObjectByType<SceneContext>();
            var session = context.Container.Resolve<GameSession>();
            var catalog = context.Container.Resolve<LevelCatalog>();
            var originalModes = new Dictionary<BalanceConfig, bool>();
            foreach (var level in catalog.Levels)
                originalModes[level.Balance] = level.Balance.PauseMovementWhileCharging;
            var world = context.Container.Resolve<LevelConfigurator>();
            var levelState = context.Container.Resolve<LevelState>();
            var hud = context.Container.Resolve<GameHud>();
            var selectionLabel = Array.Find(hud.GetComponentsInChildren<UnityEngine.UI.Text>(true),
                label => label.name == "Level selection");
            try
            {
                session.SetSuspended(false);
                session.LoadLevel(catalog.InitialLevelIndex);
                Check(world.Palette.ShotSound && world.Palette.BreakSound && world.Palette.VictorySound && world.Palette.DefeatSound,
                    "All four gameplay sound clips are assigned");
                Check(world.Palette.BackgroundMusic && world.Palette.MusicOutput && world.Palette.EffectsOutput,
                    "Background music and separate AudioMixer groups are assigned");
                VerifyAudioVolumes(context.Container.Resolve<AudioVolumeSettings>(), world.Palette, hud);
                VerifyMenus(session, catalog, hud, levelState);
                foreach (bool movingShots in new[] { false, true })
                {
                    for (int index = 0; index < catalog.Levels.Length; index++)
                    {
                        session.LoadLevel(index); session.StartGame();
                        session.Level.Balance.PauseMovementWhileCharging = !movingShots;
                        foreach (float shot in world.Data.SolutionShots)
                        {
                            if (session.State == SessionState.Won) break;
                            StepUntil(session, () => !session.Movement.IsMoving && !session.IsBusy, 2000, true);
                            session.BeginCharge(); Check(session.IsCharging, $"Level {index}, moving shots={movingShots}: charge starts");
                            session.Advance(shot / session.Level.Balance.ChargePerSecond);
                            session.ReleaseCharge();
                            StepUntil(session, () => !session.IsBusy, 2000);
                        }
                        StepUntil(session, () => session.State != SessionState.Playing, 3000);
                        Check(session.State == SessionState.Won, $"Level {index}, moving shots={movingShots}: complete playthrough");
                        Check(!selectionLabel.gameObject.activeSelf, "Victory hides the level selection label");
                        Check(world.Door.IsOpen, "Door opened during approach");
                        session.Restart(); Check(session.State == SessionState.Ready && Near(session.Movement.Z, 0) && !session.IsBusy, "Restart resets the session without reloading scene");
                        Check(selectionLabel.gameObject.activeSelf == (catalog.Count > 1), "Start screen displays the label with the level selector");
                        Check(levelState.Obstacles.All(obstacle => obstacle.Alive && !obstacle.Infected), "Restart restores every obstacle");
                    }
                }
                session.LoadLevel(0);
                session.Level.Balance.PauseMovementWhileCharging = false;
                session.StartGame();
                float edge = world.Player.Radius + 0.4f + 0.04f;
                levelState.Reset(new[] { new ObstaclePlacement(new Vector2(edge, 3), 0.4f, 1) });
                StepUntil(session, () => session.Movement.Z > 4 || session.State != SessionState.Playing, 400);
                Check(session.State == SessionState.Playing && session.Movement.Z > 4,
                    "The player passes a live obstacle wholly outside the road without stopping");
                session.Restart();
                session.StartGame();
                levelState.Reset(new[] { new ObstaclePlacement(new Vector2(0, session.Movement.Z), 0.4f, 1) });
                session.Advance(0);
                Check(session.State == SessionState.Lost, "Contact with a live obstacle causes immediate defeat");
                Check(Array.Exists(hud.GetComponentsInChildren<UnityEngine.UI.Text>(true),
                    label => label.name == "Description" && label.text.Contains("You hit an obstacle")),
                    "The result screen displays the collision as the cause of defeat");
                session.Restart();
                session.StartGame();
                levelState.Reset(new[] { new ObstaclePlacement(new Vector2(0, 3), 0.5f, 1) });
                StepUntil(session, () => !session.Movement.IsMoving, 400, true);
                Check(session.State == SessionState.Playing && session.Movement.Z < 3 - 0.5f - world.Player.Radius,
                    "The reaction window stops the ball with a visible gap");
                StepUntil(session, () => session.State == SessionState.Lost, 400);
                Check(Near(session.Movement.Z, 3 - 0.5f - world.Player.Radius) &&
                    Array.Exists(hud.GetComponentsInChildren<UnityEngine.UI.Text>(true),
                        label => label.name == "Description" && label.text.Contains("You hit an obstacle")),
                    "An idle player reaches actual contact and loses with the collision reason");
                session.Restart();
                var backdrop = world.Door.transform.parent.Find("Forest Beyond Gate");
                Check(backdrop && backdrop.childCount > 0, "Forest scenery continues beyond the gate");
                Check(world.Door.GetComponentsInChildren<LineRenderer>(true).Length == 4,
                    "The finish has three directional markings and a victory ripple");
                VerifyPortraitFrame(world, session);
                world.Door.UpdateDistance(session.Level.Length - 5.01f);
                Check(!world.Door.IsOpen, "Door stays closed beyond 5 meters");
                world.Door.UpdateDistance(session.Level.Length - 5);
                Check(world.Door.IsOpen, "Door opens at exactly 5 meters");
                session.LoadLevel(0); session.StartGame(); session.Advance(0.1f); session.BeginCharge();
                Check(session.IsCharging && session.Movement.IsMoving, "Shooting while moving is allowed when enabled");
                session.Advance(0.2f); session.Level.Balance.PauseMovementWhileCharging = true;
                float stoppedAt = session.Movement.Z; float chargeBefore = session.Budget.Charged;
                session.Advance(0.1f);
                Check(session.IsCharging && Near(session.Movement.Z, stoppedAt) && session.Budget.Charged > chargeBefore,
                    "Enabling pause during charge freezes movement without cancelling charge");
                session.CancelCharge(); session.Restart(); session.StartGame(); session.Advance(0.1f);
                Check(session.Movement.IsMoving, "Player is moving freely before tap");
                stoppedAt = session.Movement.Z; session.BeginCharge();
                Check(session.IsCharging && !session.Movement.IsMoving, "Tap stops a moving player immediately and starts charging");
                session.Advance(0.2f);
                Check(Near(session.Movement.Z, stoppedAt) && session.Budget.Charged > 0, "Position stays fixed while held");
                session.ReleaseCharge(); session.Advance(0.05f);
                Check(session.Movement.Z > stoppedAt, "Release resumes movement while the shot is active");
                session.Restart(); session.StartGame(); session.BeginCharge(); session.Advance(0.01f); session.ReleaseCharge();
                Check(!session.IsBusy && Near(session.Budget.Remaining, session.Budget.Initial), "Tiny tap is refunded");
                levelState.Reset(Array.Empty<ObstaclePlacement>());
                session.BeginCharge(); session.Advance(0.2f); session.ReleaseCharge();
                Check(!session.IsBusy && Near(session.Budget.Remaining, session.Budget.Initial),
                    "A charged shot without a threatening obstacle is refunded");
                session.BeginCharge(); session.Advance(0.1f); session.SetSuspended(true);
                Check(!session.IsCharging && Near(session.Budget.Remaining, session.Budget.Initial), "Backgrounding cancels and refunds charge");
                VerifyChargeRefund(session, levelState);
                session.SetSuspended(false); session.BeginCharge(); session.Advance(100);
                Check(session.State == SessionState.Lost, "Overcharging ends in defeat");
                Check(!selectionLabel.gameObject.activeSelf, "Defeat hides the level selection label");
                session.Restart(); session.StartGame(); session.Advance(2); session.BeginCharge(); session.Advance(0.5f); session.ReleaseCharge(); session.Restart();
                Check(!session.IsBusy && session.State == SessionState.Ready && Near(session.Budget.Remaining, session.Budget.Initial), "Restart clears an in-flight projectile and resource state");
                session.StartGame();
                session.Budget.Transfer(session.Budget.Remaining - session.Budget.Critical - session.Level.Balance.MinimumShotVolume * 0.5f); session.Budget.Release();
                StepUntil(session, () => session.State == SessionState.Lost, 2000);
                Check(session.State == SessionState.Lost, "Blocked player with insufficient ammunition loses");
                session.Restart();
                levelState.Reset(Array.Empty<ObstaclePlacement>());
                session.Budget.Transfer(session.Budget.Remaining - session.Budget.Critical - session.Level.Balance.MinimumShotVolume * 0.5f); session.Budget.Release(); session.StartGame();
                StepUntil(session, () => session.State == SessionState.Won, 3000);
                Check(session.State == SessionState.Won, "Low ammunition does not cause defeat on a cleared path");
                Write("SessionResults.txt"); Debug.Log("Clear Path session verification passed: " + results.Count + " assertions.");
            }
            catch (Exception error) { results.Add(error.ToString()); Write("SessionResults.txt"); throw; }
            finally
            {
                foreach (var entry in originalModes) entry.Key.PauseMovementWhileCharging = entry.Value;
                session.LoadLevel(0);
            }
        }
        static void VerifyAudioVolumes(AudioVolumeSettings settings, VisualPalette palette, GameHud hud)
        {
            const string musicKey = "ClearPath.MusicVolume";
            const string effectsKey = "ClearPath.EffectsVolume";
            bool hadMusic = PlayerPrefs.HasKey(musicKey), hadEffects = PlayerPrefs.HasKey(effectsKey);
            float music = settings.Music, effects = settings.Effects;
            var mixer = palette.MusicOutput.audioMixer;
            var sliders = hud.GetComponentInChildren<AudioMenu>(true).GetComponentsInChildren<UnityEngine.UI.Slider>(true);
            var musicSlider = Array.Find(sliders, slider => slider.transform.parent.name == "MUSIC");
            var effectsSlider = Array.Find(sliders, slider => slider.transform.parent.name == "SOUND EFFECTS");
            Click(hud, "Main Settings");
            try
            {
                Check(Near(musicSlider.value, music) && Near(effectsSlider.value, effects), "Audio sliders load the saved levels");
                musicSlider.value = 0.25f;
                effectsSlider.value = 0;
                var reloaded = new AudioVolumeSettings(mixer);
                Check(Near(reloaded.Music, 0.25f) && Near(reloaded.Effects, 0), "Audio levels persist through PlayerPrefs");
                Check(mixer.GetFloat("MusicVolume", out float musicDb) && Near(musicDb, 20 * Mathf.Log10(0.25f)) &&
                    mixer.GetFloat("EffectsVolume", out float effectsDb) && Near(effectsDb, -80),
                    "Music and effects apply independently to the mixer, including mute");
            }
            finally
            {
                musicSlider.value = music;
                effectsSlider.value = effects;
                if (hadMusic) PlayerPrefs.SetFloat(musicKey, music); else PlayerPrefs.DeleteKey(musicKey);
                if (hadEffects) PlayerPrefs.SetFloat(effectsKey, effects); else PlayerPrefs.DeleteKey(effectsKey);
                PlayerPrefs.Save();
                settings.Apply();
                Click(hud, "Settings Back");
            }
        }

        static void VerifyMenus(GameSession session, LevelCatalog catalog, GameHud hud, LevelState levelState)
        {
            var overlay = hud.transform.Find("Start and result overlay").gameObject;
            var pauseWindow = hud.transform.Find("Pause Window").gameObject;
            var settingsWindow = hud.transform.Find("Settings Window").gameObject;
            Check(overlay.activeSelf && !settingsWindow.activeSelf && !pauseWindow.activeSelf,
                "Main menu starts with settings and pause windows closed");
            Click(hud, "Main Settings");
            Check(!overlay.activeSelf && settingsWindow.activeSelf, "Main menu opens a separate settings window");
            Click(hud, "Settings Back");
            Check(overlay.activeSelf && !settingsWindow.activeSelf, "Settings returns to the main menu");

            Click(hud, "Start or retry");
            session.Advance(0.2f);
            session.BeginCharge();
            session.Advance(0.3f);
            float stoppedAt = session.Movement.Z;
            Click(hud, "Pause");
            session.Advance(2);
            Check(session.IsPaused && Near(Time.timeScale, 0) && pauseWindow.activeSelf && Near(stoppedAt, session.Movement.Z),
                "Pause freezes gameplay");
            Check(!session.IsCharging && Near(session.Budget.Initial, session.Budget.Remaining),
                "Pause refunds an unfinished charge");

            Click(hud, "Pause Settings");
            Check(session.IsPaused && settingsWindow.activeSelf && !pauseWindow.activeSelf,
                "Settings opened from pause keeps gameplay frozen");
            Click(hud, "Settings Back");
            Check(session.IsPaused && pauseWindow.activeSelf && !settingsWindow.activeSelf, "Settings returns to pause");
            Click(hud, "Resume");
            session.Advance(0.2f);
            Check(!session.IsPaused && Time.timeScale > 0 && session.Movement.Z > stoppedAt, "Resume continues the same run");

            levelState.Obstacles[0].Alive = false;
            Click(hud, "Pause");
            Click(hud, "Return to Main");
            catalog.GetLevel(catalog.InitialLevelIndex, out int initialIndex);
            Check(!session.IsPaused && session.State == SessionState.Ready && session.LevelIndex == initialIndex &&
                Near(session.Movement.Z, 0) && Near(session.Budget.Initial, session.Budget.Remaining) &&
                levelState.Obstacles.All(obstacle => obstacle.Alive), "Main menu resets level, progress, ammunition and obstacles");
            Check(FindButton(hud, "Main Quit") && FindButton(hud, "Pause Quit"), "Both menus provide an exit button");
        }

        static UnityEngine.UI.Button FindButton(GameHud hud, string name)
            => Array.Find(hud.GetComponentsInChildren<UnityEngine.UI.Button>(true), button => button.name == name);

        static void Click(GameHud hud, string name) => FindButton(hud, name).onClick.Invoke();
        static void VerifyChargeRefund(GameSession session, LevelState levelState)
        {
            session.Level.Balance.PauseMovementWhileCharging = false;
            session.SetSuspended(false);
            session.Restart();
            session.StartGame();
            float radius = session.Level.Balance.PlayerRadius(session.Budget.Initial);
            float contact = 4 - radius - 0.4f;
            levelState.Reset(new[] { new ObstaclePlacement(new Vector2(0, 4), 0.4f, 1) });
            StepUntil(session, () => !session.Movement.IsMoving, 1000, true);
            float stop = session.Movement.Z;
            session.BeginCharge();
            session.Advance(1);
            Check(session.Movement.Z > contact, "A shrinking ball can advance while charging");
            session.SetSuspended(true);
            Check(Near(session.Movement.Z, stop) && Near(session.Budget.Remaining, session.Budget.Initial),
                "Refunding a charge cannot leave a restored player inside a narrow passage");
            session.Restart();
            session.StartGame();
        }

        static void VerifyPortraitFrame(LevelConfigurator world, GameSession session)
        {
            var camera = Camera.main;
            var followCamera = camera.GetComponent<PlayerFollowCamera>();
            try
            {
                foreach (float aspect in new[] { 9f / 16, 9f / 20 })
                {
                    camera.aspect = aspect;
                    followCamera.SnapToPlayer();
                    Vector3 player = camera.WorldToViewportPoint(world.Player.Center);
                    Check(player.x > 0.15f && player.x < 0.28f && player.y > 0.17f && player.y < 0.25f,
                        $"Portrait aspect {aspect:0.00} keeps the ball near the bottom-left corner");
                    Check(camera.orthographicSize < 8, $"Portrait aspect {aspect:0.00} uses a close view");
                }

                Vector3 start = camera.transform.position;
                session.StartGame();
                session.Advance(0.3f);
                followCamera.UpdateFollow(0.02f);
                Check(camera.transform.position.z > start.z && camera.transform.position.z < start.z + session.Movement.Z,
                    "Camera follows forward movement with damping");
                followCamera.UpdateFollow(2);
                Check(Near(camera.transform.position.z, start.z + session.Movement.Z),
                    "Camera settles at its offset from the player");

                session.Restart();
                followCamera.UpdateFollow(0.02f);
                Check((camera.transform.position - start).sqrMagnitude < 0.000001f,
                    "Camera snaps to the new player after a restart");
            }
            finally { camera.ResetAspect(); followCamera.SnapToPlayer(); }
        }

        static void StepUntil(GameSession session, Func<bool> condition, int limit, bool advanceFirst = false)
        {
            if (advanceFirst) session.Advance(0.02f);
            for (int i = 0; i < limit && !condition(); i++) session.Advance(0.02f);
            if (!condition()) throw new Exception("Session did not reach expected condition before timeout.");
        }
        static void Write(string filename)
        { Directory.CreateDirectory("Logs/ClearPath"); File.WriteAllLines("Logs/ClearPath/" + filename, results); }
    }
}
