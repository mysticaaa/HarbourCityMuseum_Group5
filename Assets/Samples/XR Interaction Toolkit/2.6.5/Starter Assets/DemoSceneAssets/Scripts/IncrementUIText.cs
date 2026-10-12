using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))] // 支持 GrabInteractable 或 SimpleInteractable
public class ShowObjectNameOnGrab : MonoBehaviour
{
    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        // 订阅抓取/选中事件
        interactable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // 方法 1：在 Unity 的 Console 控制台打印出当前物体的名字
        Debug.Log("当前抓取的物体是: " + gameObject.name, gameObject);

        // 方法 2：如果你想在 VR 头显里或者 UI 文本（TextMeshPro）上显示，可以在这里赋值
        // myUiText.text = "目标物体: " + gameObject.name;
    }
}