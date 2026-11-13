using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace ClearPath
{
    public sealed class MenuNavigation : MonoBehaviour
    {
        [SerializeField] GameObject mainOverlay, pauseWindow, settingsWindow;
        [SerializeField] Button mainSettings, mainQuit, pause, resume, pauseSettings, mainMenu, pauseQuit, back;
        GameSession session;
        SessionStateMachine state;

        public void Configure(GameObject overlay, GameObject pausePanel, GameObject settingsPanel,
            Button openSettings, Button quit, Button pauseGame, Button resumeGame,
            Button settingsFromPause, Button returnToMain, Button quitFromPause, Button closeSettings)
        {
            mainOverlay = overlay;
            pauseWindow = pausePanel;
            settingsWindow = settingsPanel;
            mainSettings = openSettings;
            mainQuit = quit;
            pause = pauseGame;
            resume = resumeGame;
            pauseSettings = settingsFromPause;
            mainMenu = returnToMain;
            pauseQuit = quitFromPause;
            back = closeSettings;
        }

        [Inject]
        public void Construct(GameSession gameSession, SessionStateMachine stateMachine)
        {
            session = gameSession;
            state = stateMachine;
            state.Changed += ShowSession;
        }

        void Awake()
        {
            mainSettings.onClick.AddListener(OpenSettings);
            mainQuit.onClick.AddListener(Quit);
            pause.onClick.AddListener(OpenPause);
            resume.onClick.AddListener(Resume);
            pauseSettings.onClick.AddListener(OpenSettings);
            mainMenu.onClick.AddListener(ReturnToMainMenu);
            pauseQuit.onClick.AddListener(Quit);
            back.onClick.AddListener(CloseSettings);
        }

        void Update()
        {
            if (session == null || Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            if (settingsWindow.activeSelf) CloseSettings();
            else if (session.IsPaused) Resume();
            else if (session.State == SessionState.Playing) OpenPause();
        }

        void ShowSession(SessionState current)
        {
            settingsWindow.SetActive(false);
            pauseWindow.SetActive(false);
            mainOverlay.SetActive(true);
            pause.gameObject.SetActive(current == SessionState.Playing);
        }

        void OpenPause()
        {
            if (!session.Pause()) return;
            pause.gameObject.SetActive(false);
            pauseWindow.SetActive(true);
        }

        void Resume()
        {
            session.Resume();
            pauseWindow.SetActive(false);
            pause.gameObject.SetActive(true);
        }

        void OpenSettings()
        {
            pauseWindow.SetActive(false);
            mainOverlay.SetActive(false);
            settingsWindow.SetActive(true);
        }

        void CloseSettings()
        {
            settingsWindow.SetActive(false);
            pauseWindow.SetActive(session.IsPaused);
            mainOverlay.SetActive(true);
        }

        void ReturnToMainMenu() => session.ReturnToMainMenu();

        static void Quit()
        {
            PlayerPrefs.Save();
            Application.Quit();
        }

        void OnDestroy()
        {
            if (state != null) state.Changed -= ShowSession;
        }
    }
}
