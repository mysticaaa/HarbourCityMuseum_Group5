namespace VideoPlaylist
{
    public enum PlaylistState
    {
        Idle,               // 还没开始
        ShowingFirstFrame,  // 显示第一帧，等待触发
        Playing,            // 正在播放
        Holding,            // 定格，准备切下一段
        Finished            // 全部播完
    }
}
