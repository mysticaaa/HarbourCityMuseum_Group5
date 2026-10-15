using UnityEngine;
using System.Collections.Generic;

namespace ZxhMuseum
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class NarrationPlayer : MonoBehaviour
    {
        private AudioSource audioSource;
        private WorldPanel subtitle;
        private readonly List<string> segments = new List<string>();
        public void Play(string key, string transcript)
        {
            Stop();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 0; audioSource.volume = 0.65f;
            var clip = Resources.Load<AudioClip>("ZxhNarration/" + key);
            var json = Resources.Load<TextAsset>("ZxhNarration/Transcripts");
            if (json != null)
            {
                var all = JsonUtility.FromJson<Transcripts>(json.text);
                string actual = all.Get(key);
                if (!string.IsNullOrEmpty(actual)) transcript = actual;
            }
            if (clip != null) { audioSource.clip = clip; audioSource.Play(); }
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Vector3 position = camera.transform.position + forward * 1.1f + Vector3.down * 0.3f;
            subtitle = WorldPanel.Create(transform, "Guide / subtitles", position, Quaternion.LookRotation(forward), 800);
            subtitle.Status.text = transcript;
            segments.Clear();
            string[] words = transcript.Split(' ');
            for (int i = 0; i < words.Length; i += 12)
                segments.Add(string.Join(" ", words, i, Mathf.Min(12, words.Length - i)));
            subtitle.Button("Stop narration / close", Stop);
        }
        public void Stop()
        {
            if (audioSource != null) audioSource.Stop();
            if (subtitle != null) Destroy(subtitle.gameObject);
            subtitle = null;
        }
        private void OnDisable() => Stop();
        private void Update()
        {
            if (subtitle != null && audioSource != null && audioSource.isPlaying && segments.Count > 0)
            {
                int index = Mathf.Clamp(Mathf.FloorToInt(audioSource.time / audioSource.clip.length * segments.Count), 0, segments.Count - 1);
                subtitle.Status.text = segments[index];
            }
        }
        [System.Serializable]
        private sealed class Transcripts
        {
            public string Welcome, Cabin, Wheel, Helmet, Work, Migration, Memories;
            public string Get(string key)
            {
                switch (key)
                {
                    case "Welcome": return Welcome; case "Cabin": return Cabin; case "Wheel": return Wheel;
                    case "Helmet": return Helmet; case "Work": return Work; case "Migration": return Migration;
                    case "Memories": return Memories; default: return null;
                }
            }
        }
    }
}
