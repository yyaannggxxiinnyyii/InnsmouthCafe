#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using UnityEngine;

/// <summary>
/// 运行时流程测试面板，用于开发阶段快速跳转营业日、订单、结局与顾客队列。
/// </summary>
[DefaultExecutionOrder(10000)]
public class GameFlowDebugPanel : Singleton<GameFlowDebugPanel>
{
    private const KeyCode ToggleKey = KeyCode.F10;
    private const float WindowWidth = 380f;
    private const float WindowHeight = 560f;

    private readonly List<CustomerSO> _customers = new List<CustomerSO>();
    private Rect _windowRect = new Rect(16f, 80f, WindowWidth, WindowHeight);
    private Vector2 _customerScroll;
    private bool _isVisible = true;
    private bool _isCustomerDropdownOpen;
    private int _selectedCustomerIndex;
    private string _statusMessage = "F10 显示/隐藏测试面板";
    private GUIStyle _headerStyle;
    private GUIStyle _statusStyle;

    protected override void Awake()
    {
        base.Awake();

        ReloadCustomers();
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
        {
            _isVisible = !_isVisible;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!_isVisible)
        {
            DrawCollapsedButton();
            return;
        }

        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "流程测试工具");
    }

    /// <summary>
    /// 绘制折叠状态下的小按钮。
    /// </summary>
    private void DrawCollapsedButton()
    {
        if (GUI.Button(new Rect(16f, 80f, 96f, 32f), "Debug"))
        {
            _isVisible = true;
        }
    }

    /// <summary>
    /// 绘制主窗口内容。
    /// </summary>
    private void DrawWindow(int windowId)
    {
        GUILayout.BeginVertical();

        DrawRuntimeStatus();
        DrawOrderControls();
        DrawDayControls();
        DrawEndingControls();
        DrawCustomerControls();
        DrawUtilityControls();

        GUILayout.FlexibleSpace();
        GUILayout.Label(_statusMessage, _statusStyle);
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 24f));
    }

    /// <summary>
    /// 绘制当前流程状态。
    /// </summary>
    private void DrawRuntimeStatus()
    {
        GameFlowManager flow = GameFlowManager.Instance;
        CustomerManager customerManager = CustomerManager.Instance;
        CustomerSO currentCustomer = customerManager != null ? customerManager.CurrentCustomer : null;

        GUILayout.Label("当前状态", _headerStyle);
        GUILayout.Label($"游戏：{(flow != null && flow.IsGameRunning ? "运行中" : "未运行")}");
        GUILayout.Label($"天数：{(flow != null ? flow.CurrentDay.ToString() : "-")}");
        GUILayout.Label($"流程：{(flow != null ? flow.CurrentState.ToString() : "-")}");
        GUILayout.Label($"顾客：{GetCustomerDisplayName(currentCustomer)}");
        GUILayout.Label($"顾客状态：{(customerManager != null ? customerManager.CurrentState.ToString() : "-")}");
        GUILayout.Space(8f);
    }

    /// <summary>
    /// 绘制订单结算控制。
    /// </summary>
    private void DrawOrderControls()
    {
        GUILayout.Label("结束当前顾客订单", _headerStyle);

        GameFlowManager flow = GameFlowManager.Instance;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && flow != null && flow.CanDebugCompleteCurrentOrder();

        GUILayout.BeginHorizontal();
        DrawCompleteOrderButton("差评", 0);
        DrawCompleteOrderButton("中评", 1);
        DrawCompleteOrderButton("好评", 2);
        DrawCompleteOrderButton("Perfect", 3);
        GUILayout.EndHorizontal();

        GUI.enabled = previousEnabled;
        GUILayout.Space(8f);
    }

    /// <summary>
    /// 绘制单个订单结算按钮。
    /// </summary>
    /// <param name="label">按钮显示文本。</param>
    /// <param name="feedbackLevel">评价档位。</param>
    private void DrawCompleteOrderButton(string label, int feedbackLevel)
    {
        if (GUILayout.Button(label, GUILayout.Height(28f)))
        {
            GameFlowManager flow = GameFlowManager.Instance;
            bool success = flow != null && flow.DebugCompleteCurrentOrder(feedbackLevel);
            SetStatus(success, $"已按 {label} 结算当前订单", $"无法按 {label} 结算当前订单");
        }
    }

    /// <summary>
    /// 绘制营业日控制。
    /// </summary>
    private void DrawDayControls()
    {
        GUILayout.Label("营业日", _headerStyle);
        if (GUILayout.Button("结束当天营业", GUILayout.Height(30f)))
        {
            GameFlowManager flow = GameFlowManager.Instance;
            bool success = flow != null && flow.DebugEndCurrentDay();
            SetStatus(success, "已结束当天营业", "无法结束当天营业");
        }
        GUILayout.Space(8f);
    }

    /// <summary>
    /// 绘制结局触发控制。
    /// </summary>
    private void DrawEndingControls()
    {
        GUILayout.Label("触发指定结局", _headerStyle);
        GUILayout.BeginHorizontal();
        DrawEndingButton("Lost", GameEnding.Lost);
        DrawEndingButton("Return", GameEnding.Return);
        DrawEndingButton("Good", GameEnding.Good);
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
    }

    /// <summary>
    /// 绘制单个结局按钮。
    /// </summary>
    /// <param name="label">按钮显示文本。</param>
    /// <param name="ending">需要触发的结局。</param>
    private void DrawEndingButton(string label, GameEnding ending)
    {
        if (GUILayout.Button(label, GUILayout.Height(28f)))
        {
            GameFlowManager flow = GameFlowManager.Instance;
            bool success = flow != null && flow.DebugTriggerEnding(ending);
            SetStatus(success, $"已触发结局：{label}", $"无法触发结局：{label}");
        }
    }

    /// <summary>
    /// 绘制下一位顾客选择控制。
    /// </summary>
    private void DrawCustomerControls()
    {
        GUILayout.Label("指定下一位顾客", _headerStyle);

        CustomerSO selectedCustomer = GetSelectedCustomer();
        if (GUILayout.Button(GetCustomerDisplayName(selectedCustomer), GUILayout.Height(30f)))
        {
            _isCustomerDropdownOpen = !_isCustomerDropdownOpen;
        }

        if (_isCustomerDropdownOpen)
        {
            DrawCustomerDropdown();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新顾客列表", GUILayout.Height(28f)))
        {
            ReloadCustomers();
            SetStatus(true, $"已刷新顾客列表：{_customers.Count} 个", string.Empty);
        }

        if (GUILayout.Button("设为下一位", GUILayout.Height(28f)))
        {
            GameFlowManager flow = GameFlowManager.Instance;
            bool success = flow != null && flow.DebugSetNextCustomer(selectedCustomer);
            SetStatus(
                success,
                $"下一位顾客已指定为：{GetCustomerDisplayName(selectedCustomer)}",
                "无法指定下一位顾客");
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
    }

    /// <summary>
    /// 绘制顾客下拉列表。
    /// </summary>
    private void DrawCustomerDropdown()
    {
        const float dropdownHeight = 180f;

        _customerScroll = GUILayout.BeginScrollView(
            _customerScroll,
            GUI.skin.box,
            GUILayout.Height(dropdownHeight));

        if (_customers.Count == 0)
        {
            GUILayout.Label("未找到 CustomerSO");
        }

        for (int i = 0; i < _customers.Count; i++)
        {
            CustomerSO customer = _customers[i];
            if (GUILayout.Button(GetCustomerDisplayName(customer), GUILayout.Height(24f)))
            {
                _selectedCustomerIndex = i;
                _isCustomerDropdownOpen = false;
            }
        }

        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制通用工具按钮。
    /// </summary>
    private void DrawUtilityControls()
    {
        GUILayout.Label("面板", _headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("隐藏(F10)", GUILayout.Height(26f)))
        {
            _isVisible = false;
        }

        if (GUILayout.Button("清空状态", GUILayout.Height(26f)))
        {
            _statusMessage = string.Empty;
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 重新加载顾客配置列表。
    /// </summary>
    private void ReloadCustomers()
    {
        _customers.Clear();
        _customers.AddRange(Resources.LoadAll<CustomerSO>(string.Empty));
        _customers.Sort(CompareCustomers);
        _selectedCustomerIndex = Mathf.Clamp(_selectedCustomerIndex, 0, Mathf.Max(0, _customers.Count - 1));
    }

    /// <summary>
    /// 按顾客 ID 与名称排序。
    /// </summary>
    private int CompareCustomers(CustomerSO left, CustomerSO right)
    {
        string leftKey = BuildCustomerSortKey(left);
        string rightKey = BuildCustomerSortKey(right);
        return string.Compare(leftKey, rightKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// 构建顾客排序键。
    /// </summary>
    private string BuildCustomerSortKey(CustomerSO customer)
    {
        if (customer == null)
        {
            return string.Empty;
        }

        return $"{customer.customerId}_{customer.customerName}_{customer.name}";
    }

    /// <summary>
    /// 获取当前选中的顾客配置。
    /// </summary>
    private CustomerSO GetSelectedCustomer()
    {
        if (_customers.Count == 0)
        {
            return null;
        }

        _selectedCustomerIndex = Mathf.Clamp(_selectedCustomerIndex, 0, _customers.Count - 1);
        return _customers[_selectedCustomerIndex];
    }

    /// <summary>
    /// 获取顾客显示名。
    /// </summary>
    /// <param name="customer">顾客配置。</param>
    private string GetCustomerDisplayName(CustomerSO customer)
    {
        if (customer == null)
        {
            return "无";
        }

        string displayName = string.IsNullOrEmpty(customer.customerName)
            ? customer.name
            : customer.customerName;

        return string.IsNullOrEmpty(customer.customerId)
            ? displayName
            : $"{customer.customerId} - {displayName}";
    }

    /// <summary>
    /// 设置操作反馈文本。
    /// </summary>
    private void SetStatus(bool success, string successMessage, string failureMessage)
    {
        _statusMessage = success ? successMessage : failureMessage;
    }

    /// <summary>
    /// 确保 IMGUI 样式已初始化。
    /// </summary>
    private void EnsureStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };
        }

        if (_statusStyle == null)
        {
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 1f, 0.8f) }
            };
        }
    }
}
#endif
