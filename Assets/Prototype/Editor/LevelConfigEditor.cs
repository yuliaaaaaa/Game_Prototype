using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClearPath.Editor
{
    [CustomEditor(typeof(LevelConfig))]
    public sealed class LevelConfigEditor : UnityEditor.Editor
    {
        string report;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Same seed = same level. Preview applies current settings; Regenerate changes only the seed. Coordinates and resource budget are calculated automatically.", MessageType.Info);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Preview level")) Preview(false);
                if (GUILayout.Button("Regenerate (new seed)")) Preview(true);
            }
            if (!string.IsNullOrEmpty(report)) EditorGUILayout.HelpBox(report, MessageType.Info);
        }
        void Preview(bool newSeed)
        {
            var config = (LevelConfig)target;
            if (newSeed)
            {
                Undo.RecordObject(config, "Change level seed");
                config.Seed = unchecked(config.Seed + 1); EditorUtility.SetDirty(config);
            }
            try
            {
                var layout = new LevelGenerator(new LevelBudgetPlanner(new CorridorSolver(), new InfectionSolver())).Generate(config);
                var world = Object.FindFirstObjectByType<LevelConfigurator>();
                if (world)
                {
                    world.Build(config, layout);
                    var camera = Camera.main;
                    if (camera && camera.TryGetComponent<PlayerFollowCamera>(out var followCamera))
                        followCamera.SnapToPlayer();
                    EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
                    SceneView.RepaintAll();
                }
                report = $"Verified: {layout.Obstacles.Length} obstacles. Initial volume: {layout.StartingVolume:0.000}. Resource reserve: {config.Balance.ReserveFraction:P0}.";
                if (!world) report += " Open the ClearPath scene to see the preview.";
            }
            catch (System.Exception error) { report = error.Message; Debug.LogError(report); }
        }
    }
}
