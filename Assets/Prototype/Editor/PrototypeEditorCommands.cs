using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ClearPath.Editor
{
    [InitializeOnLoad]
    public static class PrototypeEditorCommands
    {
        const string CommandPath = "Temp/ClearPath.command";
        static double nextPoll;
        static PrototypeEditorCommands() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            if (!File.Exists(CommandPath)) return;
            string command = File.ReadAllText(CommandPath).Trim(); File.Delete(CommandPath);
            try
            {
                switch (command)
                {
                    case "build": PrototypeBuilder.Build(); PrototypeVerification.VerifyDomain(); break;
                    case "domain": PrototypeVerification.VerifyDomain(); break;
                    case "play":
                        if (UnityEngine.SceneManagement.SceneManager.sceneCount == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == PrototypeBuilder.ScenePath)
                            EditorApplication.isPlaying = true;
                        else
                        {
                            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Save open scenes before automated Play Mode.");
                            EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath); EditorApplication.isPlaying = true;
                        }
                        break;
                    case "session": PrototypeVerification.VerifySession(); break;
                    case "portrait": PrototypeCapture.Capture(); break;
                    case "refresh-visuals": PrototypeBuilder.RefreshVisuals(); break;
                    case "stop": EditorApplication.isPlaying = false; break;
                    case "capture": Directory.CreateDirectory("Logs/ClearPath"); ScreenCapture.CaptureScreenshot("Logs/ClearPath/Game.png"); break;
                    default: throw new ArgumentException("Unknown Clear Path command.");
                }
                File.WriteAllText("Temp/ClearPath.result", "OK " + command);
            }
            catch (Exception error) { Debug.LogException(error); File.WriteAllText("Temp/ClearPath.result", error.ToString()); }
        }
    }
}
