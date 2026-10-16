using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit;



namespace ZzxWorkspace
{
[RequireComponent(typeof(XRBaseInteractable))]
public class ReturnToOriginalWhenNotGrabbed : MonoBehaviour

{

    private XRBaseInteractable interactable;

    private Rigidbody rb;



    private Vector3 initialPosition;

    private Quaternion initialRotation;



    private void Awake()

    {

        interactable = GetComponent<XRBaseInteractable>();

        rb = GetComponent<Rigidbody>();



        initialPosition = transform.position;

        initialRotation = transform.rotation;

    }



    private void OnEnable()

    {

        interactable.selectEntered.AddListener(OnGrabbed);

        interactable.selectExited.AddListener(OnNotGrabbed);

    }



    private void OnDisable()

    {

        interactable.selectEntered.RemoveListener(OnGrabbed);

        interactable.selectExited.RemoveListener(OnNotGrabbed);

    }



    private void OnGrabbed(SelectEnterEventArgs args)

    {

        // 被抓取时，如果之前设为了 kinematic，这里可以根据 XRI 的抓取需求还原（通常 XRI 抓取会自动处理，但保险起见可以不作处理，或者让物理恢复）

        if (rb != null)

        {

            // 如果你的抓取组件本身就需要 kinematic，可以保留；如果不需要，XRI 会接管

        }

    }



    private void OnNotGrabbed(SelectExitEventArgs args)

    {

        if (rb != null)

        {

            // 锁定物理，消除惯性

            if (!rb.isKinematic)
            {

#if UNITY_2023_3_OR_NEWER

            rb.linearVelocity = Vector3.zero;

#else

            rb.velocity = Vector3.zero;

#endif

            rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;

        }



        // 强制瞬移回初始位置

        transform.position = initialPosition;

        transform.rotation = initialRotation;

    }

}
}
