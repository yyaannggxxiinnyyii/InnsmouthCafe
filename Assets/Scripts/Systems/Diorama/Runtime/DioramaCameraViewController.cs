using System;
using DG.Tweening;
using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Diorama
{
    /// <summary>
    /// 根据经营界面类型控制箱庭场景相机在各个视角锚点之间平滑过渡。
    /// </summary>
    public class DioramaCameraViewController : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField]
        [Tooltip("用于显示箱庭场景的相机；留空时自动查找 Main Camera")]
        private Camera _targetCamera;

        [SerializeField]
        [Tooltip("箱庭视差控制器；过场期间会暂时停用视差")]
        private DioramaParallaxController _parallaxController;

        [Header("视角锚点")]
        [SerializeField]
        [Tooltip("前台接待视角锚点")]
        private Transform _barViewAnchor;

        [SerializeField]
        [Tooltip("初加工视角锚点")]
        private Transform _craftBaseViewAnchor;

        [SerializeField]
        [Tooltip("后加工视角锚点")]
        private Transform _craftMixViewAnchor;

        [Header("过场")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("相机在两个经营视角之间移动的时间，单位为秒")]
        private float _transitionDuration = 0.45f;

        [SerializeField]
        [Tooltip("相机过场使用的缓动方式")]
        private Ease _transitionEase = Ease.InOutSine;

        private Tween _cameraTween;
        private ViewSwitchManager _viewSwitchManager;
        private GameViewType _targetViewType;
        private bool _isTransitioning;

        /// <summary>
        /// 获取相机当前是否正在进行视角过场。
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// 相机视角过场开始事件，参数为目标经营界面。
        /// </summary>
        public event Action<GameViewType> OnViewTransitionStarted;

        /// <summary>
        /// 相机视角过场完成事件，参数为已到达的经营界面。
        /// </summary>
        public event Action<GameViewType> OnViewTransitionCompleted;

        private void Start()
        {
            ResolveReferences();

            _viewSwitchManager = ViewSwitchManager.Instance;
            if (_viewSwitchManager == null)
            {
                Debug.LogWarning("[DioramaCamera] 未找到 ViewSwitchManager，无法响应经营界面切换", this);
                return;
            }

            _viewSwitchManager.OnViewSwitchStarted += HandleViewSwitchStarted;
            _viewSwitchManager.OnViewSwitched += HandleViewSwitched;
            SnapToView(_viewSwitchManager.CurrentViewType);
        }

        private void OnDestroy()
        {
            if (_viewSwitchManager != null)
            {
                _viewSwitchManager.OnViewSwitchStarted -= HandleViewSwitchStarted;
                _viewSwitchManager.OnViewSwitched -= HandleViewSwitched;
            }

            _cameraTween?.Kill();
        }

        /// <summary>
        /// 缓存运行时依赖，并在 Inspector 留空时查找主相机和视差控制器。
        /// </summary>
        private void ResolveReferences()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_parallaxController == null)
            {
                _parallaxController = GetComponent<DioramaParallaxController>();
            }
        }

        /// <summary>
        /// 响应界面切换开始事件，启动相机到目标锚点的平滑过场。
        /// </summary>
        private void HandleViewSwitchStarted(
            GameViewType fromView,
            GameViewType toView,
            bool isNext)
        {
            MoveToView(toView);
        }

        /// <summary>
        /// 响应直接显示界面事件，处理未经过动画事件的强制视角切换。
        /// </summary>
        private void HandleViewSwitched(GameViewType viewType)
        {
            if (_targetViewType == viewType)
            {
                return;
            }

            MoveToView(viewType);
        }

        /// <summary>
        /// 将相机立即放置到指定经营视角，用于场景初始显示。
        /// </summary>
        private void SnapToView(GameViewType viewType)
        {
            Transform anchor = GetViewAnchor(viewType);
            if (!IsValidAnchor(anchor))
            {
                return;
            }

            _cameraTween?.Kill();
            _targetViewType = viewType;
            _isTransitioning = false;
            _targetCamera.transform.SetPositionAndRotation(
                anchor.position,
                anchor.rotation);
            _parallaxController?.SetParallaxEnabled(true);
        }

        /// <summary>
        /// 平滑移动相机到指定经营视角，并在完成后恢复鼠标视差。
        /// </summary>
        private void MoveToView(GameViewType viewType)
        {
            Transform anchor = GetViewAnchor(viewType);
            if (!IsValidAnchor(anchor))
            {
                Debug.LogWarning($"[DioramaCamera] 未配置 {viewType} 的视角锚点", this);
                return;
            }

            _cameraTween?.Kill();
            _targetViewType = viewType;
            _isTransitioning = true;
            _parallaxController?.SetParallaxEnabled(false);
            OnViewTransitionStarted?.Invoke(viewType);

            if (_transitionDuration <= 0f)
            {
                CompleteCameraTransition(anchor);
                return;
            }

            _cameraTween = DOTween.Sequence()
                .Join(_targetCamera.transform.DOMove(anchor.position, _transitionDuration))
                .Join(_targetCamera.transform.DORotateQuaternion(anchor.rotation, _transitionDuration))
                .SetEase(_transitionEase)
                .SetUpdate(true)
                .OnComplete(() => CompleteCameraTransition(anchor));
        }

        /// <summary>
        /// 完成相机过场，校准锚点变换并重新启用视差。
        /// </summary>
        private void CompleteCameraTransition(Transform anchor)
        {
            _targetCamera.transform.SetPositionAndRotation(
                anchor.position,
                anchor.rotation);
            _isTransitioning = false;
            _cameraTween = null;
            _parallaxController?.SetParallaxEnabled(true);
            OnViewTransitionCompleted?.Invoke(_targetViewType);
        }

        /// <summary>
        /// 获取经营界面对应的相机锚点。
        /// </summary>
        private Transform GetViewAnchor(GameViewType viewType)
        {
            switch (viewType)
            {
                case GameViewType.Bar:
                    return _barViewAnchor;
                case GameViewType.CraftBase:
                    return _craftBaseViewAnchor;
                case GameViewType.CraftMix:
                    return _craftMixViewAnchor;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 检查相机和锚点是否已配置完成。
        /// </summary>
        private bool IsValidAnchor(Transform anchor)
        {
            return _targetCamera != null && anchor != null;
        }
    }
}
