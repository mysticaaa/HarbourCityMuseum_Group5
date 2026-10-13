using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace VideoPlaylist
{
    [RequireComponent(typeof(VideoPlayer))]
    public class PlaylistPlayer : MonoBehaviour
    {
        [Header("视频文件名（放在 StreamingAssets/Videos/ 下）")]
        public string[] videoFileNames = { "clip_01.mp4", "clip_02.mp4", "clip_03.mp4" };

        [Header("定格策略")]
        public HoldMode holdMode = HoldMode.EndFrame;

        [Header("当前状态（运行时看）")]
        public PlaylistState state = PlaylistState.Idle;

        private VideoPlayer _player;
        private int _currentIndex = -1;

        public System.Action<PlaylistState> OnStateChanged;

        void Awake()
        {
            _player = GetComponent<VideoPlayer>();
            _player.source = VideoSource.Url;
            _player.playOnAwake = false;
            _player.waitForFirstFrame = true;

            _player.loopPointReached += OnVideoEnded;
            _player.prepareCompleted += OnPrepareCompleted;
        }

        void Start()
        {
            // 初始显示第一段第一帧，不播放
            _currentIndex = 0;
            LoadClip(_currentIndex);
            ChangeState(PlaylistState.ShowingFirstFrame);
        }

        // 由触发物调用：开始/重播
        public void Play()
        {
            if (state == PlaylistState.Playing || state == PlaylistState.Holding)
            {
                // 播放中忽略
                return;
            }

            if (state == PlaylistState.Finished)
            {
                // 重播：从头开始
                _currentIndex = 0;
                LoadClip(_currentIndex);
                return;
            }

            // ShowingFirstFrame 或 Idle：直接播放当前已加载的第一帧
            _player.Play();
            ChangeState(PlaylistState.Playing);
        }

        // 后期动态绑定视频路径（StreamingAssets/Videos/xxx.mp4）
        public void SetPlaylistByPaths(string[] relativePaths)
        {
            videoFileNames = relativePaths;
            _currentIndex = 0;
            LoadClip(_currentIndex);
            ChangeState(PlaylistState.ShowingFirstFrame);
        }

        private void LoadClip(int index)
        {
            if (index < 0 || index >= videoFileNames.Length) return;

            string filePath = System.IO.Path.Combine(
                Application.streamingAssetsPath,
                "Videos",
                videoFileNames[index]);

            _player.url = filePath;
            _player.Prepare();
            ChangeState(PlaylistState.Holding);
        }

        private void OnPrepareCompleted(VideoPlayer source)
        {
            // 准备好后停在第一帧
            source.frame = 0;
            source.Pause();

            if (state == PlaylistState.Holding)
            {
                // 第一次加载完显示第一帧等待触发
                ChangeState(PlaylistState.ShowingFirstFrame);
            }
        }

        private void OnVideoEnded(VideoPlayer source)
        {
            // 当前段播完，定格
            HoldFramePolicy.Apply(source, holdMode);

            _currentIndex++;

            if (_currentIndex >= videoFileNames.Length)
            {
                ChangeState(PlaylistState.Finished);
            }
            else
            {
                // 自动切下一段
                ChangeState(PlaylistState.Holding);
                LoadClip(_currentIndex);
            }
        }

        private void ChangeState(PlaylistState newState)
        {
            state = newState;
            OnStateChanged?.Invoke(newState);
            Debug.Log($"[PlaylistPlayer] 状态切换: {newState}");
        }

        void OnDestroy()
        {
            if (_player != null)
            {
                _player.loopPointReached -= OnVideoEnded;
                _player.prepareCompleted -= OnPrepareCompleted;
            }
        }
    }
}
