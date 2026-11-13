using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Zenject;

namespace ClearPath.Editor
{
    public static class PrototypeBuilder
    {
        public const string Root = "Assets/Prototype";
        public const string ScenePath = Root + "/Scenes/ClearPath.unity";
        static Font font;
        static readonly Color Ink = new Color(0.055f, 0.12f, 0.14f);
        static readonly Color Cream = new Color(0.96f, 0.95f, 0.85f);
        static readonly Color Gold = new Color(1, 0.77f, 0.3f);

        [MenuItem("Clear Path/Create or rebuild prototype scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
            var previous = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            if (previous.IsValid() && previous.isLoaded)
            {
                if (previous.isDirty) throw new System.InvalidOperationException("Save changes to ClearPath before rebuilding.");
                EditorSceneManager.CloseScene(previous, true);
            }
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var mode = string.IsNullOrEmpty(active.path) && active.rootCount == 0
                ? NewSceneMode.Single : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Configs"); Directory.CreateDirectory(Root + "/Materials");
            var palette = CreatePalette(); var catalog = CreateLevels();
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.1f, 0.12f); camera.nearClipPlane = 0.1f; camera.farClipPlane = 150;
            var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            ConfigureLighting(light);
            var world = new GameObject("Level Configurator").AddComponent<LevelConfigurator>();
            world.Configure(palette, catalog);
            var initialLevel = catalog.GetLevel(catalog.InitialLevelIndex, out _);
            BuildPreview(world, initialLevel);
            var followCamera = cameraObject.AddComponent<PlayerFollowCamera>();
            followCamera.Configure(world);
            followCamera.SnapToPlayer();
            var hud = BuildHud();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var contextGo = new GameObject("Scene Context");
            var context = contextGo.AddComponent<SceneContext>(); var installer = contextGo.AddComponent<GameInstaller>();
            installer.Configure(world, hud); context.Installers = new[] { installer };
            contextGo.AddComponent<ApplicationLifecycle>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath) buildScenes.Add(new EditorBuildSettingsScene(existing.path, false));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = world.gameObject;
            bool otherSceneDirty = false;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var openScene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (openScene != scene && openScene.isDirty) otherSceneDirty = true;
            }
            if (!otherSceneDirty) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else EditorSceneManager.CloseScene(scene, true);
            Debug.Log("Clear Path: scene and configuration assets created. Open Assets/Prototype/Scenes/ClearPath.unity and press Play.");
        }
        [MenuItem("Clear Path/Refresh scene visuals")]
        public static void RefreshVisuals()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new System.InvalidOperationException($"Open ClearPath in Edit Mode first. Current scene: {scene.path}");

            if (scene.isDirty)
            {
                const string backup = "Temp/ClearPathBeforeVisualRefresh.unity";
                if (!EditorSceneManager.SaveScene(scene, backup, true))
                    throw new System.InvalidOperationException("Could not back up the open scene.");
            }

            var world = Object.FindFirstObjectByType<LevelConfigurator>();
            var follow = Camera.main.GetComponent<PlayerFollowCamera>();
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            ConfigureLighting(sun);
            var card = GameObject.Find("Card").GetComponent<RectTransform>();
            var hud = card.GetComponentInParent<GameHud>();
            if (!hud.GetComponent<MenuNavigation>())
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var previousAudio = card.Find("Audio Settings");
                if (previousAudio) Object.DestroyImmediate(previousAudio.gameObject);
                BuildMenus(hud, card);
            }
            foreach (var slider in hud.GetComponentsInChildren<Slider>(true))
                SizeVolumeHandle(slider.handleRect);
            BuildPreview(world, world.Catalog.GetLevel(world.Catalog.InitialLevelIndex, out _));
            follow.SnapToPlayer();
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildPreview(LevelConfigurator world, LevelConfig level)
        {
            var generator = new LevelGenerator(new LevelBudgetPlanner(new CorridorSolver(), new InfectionSolver()));
            world.Build(level, generator.Generate(level));
        }

        static void ConfigureLighting(Light sun)
        {
            sun.type = LightType.Directional;
            sun.color = new Color(1, 0.93f, 0.78f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            sun.transform.rotation = Quaternion.Euler(62, -35, 0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.53f, 0.62f, 0.57f);
        }
        static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name; material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.23f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static VisualPalette CreatePalette()
        {
            var palette = Asset<VisualPalette>(Root + "/Configs/Palette.asset");
            palette.Ground = Material("Ground", new Color(0.15f, 0.28f, 0.25f));
            palette.Path = Material("Path", new Color(0.82f, 0.29f, 0.33f));
            palette.Player = Material("Player", Gold);
            palette.Obstacle = Material("Obstacles", new Color(0.27f, 0.51f, 0.32f));
            palette.Infected = Material("Infected", new Color(1, 0.42f, 0.12f));
            palette.Door = Material("Door", new Color(0.1f, 0.63f, 0.64f));
            palette.Frame = Material("Frame", Cream);
            palette.Effect = Material("Impact", new Color(0.94f, 0.61f, 0.2f));
            if (palette.BushModels == null || palette.BushModels.Length == 0) AssignScenery(palette);
            EditorUtility.SetDirty(palette); return palette;
        }

        static void AssignScenery(VisualPalette palette)
        {
            const string nature = Root + "/Content/kenney_nature-kit/Models/FBX format/";
            const string castle = Root + "/Content/kenney_castle-kit/Models/FBX format/";
            palette.BushModels = new[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>(nature + "plant_bushLarge.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>(nature + "plant_bushDetailed.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>(nature + "plant_bush.fbx")
            };
            palette.RockModel = AssetDatabase.LoadAssetAtPath<GameObject>(nature + "rock_smallB.fbx");
            palette.GateFrame = AssetDatabase.LoadAssetAtPath<GameObject>(castle + "wall-doorway.fbx");
            palette.GatePanel = AssetDatabase.LoadAssetAtPath<GameObject>(castle + "gate.fbx");
            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssetIfDirty(palette);
        }
        static LevelCatalog CreateLevels()
        {
            var balance = Asset<BalanceConfig>(Root + "/Configs/DefaultBalance.asset");
            var catalog = Asset<LevelCatalog>(Root + "/Configs/LevelCatalog.asset");
            if (catalog.Levels != null && catalog.Levels.Length > 0) return catalog;
            catalog.Levels = new LevelConfig[3];
            for (int i = 0; i < 3; i++)
            {
                var level = Asset<LevelConfig>(Root + "/Configs/Level_" + (i + 1).ToString("00") + ".asset");
                level.DisplayName = new[] { "First clearing", "Choose your shots", "Last reserve" }[i];
                level.Balance = balance; level.Length = 24 + i * 4;
                level.Seed = 173 + i * 103;
                level.Density = 0.78f + i * 0.07f;
                level.Clustering = 0.55f + i * 0.12f;
                level.RadiusRange = new Vector2(0.36f, 0.53f);
                EditorUtility.SetDirty(level); catalog.Levels[i] = level;
            }
            EditorUtility.SetDirty(catalog); return catalog;
        }
        static GameHud BuildHud()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("Overlay Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.matchWidthOrHeight = 0.5f;
            var hud = canvasGo.AddComponent<GameHud>();
            var root = canvasGo.transform;
            var top = Panel("HUD top", root, new Vector2(0.5f, 1), new Vector2(650, 150), new Vector2(0, -125), new Color(Ink.r, Ink.g, Ink.b, 0.91f));
            var level = Label("Level", top.transform, "01 / 03     FIRST CLEARING", 23, new Vector2(610, 35), new Vector2(0, 42), Cream);
            var resource = Label("Volume", top.transform, "VOLUME 100%", 24, new Vector2(300, 35), new Vector2(-150, 0), Gold);
            var progress = Label("Progress", top.transform, "0%", 24, new Vector2(250, 35), new Vector2(170, 0), Cream);
            var resourceBar = Bar("Volume bar", top.transform, new Vector2(-105, -40), new Vector2(370, 10), Gold);
            var progressBar = Bar("Progress bar", top.transform, new Vector2(215, -40), new Vector2(150, 10), new Color(0.2f, 0.8f, 0.75f));
            var bottom = Panel("HUD bottom", root, new Vector2(0.5f, 0), new Vector2(650, 72), new Vector2(0, 90), new Color(Ink.r, Ink.g, Ink.b, 0.94f));
            var status = Label("Hint", bottom.transform, "SMALL SHOTS. SMART CHOICES.", 22, new Vector2(620, 60), Vector2.zero, Cream);
            var backdrop = Panel("Start and result overlay", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.025f, 0.07f, 0.09f, 0.58f));
            var rect = backdrop.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            backdrop.raycastTarget = true;
            var group = backdrop.gameObject.AddComponent<CanvasGroup>();
            var card = Panel("Card", backdrop.transform, new Vector2(0.5f, 0.5f), new Vector2(600, 560), Vector2.zero, new Color(Ink.r, Ink.g, Ink.b, 0.98f));
            Panel("Accent", card.transform, new Vector2(0.5f, 0.5f), new Vector2(70, 5), new Vector2(0, 225), Gold);
            var title = Label("Title", card.transform, "CLEAR PATH", 52, new Vector2(560, 80), new Vector2(0, 160), Cream); title.fontStyle = FontStyle.Bold;
            var body = Label("Description", card.transform, "HOLD TO GROW A SHOT\nRELEASE TO CLEAR THE WAY\n\nYour size is your ammunition.\nMake every shot count.", 25, new Vector2(540, 200), new Vector2(0, 25), Cream);
            var primary = Button("Start or retry", card.transform, "START", new Vector2(0, -145), new Vector2(420, 76), out var primaryLabel);
            var previous = Button("Previous level", card.transform, "<", new Vector2(-205, -230), new Vector2(80, 52), out _);
            var next = Button("Next level", card.transform, ">", new Vector2(205, -230), new Vector2(80, 52), out _);
            var selection = Label("Level selection", card.transform, "CHOOSE YOUR LEVEL", 18, new Vector2(310, 45), new Vector2(0, -230), Cream);
            hud.Configure(group, title, body, primaryLabel, level, resource, status, progress, resourceBar, progressBar, primary, previous, next, selection);
            BuildMenus(hud, card.rectTransform);
            return hud;
        }

        static void BuildMenus(GameHud hud, RectTransform card)
        {
            card.sizeDelta = new Vector2(600, 620);
            card.Find("Accent").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 255);
            card.Find("Title").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 190);
            card.Find("Description").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 54);
            card.Find("Start or retry").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -108);
            foreach (string name in new[] { "Previous level", "Next level", "Level selection" })
            {
                var item = card.Find(name).GetComponent<RectTransform>();
                item.anchoredPosition = new Vector2(item.anchoredPosition.x, -190);
            }
            var settings = SecondaryButton("Main Settings", card, "SETTINGS", new Vector2(-113, -258), new Vector2(205, 54));
            var quit = SecondaryButton("Main Quit", card, "QUIT GAME", new Vector2(113, -258), new Vector2(205, 54));

            var pauseButton = SecondaryButton("Pause", hud.transform, "PAUSE", Vector2.zero, new Vector2(128, 54));
            var pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(0.5f, 1);
            pauseRect.anchoredPosition = new Vector2(248, -238);
            pauseButton.gameObject.SetActive(false);

            var pauseCard = Modal("Pause Window", "PAUSED", hud.transform, 510);
            var resume = Button("Resume", pauseCard, "RESUME", new Vector2(0, 65), new Vector2(420, 70), out _);
            var pauseSettings = SecondaryButton("Pause Settings", pauseCard, "SETTINGS", new Vector2(0, -16), new Vector2(420, 62));
            var main = SecondaryButton("Return to Main", pauseCard, "MAIN MENU", new Vector2(0, -94), new Vector2(420, 62));
            var pauseQuit = SecondaryButton("Pause Quit", pauseCard, "QUIT GAME", new Vector2(0, -172), new Vector2(420, 62));

            var settingsCard = Modal("Settings Window", "SETTINGS", hud.transform, 490);
            var audio = Panel("Audio Settings", settingsCard, new Vector2(0.5f, 0.5f), new Vector2(510, 180),
                new Vector2(0, 6), new Color(0.09f, 0.18f, 0.18f));
            var music = VolumeSlider(audio.transform, "MUSIC", 43, out var musicValue);
            var effects = VolumeSlider(audio.transform, "SOUND EFFECTS", -43, out var effectsValue);
            audio.gameObject.AddComponent<AudioMenu>().Configure(music, effects, musicValue, effectsValue);
            var back = Button("Settings Back", settingsCard, "BACK", new Vector2(0, -155), new Vector2(420, 65), out _);

            var pauseWindow = pauseCard.parent.gameObject;
            var settingsWindow = settingsCard.parent.gameObject;
            pauseWindow.SetActive(false);
            settingsWindow.SetActive(false);
            hud.gameObject.AddComponent<MenuNavigation>().Configure(card.parent.gameObject, pauseWindow, settingsWindow,
                settings, quit, pauseButton, resume, pauseSettings, main, pauseQuit, back);
        }

        static RectTransform Modal(string name, string title, Transform parent, float height)
        {
            var backdrop = Panel(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                new Color(0.025f, 0.07f, 0.09f, 0.68f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.sizeDelta = Vector2.zero;
            backdrop.raycastTarget = true;
            var card = Panel("Panel", backdrop.transform, new Vector2(0.5f, 0.5f), new Vector2(600, height),
                Vector2.zero, new Color(Ink.r, Ink.g, Ink.b, 0.98f)).rectTransform;
            Panel("Accent", card, new Vector2(0.5f, 0.5f), new Vector2(70, 5), new Vector2(0, height / 2 - 43), Gold);
            Label("Title", card, title, 48, new Vector2(560, 68), new Vector2(0, height / 2 - 100), Cream).fontStyle = FontStyle.Bold;
            return card;
        }

        static Button SecondaryButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var button = Button(name, parent, label, position, size, out var text);
            ((Image)button.targetGraphic).color = new Color(0.12f, 0.25f, 0.25f);
            text.color = Cream;
            text.fontSize = 22;
            return button;
        }

        static Slider VolumeSlider(Transform parent, string name, float y, out Text value)
        {
            var row = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(440, 76), new Vector2(0, y));
            var label = Label("Name", row, name, 18, new Vector2(350, 26), new Vector2(-45, 22), Cream);
            label.alignment = TextAnchor.MiddleLeft;
            value = Label("Value", row, "", 18, new Vector2(80, 26), new Vector2(180, 22), Gold);
            value.alignment = TextAnchor.MiddleRight;

            var track = Panel("Track", row, new Vector2(0.5f, 0.5f), new Vector2(440, 38), new Vector2(0, -13), Color.clear);
            track.raycastTarget = true;
            Panel("Rail", track.transform, new Vector2(0.5f, 0.5f), new Vector2(440, 8), Vector2.zero, new Color(0.22f, 0.34f, 0.33f));
            var fill = Panel("Fill", track.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Gold);
            fill.rectTransform.offsetMin = new Vector2(0, 15);
            fill.rectTransform.offsetMax = new Vector2(0, -15);
            var handle = Panel("Handle", track.transform, new Vector2(0.5f, 0.5f), new Vector2(24, 0), Vector2.zero, Cream);
            SizeVolumeHandle(handle.rectTransform);
            handle.raycastTarget = true;
            var slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        static void SizeVolumeHandle(RectTransform handle)
        {
            float x = handle.anchorMin.x;
            handle.anchorMin = new Vector2(x, 0);
            handle.anchorMax = new Vector2(x, 1);
            handle.offsetMin = new Vector2(-12, 7);
            handle.offsetMax = new Vector2(12, -7);
        }
        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position, Color color)
        { var image = Rect(name, parent, anchor, size, position).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        static Text Label(string name, Transform parent, string value, int size, Vector2 bounds, Vector2 position, Color color)
        {
            var text = Rect(name, parent, new Vector2(0.5f, 0.5f), bounds, position).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
        }
        static Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var background = Panel(name, parent, new Vector2(0.5f, 0.5f), size, position, new Color(0.23f, 0.32f, 0.33f));
            var fill = Panel("Fill", background.transform, new Vector2(0.5f, 0.5f), size, Vector2.zero, color);
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; return fill;
        }
        static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size, out Text text)
        {
            var image = Panel(name, parent, new Vector2(0.5f, 0.5f), size, position, Gold); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1, 0.91f, 0.7f); colors.pressedColor = new Color(0.78f, 0.67f, 0.45f); button.colors = colors;
            text = Label("Label", image.transform, label, 27, size, Vector2.zero, Ink); text.fontStyle = FontStyle.Bold;
            return button;
        }
    }
}

