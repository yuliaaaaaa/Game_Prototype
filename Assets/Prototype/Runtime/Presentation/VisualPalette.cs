using UnityEngine;

namespace ClearPath
{
    [CreateAssetMenu(menuName = "Clear Path/Visual Palette")]
    public sealed class VisualPalette : ScriptableObject
    {
        public Material Ground, Path, Player, Obstacle, Infected, Door, Frame, Effect;
        public Material Foliage, Stone, Sparks, ShotTrail;
        public GameObject[] BushModels;
        public GameObject RockModel, GateFrame, GatePanel;
        public GameObject Undergrowth;
        public GameObject BackdropTree;
        public AudioClip ShotSound, BreakSound, VictorySound, DefeatSound;
        public AudioClip BackgroundMusic;
        public UnityEngine.Audio.AudioMixerGroup MusicOutput, EffectsOutput;
        public Material Charge;

        public void ValidateScenery()
        {
            if (BushModels == null || BushModels.Length == 0 || !RockModel || !GateFrame || !GatePanel)
                throw new System.InvalidOperationException("Assign the scenery models in the visual palette.");
            foreach (var model in BushModels)
                if (!model) throw new System.InvalidOperationException("The visual palette contains an empty bush model.");
        }
    }
}
