using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VideoPlaylist
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class VrPlayTrigger : MonoBehaviour
    {
        [Header("绑定 PlaylistPlayer")]
        public PlaylistPlayer playlistPlayer;

        [Header("高亮颜色（瞄准触发物时）")]
        public Color hoverColor = new Color(0.2f, 1f, 0.2f, 1f); // 绿色

        private XRSimpleInteractable _interactable;
        private Renderer _renderer;
        private Color _originalColor;
        private MaterialPropertyBlock _mpb;

        void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            _renderer = GetComponent<Renderer>();

            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
                _mpb = new MaterialPropertyBlock();
            }
        }

        void OnEnable()
        {
            _interactable.firstHoverEntered.AddListener(OnHoverEnter);
            _interactable.lastHoverExited.AddListener(OnHoverExit);
            _interactable.selectEntered.AddListener(OnSelect);
        }

        void OnDisable()
        {
            _interactable.firstHoverEntered.RemoveListener(OnHoverEnter);
            _interactable.lastHoverExited.RemoveListener(OnHoverExit);
            _interactable.selectEntered.RemoveListener(OnSelect);
        }

        // 射线瞄准进入：高亮
        public void OnHoverEnter(HoverEnterEventArgs args)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", hoverColor);
            _renderer.SetPropertyBlock(_mpb);
        }

        // 射线离开：恢复
        public void OnHoverExit(HoverExitEventArgs args)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", _originalColor);
            _renderer.SetPropertyBlock(_mpb);
        }

        // 按扳机：触发播放
        public void OnSelect(SelectEnterEventArgs args)
        {
            if (playlistPlayer == null)
            {
                Debug.LogError("[VrPlayTrigger] playlistPlayer 未绑定！", this);
                return;
            }

            playlistPlayer.Play();
        }
    }
}
