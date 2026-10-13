using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [Header("旋转速度（度/秒）")]
    public float rotateSpeed = 30f;

    [Header("旋转轴：0=X 1=Y 2=Z")]
    public int rotateAxis = 1;

    void Update()
    {
        Vector3 axis = Vector3.up;

        switch (rotateAxis)
        {
            case 0: axis = Vector3.right; break;
            case 1: axis = Vector3.up; break;
            case 2: axis = Vector3.forward; break;
        }

        transform.Rotate(axis, rotateSpeed * Time.deltaTime);
    }
}
