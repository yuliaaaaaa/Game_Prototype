using DG.Tweening;
using UnityEngine;
using Zenject;

namespace ClearPath
{
    public sealed class ShotVisual
    {
        public readonly Transform Transform;
        readonly TrailRenderer trail;
        float diameter;

        public ShotVisual(Transform parent, Material material, Material trailMaterial)
        {
            var shot = PrimitiveFactory.Create("Pooled shot", PrimitiveType.Sphere, parent, Vector3.zero, Vector3.one, material);
            Transform = shot.transform;
            shot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail = shot.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = 0.16f;
            trail.minVertexDistance = 0.045f;
            trail.endWidth = 0;
            trail.startColor = new Color(1, 0.78f, 0.32f, 0.85f);
            trail.endColor = new Color(1, 0.52f, 0.16f, 0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void Launch(Vector3 origin, float radius)
        {
            Transform.position = origin;
            diameter = radius * 2;
            Transform.localScale = Vector3.one * diameter;
            trail.startWidth = radius * 1.45f;
            trail.Clear();
            trail.emitting = true;
        }

        public void MoveTo(Vector3 position)
        {
            Vector3 direction = position - Transform.position;
            if (direction.sqrMagnitude > 0.000001f) Transform.rotation = Quaternion.LookRotation(direction);
            Transform.position = position;
            Transform.localScale = new Vector3(0.94f, 0.94f, 1.24f) * diameter;
        }

        public sealed class Pool : MemoryPool<ShotVisual>
        {
            protected override void OnCreated(ShotVisual item) => item.Transform.gameObject.SetActive(false);
            protected override void OnSpawned(ShotVisual item) => item.Transform.gameObject.SetActive(true);
            protected override void OnDespawned(ShotVisual item)
            {
                item.trail.emitting = false;
                item.trail.Clear();
                item.Transform.gameObject.SetActive(false);
            }
        }
    }
    public sealed class PulseVisual
    {
        readonly LineRenderer ring;
        Tween tween;

        public PulseVisual(Transform parent, Material material)
        {
            ring = new GameObject("Pooled impact ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(parent, false);
            ring.sharedMaterial = material;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
            }
        }

        public void Play(Vector3 position, float radius)
        {
            tween?.Kill();
            ring.transform.position = position + Vector3.up * 0.045f;
            Present(radius, 0);
            tween = DOVirtual.Float(0, 1, 0.42f, value => Present(radius, value)).SetEase(Ease.OutQuad);
        }

        void Present(float radius, float progress)
        {
            ring.transform.localScale = Vector3.one * Mathf.Lerp(0.03f, radius, progress);
            ring.widthMultiplier = Mathf.Lerp(0.09f, 0.025f, progress);
            ring.startColor = ring.endColor = new Color(1, 0.68f, 0.28f, 0.85f * (1 - progress));
        }

        public sealed class Pool : MemoryPool<PulseVisual>
        {
            protected override void OnCreated(PulseVisual item) => item.ring.gameObject.SetActive(false);
            protected override void OnSpawned(PulseVisual item) => item.ring.gameObject.SetActive(true);
            protected override void OnDespawned(PulseVisual item)
            {
                item.tween?.Kill();
                item.ring.gameObject.SetActive(false);
            }
        }
    }
}
