using UnityEngine;

public class DisappearOnTrigger : MonoBehaviour
{
    // 可以设置一个标签（Tag）来过滤是谁触发的，比如 "Player" 或 "Hand"
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // 检查碰撞进来的物体是否符合指定的标签
        if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
        {
            // 让当前物体失活（隐藏）
            gameObject.SetActive(false);

            // 或者彻底销毁：
            // Destroy(gameObject);
        }
    }
}