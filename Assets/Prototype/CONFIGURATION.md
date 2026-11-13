# Clear Path — Unity prototype

Unity 6000.0.70f1, URP, Input System, installed Zenject and DOTween.

## Run

Open `Assets/Prototype/Scenes/ClearPath.unity`, then Play → START. Hold to grow a shot, release to fire towards the gate. Small shots automatically angle towards a side obstacle if it still blocks the player's path and a straight shot would miss it. Start-screen arrows select a level. PLAY AGAIN resets the current seed without reloading the scene. Tiny taps below the minimum shot volume refund the charge.

Open **Settings** from the main menu or pause menu to adjust music and effects. During play, tap **Pause** or press **Esc**. **Main Menu** ends the current run and resets to the configured starting level. **Quit Game** closes the built player.

Select **DefaultBalance → Pause Movement While Charging**. The level's **Balance** field selects the rules it uses.

- Enabled: tapping immediately stops the player and starts charging, even on a clear section of the route. Holding keeps the position fixed. Releasing permits movement again, including while the projectile is in flight.
- Disabled: movement continues while charging, with enough clearance to restore the original size if the charge is refunded.
- Changing this option while charging keeps the charge; it only changes whether movement is paused.

## Configure a level

Select **Level Configurator** in the scene to find the assigned `LevelCatalog`. Set **Initial Level Index** and the available levels on that asset. Select a `LevelConfig` in `Assets/Prototype/Configs` to edit the geometry:

| Setting | Meaning |
| --- | --- |
| Seed | Reproducible random layout; restarting preserves it |
| Length | Distance to the gate |
| Density | Obstacle coverage throughout the field |
| Clustering | Preference for nearby groups versus uniform scattering |
| Thicket Count | Dense groups placed across the route at intervals |
| Radius Range | Minimum and maximum obstacle radius |
| Display Name | Name shown in the level selector and HUD |
| Balance | Rules for movement, shooting and infection |
| Ground Color | Ground tint |
| Obstacle Models / Obstacle Material | Models and material used for ordinary obstacles |
| Rock Model / Rock Interval | Alternate rock model and how often it appears |

The catalog's **Initial Level Index** starts at 0. **Levels** controls which levels appear and in what order.

**Preview level** applies the current parameters to the open scene. **Regenerate (new seed)** increments the seed and previews a new layout. Save the scene after previewing if you want that layout visible in Edit Mode. At runtime the selected catalog config is authoritative. Add LevelConfig assets to `LevelCatalog` to extend the level list.

Generation places non-overlapping obstacles across the field. Only obstacles whose footprint reaches the road can stop or hit the player. Off-road obstacles can still be affected by infection. The camera follows the ball in a close portrait view. Adjust **Orthographic Size** for zoom and **Follow Time** for follow lag on **Main Camera → Player Follow Camera**.

## Shared balance

| Setting | Effect |
| --- | --- |
| Critical Volume | Remaining volume at which overcharging ends the run |
| Minimum Shot Volume | Minimum charge required to fire |
| Charge Per Second | How much volume transfers from the ball to the shot each second |
| Reserve Fraction | Extra starting volume as a fraction of the verified route's shot cost |
| Move Speed | Player speed along the road in world units per second |
| Clearance | Forward gap between the warning stop and first contact with a blocking obstacle |
| Pause Movement While Charging | Holds the player in place while the button is held. The player can move again on release |
| Player Radius Scale | Multiplier for the ball's visible and collision radius. The road follows the collision width and retains the pre-charge width until release or cancellation |
| Reaction Time | Time to start charging after the ball reaches the warning stop |
| Shot Speed | Projectile speed in world units per second |
| Base Blast Radius | Fixed part of the infection radius |
| Blast Per Shot Radius | Infection radius gained per unit of shot radius |
| Charged Blast Bonus | Extra infection radius for shots with a radius above 0.28 |
| Infection Link Gap | Maximum edge-to-edge gap that infection can jump within the blast radius |
| Infection Step Delay | Time between successive steps of an infection chain |
| Infection Fuse | Time before an infected obstacle is destroyed |

## Automatic resource budget

`LevelBudgetPlanner` searches a bounded set of shot sizes using the actual clearance, collision and infection rules. It then replays the resulting solution with its calculated starting resource:

`initial volume = critical volume + verified solution cost × (1 + reserve fraction)`

The default reserve adds 50% of the verified solution's expenditure above the critical remainder. A deterministic bounded retry selects another scatter if the planner cannot verify a route. Verification output is runtime data. Generation results are cached across retries and restarts, and invalidated when config or balance changes. The blast radius grows faster for larger charges to help clear thickets in fewer shots.

Volume units omit the common sphere factor 4π/3: radius = volume^(1/3). Charging transfers volume, not scale. Infection follows surface-neighbor links inside the shot's global impact radius. The ring shows that maximum envelope; disconnected objects inside it are not guaranteed to be infected. Movement and shots use swept circular footprints in XZ; obstacle height is visual. Doors open at 5 world units from the player center.

## Architecture

- `GameInstaller`: Zenject composition root; shared services and memory pools.
- `LevelConfig` / `BalanceConfig` / `LevelCatalog`: authored parameters.
- `LevelGenerator`: deterministic scattering and feasibility validation; `GeneratedLevel`: immutable-by-convention runtime result.
- `LevelBudgetPlanner`, `CorridorSolver`, `InfectionSolver`, `ResourceBudget`: geometry, budgeting and resource rules without scene or tween dependencies.
- `LevelState`: owns the obstacle state of the current run. Placement indices are stable IDs within a layout.
- `LevelConfigurator`: holds the scene's catalog reference, builds level visuals, and maps obstacle IDs to their views.
- `PlayerFollowCamera`: follows the current player view in LateUpdate and snaps to newly built players.
- `GameSession`: lifecycle and commands; `PlayerMovement`, `ProjectileSystem`, `InfectionSystem`: separate mechanics.
- `SessionPresentation`: updates the HUD and audio, and handles infection events for obstacle visuals and the pooled impact effect.
- `IChargeInput`: input-device boundary; `ApplicationLifecycle`: focus/pause adapter.
- Views and HUD: DOTween presentation, radius preview and UI.

Shot and impact visuals use Zenject memory pools. Scene obstacles are rebuilt on reset. Active tweens are killed on destruction/despawn. One projectile/infection sequence is allowed at a time, independently of movement. Backgrounding suspends play and refunds unfinished charge.

## Verification

- **Clear Path → Verify domain and reference solutions** verifies resources, geometry, deterministic generation and automatically calculated solutions.
- In Play Mode, **Clear Path → Verify session in Play Mode** exercises all catalog levels, both movement modes, immediate stop-on-tap, resume-on-release, switching modes during charge, victory/defeat, doors and restart.
- Reports and rendered previews are in `Logs/ClearPath`.
- **Clear Path → Create or rebuild prototype scene** recreates the scene while preserving existing configs. It intentionally replaces scene-layout edits; save unrelated scene work first.
- `PrototypeBatch.Run` runs verification in a separate Unity process (`-executeMethod ClearPath.Editor.PrototypeBatch.Run`, without `-quit`).

No Android build or real-device performance validation is included yet.
