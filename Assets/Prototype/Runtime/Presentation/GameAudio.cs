using UnityEngine;

namespace ClearPath
{
    public sealed class GameAudio
    {
        readonly VisualPalette palette;
        readonly AudioSource source;
        readonly AudioSource music;
        readonly AudioVolumeSettings settings;
        float nextBreakTime;

        public GameAudio(Transform parent, VisualPalette palette, AudioVolumeSettings settings)
        {
            this.palette = palette;
            this.settings = settings;
            var root = new GameObject("Gameplay Audio");
            root.transform.SetParent(parent, false);
            source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.outputAudioMixerGroup = palette.EffectsOutput;
            music = root.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.clip = palette.BackgroundMusic;
            music.outputAudioMixerGroup = palette.MusicOutput;
        }

        public void StartMusic()
        {
            settings.Apply();
            if (music.clip) music.Play();
        }

        public void Shot() => Play(palette.ShotSound, 0.65f);

        public void ObstacleBroken()
        {
            if (Time.unscaledTime < nextBreakTime) return;
            nextBreakTime = Time.unscaledTime + 0.075f;
            Play(palette.BreakSound, 0.45f);
        }

        public void Result(bool won) => Play(won ? palette.VictorySound : palette.DefeatSound, 0.7f);

        public void Reset()
        {
            source.Stop();
            nextBreakTime = 0;
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip) source.PlayOneShot(clip, volume);
        }
    }
}
