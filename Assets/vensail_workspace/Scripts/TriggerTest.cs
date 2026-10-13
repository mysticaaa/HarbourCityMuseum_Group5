using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Renderer))]
public class DirectTriggerWithoutGrab : MonoBehaviour
{
    private XRSimpleInteractable interactable;
    private Renderer objectRenderer;

    public GameObject ShowText;
    [SerializeField] private Color targetColor = Color.blue;
    [SerializeField] private InputActionProperty activateAction; // 绑定手柄的 Trigger 动作（可在面板指定）

    private Color originalColor = Color.white;
    private bool isHovered = false;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }

    private void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);

        // 启用输入动作
        if (activateAction.action != null)
            activateAction.action.Enable();
    }

    private void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);

        if (activateAction.action != null)
            activateAction.action.Disable();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        isHovered = true;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        isHovered = false;
    }

    private void Update()
    {
        // 核心逻辑：只要射线指着物体（Hover），并且按下了 Trigger 键
        if (isHovered && activateAction.action != null && activateAction.action.triggered)
        {
            ToggleState();
        }
    }

    private void ToggleState()
    {
        if (ShowText != null)
        {
            bool nextState = !ShowText.activeSelf;
            ShowText.SetActive(nextState);

            if (objectRenderer != null)
            {
                objectRenderer.material.color = nextState ? targetColor : originalColor;
            }
        }
    }
}