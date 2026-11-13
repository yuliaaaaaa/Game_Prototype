using UnityEngine;

namespace ClearPath
{
    [CreateAssetMenu(menuName = "Clear Path/Balance")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Volume units are radius cubed (common 4π/3 factor omitted)")]
        [Min(0.001f)] public float CriticalVolume = 0.035f;
        [Min(0.001f)] public float MinimumShotVolume = 0.009f;
        [Min(0.001f)] public float ChargePerSecond = 0.11f;
        [Min(0)] public float ReserveFraction = 0.5f;
        [Header("Movement")]
        [Min(0.1f)] public float MoveSpeed = 3.5f;
        [Tooltip("Forward distance between the warning stop and first contact.")]
        [Min(0.01f)] public float Clearance = 0.24f;
        [Tooltip("Hold the player in place while charging. Releasing the shot resumes movement.")]
        public bool PauseMovementWhileCharging = true;
        [Range(0.3f, 1)] public float PlayerRadiusScale = 0.7f;
        [Min(0.1f)] public float ReactionTime = 1.8f;
        [Min(0.1f)] public float ShotSpeed = 15;
        [Header("Infection")]
        [Min(0)] public float BaseBlastRadius = 0.35f;
        [Min(0)] public float BlastPerShotRadius = 3.2f;
        [Min(0)] public float ChargedBlastBonus = 6f;
        [Min(0)] public float InfectionLinkGap = 0.65f;
        [Min(0)] public float InfectionStepDelay = 0.09f;
        [Min(0)] public float InfectionFuse = 0.22f;
        public float PlayerRadius(float volume) => ResourceBudget.Radius(volume) * PlayerRadiusScale;

        public float BlastRadius(float volume)
        {
            float radius = ResourceBudget.Radius(volume);
            return BaseBlastRadius + radius * (BlastPerShotRadius + Mathf.Max(0, radius - 0.28f) * ChargedBlastBonus);
        }
    }
}
