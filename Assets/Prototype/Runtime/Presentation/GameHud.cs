using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ClearPath
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] CanvasGroup overlay;
        [SerializeField] Text title, description, primaryLabel, levelLabel, resourceLabel, statusLabel, progressLabel;
        [SerializeField] Image resourceFill, progressFill;
        [SerializeField] Button primary, previous, next;
        [SerializeField] Text levelSelectionLabel;
        Tween transition;
        public event Action PrimaryClicked, PreviousClicked, NextClicked;
        public void Configure(CanvasGroup panel, Text heading, Text body, Text actionLabel, Text level,
            Text resource, Text status, Text progress, Image resourceBar, Image progressBar, Button action, Button back, Button forward,
            Text selectionLabel)
        {
            overlay = panel; title = heading; description = body; primaryLabel = actionLabel;
            levelLabel = level; resourceLabel = resource; statusLabel = status; progressLabel = progress;
            resourceFill = resourceBar; progressFill = progressBar; primary = action; previous = back; next = forward;
            levelSelectionLabel = selectionLabel;
        }
        void Awake()
        {
            primary.onClick.AddListener(OnPrimary); previous.onClick.AddListener(OnPrevious); next.onClick.AddListener(OnNext);
        }
        void OnPrimary() => PrimaryClicked?.Invoke();
        void OnPrevious() => PreviousClicked?.Invoke();
        void OnNext() => NextClicked?.Invoke();
        public void ShowReady(string levelName, int index, int count)
        {
            levelLabel.text = $"{index + 1:00} / {count:00}     {levelName.ToUpperInvariant()}";
            title.text = "CLEAR PATH";
            description.text = "HOLD TO GROW A SHOT\nRELEASE TO CLEAR THE WAY\n\nYour size is your ammunition.\nMake every shot count.";
            primaryLabel.text = "START";
            ShowLevelSelection(count > 1);
            ShowOverlay();
        }
        public void HideOverlay()
        {
            transition?.Kill(); overlay.interactable = false; overlay.blocksRaycasts = false;
            transition = DOTween.To(() => overlay.alpha, value => overlay.alpha = value, 0, 0.22f).SetUpdate(true);
        }
        public void ShowResult(bool won, string reason)
        {
            title.text = won ? "PATH CLEARED" : "TRY AGAIN";
            description.text = reason; primaryLabel.text = "PLAY AGAIN";
            ShowLevelSelection(false);
            ShowOverlay(won ? 0.42f : 0);
        }
        void ShowLevelSelection(bool visible)
        {
            levelSelectionLabel.gameObject.SetActive(visible);
            previous.gameObject.SetActive(visible);
            next.gameObject.SetActive(visible);
        }
        void ShowOverlay(float delay = 0)
        {
            transition?.Kill(); overlay.interactable = true; overlay.blocksRaycasts = true;
            transition = DOTween.To(() => overlay.alpha, value => overlay.alpha = value, 1, 0.25f)
                .SetDelay(delay).SetUpdate(true);
        }
        public void Present(ResourceBudget budget, float progress, string status)
        {
            float available = Mathf.InverseLerp(budget.Critical, budget.Initial, budget.Remaining);
            resourceFill.fillAmount = available;
            resourceFill.color = Color.Lerp(new Color(1, 0.3f, 0.3f), new Color(1, 0.78f, 0.31f), Mathf.Clamp01(available * 3));
            resourceLabel.text = $"VOLUME  {available * 100:0}%";
            progressFill.fillAmount = progress;
            progressLabel.text = $"{Mathf.RoundToInt(progress * 100)}%";
            statusLabel.text = status;
        }
        void OnDestroy()
        {
            transition?.Kill(); primary.onClick.RemoveListener(OnPrimary);
            previous.onClick.RemoveListener(OnPrevious); next.onClick.RemoveListener(OnNext);
        }
    }
}
