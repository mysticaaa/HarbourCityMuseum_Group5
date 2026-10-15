using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class VideoCtrl : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    private XRBaseInteractable interactable;
    private bool isHovered = false;
    private ActionBasedController targetController;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        isHovered = true;
        if (args.interactorObject is XRBaseControllerInteractor controllerInteractor)
        {
            targetController = controllerInteractor.xrController as ActionBasedController;
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        isHovered = false;
        targetController = null;
    }

    private void Update()
    {
        // 当射线指着视频（或控制按钮），并且扣动了 Trigger (Activate动作)
        if (isHovered && targetController != null)
        {
            if (targetController.activateAction.action.WasPressedThisFrame())
            {
                TogglePlayPause();
            }
        }
    }

    public void TogglePlayPause()
    {
        if (videoPlayer == null) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
        }
    }
}