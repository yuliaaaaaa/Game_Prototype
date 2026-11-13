using UnityEngine;
using UnityEngine.Audio;

namespace ClearPath
{
    public sealed class AudioVolumeSettings
    {
        const string MusicKey = "ClearPath.MusicVolume";
        const string EffectsKey = "ClearPath.EffectsVolume";
        const string MusicParameter = "MusicVolume";
        const string EffectsParameter = "EffectsVolume";
        readonly AudioMixer mixer;

        public AudioVolumeSettings(AudioMixer mixer) => this.mixer = mixer;

        public float Music => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 0.6f));
        public float Effects => Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 0.8f));

        public void Apply()
        {
            Apply(MusicParameter, Music);
            Apply(EffectsParameter, Effects);
        }

        public void SetMusic(float volume) => Save(MusicKey, MusicParameter, volume);
        public void SetEffects(float volume) => Save(EffectsKey, EffectsParameter, volume);

        void Save(string key, string parameter, float volume)
        {
            volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(key, volume);
            PlayerPrefs.Save();
            Apply(parameter, volume);
        }

        void Apply(string parameter, float volume)
        {
            float decibels = volume <= 0.0001f ? -80 : 20 * Mathf.Log10(volume);
            mixer.SetFloat(parameter, decibels);
        }
    }
}
