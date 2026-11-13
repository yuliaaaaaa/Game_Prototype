using System;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ClearPath.Editor
{
    [InitializeOnLoad]
    public static class PrototypeBatch
    {
        const string Pending = "ClearPath.BatchPending";
        static PrototypeBatch()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredPlayMode || !UnityEditor.SessionState.GetBool(Pending, false)) return;
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        PrototypeVerification.VerifySession();
                        PrototypeCapture.Capture();
                        UnityEditor.SessionState.SetBool(Pending, false);
                        EditorApplication.Exit(0);
                    }
                    catch (Exception error) { UnityEngine.Debug.LogException(error); EditorApplication.Exit(1); }
                };
            };
        }
        public static void Run()
        {
            try
            {
                PrototypeBuilder.Build(); PrototypeVerification.VerifyDomain();
                EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
                UnityEditor.SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
            }
            catch (Exception error) { UnityEngine.Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
