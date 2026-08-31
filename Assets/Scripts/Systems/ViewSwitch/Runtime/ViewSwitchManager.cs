using UnityEngine;
using System;
using System.Collections.Generic;
using InnsmouthCafe.Data;

/// <summary>
/// 界面切换管理器
/// 负责管理三个主要游戏界面的循环切换
/// 支持手动切换和根据制作状态自动切换。
/// 负责维护视角状态并发出切换事件，实际相机动画由 DioramaCameraViewController 处理。
/// </summary>
public class ViewSwitchManager : Singleton<ViewSwitchManager>
{

    [Header("切换按钮")]
    [SerializeField]
    [Tooltip("左切换按钮")]
    private GameObject _leftSwitchButton;

    [SerializeField]
    [Tooltip("右切换按钮")]
    private GameObject _rightSwitchButton;

    [Header("自动切换")]
    [SerializeField]
    [Tooltip("是否启用根据制作状态自动切换")]
    private bool _enableAutoSwitch = true;

    [Header("调试")]
    [SerializeField]
    [Tooltip("是否显示调试日志")]
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
    /// <summary>
    /// 咖啡制作管理器引用
    /// </summary>
    private CoffeeCraftManager _craftManager;

    /// <summary>
    /// 已进入过的界面集合
    /// </summary>
    private readonly HashSet<GameViewType> _visitedViews = new HashSet<GameViewType>();

    /// <summary>
    /// 获取当前界面类型
    /// </summary>
    public GameViewType CurrentViewType => _currentViewType;

    /// <summary>
    /// 场景切换完成事件（参数：目标场景类型）
    /// </summary>
    public event Action<GameViewType> OnViewSwitched;

    /// <summary>
    /// 场景切换开始事件（参数：起始场景, 目标场景, 是否向右切换）
    /// </summary>
    public event Action<GameViewType, GameViewType, bool> OnViewSwitchStarted;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        InitializeViewList();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        // 默认显示吧台视角
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

    /// <summary>
    /// 处理键盘左右切换输入，保持与界面按钮使用同一套切换逻辑。
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwitchPreviousView();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwitchNextView();
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

    /// <summary>向教学事件总线发布视图切换事件</summary>
    private void PublishViewEvent(GameViewType viewType)
    {
        switch (viewType)
        {
            case GameViewType.Bar:
                TutorialEventBus.Publish(TutorialEvents.ViewSwitchBar);
                break;
            case GameViewType.CraftBase:
                TutorialEventBus.Publish(TutorialEvents.ViewSwitchCraftBase);
                break;
            case GameViewType.CraftMix:
                TutorialEventBus.Publish(TutorialEvents.ViewSwitchCraftMix);
                break;
        }
    }

    private void HandleViewEntered(GameViewType viewType)
    {
        bool isFirstEntry = _visitedViews.Add(viewType);
        if (!isFirstEntry)
        {
            return;
        }

        if (viewType == GameViewType.CraftBase)
        {
            TutorialEventBus.Publish(TutorialEvents.FirstBarToCraftBaseSwitchComplete);
        }
        else if (viewType == GameViewType.CraftMix)
        {
            TutorialEventBus.Publish(TutorialEvents.FirstCraftMixSwitchComplete);
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
        if (!_canSwitch)
        {
            if (_showDebugLog)
            {
                ActionLogBus.LogWarning("当前不允许切换界面");
            }
            return;
        }

        // 萃取中禁止切换
        if (CoffeeCraftManager.Instance != null && CoffeeCraftManager.Instance.IsExtracting)
        {
            ActionLogBus.Log("萃取中，无法切换界面", new Color(1f, 0.6f, 0f));
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
        if (!_canSwitch)
        {
            if (_showDebugLog)
            {
                ActionLogBus.LogWarning("当前不允许切换界面");
            }
            return;
        }

        // 萃取中禁止切换
        if (CoffeeCraftManager.Instance != null && CoffeeCraftManager.Instance.IsExtracting)
        {
            ActionLogBus.Log("萃取中，无法切换界面", new Color(1f, 0.6f, 0f));
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
        GameViewType fromView = _currentViewType;
        _currentViewType = targetView;
        _currentViewIndex = _viewList.IndexOf(targetView);

        OnViewSwitchStarted?.Invoke(fromView, targetView, isNext);
        OnViewSwitched?.Invoke(targetView);
        PublishViewEvent(targetView);
        HandleViewEntered(targetView);

        if (_showDebugLog)
        {
            Debug.Log($"[ViewSwitchManager] 切换界面: {fromView} → {targetView} (方向: {(isNext ? "下一个" : "上一个")})");
        }
    }

    /// <summary>
    /// 显示指定视角并通知监听者。
    /// </summary>
    /// <param name="viewType">界面类型</param>
    public void ShowView(GameViewType viewType)
    {
        _currentViewType = viewType;
        _currentViewIndex = _viewList.IndexOf(viewType);

        OnViewSwitched?.Invoke(viewType);
        PublishViewEvent(viewType);
        HandleViewEntered(viewType);

        if (_showDebugLog)
        {
            Debug.Log($"[ViewSwitchManager] 显示界面: {_currentViewType}");
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
    /// 兼容旧调用：当前视角切换不依赖 CanvasGroup 面板显隐。
    /// </summary>
    public void SetPanelReferences(CanvasGroup barPanel, CanvasGroup craftBasePanel, CanvasGroup craftMixPanel)
    {
        if (_showDebugLog)
        {
            Debug.Log("[ViewSwitchManager] 已忽略 SetPanelReferences：当前使用场景容器坐标切换");
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
