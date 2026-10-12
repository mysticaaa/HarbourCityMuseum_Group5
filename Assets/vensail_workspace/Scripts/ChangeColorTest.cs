using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))] // 支持 SimpleInteractable
[RequireComponent(typeof(Renderer))]
public class ChangeColorOnHoverAndTrigger : MonoBehaviour
{
    private XRBaseInteractable interactable;
    private Renderer objectRenderer;

    [SerializeField] private Color targetColor = Color.blue; // 触发后的颜色

    private bool isHovered = false; // 记录是否被光标指着
    private ActionBasedController targetController; // 记录当前指着它的控制器

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        objectRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        // 监听悬停（Hover）事件：当光标指过来时
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

        // 获取当前指向该物体的控制器
        var interactor = args.interactorObject;
        if (interactor is XRBaseControllerInteractor controllerInteractor)
        {
            targetController = controllerInteractor.xrController as ActionBasedController;
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        isHovered = false;
        targetController = null;
    }

    public new GameObject gameObject;
    void guanbi()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        // 只要光标指着它（isHovered 为真），并且对应的手柄存在
        if (isHovered && targetController != null)
        {
            // 检测该手柄的 Activate 动作（也就是 Trigger 键）是否在这个瞬间被按下
            if (targetController.activateAction.action.WasPressedThisFrame())
            {
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = targetColor;
                    guanbi();
                }
            }
        }
    }
}