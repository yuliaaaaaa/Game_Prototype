using System.Collections.Generic;
using UnityEngine;

namespace ClearPath
{
    public readonly struct ObstaclePlacement
    {
        public readonly Vector2 Position;
        public readonly float Radius, Height;
        public ObstaclePlacement(Vector2 position, float radius, float height)
        { Position = position; Radius = radius; Height = height; }
    }
    public sealed class GeneratedLevel
    {
        public readonly ObstaclePlacement[] Obstacles;
        public readonly float StartingVolume;
        public readonly float[] SolutionShots;
        public GeneratedLevel(ObstaclePlacement[] obstacles, float startingVolume, float[] solutionShots)
        { Obstacles = obstacles; StartingVolume = startingVolume; SolutionShots = solutionShots; }
        public List<ObstacleState> CreateStates()
        {
            var states = new List<ObstacleState>(Obstacles.Length);
            foreach (var p in Obstacles) states.Add(new ObstacleState(p.Position, p.Radius));
            return states;
        }
    }
}
