using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;

namespace Wjy.Museum
{
    [DefaultExecutionOrder(40), DisallowMultipleComponent]
    public sealed class WjyOceanFishingSession : MonoBehaviour
    {
        [SerializeField] private bool useMuseumExhibitLayout;
        public bool UseMuseumExhibitLayout => useMuseumExhibitLayout;

        private WjyFishingRodCatchInteraction rod;
        private XROrigin rig;
        private WjyOceanFishingUI ui;
        private WjyOceanCatchReveal reveal;
        private GameObject raft;
        private BoxCollider sea;
        private Scene museum, ocean;
        private Vector3 displayPosition, museumFeet, museumFacing;
        private Quaternion displayRotation;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private readonly List<AudioSource> audioToResume = new List<AudioSource>();
        private readonly List<VideoPlayer> videosToResume = new List<VideoPlayer>();
        public bool AtSea { get; private set; }

        private void Start()
        {
            museum = gameObject.scene;
            rod = GetComponentInChildren<WjyFishingRodCatchInteraction>();
            rig = rod ? rod.SharedRig : null;
            if (!rig || !rig.Camera || !rod.PedestalInstructions)
            {
                Debug.LogError("Ocean fishing needs an initialized rod and its museum XR camera.", this);
                enabled = false;
                return;
            }
            displayPosition = rod.transform.localPosition;
            displayRotation = rod.transform.localRotation;
            ui = gameObject.AddComponent<WjyOceanFishingUI>();
            ui.BuildMuseum(transform, rod.PedestalInstructions, rig.Camera, EnterOcean);
        }

        public void EnterOcean()
        {
            if (AtSea || !ui || !isActiveAndEnabled) return;
            var view = rig.Camera.transform;
            museumFeet = new Vector3(view.position.x, rig.transform.position.y, view.position.z);
            museumFacing = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (museumFacing.sqrMagnitude < .001f) museumFacing = rig.transform.forward;
            SuspendMuseum();
            AtSea = true;
            if (!raft) CreateOcean();
            SceneManager.SetActiveScene(ocean);
            raft.SetActive(true);
            ui.ShowOcean(true);
            rod.PlaceAt(raft.transform, new Vector3(.66f,.18f,.88f), Quaternion.identity, sea, reveal);
            MoveTo(raft.transform.position, raft.transform.forward);
        }

        private void CreateOcean()
        {
            // An additive scene keeps the museum visit and collection alive without a build-list dependency.
            ocean = SceneManager.CreateScene("WJY Open Ocean");
            raft = new GameObject("Ocean Fishing Raft");
            SceneManager.MoveGameObjectToScene(raft, ocean);
            raft.SetActive(false);
            raft.transform.position = new Vector3(0f, 0f, 2048f);
            raft.AddComponent<WjyOceanEnvironment>();
            reveal = raft.AddComponent<WjyOceanCatchReveal>();
            var water = new GameObject("Open sea casting surface");
            water.layer = Physics.IgnoreRaycastLayer;
            water.transform.SetParent(raft.transform, false);
            sea = water.AddComponent<BoxCollider>();
            sea.isTrigger = true;
            sea.center = new Vector3(0f, WjyOceanEnvironment.SeaLevel - .05f, 0f);
            sea.size = new Vector3(100f,.1f,100f);
            ui.BuildRaft(raft.transform, rig.Camera, ReturnToMuseum);
        }

        private void MoveTo(Vector3 floorPosition, Vector3 facing)
        {
            var controller = rig.GetComponent<CharacterController>();
            bool restoreController = controller && controller.enabled;
            if (restoreController) controller.enabled = false;
            float height = rig.Camera.transform.position.y - rig.transform.position.y;
            rig.MatchOriginUpCameraForward(Vector3.up, facing);
            rig.MoveCameraToWorldLocation(floorPosition + Vector3.up * height);
            if (restoreController) controller.enabled = true;
            Physics.SyncTransforms();
        }

        public void ReturnToMuseum()
        {
            if (!AtSea) return;
            AtSea = false;
            rod.PlaceAt(transform, displayPosition, displayRotation, null, null);
            ui.ShowOcean(false);
            raft.SetActive(false);
            SceneManager.SetActiveScene(museum);
            MoveTo(museumFeet, museumFacing);
            foreach (var behaviour in suspended)
                if (behaviour) behaviour.enabled = true;
            suspended.Clear();
            foreach (var source in audioToResume)
                if (source) source.UnPause();
            audioToResume.Clear();
            foreach (var video in videosToResume)
                if (video) video.Play();
            videosToResume.Clear();
        }

        private void SuspendMuseum()
        {
            Suspend(GetComponent<WjyFishingTeleportRing>());
            Suspend(GetComponent<WjyMuseumPauseMenu>());
            foreach (var root in museum.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>()) Suspend(light);
                foreach (var canvas in root.GetComponentsInChildren<Canvas>()) Suspend(canvas);
                foreach (var move in root.GetComponentsInChildren<ContinuousMoveProviderBase>())
                    if (move.system && move.system.xrOrigin == rig) Suspend(move);
                foreach (var source in root.GetComponentsInChildren<AudioSource>())
                    if (source.isPlaying) { source.Pause(); audioToResume.Add(source); }
                foreach (var video in root.GetComponentsInChildren<VideoPlayer>())
                    if (video.isPlaying) { video.Pause(); videosToResume.Add(video); }
            }
        }

        private void Suspend(Behaviour component)
        {
            if (!component || !component.isActiveAndEnabled) return;
            component.enabled = false;
            suspended.Add(component);
        }

        private void OnDisable()
        {
            if (Application.isPlaying && AtSea && museum.isLoaded && rig && raft && rod)
                ReturnToMuseum();
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && ocean.IsValid() && ocean.isLoaded && SceneManager.sceneCount > 1)
                SceneManager.UnloadSceneAsync(ocean);
        }
    }
}
