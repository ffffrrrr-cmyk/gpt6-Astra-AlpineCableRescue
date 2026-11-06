using UnityEngine;

namespace Frostline
{
    public sealed class RescueAudio : MonoBehaviour
    {
        AudioSource motor;
        AudioSource cue;
        AudioSource wind;
        public void Initialize()
        {
            motor = gameObject.AddComponent<AudioSource>(); motor.loop = true; motor.clip = Tone("Winch motor", 75, 1, true); motor.volume = 0; motor.Play();
            wind = gameObject.AddComponent<AudioSource>(); wind.loop = true; wind.clip = Tone("Alpine air", 30, 3, true); wind.volume = 0.055f; wind.pitch = 0.6f; wind.Play();
            cue = gameObject.AddComponent<AudioSource>();
        }
        public void UpdateMotor(float movement, float windStrength, bool active)
        {
            motor.volume = Mathf.Lerp(motor.volume, active ? movement * 0.13f : 0, Time.unscaledDeltaTime * 5);
            motor.pitch = 0.65f + movement * 0.6f; wind.volume = active ? 0.035f + windStrength * 0.008f : 0.02f;
        }
        public void PlayCue(float frequency, float duration = 0.16f)
        {
            var clip = Tone("Signal", frequency, duration, false); cue.PlayOneShot(clip, 0.24f); Destroy(clip, duration + 1);
        }
        static AudioClip Tone(string name, float frequency, float duration, bool texture)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            var random = new System.Random(61);
            for (int sample = 0; sample < samples.Length; sample++)
            {
                float time = sample / (float)sampleRate;
                float signal = Mathf.Sin(time * frequency * Mathf.PI * 2) * 0.6f + Mathf.Sin(time * frequency * 4 * Mathf.PI) * 0.13f;
                samples[sample] = texture ? signal * 0.4f + ((float)random.NextDouble() - 0.5f) * 0.12f : signal * Mathf.Sin(Mathf.PI * sample / samples.Length);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false); clip.SetData(samples, 0); return clip;
        }
    }
}
