using DG.Tweening;
using UnityEngine;

namespace ClearPath
{
    public sealed class DoorView : MonoBehaviour
    {
        public const float OpenDistance = 5f;
        [SerializeField] Transform panel;
        [SerializeField] FinishEffect finish;
        Tween opening;
        public bool IsOpen { get; private set; }
        public void Configure(Transform gatePanel, FinishEffect effect)
        {
            panel = gatePanel;
            finish = effect;
        }

        public void Celebrate() => finish.Celebrate();
        public void UpdateDistance(float playerZ)
        {
            if (IsOpen || transform.position.z - playerZ > OpenDistance) return;
            IsOpen = true;
            finish.Reveal();
            opening = panel.DOLocalMoveY(2.8f, 0.65f).SetEase(Ease.OutCubic).SetTarget(this)
                .OnComplete(() => panel.gameObject.SetActive(false));
        }
        void OnDestroy() => opening?.Kill();
    }
}
