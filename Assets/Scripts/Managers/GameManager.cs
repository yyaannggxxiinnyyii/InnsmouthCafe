using UnityEngine;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 游戏全局管理器
    /// 负责分辨率/窗口模式设置等全局初始化
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("分辨率设置")]
        [SerializeField] [Tooltip("目标宽度")]
        private int _targetWidth = 1920;

        [SerializeField] [Tooltip("目标高度")]
        private int _targetHeight = 1080;

        [SerializeField] [Tooltip("窗口模式")]
        private FullScreenMode _fullScreenMode = FullScreenMode.Windowed;

        protected override void Awake()
        {
            base.Awake();
            Screen.SetResolution(_targetWidth, _targetHeight, _fullScreenMode);
        }
    }
}
