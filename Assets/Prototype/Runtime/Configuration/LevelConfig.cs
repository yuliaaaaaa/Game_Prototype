using System;
using UnityEngine;

namespace ClearPath
{
    [CreateAssetMenu(menuName = "Clear Path/Level")]
    public sealed class LevelConfig : ScriptableObject
    {
        public string DisplayName = "First clearing";
        public BalanceConfig Balance;
        public int Seed = 173;
        [Range(12, 40)] public float Length = 24;
        [Tooltip("Coverage of the whole field, including the path.")]
        [Range(0.2f, 1)] public float Density = 0.8f;
        [Tooltip("Preference for placing obstacles near existing ones rather than uniformly.")]
        [Range(0, 1)] public float Clustering = 0.65f;
        [Tooltip("Dense groups across the route, spaced between the start and the gate.")]
        [Range(0, 4)] public int ThicketCount;
        [Tooltip("Minimum and maximum obstacle radius in meters.")]
        public Vector2 RadiusRange = new Vector2(0.36f, 0.53f);
        public Color GroundColor = new Color(0.13f, 0.24f, 0.22f);
        [Header("Obstacle appearance")]
        public GameObject[] ObstacleModels;
        public Material ObstacleMaterial;
        public GameObject RockModel;
        [Min(1)] public int RockInterval = 7;
        public bool IsValid(out string reason)
        {
            if (!Balance) { reason = "Assign a Balance config."; return false; }
            if (Length < 12 || Length > 40 || Density < 0.2f || Density > 1 || Clustering < 0 || Clustering > 1 ||
                ThicketCount < 0 || ThicketCount > 4)
            { reason = "Length, density or clustering is outside the supported range."; return false; }
            if (RadiusRange.x < 0.2f || RadiusRange.y > 0.8f || RadiusRange.y < RadiusRange.x)
            { reason = "Obstacle radii must be ordered and between 0.2 and 0.8 meters."; return false; }
            if (Balance.ChargePerSecond <= 0 || Balance.MoveSpeed <= 0 || Balance.ShotSpeed <= 0 || Balance.CriticalVolume <= 0 || Balance.MinimumShotVolume <= 0 ||
                Balance.ReserveFraction < 0 || Balance.MinimumReserveVolume < 0 || Balance.PlayerRadiusScale <= 0 || Balance.ReactionTime <= 0)
            { reason = "Speeds and critical volume must be positive."; return false; }
            reason = null; return true;
        }
    }
}
