# Clear Path

[English](#english) · [Українська](#українська)

## English

A portrait Unity puzzle prototype. The ball spends part of its own volume to fire shots, clear obstacles, and reach the gate before it runs out of usable volume. Three seeded levels combine isolated obstacles with dense groups where a larger shot can trigger a chain reaction.

### Run locally

1. Open this project in **Unity 6.0.70f1 (6000.0.70f1)** and let Unity import the assets. The project uses URP, the Unity Input System, Zenject, and DOTween.
2. Open [ClearPath.unity](Assets/Prototype/Scenes/ClearPath.unity). Select a portrait Game view such as **9:16**, enter Play Mode, and press **Start**. Use the arrows to choose a level.
3. Hold the left mouse button or touch the screen to charge a shot. Release to fire. Press **Esc** or use **Pause** during play. **Settings** has separate music and effects sliders. **Main Menu** ends the current run.

### Gameplay decisions

- **Volume is ammunition.** Charging transfers volume from the player to the projectile. The visible radius derives from the cube root of volume, and the road narrows as the player spends it. Tiny charges are refunded. Overcharging or running out of usable volume at a blocked path ends the run.
- **Position matters.** Obstacles whose footprints reach the road can stop the ball and trigger a short reaction window. Physical contact ends the run. A small shot can automatically aim toward a side obstacle that blocks the road.
- **Infection propagates locally.** A shot hits the first obstacle along its swept path. Infection then follows neighboring obstacles within the blast radius and destroys them after a short delay. Larger charges expand the blast radius, which makes them useful against dense groups.
- **Levels are reproducible and checked.** A seed controls placement. The budget planner searches for a feasible sequence, then replays it with the calculated starting volume. The default budget adds a 50% reserve on top of the verified shot cost and the critical remainder.

### Architecture

| Area | Responsibility |
| --- | --- |
| [Configuration](Assets/Prototype/Runtime/Configuration) | ScriptableObject assets define the level catalog, obstacle distribution, and balance. |
| [Domain](Assets/Prototype/Runtime/Domain) | Swept-circle collision on the XZ plane, resource accounting, and infection propagation. |
| [Level](Assets/Prototype/Runtime/Level) | Deterministic generation and budget planning. `LevelState` holds obstacle state for the current run. Placement indices serve as stable IDs within that layout. |
| [Gameplay](Assets/Prototype/Runtime/Gameplay) | `GameSession` coordinates the run. Dedicated systems handle movement, projectile travel, and infection timing. |
| [Presentation](Assets/Prototype/Runtime/Presentation) | Camera, HUD, menus, audio, and effects. `SessionPresentation` subscribes to infection events and updates obstacle models by ID. Audio levels use an AudioMixer and persist in PlayerPrefs. |
| [Composition](Assets/Prototype/Runtime/Composition) | `GameInstaller` connects dependencies, lifecycle interfaces, and visual pools through Zenject. |
| [Editor](Assets/Prototype/Editor) | Level preview, scene tools, and verification commands in a separate editor assembly. |

### Performance

Zenject memory pools reuse projectile and impact-ring visuals. A shared particle system handles obstacle debris. The level generator caches verified layouts, scenery caches model fitting data, and the HUD reuses the movement system's stopping point. The mobile URP profile uses the SRP Batcher and limits shadow distance. Level geometry is rebuilt on restart. Android device profiling remains pending.

### Configuration and verification

Edit the assets in [Configs](Assets/Prototype/Configs). `LevelCatalog` controls the level order, and `DefaultBalance` controls movement, charge, reserve, and infection. **Pause Movement While Charging** determines whether holding a charge anchors the player. See the [configuration reference](Assets/Prototype/CONFIGURATION.md) for the full list.

- **Clear Path → Verify domain and reference solutions** checks generation, collision, infection, and the resource budget.
- **Clear Path → Verify session in Play Mode** checks all three levels in both charge movement modes, as well as collisions, pause, restart, menus, and audio settings.
- Reports are generated locally in `Logs/ClearPath`.

### Credits

Nature, castle, and particle assets are by [Kenney](https://kenney.nl/assets). The original license files are included with each pack under [Content](Assets/Prototype/Content). Zenject and DOTween are included under [Assets/Plugins](Assets/Plugins) with their distribution notices.

---

## Українська

Портретний прототип головоломки на Unity. Куля витрачає частину власного об’єму на постріли, щоб розчистити перешкоди й дістатися воріт до вичерпання ресурсу. Три рівні з відтворюваною генерацією поєднують поодинокі перешкоди зі щільними групами, де сильний постріл запускає ланцюгову реакцію.

### Запуск

1. Відкрий проєкт у **Unity 6.0.70f1 (6000.0.70f1)** і дочекайся імпорту. Проєкт використовує URP, Unity Input System, Zenject і DOTween.
2. Відкрий [ClearPath.unity](Assets/Prototype/Scenes/ClearPath.unity). Обери портретний формат вікна Game, наприклад **9:16**, запусти Play Mode і натисни **Start**. Стрілки перемикають рівень.
3. Затисни ліву кнопку миші або торкнися екрана, щоб зарядити постріл. Відпусти, щоб вистрілити. Під час гри натисни **Esc** або **Pause**. У **Settings** можна окремо змінити гучність музики й ефектів. **Main Menu** завершує поточне проходження.

### Підходи до геймплею

- **Об’єм кулі є боєзапасом.** Під час заряду об’єм переходить від гравця до снаряда. Видимий радіус залежить від кубічного кореня об’єму, а доріжка звужується в міру його витрати. Замалий заряд повертається. Перезаряд або вичерпання ресурсу перед заблокованим проходом призводить до поразки.
- **Розташування перешкоди має значення.** Перешкоди, чий контур заходить на дорогу, можуть зупинити кулю й запустити короткий відлік. Контакт завершує гру поразкою. Малий постріл може автоматично націлитися на бічну перешкоду, яка блокує дорогу.
- **Зараження поширюється між сусідами.** Снаряд влучає в першу перешкоду на траєкторії. Далі зараження переходить між сусідніми перешкодами в радіусі удару, після чого вони руйнуються із затримкою. Сильніший заряд збільшує радіус і допомагає розчищати щільні групи.
- **Генерація відтворюється й перевіряється.** Розташування визначає seed. Планувальник знаходить можливу послідовність пострілів і повторно перевіряє її з розрахованим стартовим об’ємом. Типовий баланс додає запас у 50% від перевіреної витрати понад критичний залишок.

### Архітектура

| Частина | Відповідальність |
| --- | --- |
| [Configuration](Assets/Prototype/Runtime/Configuration) | ScriptableObject-асети задають каталог рівнів, розподіл перешкод і баланс. |
| [Domain](Assets/Prototype/Runtime/Domain) | Перевірка зіткнень рухомих кіл на площині XZ, облік об’єму та поширення зараження. |
| [Level](Assets/Prototype/Runtime/Level) | Детермінована генерація й розрахунок бюджету. `LevelState` зберігає стан перешкод поточного проходження. Індекс розміщення є стабільним ID у межах розкладки. |
| [Gameplay](Assets/Prototype/Runtime/Gameplay) | `GameSession` координує проходження. Окремі системи відповідають за рух, політ снаряда та час зараження. |
| [Presentation](Assets/Prototype/Runtime/Presentation) | Камера, HUD, меню, аудіо й ефекти. `SessionPresentation` слухає події зараження й оновлює моделі за ID. Гучність керується через AudioMixer і зберігається в PlayerPrefs. |
| [Composition](Assets/Prototype/Runtime/Composition) | `GameInstaller` підключає залежності, життєвий цикл і пули візуальних об’єктів через Zenject. |
| [Editor](Assets/Prototype/Editor) | Попередній перегляд рівнів, інструменти сцени й перевірки в окремій збірці редактора. |

### Оптимізація

Пули Zenject повторно використовують візуал снаряда та кільце удару. Уламки використовують спільну систему частинок. Генератор кешує перевірені розкладки, декорації кешують параметри підгонки моделей, а HUD використовує розраховану системою руху точку зупинки. У мобільному профілі URP увімкнено SRP Batcher і обмежено дальність тіней. Геометрія рівня перебудовується під час перезапуску. Профілювання на Android-пристрої ще попереду.

### Конфігурація й перевірка

Налаштування лежать у [Configs](Assets/Prototype/Configs). `LevelCatalog` визначає порядок рівнів, а `DefaultBalance` задає рух, заряд, запас об’єму та зараження. Перемикач **Pause Movement While Charging** визначає, чи куля зупиняється під час утримання заряду. Повний список полів є в [довіднику конфігурації](Assets/Prototype/CONFIGURATION.md).

- **Clear Path → Verify domain and reference solutions** перевіряє генерацію, зіткнення, зараження та бюджет.
- **Clear Path → Verify session in Play Mode** перевіряє три рівні в обох режимах руху під час заряду, а також зіткнення, паузу, перезапуск, меню й налаштування звуку.
- Звіти створюються локально в `Logs/ClearPath`.

### Авторство асетів

Моделі природи, замку й текстури частинок створені [Kenney](https://kenney.nl/assets). Оригінальні ліцензії збережені разом із наборами в [Content](Assets/Prototype/Content). Zenject і DOTween містяться в [Assets/Plugins](Assets/Plugins) разом із супровідними повідомленнями.
