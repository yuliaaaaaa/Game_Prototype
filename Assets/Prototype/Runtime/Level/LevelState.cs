using System.Collections.Generic;

namespace ClearPath
{
    public sealed class LevelState
    {
        public IReadOnlyList<ObstacleState> Obstacles { get; private set; } = System.Array.Empty<ObstacleState>();

        public void Reset(GeneratedLevel layout) => Reset(layout.Obstacles);

        public void Reset(IReadOnlyList<ObstaclePlacement> placements)
        {
            var obstacles = new ObstacleState[placements.Count];
            for (int id = 0; id < obstacles.Length; id++)
            {
                var placement = placements[id];
                obstacles[id] = new ObstacleState(placement.Position, placement.Radius);
            }
            Obstacles = System.Array.AsReadOnly(obstacles);
        }
    }
}
