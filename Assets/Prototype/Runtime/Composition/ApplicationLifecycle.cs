using UnityEngine;
using Zenject;

namespace ClearPath
{
    public sealed class ApplicationLifecycle : MonoBehaviour
    {
        GameSession session;
        bool paused, unfocused;
        [Inject] public void Construct(GameSession gameSession) => session = gameSession;
        void OnApplicationPause(bool value) { paused = value; UpdateSuspension(); }
        void OnApplicationFocus(bool value) { unfocused = !value; UpdateSuspension(); }
        void UpdateSuspension() => session?.SetSuspended(paused || unfocused);
    }
}
