using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Renderer))]
public class ChangeColorOnHoverAndTrigger : MonoBehaviour
{
    private XRSimpleInteractable interactable;
    private Renderer objectRenderer;

    public GameObject ShowText;
    [SerializeField] private Color targetColor = Color.blue; // 触发后的颜色

    private Color originalColor = Color.white; // 记录初始颜色

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
        // 核心：只要射线指着它，直接按下 Trigger 就会触发
        interactable.activated.AddListener(OnTriggerActivated);
    }

    private void OnDisable()
    {
        interactable.activated.RemoveListener(OnTriggerActivated);
    }

    private void OnTriggerActivated(ActivateEventArgs args)
    {
        if (ShowText != null)
        {
            bool nextState = !ShowText.activeSelf;
            ShowText.SetActive(nextState);

            if (nextState)
            {
                objectRenderer.material.color = targetColor;
            }
            else
            {
                objectRenderer.material.color = originalColor;
            }
        }
    }
}