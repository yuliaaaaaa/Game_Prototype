using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace ClearPath
{
    public sealed class FinishEffect : MonoBehaviour
    {
        [SerializeField] LineRenderer[] markers;
        [SerializeField] LineRenderer ripple;
        Tween opening, celebration;

        public void Configure(VisualPalette palette)
        {
            markers = new LineRenderer[3];
            for (int i = 0; i < markers.Length; i++)
            {
                var marker = CreateLine($"Guiding Chevron {i + 1}", palette.ShotTrail);
                float z = -2.35f + i * 0.62f;
                marker.positionCount = 3;
                marker.widthMultiplier = 0.07f;
                marker.SetPosition(0, new Vector3(-0.48f, 0.025f, z - 0.24f));
                marker.SetPosition(1, new Vector3(0, 0.025f, z));
                marker.SetPosition(2, new Vector3(0.48f, 0.025f, z - 0.24f));
                markers[i] = marker;
            }

            ripple = CreateLine("Victory Ripple", palette.ShotTrail);
            ripple.transform.localPosition = new Vector3(0, 0.035f, -0.48f);
            ripple.loop = true;
            ripple.positionCount = 48;
            for (int i = 0; i < ripple.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ripple.positionCount;
                ripple.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
            }
        }

        LineRenderer CreateLine(string label, Material material)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        public void Reveal()
        {
            PresentMarkers(0);
            opening = DOVirtual.Float(0, 1, 0.75f, PresentMarkers).SetEase(Ease.Linear).SetTarget(this);
        }

        void PresentMarkers(float progress)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                float alpha = Mathf.Clamp01(progress * 2.4f - i * 0.7f);
                var marker = markers[i];
                marker.enabled = alpha > 0;
                marker.startColor = marker.endColor = new Color(1, 0.84f, 0.39f, alpha);
            }
        }

        public void Celebrate()
        {
            ripple.enabled = true;
            PresentRipple(0);
            celebration = DOVirtual.Float(0, 1, 0.85f, PresentRipple).SetEase(Ease.OutQuad)
                .OnComplete(() => ripple.enabled = false);
        }

        void PresentRipple(float progress)
        {
            ripple.transform.localScale = Vector3.one * Mathf.Lerp(0.42f, 3.3f, progress);
            ripple.widthMultiplier = Mathf.Lerp(0.18f, 0.035f, progress);
            ripple.startColor = ripple.endColor = new Color(1, 0.93f, 0.67f, 1 - progress);
        }

        void OnDestroy()
        {
            opening?.Kill();
            celebration?.Kill();
        }
    }
}
