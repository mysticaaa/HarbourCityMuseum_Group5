using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using ZxhMuseum;
using Wjy.Museum;

namespace CsyMuseum
{
    public sealed class CSYGuideCoordinator : MonoBehaviour
    {
        public Camera visitorCamera;
        public bool automaticAudio = true;
        public AudioSource[] additionalNarration = new AudioSource[0];
        public AudioSource[] duckableMusic = new AudioSource[0];
        public readonly List<CSYExhibitGuide> Guides = new List<CSYExhibitGuide>();
        public CSYExhibitGuide Speaking { get; private set; }
        public bool GalleryAvailable { get; private set; }
        private AudioSource voice;
        private CSYExhibitGuide focus;
        private float focusSince, nextScan;
        private bool foreground = true, paused;
        private readonly Dictionary<AudioSource, float> ducked = new Dictionary<AudioSource, float>();
        private NarrationPlayer[] narrators;
        private VideoPlayer[] videos;
        private WjyMuseumPauseMenu[] menus;
        private VensailCabinEntry[] boarding;
        private MuseumQuickMenu[] quickMenus;
        private MuseumControllerTutorial[] tutorials;
        private bool Muted => AudioListener.pause || AudioListener.volume <= .001f;

        private void Awake()
        {
            voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false;
            voice.loop = false; voice.spatialBlend = 0; voice.volume = .7f;
        }
        private void RefreshSources(bool force = false)
        {
            if (!force && menus != null && Time.unscaledTime < nextScan) return;
            narrators = FindObjectsOfType<NarrationPlayer>(); videos = FindObjectsOfType<VideoPlayer>();
            menus = FindObjectsOfType<WjyMuseumPauseMenu>(true);
            boarding = FindObjectsOfType<VensailCabinEntry>(true);
            quickMenus = FindObjectsOfType<MuseumQuickMenu>(true);
            tutorials = FindObjectsOfType<MuseumControllerTutorial>(true);
            nextScan = Time.unscaledTime + 1;
        }
        private bool AtGallery()
        {
            if (!visitorCamera || !visitorCamera.isActiveAndEnabled) visitorCamera = Camera.main;
            var session = MuseumSession.Instance;
            return visitorCamera && visitorCamera.isActiveAndEnabled
                && visitorCamera.gameObject.scene == gameObject.scene
                && SceneManager.GetActiveScene() == gameObject.scene
                && (!session || session.gameObject.scene != gameObject.scene || (!session.Busy && !session.InCabin));
        }
        private bool ModalOpen()
        {
            RefreshSources();
            foreach (var menu in menus) if (menu && menu.gameObject.scene == gameObject.scene && menu.IsOpen) return true;
            foreach (var entry in boarding) if (entry && entry.gameObject.scene == gameObject.scene && entry.ConfirmationOpen) return true;
            foreach (var menu in quickMenus) if (menu && menu.gameObject.scene == gameObject.scene && menu.IsOpen) return true;
            foreach (var tutorial in tutorials) if (tutorial && tutorial.gameObject.scene == gameObject.scene && tutorial.isActiveAndEnabled) return true;
            return false;
        }

        // Also checked at callback time; colliders alone cannot guard stale hover/click events.
        public bool CanInteract(CSYExhibitGuide guide)
        {
            if (!isActiveAndEnabled || !foreground || paused || !guide || !guide.isActiveAndEnabled
                || guide.Coordinator != this || !AtGallery() || ModalOpen() || !guide.Visit.Inside) return false;
            Vector3 delta = visitorCamera.transform.position - guide.RangePosition;
            return new Vector2(delta.x, delta.z).magnitude <= Mathf.Max(guide.enterRadius, guide.exitRadius)
                && Mathf.Abs(delta.y) <= guide.maxVertical;
        }

