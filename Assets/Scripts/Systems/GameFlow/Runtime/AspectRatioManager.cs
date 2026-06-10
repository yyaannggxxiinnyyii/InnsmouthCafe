using UnityEngine;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 宽高比管理器
    /// 强制保持目标宽高比（默认16:9），多余部分显示黑边
    /// 挂在任意场景常驻对象上，或放入 GameManager 所在对象
    /// </summary>
    public class AspectRatioManager : Singleton<AspectRatioManager>
    {
        [Header("目标宽高比")]
        [SerializeField] [Tooltip("目标宽度比（默认16）")]
        private float _targetWidth = 16f;

        [SerializeField] [Tooltip("目标高度比（默认9）")]
        private float _targetHeight = 9f;

        [Header("黑边颜色")]
        [SerializeField] [Tooltip("黑边背景颜色（默认纯黑）")]
        private Color _letterboxColor = Color.black;

        private float _targetAspect;
        private Camera _mainCamera;

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            _targetAspect = _targetWidth / _targetHeight;
        }

        private void Start()
        {
            ApplyAspectRatio();
        }

        private void OnEnable()
        {
            // 分辨率变化时重新计算（如切换全屏/窗口）
#if UNITY_2022_2_OR_NEWER
            // Unity 2022.2+ 有 Screen.resolutionChanged 事件，旧版本用 Update 轮询
#endif
        }

        /// <summary>
        /// 应用宽高比，设置 Camera viewport rect 实现黑边
        /// </summary>
        public void ApplyAspectRatio()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[AspectRatio] 找不到 Main Camera");
                return;
            }

            float screenAspect = (float)Screen.width / Screen.height;

            if (Mathf.Approximately(screenAspect, _targetAspect))
            {
                // 比例完全一致，全屏显示
                _mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            if (screenAspect > _targetAspect)
            {
                // 屏幕比目标更宽（如 21:9）→ 左右加黑边（Pillarbox）
                float normalizedWidth = _targetAspect / screenAspect;
                float xOffset = (1f - normalizedWidth) / 2f;
                _mainCamera.rect = new Rect(xOffset, 0f, normalizedWidth, 1f);
            }
            else
            {
                // 屏幕比目标更高（如 16:10）→ 上下加黑边（Letterbox）
                float normalizedHeight = screenAspect / _targetAspect;
                float yOffset = (1f - normalizedHeight) / 2f;
                _mainCamera.rect = new Rect(0f, yOffset, 1f, normalizedHeight);
            }

            Debug.Log($"[AspectRatio] 屏幕 {Screen.width}x{Screen.height} ({screenAspect:F3})，" +
                      $"目标 {_targetAspect:F3}，Camera rect: {_mainCamera.rect}");
        }

        private void Update()
        {
            // 每帧检测分辨率变化（切换全屏/窗口时响应）
            // 性能开销极低，仅做浮点比较
            if (_mainCamera == null) return;

            float screenAspect = (float)Screen.width / Screen.height;
            float currentAspect = _mainCamera.rect.width * screenAspect;

            // 用误差范围判断是否需要重新计算
            if (!Mathf.Approximately(currentAspect, _targetAspect))
            {
                ApplyAspectRatio();
            }
        }
    }
}
