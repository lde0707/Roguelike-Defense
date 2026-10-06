using UnityEngine;
namespace Rushitadel.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioChannel : MonoBehaviour
    {
        public bool music;
        [Range(0f, 1f)] public float baseVolume = 1f;
        private void OnEnable() { GameSettings.AudioChanged += Apply; Apply(); }
        private void OnDisable() => GameSettings.AudioChanged -= Apply;
        private void Apply()
        {
            if (GameSettings.Live != null) GetComponent<AudioSource>().volume = baseVolume * (music ? GameSettings.Live.music : GameSettings.Live.effects);
        }
    }
}
