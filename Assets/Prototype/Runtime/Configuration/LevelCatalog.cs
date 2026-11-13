using System;
using UnityEngine;

namespace ClearPath
{
    [CreateAssetMenu(menuName = "Clear Path/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [Tooltip("Zero-based index of the level selected when the game opens.")]
        [Min(0)] public int InitialLevelIndex;
        public LevelConfig[] Levels;

        public int Count => Levels?.Length ?? 0;

        public LevelConfig GetLevel(int requestedIndex, out int index)
        {
            if (Count == 0) throw new InvalidOperationException("Level catalog is empty.");
            index = requestedIndex % Count;
            if (index < 0) index += Count;
            var level = Levels[index];
            if (!level) throw new InvalidOperationException($"Level catalog slot {index} is empty.");
            return level;
        }
    }
}
