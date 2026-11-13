using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace ClearPath
{
    public sealed class AudioMenu : MonoBehaviour
    {
        [SerializeField] Slider music, effects;
        [SerializeField] Text musicValue, effectsValue;
        AudioVolumeSettings settings;

        public void Configure(Slider musicSlider, Slider effectsSlider, Text musicLabel, Text effectsLabel)
        {
            music = musicSlider;
            effects = effectsSlider;
            musicValue = musicLabel;
            effectsValue = effectsLabel;
        }

        [Inject]
        public void Construct(AudioVolumeSettings volumeSettings)
        {
            settings = volumeSettings;
            Refresh();
        }

        void Awake()
        {
            music.onValueChanged.AddListener(ChangeMusic);
            effects.onValueChanged.AddListener(ChangeEffects);
        }

        void OnEnable()
        {
            if (settings != null) Refresh();
        }

        void Refresh()
        {
            music.SetValueWithoutNotify(settings.Music);
            effects.SetValueWithoutNotify(settings.Effects);
            SetLabel(musicValue, music.value);
            SetLabel(effectsValue, effects.value);
        }

        void ChangeMusic(float value)
        {
            settings.SetMusic(value);
            SetLabel(musicValue, value);
        }

        void ChangeEffects(float value)
        {
            settings.SetEffects(value);
            SetLabel(effectsValue, value);
        }

        static void SetLabel(Text label, float value) => label.text = $"{Mathf.RoundToInt(value * 100)}%";

        void OnDestroy()
        {
            music.onValueChanged.RemoveListener(ChangeMusic);
            effects.onValueChanged.RemoveListener(ChangeEffects);
        }
    }
}
