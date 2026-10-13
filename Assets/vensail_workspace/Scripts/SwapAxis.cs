using UnityEngine;

public class SwapXAndZAxis : MonoBehaviour
{
    private void Start()
    {
        // 获取当前的本地旋转
        Quaternion currentRotation = transform.localRotation;

        // 通过应用一个旋转来对调 X 轴和 Z 轴（相当于在本地坐标系下顺时针/逆时针旋转 90 度并镜像）
        // 这里的 Quaternion.Euler(0, 90, 0) 可以将原本的 X 轴方向转到 Z 轴上
        transform.localRotation = currentRotation * Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(0f, 0f, 180f);
    }
}