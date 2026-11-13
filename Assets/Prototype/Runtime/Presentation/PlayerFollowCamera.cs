using UnityEngine;

namespace ClearPath
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class PlayerFollowCamera : MonoBehaviour
    {
        [SerializeField] LevelConfigurator level;
        [SerializeField, Min(1)] float orthographicSize = 5.7f;
        [SerializeField, Min(0.01f)] float followTime = 0.12f;

        Camera view;
        PlayerView target;
        Vector3 followCenter;
        float aspect;

        public void Configure(LevelConfigurator world) => level = world;

        void LateUpdate() => UpdateFollow(Time.deltaTime);

        public void SnapToPlayer()
        {
            target = null;
            UpdateFollow(0);
        }

        public void UpdateFollow(float deltaTime)
        {
            var player = level ? level.Player : null;
            if (!player) return;
            if (!view) view = GetComponent<Camera>();

            float currentAspect = Mathf.Max(0.01f, view.aspect);
            Vector3 center = player.Center;
            if (target != player || !Mathf.Approximately(aspect, currentAspect))
            {
                target = player;
                aspect = currentAspect;
                followCenter = center;
            }
            else
            {
                followCenter = Vector3.Lerp(followCenter, center, 1 - Mathf.Exp(-deltaTime / followTime));
            }

            const float pitch = 55;
            const float depth = 18;
            float yaw = Mathf.Atan(currentAspect * Mathf.Sin(pitch * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(pitch, -yaw, 0);

            view.orthographic = true;
            view.orthographicSize = Mathf.Max(orthographicSize, 2.8f / currentAspect);
            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * currentAspect;
            float anchorX = Mathf.Max(0.19f, 0.05f + player.Radius / (2 * halfWidth));
            float anchorY = Mathf.Max(0.20f, 0.12f + player.Radius / (2 * halfHeight));
            var cameraOffset = new Vector3((1 - 2 * anchorX) * halfWidth, (1 - 2 * anchorY) * halfHeight, -depth);
            Vector3 focus = followCenter;
            if (level.Door)
            {
                float goalZ = level.Door.transform.position.z;
                float approach = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(goalZ - 6, goalZ, center.z));
                focus.z -= approach * 2.4f;
            }
            transform.SetPositionAndRotation(focus + rotation * cameraOffset, rotation);
        }
    }
}
