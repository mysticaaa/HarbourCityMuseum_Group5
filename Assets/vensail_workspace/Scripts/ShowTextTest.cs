using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class ShowObjectNameOnGrab : MonoBehaviour
{
    private XRBaseInteractable interactable;

    // 改名以避免与类名 GameObject 冲突，方便在 Inspector 里拖拽赋值
    [SerializeField] private GameObject targetObject;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        // 订阅抓取和松开事件
        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(NotGrabbed);
    }

    private void OnDisable()
    {
        // 取消订阅，防止报错
        interactable.selectEntered.RemoveListener(OnGrabbed);
        interactable.selectExited.RemoveListener(NotGrabbed);
    }

    // 抓取时触发
    private void OnGrabbed(SelectEnterEventArgs args)
    {


        if (targetObject != null)
        {
            targetObject.SetActive(true); // 抓取时激活目标物体
        }
    }

    // 松开时触发（注意：必须加上 SelectExitEventArgs 参数）
    private void NotGrabbed(SelectExitEventArgs args)
    {

        if (targetObject != null)
        {
            // 如果你想松开时让它隐藏，可以改为 false
            targetObject.SetActive(false);
        }
    }
}