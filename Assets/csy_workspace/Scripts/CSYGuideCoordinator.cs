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
        public readonly List<CSYExhibitGuide> Guides = new List<CSYExhibitGuide>();
        public CSYExhibitGuide Speaking { get; private set; }
        public bool GalleryAvailable { get; private set; }
        private AudioSource voice;
        private CSYExhibitGuide focus;
        private float focusSince;
        private bool foreground = true;
        private readonly Dictionary<AudioSource, float> ducked = new Dictionary<AudioSource, float>();
        private AudioSource[] otherAudio;
        private VideoPlayer[] videos;
        private WjyMuseumPauseMenu[] menus;
        private VensailCabinEntry[] boarding;
        private MuseumQuickMenu[] quickMenus;
        private MuseumControllerTutorial[] tutorials;
        private float nextScan;
        private void Awake()
        {
            voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false;
            voice.loop = false; voice.spatialBlend = 0; voice.volume = .7f;
        }
        private void Update()
        {
            if (!visitorCamera || !visitorCamera.isActiveAndEnabled) visitorCamera = Camera.main;
            var session = MuseumSession.Instance;
            if (menus == null) menus = FindObjectsOfType<WjyMuseumPauseMenu>();
            if (boarding == null) boarding = FindObjectsOfType<VensailCabinEntry>();
            if (quickMenus == null) quickMenus = FindObjectsOfType<MuseumQuickMenu>(true);
            if (tutorials == null) tutorials = FindObjectsOfType<MuseumControllerTutorial>(true);
            bool modal = false;
            foreach (var menu in menus) if (menu && menu.IsOpen) modal = true;
            foreach (var entry in boarding) if (entry && entry.ConfirmationOpen) modal = true;
            foreach (var menu in quickMenus) if (menu && menu.gameObject.scene == gameObject.scene && menu.IsOpen) modal = true;
            foreach (var tutorial in tutorials) if (tutorial && tutorial.gameObject.scene == gameObject.scene && tutorial.isActiveAndEnabled) modal = true;
            bool atGallery = foreground && visitorCamera && visitorCamera.isActiveAndEnabled
                && visitorCamera.gameObject.scene == gameObject.scene
                && SceneManager.GetActiveScene() == gameObject.scene
                && (!session || (!session.Busy && !session.InCabin));
            GalleryAvailable = atGallery && !modal;
            if (Time.unscaledTime >= nextScan)
            {
                otherAudio = FindObjectsOfType<AudioSource>(); videos = FindObjectsOfType<VideoPlayer>(); nextScan = Time.unscaledTime + 1;
            }
            bool occupied = MediaPlaying();
            CSYExhibitGuide nearest = null; float best = float.MaxValue;
            foreach (var guide in Guides)
            {
                if (!guide) continue;
                Vector3 delta = visitorCamera ? visitorCamera.transform.position - guide.RangePosition : Vector3.zero;
                float horizontal = new Vector2(delta.x, delta.z).magnitude;
                bool available = atGallery && guide.isActiveAndEnabled;
                guide.Visit.Update(available, horizontal, Mathf.Abs(delta.y), guide.enterRadius, guide.exitRadius, guide.maxVertical, Time.unscaledTime);
                guide.SetPresentation(GalleryAvailable && available && horizontal < 5, visitorCamera);
                if (!GalleryAvailable || !guide.Visit.Inside) continue;
                Vector3 toGuide = guide.RangePosition - visitorCamera.transform.position; toGuide.y = 0;
                Vector3 forward = visitorCamera.transform.forward; forward.y = 0;
                // Intent gate: looking within 60 degrees, not merely passing alongside a station.
                if (Vector3.Dot(toGuide.normalized, forward.normalized) < .5f) continue;
                Vector3 sight = guide.RangePosition - visitorCamera.transform.position;
                if (Physics.Raycast(visitorCamera.transform.position, sight.normalized, out var hit, sight.magnitude, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(guide.transform)
                    && (!guide.displayModel || !hit.transform.IsChildOf(guide.displayModel))) continue;
                if (horizontal < best) { best = horizontal; nearest = guide; }
            }
            if (focus != nearest) { focus = nearest; focusSince = Time.unscaledTime; }
            if (Speaking && (!GalleryAvailable || !Speaking.Visit.Inside || occupied)) StopFor(Speaking);
            if (Speaking && !voice.isPlaying) { Speaking = null; RestoreMusic(); }
            if (GalleryAvailable && automaticAudio && !occupied && !Speaking && focus
                && focus.Visit.Ready(Time.unscaledTime, focus.dwellSeconds)
                && Time.unscaledTime - focusSince >= focus.dwellSeconds) Replay(focus);
            foreach (var guide in Guides)
                if (guide) guide.ShowState(Speaking == guide ? "Playing introduction - Stop skips audio."
                    : occupied ? "Video or another narration is playing. Read here or replay later."
                    : automaticAudio ? "Auto audio on. Replay to hear again; Stop to skip."
                    : "Auto audio off. Read here or select Replay.");
        }
        private bool MediaPlaying()
        {
            if (videos != null) foreach (var video in videos) if (video && video.isPlaying) return true;
            if (otherAudio != null) foreach (var source in otherAudio)
                if (source && source != voice && source.isPlaying && !source.loop) return true;
            return false;
        }
        public void Replay(CSYExhibitGuide guide)
        {
            if (!GalleryAvailable || !guide || !guide.Visit.Inside || !guide.narration || MediaPlaying()) return;
            StopAll(); Speaking = guide; guide.Visit.MarkPlayed();
            if (otherAudio != null) foreach (var source in otherAudio)
                if (source && source != voice && source.isPlaying && source.loop)
                { ducked[source] = source.volume; source.volume *= .25f; }
            voice.clip = guide.narration; voice.Play();
        }
        public void StopFor(CSYExhibitGuide guide)
        {
            if (guide) guide.Visit.MarkPlayed();
            if (Speaking == guide) StopAll();
        }
        public void ToggleAutomatic()
        {
            automaticAudio = !automaticAudio; StopAll();
            foreach (var guide in Guides) if (guide && guide.Visit.Inside) guide.Visit.MarkPlayed();
        }
        private void StopAll() { if (voice) voice.Stop(); Speaking = null; RestoreMusic(); }
        private void RestoreMusic()
        {
            foreach (var pair in ducked) if (pair.Key && Mathf.Approximately(pair.Key.volume, pair.Value * .25f)) pair.Key.volume = pair.Value;
            ducked.Clear();
        }
        private void OnApplicationFocus(bool value) { foreground = value; if (!value) StopAll(); }
        private void OnApplicationPause(bool value) { if (value) StopAll(); }
        private void OnDisable()
        {
            StopAll();
            foreach (var guide in Guides) if (guide) { guide.Visit.Reset(); guide.SetPresentation(false, visitorCamera); }
        }
    }
}
