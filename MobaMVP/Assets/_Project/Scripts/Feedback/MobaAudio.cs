using UnityEngine;

namespace Moba.Feedback
{
    /// <summary>Placeholder SFX via tiny procedural clips (no external audio assets).</summary>
    public sealed class MobaAudio : MonoBehaviour
    {
        public static MobaAudio Instance { get; private set; }

        [SerializeField] private AudioSource _source;
        private AudioClip _hitClip;
        private AudioClip _skillClip;
        private AudioClip _uiClip;

        private void Awake()
        {
            Instance = this;
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }

            _hitClip = MakeBeep(440f, 0.05f);
            _skillClip = MakeBeep(660f, 0.08f);
            _uiClip = MakeBeep(520f, 0.04f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayHit(bool skill) => Play(skill ? _skillClip : _hitClip, 0.35f);

        public void PlaySkill(byte slot) => Play(_skillClip, 0.4f + slot * 0.05f);

        public void PlayUi() => Play(_uiClip, 0.25f);

        private void Play(AudioClip clip, float volume)
        {
            if (_source == null || clip == null)
            {
                return;
            }

            _source.PlayOneShot(clip, volume);
        }

        private static AudioClip MakeBeep(float freq, float duration)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create($"beep_{freq}", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var env = 1f - t / duration;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
            }

            clip.SetData(data, 0);
            return clip;
        }
    }
}
