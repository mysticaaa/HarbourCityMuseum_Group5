using UnityEngine;
using VideoPlaylist;

public class TestPlayByKey : MonoBehaviour
{
    public PlaylistPlayer playlistPlayer;

    void Update()
    {
        // 按空格键触发播放
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (playlistPlayer != null)
            {
                Debug.Log("按了空格，触发播放");
                playlistPlayer.Play();
            }
            else
            {
                Debug.LogError("playlistPlayer 没绑定！");
            }
        }
    }
}
