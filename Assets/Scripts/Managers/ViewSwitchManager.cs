using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.UI;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 界面切换管理器
    /// 负责管理三个主要游戏界面的循环切换
    /// 支持手动切换和根据制作状态自动切换
    /// 支持水平滑动切换动画
    /// </summary>
    public class ViewSwitchManager : MonoBehaviour
    {
        private static ViewSwitchManager _instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static ViewSwitchManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ViewSwitchManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ViewSwitchManager");
                        _instance = go.AddComponent<ViewSwitchManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("界面面板")]
        [SerializeField] [Tooltip("场景容器（用于水平滑动）")]
        private RectTransform _scenePanelsContainer;

        [SerializeField] [Tooltip("吧台接单界面")]
        private CanvasGroup _barPanel;

        [SerializeField] [Tooltip("制作界面1：基础咖啡制作")]
        private CanvasGroup _craftBasePanel;

        [SerializeField] [Tooltip("制作界面2：调味与完成")]
        private CanvasGroup _craftMixPanel;

        [Header("杯子动画")]
        [SerializeField] [Tooltip("杯子动画管理器")]
        private CupAnimationManager _cupAnimationManager;

        [Header("动画配置")]
        [SerializeField] [Tooltip("场景切换动画时长")]
        private float _sceneSwitchDuration = 0.3f;

        [SerializeField] [Tooltip("是否启用水平滑动动画")]
        private bool _enableSlideAnimation = true;

        [Header("切换按钮")]
        [SerializeField] [Tooltip("左切换按钮")]
        private GameObject _leftSwitchButton;

        [SerializeField] [Tooltip("右切换按钮")]
        private GameObject _rightSwitchButton;

        [Header("自动切换")]
        [SerializeField] [Tooltip("是否启用根据制作状态自动切换")]
        private bool _enableAutoSwitch = true;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        /// <summary>
        /// 界面列表（按顺序）
        /// </summary>
        private List<GameViewType> _viewList;

        /// <summary>
        /// 当前界面索引
        /// </summary>
        private int _currentViewIndex;

        /// <summary>
        /// 当前界面类型
        /// </summary>
        private GameViewType _currentViewType;

        /// <summary>
        /// 是否允许切换
        /// </summary>
        private bool _canSwitch = true;

        /// <summary>
        /// 是否正在切换中
        /// </summary>
        private bool _isSwitching = false;

        /// <summary>
        /// 屏幕宽度（用于计算滑动距离）
        /// </summary>
        private float _screenWidth;

        /// <summary>
        /// 咖啡制作管理器引用
        /// </summary>
        private CoffeeCraftManager _craftManager;

        /// <summary>
        /// 获取当前界面类型
        /// </summary>
        public GameViewType CurrentViewType => _currentViewType;

        private void Awake()
        {
            // 单例检查
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeViewList();
            InitializeScreenWidth();
        }

        /// <summary>
        /// 初始化屏幕宽度
        /// </summary>
        private void InitializeScreenWidth()
        {
            // 获取Canvas的宽度作为单个屏幕宽度
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                _screenWidth = canvasRect.rect.width;
            }
            else
            {
                // 备用方案：使用屏幕宽度
                _screenWidth = Screen.width;
            }

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 屏幕宽度初始化: {_screenWidth}");
            }
        }

        private void Start()
        {
            // 初始化场景容器位置
            if (_enableSlideAnimation && _scenePanelsContainer != null)
            {
                // 设置初始位置为吧台场景（index=0）
                _scenePanelsContainer.anchoredPosition = new Vector2(0, _scenePanelsContainer.anchoredPosition.y);

                if (_showDebugLog)
                {
                    Debug.Log($"[ViewSwitchManager] 场景容器初始位置: {_scenePanelsContainer.anchoredPosition}");
                }
            }

            // 默认显示吧台接单界面
            ShowView(GameViewType.Bar);

            // 订阅咖啡制作状态变化事件
            if (_enableAutoSwitch)
            {
                _craftManager = CoffeeCraftManager.Instance;
                if (_craftManager != null)
                {
                    _craftManager.OnModuleStateChanged += OnModuleStateChanged;
                    if (_showDebugLog)
                    {
                        Debug.Log("[ViewSwitchManager] 已订阅制作状态变化事件");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // 取消订阅事件
            if (_craftManager != null)
            {
                _craftManager.OnModuleStateChanged -= OnModuleStateChanged;
            }
        }

        /// <summary>
        /// 初始化界面列表
        /// </summary>
        private void InitializeViewList()
        {
            _viewList = new List<GameViewType>
            {
                GameViewType.Bar,
                GameViewType.CraftBase,
                GameViewType.CraftMix
            };

            _currentViewIndex = 0;
            _currentViewType = GameViewType.Bar;

            if (_showDebugLog)
            {
                Debug.Log("[ViewSwitchManager] 界面列表初始化完成");
            }
        }

        /// <summary>
        /// 制作模块状态变化回调
        /// 根据状态自动切换界面
        /// </summary>
        private void OnModuleStateChanged(CraftModuleState state)
        {
            if (!_enableAutoSwitch)
            {
                return;
            }

            switch (state)
            {
                case CraftModuleState.BeanSelect:
                case CraftModuleState.GrindSelect:
                case CraftModuleState.Extract:
                    // 取豆、研磨、萃取阶段 → 显示制作界面1
                    ShowView(GameViewType.CraftBase);
                    break;

                case CraftModuleState.LiquidAdd:
                case CraftModuleState.ToppingAdd:
                    // 加液、加料阶段 → 显示制作界面2
                    ShowView(GameViewType.CraftMix);
                    break;
            }

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 根据制作状态自动切换: {state} → {_currentViewType}");
            }
        }

        /// <summary>
        /// 切换到下一个界面
        /// </summary>
        public void SwitchNextView()
        {
            if (!_canSwitch || _isSwitching)
            {
                if (_showDebugLog)
                {
                    Debug.LogWarning("[ViewSwitchManager] 当前不允许切换界面");
                }
                return;
            }

            int nextIndex = _currentViewIndex + 1;
            if (nextIndex >= _viewList.Count)
            {
                nextIndex = 0;
            }

            SwitchToView(_viewList[nextIndex], true);
        }

        /// <summary>
        /// 切换到上一个界面
        /// </summary>
        public void SwitchPreviousView()
        {
            if (!_canSwitch || _isSwitching)
            {
                if (_showDebugLog)
                {
                    Debug.LogWarning("[ViewSwitchManager] 当前不允许切换界面");
                }
                return;
            }

            int prevIndex = _currentViewIndex - 1;
            if (prevIndex < 0)
            {
                prevIndex = _viewList.Count - 1;
            }

            SwitchToView(_viewList[prevIndex], false);
        }

        /// <summary>
        /// 切换到指定界面
        /// </summary>
        /// <param name="targetView">目标界面</param>
        /// <param name="isNext">是否向下一个方向切换</param>
        private void SwitchToView(GameViewType targetView, bool isNext)
        {
            if (_isSwitching)
            {
                return;
            }

            GameViewType fromView = _currentViewType;
            _currentViewType = targetView;
            _currentViewIndex = _viewList.IndexOf(targetView);

            if (_enableSlideAnimation && _scenePanelsContainer != null)
            {
                // 使用水平滑动动画
                AnimateSceneSwitch(fromView, targetView, isNext);
            }
            else
            {
                // 使用CanvasGroup淡入淡出
                ShowView(targetView);
            }

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 切换界面: {fromView} → {targetView} (方向: {(isNext ? "下一个" : "上一个")})");
            }
        }

        /// <summary>
        /// 水平滑动场景切换动画
        /// </summary>
        private void AnimateSceneSwitch(GameViewType fromView, GameViewType toView, bool isNext)
        {
            _isSwitching = true;

            // 1. 先触发杯子退出动画
            if (_cupAnimationManager != null)
            {
                _cupAnimationManager.AnimateCupSceneTransition(fromView, toView, isNext);
            }

            // 2. 等待杯子退出后，开始场景切换
            DOVirtual.DelayedCall(_cupAnimationManager != null ? 0.3f : 0f, () =>
            {
                // 计算目标位置
                float targetX = -_currentViewIndex * _screenWidth;

                // 场景容器滑动
                _scenePanelsContainer.DOAnchorPosX(targetX, _sceneSwitchDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        _isSwitching = false;

                        if (_showDebugLog)
                        {
                            Debug.Log($"[ViewSwitchManager] 场景切换完成: {toView}");
                        }
                    });
            });
        }

        /// <summary>
        /// 显示指定界面
        /// </summary>
        /// <param name="viewType">界面类型</param>
        public void ShowView(GameViewType viewType)
        {
            // 隐藏所有界面
            HidePanel(_barPanel);
            HidePanel(_craftBasePanel);
            HidePanel(_craftMixPanel);

            // 显示指定界面
            switch (viewType)
            {
                case GameViewType.Bar:
                    ShowPanel(_barPanel);
                    break;

                case GameViewType.CraftBase:
                    ShowPanel(_craftBasePanel);
                    break;

                case GameViewType.CraftMix:
                    ShowPanel(_craftMixPanel);
                    break;
            }

            // 更新当前界面
            _currentViewType = viewType;
            _currentViewIndex = _viewList.IndexOf(viewType);

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 显示界面: {_currentViewType}");
            }
        }

        /// <summary>
        /// 显示面板（使用CanvasGroup）
        /// </summary>
        /// <param name="panel">面板的CanvasGroup</param>
        private void ShowPanel(CanvasGroup panel)
        {
            if (panel != null)
            {
                panel.alpha = 1f;
                panel.interactable = true;
                panel.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// 隐藏面板（使用CanvasGroup）
        /// </summary>
        /// <param name="panel">面板的CanvasGroup</param>
        private void HidePanel(CanvasGroup panel)
        {
            if (panel != null)
            {
                panel.alpha = 0f;
                panel.interactable = false;
                panel.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 设置是否允许切换
        /// </summary>
        /// <param name="canSwitch">是否允许切换</param>
        public void SetCanSwitch(bool canSwitch)
        {
            _canSwitch = canSwitch;

            // 更新按钮状态
            if (_leftSwitchButton != null)
            {
                _leftSwitchButton.SetActive(canSwitch);
            }

            if (_rightSwitchButton != null)
            {
                _rightSwitchButton.SetActive(canSwitch);
            }

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 设置切换状态: {canSwitch}");
            }
        }

        /// <summary>
        /// 设置是否启用自动切换
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetAutoSwitch(bool enable)
        {
            _enableAutoSwitch = enable;

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 设置自动切换: {enable}");
            }
        }

        /// <summary>
        /// 获取当前界面类型
        /// </summary>
        /// <returns>当前界面类型</returns>
        public GameViewType GetCurrentViewType()
        {
            return _currentViewType;
        }

        /// <summary>
        /// 设置界面面板引用（用于运行时动态设置）
        /// </summary>
        public void SetPanelReferences(CanvasGroup barPanel, CanvasGroup craftBasePanel, CanvasGroup craftMixPanel)
        {
            _barPanel = barPanel;
            _craftBasePanel = craftBasePanel;
            _craftMixPanel = craftMixPanel;

            if (_showDebugLog)
            {
                Debug.Log("[ViewSwitchManager] 界面面板引用已设置");
            }
        }

        /// <summary>
        /// 设置切换按钮引用（用于运行时动态设置）
        /// </summary>
        public void SetButtonReferences(GameObject leftButton, GameObject rightButton)
        {
            _leftSwitchButton = leftButton;
            _rightSwitchButton = rightButton;

            if (_showDebugLog)
            {
                Debug.Log("[ViewSwitchManager] 切换按钮引用已设置");
            }
        }
    }
}
