using UnityEngine;
using UnityEngine.Video;

namespace VideoPlaylist
{
    public enum HoldMode
    {
        EndFrame,     // 定格在当前视频最后一帧
        FirstFrame    // 切回第一帧
    }

    public static class HoldFramePolicy
    {
        public static void Apply(VideoPlayer player, HoldMode mode)
        {
            if (player == null) return;

            if (mode == HoldMode.FirstFrame)
            {
                player.frame = 0;
                player.Pause();
            }
            else
            {
                // EndFrame：播完自然停在最后一帧，什么都不用做
                player.Pause();
            }
        }
    }
}
