using UnityEngine;
using System;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.CoffeeCraft;

/// <summary>
/// 界面切换管理器
/// 负责管理三个主要游戏界面的循环切换
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
        if (NewCoffeeCraftManager.Instance != null && NewCoffeeCraftManager.Instance.IsExtracting)
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
        if (NewCoffeeCraftManager.Instance != null && NewCoffeeCraftManager.Instance.IsExtracting)
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
