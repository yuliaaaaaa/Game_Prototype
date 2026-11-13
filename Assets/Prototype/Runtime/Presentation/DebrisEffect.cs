using UnityEngine;
using UnityEngine.Rendering;

namespace ClearPath
{
    public sealed class DebrisEffect
    {
        readonly ParticleSystem particles;

        public DebrisEffect(Transform parent, Material material)
        {
            var root = new GameObject("Obstacle Debris");
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            particles = root.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 192;
            main.gravityModifier = 0.9f;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            fade.color = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.9f, 0.45f), new GradientAlphaKey(0, 1) }
            };
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            root.SetActive(true);
            if (Application.isPlaying) particles.Play();
        }

        public void Emit(Vector3 position, float radius)
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2 / 9;
                var outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var fragment = new ParticleSystem.EmitParams
                {
                    position = position + Vector3.up * (radius * 0.85f) + outward * (radius * 0.4f),
                    velocity = outward * Mathf.Lerp(1.4f, 2.4f, i % 3 / 2f) + Vector3.up * (2.1f + i % 4 * 0.55f),
                    startColor = i % 3 == 0 ? new Color(1, 0.65f, 0.23f) : new Color(1, 0.89f, 0.53f),
                    startSize = radius * (0.24f + i % 3 * 0.08f),
                    startLifetime = 0.35f + i % 4 * 0.07f,
                    rotation = i * 39
                };
                particles.Emit(fragment, 1);
            }
        }
    }
}
