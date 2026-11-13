using DG.Tweening;
using UnityEngine;

namespace ClearPath
{
    public sealed class ObstacleView : MonoBehaviour
    {
        Tween tween;
        DebrisEffect debris;
        float radius;

        public void Configure(DebrisEffect effect, float footprintRadius)
        {
            debris = effect;
            radius = footprintRadius;
        }

        public void Infect(Material material)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            tween?.Kill();
            Vector3 scale = transform.localScale;
            transform.localScale = scale * 0.88f;
            tween = transform.DOScale(scale, 0.2f).SetEase(Ease.OutSine);
        }
        public void Explode()
        {
            debris.Emit(transform.position, radius);
            tween?.Kill();
            gameObject.SetActive(false);
        }
        void OnDestroy() => tween?.Kill();
    }
}