        private void Update()
        {
            RefreshSources();
            bool atGallery = AtGallery();
            GalleryAvailable = atGallery && foreground && !paused && !ModalOpen();
            bool occupied = MediaPlaying();
            CSYExhibitGuide nearest = null; float best = float.MaxValue;
            foreach (var guide in Guides)
            {
                if (!guide) continue;
                Vector3 delta = visitorCamera ? visitorCamera.transform.position - guide.RangePosition : Vector3.zero;
                float horizontal = new Vector2(delta.x, delta.z).magnitude;
                bool available = atGallery && guide.isActiveAndEnabled;
                guide.Visit.Update(available, horizontal, Mathf.Abs(delta.y), guide.enterRadius, guide.exitRadius, guide.maxVertical, Time.unscaledTime);
                if (!guide.Visit.Inside && Speaking != guide) guide.SetAudioState(CSYGuideAudioState.None);
                guide.SetPresentation(GalleryAvailable && available && horizontal < 5, visitorCamera);
                if (!GalleryAvailable || !guide.Visit.Inside) continue;
                Vector3 toGuide = guide.RangePosition - visitorCamera.transform.position; toGuide.y = 0;
                Vector3 forward = visitorCamera.transform.forward; forward.y = 0;
                if (Vector3.Dot(toGuide.normalized, forward.normalized) < .5f) continue;
                Vector3 sight = guide.RangePosition - visitorCamera.transform.position;
                if (Physics.Raycast(visitorCamera.transform.position, sight.normalized, out var hit, sight.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(guide.transform)
                    && (!guide.displayModel || !hit.transform.IsChildOf(guide.displayModel))) continue;
                if (horizontal < best) { best = horizontal; nearest = guide; }
            }
            if (focus != nearest) { focus = nearest; focusSince = Time.unscaledTime; }
            if (Speaking && (!GalleryAvailable || !Speaking.Visit.Inside || occupied || Muted)) EndSpeech(CSYGuideAudioState.Interrupted);
            if (Speaking && !voice.isPlaying) EndSpeech(CSYGuideAudioState.Finished);
            if (GalleryAvailable && automaticAudio && !Muted && !occupied && !Speaking && focus
                && focus.Visit.Ready(Time.unscaledTime, focus.dwellSeconds)
                && Time.unscaledTime - focusSince >= focus.dwellSeconds) Replay(focus);
            foreach (var guide in Guides)
                if (guide)
                {
                    var controls = CSYGuidePresentation.Build(GalleryAvailable && guide.isActiveAndEnabled,
                        guide.Visit.Inside, guide.narration, occupied, automaticAudio, Muted, guide.AudioState);
                    guide.ShowControls(controls, CanInteract(guide));
                }
        }

        private bool MediaPlaying()
        {
            if (videos != null) foreach (var video in videos)
                if (video && video.gameObject.scene == gameObject.scene && video.isPlaying) return true;
            if (narrators != null) foreach (var narrator in narrators)
                if (narrator && Blocks(narrator.GetComponent<AudioSource>(), true, false)) return true;
            if (additionalNarration != null) foreach (var source in additionalNarration)
                if (Blocks(source, false, true)) return true;
            return false;
        }
        private bool Blocks(AudioSource source, bool known, bool explicitlyRegistered)
        {
            return source && CSYGuideAudioRules.BlocksSpeech(source.gameObject.scene == gameObject.scene,
                source.isPlaying, known, explicitlyRegistered, source == voice);
        }

        public void Replay(CSYExhibitGuide guide)
        {
            if (!CanInteract(guide) || !guide.narration || Muted) return;
            RefreshSources(true);
            if (MediaPlaying()) return;
            EndSpeech(CSYGuideAudioState.Interrupted);
            Speaking = guide; guide.Visit.MarkPlayed(); guide.SetAudioState(CSYGuideAudioState.Playing);
            // No implicit global ducking: only explicitly opted-in, same-gallery music.
            if (duckableMusic != null) foreach (var source in duckableMusic)
                if (source && !ducked.ContainsKey(source)
                    && CSYGuideAudioRules.MayDuck(source.gameObject.scene == gameObject.scene, source.isPlaying, source.loop, true, source == voice))
                { ducked[source] = source.volume; source.volume *= .25f; }
            voice.clip = guide.narration; voice.Play();
        }
        public void StopFor(CSYExhibitGuide guide)
        {
            if (!CanInteract(guide)) return;
            if (guide) { guide.Visit.MarkPlayed(); guide.SetAudioState(CSYGuideAudioState.Skipped); }
            if (Speaking == guide) EndSpeech(CSYGuideAudioState.Skipped);
        }
        internal void GuideDisabled(CSYExhibitGuide guide)
        { if (Speaking == guide) EndSpeech(CSYGuideAudioState.Interrupted); }
        public void ToggleAutomatic()
        {
            if (!isActiveAndEnabled || !foreground || paused || !AtGallery() || ModalOpen()) return;
            automaticAudio = !automaticAudio; EndSpeech(CSYGuideAudioState.Skipped);
            foreach (var guide in Guides) if (guide && guide.Visit.Inside)
            { guide.Visit.MarkPlayed(); guide.SetAudioState(CSYGuideAudioState.Skipped); }
        }
        private void EndSpeech(CSYGuideAudioState state)
        {
            if (Speaking) Speaking.SetAudioState(state);
            if (voice) voice.Stop(); Speaking = null; RestoreMusic();
        }
        private void RestoreMusic()
        {
            foreach (var pair in ducked)
                if (pair.Key && CSYGuideAudioRules.CanRestore(pair.Key.volume, pair.Value)) pair.Key.volume = pair.Value;
            ducked.Clear();
        }
        private void OnApplicationFocus(bool value) { foreground = value; if (!value) EndSpeech(CSYGuideAudioState.Interrupted); }
        private void OnApplicationPause(bool value) { paused = value; if (value) EndSpeech(CSYGuideAudioState.Interrupted); }
        private void OnDisable()
        {
            EndSpeech(CSYGuideAudioState.Interrupted); GalleryAvailable = false; focus = null;
            foreach (var guide in Guides) if (guide)
            { guide.Visit.Reset(); guide.SetAudioState(CSYGuideAudioState.None); guide.SetPresentation(false, visitorCamera); }
        }
    }
}
