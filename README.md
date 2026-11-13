# Clear Path

[Українська версія](README.uk.md)

A portrait Unity puzzle prototype. The ball spends part of its own volume to fire shots, clear obstacles, and reach the gate before it runs out of usable volume. Three seeded levels combine isolated obstacles with dense groups where a larger shot can trigger a chain reaction.

## Run locally

1. Open this project in **Unity 6.0.70f1 (6000.0.70f1)** and let Unity import the assets. The project uses URP, the Unity Input System, Zenject, and DOTween.
2. Open [ClearPath.unity](Assets/Prototype/Scenes/ClearPath.unity). Select a portrait Game view such as **9:16**, enter Play Mode, and press **Start**. Use the arrows to choose a level.
3. Hold the left mouse button or touch the screen to charge a shot. Release to fire. Press **Esc** or use **Pause** during play. **Settings** has separate music and effects sliders. **Main Menu** ends the current run.

## Gameplay decisions

- **Volume is ammunition.** Charging transfers volume from the player to the projectile. The visible radius derives from the cube root of volume, and the road narrows as the player spends it. Tiny charges are refunded. Overcharging or running out of usable volume at a blocked path ends the run.
- **Position matters.** Obstacles whose footprints reach the road can stop the ball and trigger a short reaction window. Physical contact ends the run. A small shot can automatically aim toward a side obstacle that blocks the road.
- **Infection propagates locally.** A shot hits the first obstacle along its swept path. Infection then follows neighboring obstacles within the blast radius and destroys them after a short delay. Larger charges expand the blast radius, which makes them useful against dense groups.
- **Levels are reproducible and checked.** A seed controls placement. The budget planner searches for a feasible sequence, then replays it with the calculated starting volume. The default budget adds a 50% reserve on top of the verified shot cost and the critical remainder.

## Architecture

| Area | Responsibility |
| --- | --- |
| [Configuration](Assets/Prototype/Runtime/Configuration) | ScriptableObject assets define the level catalog, obstacle distribution, and balance. |
| [Domain](Assets/Prototype/Runtime/Domain) | Swept-circle collision on the XZ plane, resource accounting, and infection propagation. |
| [Level](Assets/Prototype/Runtime/Level) | Deterministic generation and budget planning. `LevelState` holds obstacle state for the current run. Placement indices serve as stable IDs within that layout. |
| [Gameplay](Assets/Prototype/Runtime/Gameplay) | `GameSession` coordinates the run. Dedicated systems handle movement, projectile travel, and infection timing. |
| [Presentation](Assets/Prototype/Runtime/Presentation) | Camera, HUD, menus, audio, and effects. `SessionPresentation` subscribes to infection events and updates obstacle models by ID. Audio levels use an AudioMixer and persist in PlayerPrefs. |
| [Composition](Assets/Prototype/Runtime/Composition) | `GameInstaller` connects dependencies, lifecycle interfaces, and visual pools through Zenject. |
| [Editor](Assets/Prototype/Editor) | Level preview, scene tools, and verification commands in a separate editor assembly. |

## Performance

Zenject memory pools reuse projectile and impact-ring visuals. A shared particle system handles obstacle debris. The level generator caches verified layouts, scenery caches model fitting data, and the HUD reuses the movement system's stopping point. The mobile URP profile uses the SRP Batcher and limits shadow distance. Level geometry is rebuilt on restart. Android device profiling remains pending.

## Configuration and verification

Edit the assets in [Configs](Assets/Prototype/Configs). `LevelCatalog` controls the level order, and `DefaultBalance` controls movement, charge, reserve, and infection. **Pause Movement While Charging** determines whether holding a charge anchors the player. See the [configuration reference](Assets/Prototype/README.md) for the full list.

- **Clear Path → Verify domain and reference solutions** checks generation, collision, infection, and the resource budget.
- **Clear Path → Verify session in Play Mode** checks all three levels in both charge movement modes, as well as collisions, pause, restart, menus, and audio settings.
- Reports are generated locally in `Logs/ClearPath`.

## Credits

Nature, castle, and particle assets are by [Kenney](https://kenney.nl/assets). The original license files are included with each pack under [Content](Assets/Prototype/Content). Zenject and DOTween are included under [Assets/Plugins](Assets/Plugins) with their distribution notices.
