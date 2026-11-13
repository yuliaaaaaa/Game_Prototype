using DG.Tweening;
using UnityEngine;

namespace ClearPath
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] Transform charge;
        [SerializeField] Transform[] motes;
        float chargeRadius;
        Renderer surface;
        MaterialPropertyBlock shimmer;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        Tween feedback;
        public Vector3 Center => transform.position + Vector3.up * Radius;
        public float Radius => body.lossyScale.x * 0.5f;
        public void Configure(Transform bodyTransform, Transform chargeTransform, VisualPalette palette)
        {
            body = bodyTransform;
            charge = chargeTransform;
            surface = body.GetComponent<Renderer>();
            shimmer = new MaterialPropertyBlock();
            motes = new Transform[4];
            for (int i = 0; i < motes.Length; i++)
                motes[i] = AddDetail("Charge spark", charge, Vector3.zero, Vector3.one * 0.10f, palette.Frame);
        }

        static Transform AddDetail(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var detail = PrimitiveFactory.Create(name, PrimitiveType.Sphere, parent, position, scale, material);
            detail.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return detail.transform;
        }
        public void Present(float z, float radius, float shotVolume)
        {
            transform.position = new Vector3(0, 0, z);
            float hop = Mathf.Pow(Mathf.Abs(Mathf.Sin(z * 4.8f)), 1.3f) * radius * 0.33f;
            body.localPosition = Vector3.up * (radius + hop);
            body.localScale = Vector3.one * radius * 2;
            chargeRadius = ResourceBudget.Radius(shotVolume);
            charge.gameObject.SetActive(shotVolume > 0);
            charge.localPosition = new Vector3(0, radius, radius + chargeRadius * 0.65f);
            AnimateCharge();
        }

        void Update()
        {
            AnimateCharge();
            if (!surface) return;
            float phase = 0.5f + Mathf.Sin(Time.time * 2.1f) * 0.5f;
            Color glow = Color.Lerp(new Color(0.24f, 0.055f, 0.008f), new Color(0.54f, 0.29f, 0.10f), phase);
            shimmer.SetColor(Emission, glow);
            surface.SetPropertyBlock(shimmer);
        }

        void AnimateCharge()
        {
            if (!charge || !charge.gameObject.activeSelf || motes == null) return;
            float phase = Time.time * 11;
            charge.localScale = Vector3.one * chargeRadius * (2 + Mathf.Sin(phase * 1.4f) * 0.12f);
            for (int i = 0; i < motes.Length; i++)
            {
                float angle = phase + i * (Mathf.PI * 2 / motes.Length);
                motes[i].localPosition = new Vector3(Mathf.Cos(angle) * 0.72f,
                    Mathf.Sin(angle * 1.1f + i) * 0.46f, Mathf.Sin(angle) * 0.68f);
            }
        }
        public void AimCharge(Vector2 direction)
        {
            if (!charge.gameObject.activeSelf) return;
            direction.Normalize();
            float offset = charge.localPosition.z;
            charge.localPosition = new Vector3(direction.x * offset, charge.localPosition.y, direction.y * offset);
        }
        public void FireFeedback()
        {
            feedback?.Kill(); body.localRotation = Quaternion.identity;
            feedback = body.DOPunchRotation(new Vector3(14, 0, 0), 0.22f, 5, 0.4f);
        }
        public void ResetFeedback() { feedback?.Kill(); if (body) body.localRotation = Quaternion.identity; }
        void OnDestroy() => feedback?.Kill();
    }
}
