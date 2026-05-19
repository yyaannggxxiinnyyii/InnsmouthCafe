using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 研磨机UI组件
    /// 模拟压力表盘式研磨机，通过指针旋转显示研磨度
    /// </summary>
    public class GrinderUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("指针Transform")]
        private RectTransform _needle;

        [SerializeField] [Tooltip("研磨按钮")]
        private Button _grindButton;

        [Header("角度配置")]
        [SerializeField] [Tooltip("初始角度（6点钟方向，指针图标默认朝下）")]
        private float _initialAngle = 0f;

        [SerializeField] [Tooltip("粗磨角度（10点钟方向）")]
        private float _coarseAngle = -120f;

        [SerializeField] [Tooltip("细磨角度（12点钟方向）")]
        private float _fineAngle = 180f;

        [SerializeField] [Tooltip("精磨角度（2点钟方向）")]
        private float _extraFineAngle = 120f;

        [Header("动画配置")]
        [SerializeField] [Tooltip("指针旋转动画时长（秒）")]
        private float _rotationDuration = 0.3f;

        [SerializeField] [Tooltip("旋转动画曲线")]
        private Ease _rotationEase = Ease.OutQuad;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        private CoffeeCraftManager _manager;
        private int _grindCount = 0; // 当前研磨次数（0-3）
        private Tween _currentTween;
        private bool _isAnimating = false; // 是否正在播放动画

        private void Awake()
        {
            // 初始化DOTween（确保第一次动画能正常播放）
            DOTween.Init();

            _manager = CoffeeCraftManager.Instance;

            // 初始化指针角度
            if (_needle != null)
            {
                _needle.localRotation = Quaternion.Euler(0f, 0f, _initialAngle);
            }

            // 绑定按钮事件
            if (_grindButton != null)
            {
                _grindButton.onClick.AddListener(OnGrindButtonClick);
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged += OnBatchDataChanged;
            }

            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged -= OnBatchDataChanged;
            }

            // 清理动画
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
            }
        }

        /// <summary>
        /// 批次数据变化回调
        /// </summary>
        private void OnBatchDataChanged(CurrentBeanBatchData batch)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void RefreshDisplay()
        {
            if (_manager == null || _manager.CurrentBatch == null)
            {
                return;
            }

            var batch = _manager.CurrentBatch;

            // 更新研磨次数
            if (batch.grindType.HasValue)
            {
                // 已经研磨过，根据研磨度确定次数
                _grindCount = GetGrindCountFromType(batch.grindType.Value);
            }
            else
            {
                // 未研磨
                _grindCount = 0;
            }

            // 更新按钮状态
            UpdateButtonState();

            // 如果正在播放动画，不要覆盖指针位置
            if (!_isAnimating)
            {
                // 更新指针位置（不播放动画）
                UpdateNeedlePosition(false);
            }
        }

        /// <summary>
        /// 研磨按钮点击
        /// </summary>
        private void OnGrindButtonClick()
        {
            if (_manager == null)
            {
                return;
            }

            // 检查是否有豆子
            if (_manager.CurrentBatch.beanGram <= 0f)
            {
                Debug.LogWarning("[GrinderUI] 没有豆子，无法研磨");
                return;
            }

            // 检查是否已经研磨3次
            if (_grindCount >= 3)
            {
                Debug.LogWarning("[GrinderUI] 已达到最大研磨次数");
                return;
            }

            // 增加研磨次数
            _grindCount++;

            // 获取对应的研磨度
            GrindType grindType = GetGrindTypeFromCount(_grindCount);

            // 先标记正在动画，防止管理器回调覆盖指针位置
            _isAnimating = true;

            // 先调用管理器执行研磨（会触发OnBatchDataChanged）
            _manager.SelectGrind(grindType);

            // 然后播放指针旋转动画（覆盖RefreshDisplay的直接设置）
            UpdateNeedlePosition(true);

            if (_showDebugLog)
            {
                Debug.Log($"[GrinderUI] 研磨次数: {_grindCount}, 研磨度: {grindType}");
            }
        }

        /// <summary>
        /// 更新指针位置
        /// </summary>
        /// <param name="playAnimation">是否播放动画</param>
        private void UpdateNeedlePosition(bool playAnimation)
        {
            if (_needle == null)
            {
                return;
            }

            float targetAngle = GetAngleFromCount(_grindCount);

            // 清理之前的动画
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
            }

            if (playAnimation)
            {
                // 设置动画标志
                _isAnimating = true;

                // 播放旋转动画（使用本地旋转，避免与 RectTransform 的 localRotation 冲突）
                _currentTween = _needle.DOLocalRotate(new Vector3(0f, 0f, targetAngle), _rotationDuration)
                    .SetEase(_rotationEase)
                    .OnComplete(() =>
                    {
                        // 动画完成后清除标志和引用
                        _isAnimating = false;
                        _currentTween = null;

                        if (_showDebugLog)
                        {
                            Debug.Log($"[GrinderUI] 指针旋转动画完成，目标角度: {targetAngle}");
                        }
                    });

                if (_showDebugLog)
                {
                    Debug.Log($"[GrinderUI] 开始播放指针旋转动画，目标角度: {targetAngle}");
                }
            }
            else
            {
                // 直接设置角度
                _isAnimating = false;
                _needle.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            }
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonState()
        {
            if (_grindButton == null)
            {
                return;
            }

            // 检查是否有豆子
            bool hasBeans = _manager.CurrentBatch.beanGram > 0f;

            // 检查是否已经研磨3次
            bool reachedMaxGrind = _grindCount >= 3;

            // 按钮可用条件：有豆子 且 未达到最大研磨次数
            _grindButton.interactable = hasBeans && !reachedMaxGrind;
        }

        /// <summary>
        /// 根据研磨次数获取角度
        /// </summary>
        private float GetAngleFromCount(int count)
        {
            switch (count)
            {
                case 0:
                    return _initialAngle;
                case 1:
                    return _coarseAngle;
                case 2:
                    return _fineAngle;
                case 3:
                    return _extraFineAngle;
                default:
                    return _initialAngle;
            }
        }

        /// <summary>
        /// 根据研磨次数获取研磨度
        /// </summary>
        private GrindType GetGrindTypeFromCount(int count)
        {
            switch (count)
            {
                case 1:
                    return GrindType.Coarse;
                case 2:
                    return GrindType.Fine;
                case 3:
                    return GrindType.ExtraFine;
                default:
                    return GrindType.Coarse;
            }
        }

        /// <summary>
        /// 根据研磨度获取研磨次数
        /// </summary>
        private int GetGrindCountFromType(GrindType grindType)
        {
            switch (grindType)
            {
                case GrindType.Coarse:
                    return 1;
                case GrindType.Fine:
                    return 2;
                case GrindType.ExtraFine:
                    return 3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 手动刷新（用于测试）
        /// </summary>
        public void ManualRefresh()
        {
            RefreshDisplay();
        }
    }
}
