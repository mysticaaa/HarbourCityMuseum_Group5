using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))] // 支持 SimpleInteractable
public class ship_museum : MonoBehaviour
{
    private XRBaseInteractable interactable;
    [Header("场景切换设置")]
    [Tooltip("扣动扳机后要切换的目标场景名称")]
    [SerializeField] private string targetSceneName = "NextSceneName";
    private bool isHovered = false; // 记录是否被光标指着
    private ActionBasedController targetController; // 记录当前指着它的控制器

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
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
    private void SwitchScene()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
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

    private void Update()
    {
        // 只要光标指着它（isHovered 为真），并且对应的手柄存在
        if (isHovered && targetController != null)
        {
            // 检测该手柄的 Activate 动作（也就是 Trigger 键）是否在这个瞬间被按下
            if (targetController.activateAction.action.WasPressedThisFrame())
            {
                SwitchScene();
            }
        }
    }
}