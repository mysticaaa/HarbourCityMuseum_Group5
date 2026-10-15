using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class VideoButton : MonoBehaviour
{
    public enum ButtonType { Play, Pause }
    [Header("按钮功能类型")]
    [SerializeField] private ButtonType buttonType;

    [Header("要控制的目标 Video Player")]
    [SerializeField] private VideoPlayer targetVideoPlayer;

    private XRBaseInteractable interactable;
    private bool isHovered = false;
    private ActionBasedController targetController;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
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
        if (isHovered && targetController != null)
        {
            if (targetController.activateAction.action.WasPressedThisFrame())
            {
                ExecuteAction();
            }
        }
    }

    private void ExecuteAction()
    {
        if (targetVideoPlayer == null) return;

        if (buttonType == ButtonType.Play)
        {
            // 如果视频播完了（停在末尾），再次点击 Play 时让它从头开始播放
            if (!targetVideoPlayer.isPlaying)
            {
                targetVideoPlayer.Play();
            }
        }
        else if (buttonType == ButtonType.Pause)
        {
            // Pause 按钮只负责暂停
            targetVideoPlayer.Pause();
        }
    }
}