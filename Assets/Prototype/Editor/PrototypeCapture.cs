using System.IO;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace ClearPath.Editor
{
    public static class PrototypeCapture
    {
        [MenuItem("Clear Path/Capture portrait previews in Play Mode")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter Play Mode first.");
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var context = Object.FindFirstObjectByType<SceneContext>();
            var session = context.Container.Resolve<GameSession>();
            var world = context.Container.Resolve<LevelConfigurator>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var group = canvas.GetComponentInChildren<CanvasGroup>();
            var buttons = canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            var camera = Camera.main;
            var followCamera = camera.GetComponent<PlayerFollowCamera>();
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            var target = new RenderTexture(720, 1280, 24); target.Create();
            try
            {
                session.SetSuspended(false); session.LoadLevel(0);
                camera.targetTexture = target; camera.aspect = 720f / 1280; followCamera.SnapToPlayer();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                group.alpha = 1; Render(camera, target, "Start.png");
                Click(buttons, "Main Settings");
                Render(camera, target, "Settings.png");
                Click(buttons, "Settings Back");
                session.StartGame(); session.Advance(1.5f); session.BeginCharge(); session.Advance(0.9f);
                followCamera.SnapToPlayer();
                group.alpha = 0; Render(camera, target, "Gameplay.png");
                Click(buttons, "Pause");
                Render(camera, target, "Pause.png");
                Click(buttons, "Resume");

                session.CancelCharge();
                session.LoadLevel(0);
                session.StartGame();
                group.alpha = 0;
                foreach (float shot in world.Data.SolutionShots)
                {
                    for (int i = 0; i < 600; i++)
                    {
                        session.Advance(0.02f);
                        if (!session.IsBusy && !session.Movement.IsMoving) break;
                    }
                    if (session.State != SessionState.Playing) break;
                    session.BeginCharge();
                    session.Advance(shot / session.Level.Balance.ChargePerSecond);
                    session.ReleaseCharge();
                }
                for (int i = 0; i < 600 && !world.Door.IsOpen; i++) session.Advance(0.02f);
                if (world.Door.IsOpen)
                {
                    for (int i = 0; i < 30; i++) session.Advance(0.02f);
                    DOTween.Complete(world.Door);
                    DOTween.Complete(world.Door.GetComponentInChildren<FinishEffect>());
                    followCamera.SnapToPlayer();
                    Render(camera, target, "Gate.png");

                    for (int i = 0; i < 600 && session.State == SessionState.Playing; i++) session.Advance(0.02f);
                    if (session.State == SessionState.Won)
                    {
                        followCamera.SnapToPlayer();
                        Render(camera, target, "Finish.png");
                    }
                }
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.ResetAspect(); canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                target.Release(); Object.DestroyImmediate(target); session.LoadLevel(0);
                followCamera.SnapToPlayer();
            }
        }

        static void Click(UnityEngine.UI.Button[] buttons, string name)
            => System.Array.Find(buttons, button => button.name == name).onClick.Invoke();

        static void Render(Camera camera, RenderTexture target, string name)
        {
            Canvas.ForceUpdateCanvases(); camera.Render(); camera.Render();
            var old = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
            Directory.CreateDirectory("Logs/ClearPath"); File.WriteAllBytes("Logs/ClearPath/" + name, texture.EncodeToPNG());
            Object.DestroyImmediate(texture); RenderTexture.active = old;
        }
    }
}
