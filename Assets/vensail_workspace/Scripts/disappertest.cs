using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // 2.x 版本直接引用这个即可

[RequireComponent(typeof(XRGrabInteractable))]
public class DisappearOnGrab : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        // 订阅抓取事件（当物体被 Grab 时触发 OnGrabbed）
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // 让物体失活（隐藏）
        gameObject.SetActive(false);

        // 或者如果你想彻底从内存中销毁它，用下面这行：
        // Destroy(gameObject);
    }
}