using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class IfGrab : MonoBehaviour
{
    private XRBaseInteractable interactable;
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
    }

    // 松开时触发（注意：必须加上 SelectExitEventArgs 参数）
    private void NotGrabbed(SelectExitEventArgs args)
    {
    }
}