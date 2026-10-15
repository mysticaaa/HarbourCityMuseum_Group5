using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitButton : MonoBehaviour
{
    // 这个方法将会绑定到按钮的点击事件上
    public void QuitGame()
    {
        // 如果是在 Unity 编辑器中测试，停止播放模式
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 如果是打包后的实际运行程序（PC端或VR一体机），直接退出应用
        Application.Quit();
#endif
    }
}
