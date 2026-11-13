using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public sealed class LevelConfigurator : MonoBehaviour
    {
        [SerializeField] VisualPalette palette;
        [SerializeField] LevelCatalog catalog;
        [SerializeField] Transform generated;
        readonly Dictionary<int, ObstacleView> views = new Dictionary<int, ObstacleView>();
        readonly LevelScenery scenery = new LevelScenery();
        public PlayerView Player { get; private set; }
        public DoorView Door { get; private set; }
        public VisualPalette Palette => palette;
        public LineRenderer AimRing { get; private set; }
        public GeneratedLevel Data { get; private set; }
        public LevelCatalog Catalog => catalog;
        Transform path;
        LevelConfig current;
        public void Configure(VisualPalette colors, LevelCatalog levelCatalog)
        { palette = colors; catalog = levelCatalog; }
        public void Build(LevelConfig level, GeneratedLevel data)
        {
            if (!level) throw new System.InvalidOperationException("Missing level configuration.");
            if (!level.IsValid(out var reason)) throw new System.InvalidOperationException("Invalid level configuration: " + reason);
            palette.ValidateScenery();
            if (generated) PrimitiveFactory.Remove(generated.gameObject);
            generated = new GameObject("Generated Level").transform; generated.SetParent(transform, false);
            current = level; Data = data; views.Clear();
            AimRing = new GameObject("Shot radius preview").AddComponent<LineRenderer>();
            AimRing.transform.SetParent(generated, false); AimRing.loop = true;
            AimRing.positionCount = 64; AimRing.widthMultiplier = 0.035f;
            AimRing.sharedMaterial = palette.ShotTrail; AimRing.useWorldSpace = true;
            AimRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AimRing.receiveShadows = false; AimRing.enabled = false;
            var ground = Make("Ground", PrimitiveType.Cube, new Vector3(0, -0.18f, level.Length / 2), new Vector3(1000, 0.35f, 1000), palette.Ground);
            var props = new MaterialPropertyBlock(); props.SetColor("_BaseColor", level.GroundColor); ground.GetComponent<Renderer>().SetPropertyBlock(props);
            path = Make("Living path", PrimitiveType.Cube, new Vector3(0, -0.025f, level.Length / 2), new Vector3(2, 0.06f, level.Length + 1), palette.Path).transform;
            path.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var debris = new DebrisEffect(generated, palette.Sparks);
            for (int i = 0; i < data.Obstacles.Length; i++)
            {
                var placement = data.Obstacles[i];
                views.Add(i, scenery.CreateObstacle(generated, palette, level, placement, i, debris));
            }
            scenery.AddUndergrowth(generated, palette, level, data);
            scenery.AddForestBackdrop(generated, palette, level, data);
            var playerRoot = new GameObject("Player"); playerRoot.transform.SetParent(generated, false);
            Player = playerRoot.AddComponent<PlayerView>();
            var body = PrimitiveFactory.Create("Body", PrimitiveType.Sphere, playerRoot.transform, Vector3.up, Vector3.one, palette.Player);
            var charge = PrimitiveFactory.Create("Charging shot", PrimitiveType.Sphere, playerRoot.transform, Vector3.up, Vector3.one, palette.Charge);
            charge.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float initialRadius = level.Balance.PlayerRadius(data.StartingVolume);
            Player.Configure(body.transform, charge.transform, palette); Player.Present(0, initialRadius, 0);
            Door = scenery.CreateGate(generated, palette, level.Length);
            UpdatePath(initialRadius);
        }
        public void ShowInfection(int id)
        {
            if (views.TryGetValue(id, out var view)) view.Infect(palette.Infected);
        }

        public void ShowDestruction(int id)
        {
            if (views.TryGetValue(id, out var view)) view.Explode();
        }
        GameObject Make(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
            => PrimitiveFactory.Create(name, type, generated, position, scale, material);
        public void UpdatePath(float radius)
        { if (path) path.localScale = new Vector3(radius * 2, 0.06f, current.Length + 1); }
        void OnDrawGizmosSelected()
        {
            if (!current) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(new Vector3(-3, 0.1f, current.Length - DoorView.OpenDistance), new Vector3(3, 0.1f, current.Length - DoorView.OpenDistance));
        }
    }
}
