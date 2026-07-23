using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.UI;

/// <summary>
/// 游戏流程管理器
/// 负责驱动游戏内完整流程：开始营业 → 顾客循环 → 日结算 → 下一天 → 游戏结束
/// 通过状态机串联各个独立系统
/// </summary>
public class GameFlowManager : Singleton<GameFlowManager>
{
    [Header("游戏模式配置")]
    [SerializeField]
    [Tooltip("当前游戏模式配置（测试用，正式版由外部传入）")]
    private GameModeConfigSO _gameModeConfig;

    [Header("顾客动画")]
    [SerializeField]
    [Tooltip("顾客立绘动画组件（用于同步进场/退场动画与流程推进）")]
    private CustomerDisplayUI _customerDisplayUI;

    [Header("订单小票")]
    [SerializeField]
    [Tooltip("订单小票控制器（提交订单和重置时使用）")]
    private OrderTicketController _orderTicketController;

    [Header("收集物提示")]
    [SerializeField]
    [Tooltip("收集物获得提示UI")]
    private CollectibleNotifyUI _collectibleNotifyUI;

    [Header("材料解锁提示")]
    [SerializeField]
    [Tooltip("材料解锁提示UI")]
    private IngredientUnlockNotifyUI _ingredientUnlockNotifyUI;

    [Header("小章鱼NPC")]
    [SerializeField]
    [Tooltip("小章鱼NPC控制器（每天开门前播放开场对话）")]
    private OctopusNPCController _octopusNPC;

    [Header("调试")]
    [SerializeField]
    [Tooltip("是否显示调试日志")]
    private bool _showDebugLog = true;

    [Header("结局阈值")]
    [SerializeField]
    [Tooltip("好结局最低理智值（>= 此值为好结局）")]
    private float _goodEndingThreshold = 90f;

    [SerializeField]
    [Tooltip("回归结局最低理智值（>= 此值为回归结局，< 好结局阈值）")]
    private float _returnEndingThreshold = 60f;

    /// <summary>
    /// 当前游戏流程状态
    /// </summary>
    private GameFlowState _currentState = GameFlowState.None;

    /// <summary>
    /// 当前天数（从1开始）
    /// </summary>
    private int _currentDay = 0;

    /// <summary>
    /// 当天已服务的顾客数
    /// </summary>
    private int _todayCustomersServed = 0;

    /// <summary>
    /// 当天好评数
    /// </summary>
    private int _todaySatisfiedCount = 0;

    /// <summary>
    /// 当天一般数
    /// </summary>
    private int _todayNeutralCount = 0;

    /// <summary>
    /// 当天差评数
    /// </summary>
    private int _todayDissatisfiedCount = 0;

    /// <summary>
    /// 当天开始时的理智值
    /// </summary>
    private float _todaySanityStart = 0f;

    /// <summary>
    /// 当前顾客的订单SO（用于评分和对话）
    /// </summary>
    private OrderSO _currentOrderSO;

    /// <summary>
    /// 当前顾客的订单需求数据（用于制作系统）
    /// </summary>
    private OrderRequirementData _currentOrderData;

    /// <summary>
    /// 当前顾客的评分数据（用于收集物判定）
    /// </summary>
    private CoffeeScoringData _currentScoringData;

    /// <summary>
    /// 当前顾客组订单会话；普通顾客也会以单个订单槽的形式进入该会话。
    /// </summary>
    private CustomerOrderSessionData _currentOrderSession = new CustomerOrderSessionData();

    /// <summary>
    /// 当天剩余顾客数
    /// </summary>
    private int _remainingCustomers = 0;

    /// <summary>
    /// 当天开始时新解锁的辅助液，等待小章鱼播报后展示获得面板。
    /// </summary>
    private readonly List<LiquidSO> _pendingDayUnlockedLiquids = new List<LiquidSO>();

    /// <summary>
    /// 当天开始时新解锁的小料，等待小章鱼播报后展示获得面板。
    /// </summary>
    private readonly List<ToppingSO> _pendingDayUnlockedToppings = new List<ToppingSO>();

    /// <summary>
    /// 游戏是否正在运行
    /// </summary>
    private bool _isGameRunning = false;

    /// <summary>
    /// 当前游戏流程状态（只读）
    /// </summary>
    public GameFlowState CurrentState => _currentState;

    /// <summary>
    /// 当前天数（只读）
    /// </summary>
    public int CurrentDay => _currentDay;

    /// <summary>
    /// 游戏是否正在运行（只读）
    /// </summary>
    public bool IsGameRunning => _isGameRunning;

    /// <summary>
    /// 游戏流程状态变化事件
    /// </summary>
    public event Action<GameFlowState> OnGameFlowStateChanged;

    /// <summary>
    /// 游戏开始事件
    /// </summary>
    public event Action OnGameStart;

    /// <summary>
    /// 游戏结束事件，参数为最终天数和结局类型
    /// </summary>
    public event Action<int> OnGameEnd;

    /// <summary>
    /// 结局确定事件，参数为结局类型
    /// </summary>
    public event Action<GameEnding> OnGameEnding;

    /// <summary>
    /// 新一天开始事件
    /// </summary>
    public event Action<int> OnDayStart;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        // 订阅系统事件
        SubscribeEvents();
        CacheReferences();

        // GameManager.SelectedModeConfig 是玩家主动选择的，优先级最高
        if (GameManager.Instance != null && GameManager.Instance.SelectedModeConfig != null)
            _gameModeConfig = GameManager.Instance.SelectedModeConfig;

        // 如果有配置，延迟自动开始
        if (_gameModeConfig != null)
        {
            float delay = _gameModeConfig.startDelay;
            StartCoroutine(DelayedStartGame(delay));
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    /// <summary>
    /// 外部调用：启动游戏
    /// </summary>
    /// <param name="config">游戏模式配置</param>
    public void StartGame(GameModeConfigSO config)
    {
        if (_isGameRunning)
        {
            Debug.LogWarning("[GameFlow] 游戏已在运行中");
            return;
        }

        if (config == null)
        {
            Debug.LogError("[GameFlow] GameModeConfigSO 为空，无法启动游戏");
            return;
        }

        _gameModeConfig = config;

        // 检查关键管理器是否就绪
        if (!CheckRequiredManagers()) return;

        _isGameRunning = true;
        _currentDay = 0;
        ClearCurrentOrderSession();

        // 重置理智值
        SanityManager.Instance.ResetSanity();

        // 清空结算历史
        DaySettlementManager.Instance.ClearHistory();

        // 重置特殊客人出现记录（新局开始，所有特殊客人重新可用）
        CustomerManager.Instance.ResetSeenSpecials();

        OnGameStart?.Invoke();

        // 启动非主轨道BGM（环境音等）
        AudioManager.Instance?.StartSecondaryBgms();

        if (_showDebugLog)
        {
            Debug.Log($"[GameFlow] 游戏开始，模式: {config.gameMode}，总天数: {config.totalDays}");
        }

        // 开始第一天
        StartNewDay();
    }

    /// <summary>
    /// 延迟启动游戏（测试用）
    /// </summary>
    private IEnumerator DelayedStartGame(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartGame(_gameModeConfig);
    }

    /// <summary>
    /// 订阅各系统事件
    /// </summary>
    private void SubscribeEvents()
    {
        // 顾客系统事件
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnCustomerSpawned += OnCustomerSpawned;
            CustomerManager.Instance.OnCustomerLeft += OnCustomerLeft;
        }

        // 理智值归零事件
        if (SanityManager.Instance != null)
        {
            SanityManager.Instance.OnSanityDepleted += OnSanityDepleted;
        }

        // 日结算下一天事件
        if (DaySettlementManager.Instance != null)
        {
            DaySettlementManager.Instance.OnNextDayStart += OnNextDayFromSettlement;
        }

        // 顾客动画事件（有组件才订阅）
        if (_customerDisplayUI != null)
        {
            _customerDisplayUI.OnEnterAnimationComplete += OnCustomerEnterAnimationComplete;
            _customerDisplayUI.OnExitAnimationComplete += OnCustomerExitAnimationComplete;
        }
    }

    /// <summary>
    /// 缓存场景引用，避免依赖 Inspector 逐个手动拖拽
    /// </summary>
    private void CacheReferences()
    {
        if (_orderTicketController == null)
        {
            _orderTicketController = FindObjectOfType<OrderTicketController>();
        }

        if (_ingredientUnlockNotifyUI == null)
        {
            _ingredientUnlockNotifyUI = FindObjectOfType<IngredientUnlockNotifyUI>();
        }
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.OnCustomerSpawned -= OnCustomerSpawned;
            CustomerManager.Instance.OnCustomerLeft -= OnCustomerLeft;
        }

        if (SanityManager.Instance != null)
        {
            SanityManager.Instance.OnSanityDepleted -= OnSanityDepleted;
        }

        if (DaySettlementManager.Instance != null)
        {
            DaySettlementManager.Instance.OnNextDayStart -= OnNextDayFromSettlement;
        }

        if (_customerDisplayUI != null)
        {
            _customerDisplayUI.OnEnterAnimationComplete -= OnCustomerEnterAnimationComplete;
            _customerDisplayUI.OnExitAnimationComplete -= OnCustomerExitAnimationComplete;
        }
    }

    /// <summary>
    /// 切换游戏流程状态
    /// </summary>
    private void SetState(GameFlowState newState)
    {
        if (_currentState == newState) return;

        GameFlowState oldState = _currentState;
        _currentState = newState;

        OnGameFlowStateChanged?.Invoke(newState);

        if (_showDebugLog)
        {
            Debug.Log($"[GameFlow] 状态切换: {oldState} → {newState}");
        }
    }

    /// <summary>
    /// 检查所有必需的管理器是否存在
    /// </summary>
    /// <returns>所有管理器就绪返回true，否则返回false并输出错误日志</returns>
    private bool CheckRequiredManagers()
    {
        bool allReady = true;

        if (CustomerManager.Instance == null)
        {
            Debug.LogError("[GameFlow] CustomerManager 未找到，请确保场景中存在 CustomerManager");
            allReady = false;
        }

        if (SanityManager.Instance == null)
        {
            Debug.LogError("[GameFlow] SanityManager 未找到，请确保场景中存在 SanityManager");
            allReady = false;
        }

        if (OrderManager.Instance == null)
        {
            Debug.LogError("[GameFlow] OrderManager 未找到，请确保场景中存在 OrderManager");
            allReady = false;
        }

        if (CoffeeCraftManager.Instance == null)
        {
            Debug.LogError("[GameFlow] CoffeeCraftManager 未找到，请确保场景中存在 CoffeeCraftManager");
            allReady = false;
        }

        if (ScoringManager.Instance == null)
        {
            Debug.LogError("[GameFlow] ScoringManager 未找到，请确保场景中存在 ScoringManager");
            allReady = false;
        }

        if (ViewSwitchManager.Instance == null)
        {
            Debug.LogError("[GameFlow] ViewSwitchManager 未找到，请确保场景中存在 ViewSwitchManager");
            allReady = false;
        }

        if (DialogueUIManager.Instance == null)
        {
            Debug.LogError("[GameFlow] DialogueUIManager 未找到，请确保场景中存在 DialogueUIManager");
            allReady = false;
        }

        if (DaySettlementManager.Instance == null)
        {
            Debug.LogError("[GameFlow] DaySettlementManager 未找到，请确保场景中存在 DaySettlementManager");
            allReady = false;
        }

        return allReady;
    }

    /// <summary>
    /// 开始新的一天
    /// </summary>
    private void StartNewDay()
    {
        // 检查关键管理器是否就绪
        if (!CheckRequiredManagers()) return;

        _currentDay++;

        _orderTicketController?.ResetTicket();
        DialogueUIManager.Instance?.ForceClearDialogue();

        // 重置当天统计
        _todayCustomersServed = 0;
        _todaySatisfiedCount = 0;
        _todayNeutralCount = 0;
        _todayDissatisfiedCount = 0;
        _todaySanityStart = SanityManager.Instance.CurrentSanity;

        // 清空理智值历史记录（用于当天结算明细）
        SanityManager.Instance.ClearHistory();

        // 获取当天顾客配置
        DayCustomerConfigSO dayConfig = _gameModeConfig.GetDayConfig(_currentDay - 1);
        if (dayConfig == null)
        {
            Debug.LogError($"[GameFlow] 第{_currentDay}天的顾客配置为空");
            return;
        }

        ApplyDayIngredientUnlocks(
            dayConfig,
            out List<LiquidSO> unlockedLiquids,
            out List<ToppingSO> unlockedToppings);

        bool shouldSkipDayUnlockNotice = IsTutorialFirstDay();
        CachePendingDayIngredientUnlocks(unlockedLiquids, unlockedToppings, shouldSkipDayUnlockNotice);
        ContinueStartNewDay(dayConfig);
    }

    /// <summary>
    /// 材料解锁提示结束后继续推进每日开场流程。
    /// </summary>
    private void ContinueStartNewDay(DayCustomerConfigSO dayConfig)
    {
        // 生成当天顾客队列
        CustomerManager.Instance.GenerateTodayQueue(dayConfig, _gameModeConfig.gameMode);
        _remainingCustomers = CustomerManager.Instance.QueueCount;
        bool hasSpecialCustomerToday = CustomerManager.Instance.HasSpecialCustomerInTodayQueue();

        // 切换到吧台视图
        ViewSwitchManager.Instance.ShowView(GameViewType.Bar);
        ViewSwitchManager.Instance.SetCanSwitch(false);

        OnDayStart?.Invoke(_currentDay);
        TutorialEventBus.Publish("DayStart");

        if (_showDebugLog)
        {
            Debug.Log($"[GameFlow] === 第 {_currentDay} 天开始 === 顾客数: {_remainingCustomers}");
        }

        // 小章鱼开场对话，说完后再根据教学门控生成第一位顾客
        if (_octopusNPC != null)
        {
            bool isTutorial = IsTutorialFirstDay();
            _octopusNPC.PlayDailyOpening(
                isTutorial,
                dayConfig,
                _remainingCustomers,
                hasSpecialCustomerToday,
                _pendingDayUnlockedToppings.Count,
                _pendingDayUnlockedLiquids.Count,
                OnDailyOpeningComplete);
        }
        else
        {
            OnDailyOpeningComplete();
        }
    }

    /// <summary>
    /// 缓存当天开始时新解锁的材料，用于小章鱼播报后展示获得面板。
    /// </summary>
    private void CachePendingDayIngredientUnlocks(
        IReadOnlyList<LiquidSO> unlockedLiquids,
        IReadOnlyList<ToppingSO> unlockedToppings,
        bool skipNotice)
    {
        _pendingDayUnlockedLiquids.Clear();
        _pendingDayUnlockedToppings.Clear();

        if (skipNotice)
        {
            return;
        }

        if (unlockedLiquids != null)
        {
            _pendingDayUnlockedLiquids.AddRange(unlockedLiquids);
        }

        if (unlockedToppings != null)
        {
            _pendingDayUnlockedToppings.AddRange(unlockedToppings);
        }
    }

    /// <summary>
    /// 判断当前是否处于教学模式第 1 天，用于跳过会打断教程的新货提示。
    /// </summary>
    private bool IsTutorialFirstDay()
    {
        return _gameModeConfig != null
            && _gameModeConfig.gameMode == GameMode.Tutorial
            && _currentDay == 1;
    }

    /// <summary>
    /// 每日开场完成后，先展示新材料获得面板，再等待教学首日介绍放行并生成第一位顾客。
    /// </summary>
    private void OnDailyOpeningComplete()
    {
        ShowIngredientUnlockPopupIfNeeded(
            _pendingDayUnlockedLiquids,
            _pendingDayUnlockedToppings,
            () =>
            {
                _pendingDayUnlockedLiquids.Clear();
                _pendingDayUnlockedToppings.Clear();
                StartCoroutine(SpawnNextCustomerAfterOpeningGate());
            });
    }

    /// <summary>
    /// 等待开场教学门控后生成下一位顾客。
    /// </summary>
    private IEnumerator SpawnNextCustomerAfterOpeningGate()
    {
        yield return TutorialGate.WaitForRelease(TutorialGateKey.BeforeFirstCustomerEnter);
        SpawnNextCustomer();
    }

    /// <summary>
    /// 生成下一位顾客
    /// </summary>
    private void SpawnNextCustomer()
    {
        SetState(GameFlowState.CustomerEntering);
        CustomerManager.Instance.SpawnNextCustomer();
    }

    /// <summary>
    /// 顾客生成完成回调
    /// </summary>
    private void OnCustomerSpawned(CustomerSO customer)
    {
        if (_currentState != GameFlowState.CustomerEntering) return;

        if (_showDebugLog)
            Debug.Log($"[GameFlow] 顾客到达: {customer.customerName}");

        GalleryManager.Instance?.MarkCharacterEncountered(customer);

        // 播放客人进店音效
        AudioManager.Instance?.PlaySfx(SoundId.CustomerEnter);

        ActionLogBus.Log($"客人进店");

        // 进入顾客流程时，先禁止切换，直到订单确认后再开启
        ViewSwitchManager.Instance.SetCanSwitch(false);

        // 强制切换回吧台视图（玩家可能在制作界面）
        ViewSwitchManager.Instance.ShowView(GameViewType.Bar);

        // 先强制清空残留对话，再由入场动画完成事件触发进店对白
        DialogueUIManager.Instance?.ForceClearDialogue();

        // 没有动画组件时，直接进入进店对白前的教学门控
        if (_customerDisplayUI == null)
        {
            StartCustomerEnterDialogueWithGate(customer);
        }
    }

    /// <summary>
    /// 进场动画完成回调 — 此时顾客已到位，开始进店对白
    /// </summary>
    private void OnCustomerEnterAnimationComplete()
    {
        if (_currentState != GameFlowState.CustomerEntering) return;

        var customer = CustomerManager.Instance?.CurrentCustomer;
        if (customer != null)
            StartCustomerEnterDialogueWithGate(customer);
    }

    /// <summary>
    /// 顾客到位后，等待教学介绍顾客放行再开始进店对白。
    /// </summary>
    private void StartCustomerEnterDialogueWithGate(CustomerSO customer)
    {
        StartCoroutine(StartCustomerEnterDialogueAfterGate(customer));
    }

    /// <summary>
    /// 发布顾客到位事件，并等待教学门控后开始顾客对白。
    /// </summary>
    private IEnumerator StartCustomerEnterDialogueAfterGate(CustomerSO customer)
    {
        TutorialEventBus.Publish(TutorialEvents.CustomerArrived);
        yield return TutorialGate.WaitForRelease(TutorialGateKey.BeforeFirstCustomerDialogue);
        PlaySpecialCustomerAnnouncementIfNeeded(customer, () =>
        {
            StartCustomerEnterDialogue(customer);
        });
    }

    /// <summary>
    /// 如当前顾客带有特殊客人播报，则先让小章鱼播报后再继续顾客对白。
    /// </summary>
    private void PlaySpecialCustomerAnnouncementIfNeeded(CustomerSO customer, Action onComplete)
    {
        if (customer == null || IsTutorialFirstDay() || _octopusNPC == null)
        {
            onComplete?.Invoke();
            return;
        }

        string announcementLine = GetSpecialCustomerAnnouncementLine(customer);
        if (string.IsNullOrEmpty(announcementLine))
        {
            onComplete?.Invoke();
            return;
        }

        _octopusNPC.PlaySpecialCustomerAnnouncement(new List<string> { announcementLine }, onComplete);
    }

    /// <summary>
    /// 获取当前特殊顾客配置中的小章鱼入场播报文案。
    /// </summary>
    private string GetSpecialCustomerAnnouncementLine(CustomerSO customer)
    {
        SpecialCustomerProfileSO profile = customer?.specialProfile;
        return profile == null ? string.Empty : profile.specialCustomerAnnouncementLine;
    }

    /// <summary>
    /// 开始顾客进店对白
    /// </summary>
    private void StartCustomerEnterDialogue(CustomerSO customer)
    {
        if (customer == null) return;

        SetState(GameFlowState.CustomerEntering);

        TutorialEventBus.Publish(TutorialEvents.CustomerReadyToTalk);

        CustomerManager.Instance.StartTalking();

        string enterDialogue = CustomerManager.Instance.GetRandomEnterDialogue();
        DialogueUIManager.Instance.ShowDialogue(enterDialogue, () =>
        {
            StartCustomerDialogue(customer);
        });
    }

    /// <summary>
    /// 开始顾客点单对白
    /// </summary>
    private void StartCustomerDialogue(CustomerSO customer)
    {
        SetState(GameFlowState.CustomerTalking);

        if (ShouldUseCustomerOrderGroup(customer))
        {
            StartCustomerGroupDialogue(customer);
            return;
        }

        // 先预生成订单，用于点单对白内容，但暂不触发小票显示
        _currentOrderSO = OrderManager.Instance.PrepareOrderForCustomer(customer);
        if (_currentOrderSO == null)
        {
            Debug.LogError($"[GameFlow] 为顾客 {customer.customerName} 预生成订单失败");
            return;
        }

        // 获取点单对话
        string orderDialogue = OrderManager.Instance.GetRandomOrderDialogue(_currentOrderSO);

        // 显示对话气泡，结束后正式确认订单并进入等待制作阶段
        DialogueUIManager.Instance.ShowDialogue(orderDialogue, () =>
        {
            _currentOrderSO = OrderManager.Instance.ConfirmPreparedOrder();
            if (_currentOrderSO == null)
            {
                Debug.LogError($"[GameFlow] 为顾客 {customer.customerName} 确认订单失败");
                return;
            }

            InitializeSingleOrderSession(customer, _currentOrderSO);
            StartWaitingForCraft();
        });
    }

    /// <summary>
    /// 判断当前顾客是否启用顾客组多订单。
    /// </summary>
    /// <param name="customer">当前接待的顾客。</param>
    /// <returns>启用顾客组且存在成员配置时返回 true。</returns>
    private bool ShouldUseCustomerOrderGroup(CustomerSO customer)
    {
        SpecialCustomerProfileSO profile = customer?.specialProfile;
        return profile != null
            && profile.useCustomerOrderGroup
            && profile.orderParticipants != null
            && profile.orderParticipants.Count > 0;
    }

    /// <summary>
    /// 开始顾客组多订单点单对白。
    /// </summary>
    /// <param name="customer">当前接待的主顾客。</param>
    private void StartCustomerGroupDialogue(CustomerSO customer)
    {
        if (!InitializeCustomerOrderGroupSession(customer, out List<string> orderDialogues))
        {
            Debug.LogError($"[GameFlow] 为顾客组 {customer.customerName} 初始化多订单失败");
            return;
        }

        DialogueUIManager.Instance.ShowDialogueSequence(orderDialogues, () =>
        {
            RefreshCurrentOrderSessionTickets();
            StartWaitingForCraft();
        });
    }

    /// <summary>
    /// 初始化顾客组订单会话，为每个成员生成一张订单小票。
    /// </summary>
    /// <param name="customer">当前接待的主顾客。</param>
    /// <param name="orderDialogues">生成的点单对白列表。</param>
    /// <returns>初始化成功返回 true。</returns>
    private bool InitializeCustomerOrderGroupSession(CustomerSO customer, out List<string> orderDialogues)
    {
        orderDialogues = new List<string>();

        if (customer == null || !ShouldUseCustomerOrderGroup(customer))
        {
            return false;
        }

        if (_currentOrderSession == null)
        {
            _currentOrderSession = new CustomerOrderSessionData();
        }

        string sessionId = BuildOrderSessionId(customer);
        _currentOrderSession.Initialize(sessionId, customer);

        List<CustomerOrderParticipantConfig> participants = customer.specialProfile.orderParticipants;
        for (int i = 0; i < participants.Count; i++)
        {
            CustomerOrderParticipantConfig participant = participants[i];
            if (participant == null)
            {
                Debug.LogWarning($"[GameFlow] 顾客组 {customer.customerName} 的第 {i + 1} 个成员为空，已跳过");
                continue;
            }

            string participantName = BuildParticipantDisplayName(participant, i);
            OrderSO order = OrderManager.Instance.GenerateOrderFromPoolEntries(
                participant.orderPoolEntries,
                $"{customer.customerName}-{participantName}");
            if (order == null)
            {
                Debug.LogError($"[GameFlow] 顾客组 {customer.customerName} 的成员 {participantName} 生成订单失败");
                _currentOrderSession.Clear();
                return false;
            }

            string slotId = BuildCustomerOrderGroupSlotId(sessionId, participant, i);
            CustomerOrderSlotData slot = new CustomerOrderSlotData();
            slot.Initialize(slotId, participantName, order, participant.ordererAvatarSprite);
            _currentOrderSession.AddSlot(slot);
            orderDialogues.Add(GetParticipantOrderDialogue(participant, order, participantName));
        }

        if (_currentOrderSession.SlotCount == 0)
        {
            return false;
        }

        _currentOrderSession.TrySelectSlot(0);
        SyncCurrentOrderFields(_currentOrderSession.SelectedSlot);
        return true;
    }

    /// <summary>
    /// 构建顾客组成员显示名。
    /// </summary>
    /// <param name="participant">顾客组成员配置。</param>
    /// <param name="index">成员索引。</param>
    private string BuildParticipantDisplayName(CustomerOrderParticipantConfig participant, int index)
    {
        return !string.IsNullOrEmpty(participant.displayName)
            ? participant.displayName
            : $"订单者{index + 1}";
    }

    /// <summary>
    /// 构建顾客组订单槽ID。
    /// </summary>
    /// <param name="sessionId">订单会话ID。</param>
    /// <param name="participant">顾客组成员配置。</param>
    /// <param name="index">成员索引。</param>
    private string BuildCustomerOrderGroupSlotId(
        string sessionId,
        CustomerOrderParticipantConfig participant,
        int index)
    {
        string participantId = !string.IsNullOrEmpty(participant.participantId)
            ? participant.participantId
            : $"participant_{index}";
        return $"{sessionId}_{participantId}";
    }

    /// <summary>
    /// 获取顾客组成员的点单对白。
    /// </summary>
    /// <param name="participant">顾客组成员配置。</param>
    /// <param name="order">该成员生成的订单。</param>
    /// <param name="participantName">成员显示名。</param>
    private string GetParticipantOrderDialogue(
        CustomerOrderParticipantConfig participant,
        OrderSO order,
        string participantName)
    {
        if (participant.orderDialogueTexts != null && participant.orderDialogueTexts.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, participant.orderDialogueTexts.Count);
            string dialogue = participant.orderDialogueTexts[randomIndex];
            if (!string.IsNullOrEmpty(dialogue))
            {
                return dialogue;
            }
        }

        string orderDialogue = OrderManager.Instance.GetRandomOrderDialogue(order);
        return string.IsNullOrEmpty(participantName)
            ? orderDialogue
            : $"{participantName}：{orderDialogue}";
    }

    /// <summary>
    /// 初始化普通单订单会话，为后续顾客组多订单流程提供统一入口。
    /// </summary>
    /// <param name="customer">当前接待的主顾客。</param>
    /// <param name="order">当前顾客确认后的订单。</param>
    private void InitializeSingleOrderSession(CustomerSO customer, OrderSO order)
    {
        if (customer == null || order == null)
        {
            Debug.LogError("[GameFlow] 初始化单订单会话失败：顾客或订单为空");
            ClearCurrentOrderSession();
            return;
        }

        if (_currentOrderSession == null)
        {
            _currentOrderSession = new CustomerOrderSessionData();
        }

        string sessionId = BuildOrderSessionId(customer);
        string displayName = string.IsNullOrEmpty(customer.customerName)
            ? customer.name
            : customer.customerName;

        CustomerOrderSlotData slot = new CustomerOrderSlotData();
        slot.Initialize($"{sessionId}_slot_0", displayName, order, customer.ordererAvatarSprite);

        _currentOrderSession.Initialize(sessionId, customer);
        _currentOrderSession.AddSlot(slot);
        SyncCurrentOrderFields(slot);
    }

    /// <summary>
    /// 构建当前接待使用的订单会话ID。
    /// </summary>
    /// <param name="customer">当前接待的主顾客。</param>
    private string BuildOrderSessionId(CustomerSO customer)
    {
        string customerId = customer == null || string.IsNullOrEmpty(customer.customerId)
            ? "customer"
            : customer.customerId;

        return $"day_{_currentDay}_served_{_todayCustomersServed}_{customerId}";
    }

    /// <summary>
    /// 获取当前选中的订单槽。
    /// </summary>
    private CustomerOrderSlotData GetCurrentOrderSlot()
    {
        return _currentOrderSession?.SelectedSlot;
    }

    /// <summary>
    /// 将当前订单槽同步到旧单订单字段，保持现有评分、奖励和UI流程可继续复用。
    /// </summary>
    /// <param name="slot">当前选中的订单槽。</param>
    private void SyncCurrentOrderFields(CustomerOrderSlotData slot)
    {
        _currentOrderSO = slot?.orderSO;
        _currentOrderData = slot?.requirementData;
        _currentScoringData = slot?.scoringData;
    }

    /// <summary>
    /// 刷新当前订单会话小票，普通单订单和顾客组多订单都使用同一套完成态显示流程。
    /// </summary>
    /// <param name="allowAutoShowSelectedDetail">是否允许小票控制器按配置自动展开当前小票详情。</param>
    private void RefreshCurrentOrderSessionTickets(bool allowAutoShowSelectedDetail = true)
    {
        if (_currentOrderSession == null || _currentOrderSession.SlotCount <= 0)
        {
            return;
        }

        CacheReferences();
        _orderTicketController?.RefreshSession(_currentOrderSession, allowAutoShowSelectedDetail);
    }

    /// <summary>
    /// 当前是否处于单个顾客组的多订单接待。
    /// </summary>
    private bool IsCurrentCustomerOrderGroupSession()
    {
        return _currentOrderSession != null && _currentOrderSession.SlotCount > 1;
    }

    /// <summary>
    /// 尝试切换到下一张仍待提交的小票。
    /// </summary>
    /// <returns>存在下一张待提交小票并切换成功时返回 true。</returns>
    private bool TrySelectNextWaitingOrderSlot()
    {
        if (_currentOrderSession == null || !_currentOrderSession.SelectFirstWaitingSlot())
        {
            return false;
        }

        SyncCurrentOrderFields(_currentOrderSession.SelectedSlot);
        RefreshCurrentOrderSessionTickets();
        return true;
    }

    /// <summary>
    /// 清理当前订单会话和兼容旧流程的当前订单字段。
    /// </summary>
    private void ClearCurrentOrderSession()
    {
        _currentOrderSession?.Clear();
        _currentOrderSO = null;
        _currentOrderData = null;
        _currentScoringData = null;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 测试工具：按指定评价档位直接结算当前顾客订单，并继续走反馈、奖励与离场流程。
    /// </summary>
    /// <param name="feedbackLevel">评价档位，0=差评，1=中评，2=好评，3=Perfect。</param>
    /// <returns>成功发起结算返回 true。</returns>
    public bool DebugCompleteCurrentOrder(int feedbackLevel)
    {
        if (!CanDebugCompleteCurrentOrder())
        {
            Debug.LogWarning($"[GameFlowDebug] 当前状态不能结束订单：{_currentState}");
            return false;
        }

        if (CustomerManager.Instance == null || CustomerManager.Instance.CurrentCustomer == null)
        {
            Debug.LogWarning("[GameFlowDebug] 当前没有可结算的顾客");
            return false;
        }

        if (_currentOrderSO == null)
        {
            _currentOrderSO = OrderManager.Instance?.CurrentOrder;
        }

        CustomerOrderSlotData currentSlot = GetCurrentOrderSlot();
        if (currentSlot == null && _currentOrderSO != null)
        {
            InitializeSingleOrderSession(CustomerManager.Instance.CurrentCustomer, _currentOrderSO);
            currentSlot = GetCurrentOrderSlot();
        }

        CacheReferences();
        StopAllCoroutines();
        _orderTicketController?.ResetTicket();
        _customerDisplayUI?.HidePatienceBar();
        DialogueUIManager.Instance?.ForceClearDialogue();
        ViewSwitchManager.Instance?.SetCanSwitch(false);
        ViewSwitchManager.Instance?.ShowView(GameViewType.Bar);
        CoffeeCraftManager.Instance?.DebugResetCraftState();

        CoffeeScoringData scoringData = CreateDebugScoringData(feedbackLevel);
        currentSlot?.TryCompleteScoring(scoringData);
        SyncCurrentOrderFields(currentSlot);
        RecordDebugOrderStatistics(scoringData.feedbackLevel);
        SanityManager.Instance?.ApplyFeedbackSanityChange(scoringData.feedbackLevel);
        ActionLogBus.Log($"测试结算订单：{BuildFeedbackLabel(feedbackLevel)}", Color.cyan);

        StartCustomerFeedback(scoringData);
        return true;
    }

    /// <summary>
    /// 测试工具：结束当天营业并进入日结算流程。
    /// </summary>
    /// <returns>成功发起日结返回 true。</returns>
    public bool DebugEndCurrentDay()
    {
        if (!_isGameRunning)
        {
            Debug.LogWarning("[GameFlowDebug] 游戏未运行，无法结束当天营业");
            return false;
        }

        if (_currentState == GameFlowState.DayEnd || _currentState == GameFlowState.GameEnd)
        {
            Debug.LogWarning($"[GameFlowDebug] 当前状态不能重复结束当天：{_currentState}");
            return false;
        }

        StopAllCoroutines();
        PrepareDebugFlowInterruption();
        _remainingCustomers = 0;
        StartDayEnd();
        return true;
    }

    /// <summary>
    /// 测试工具：按正式结局分发流程触发指定结局，并写入图鉴。
    /// </summary>
    /// <param name="ending">需要触发的结局。</param>
    /// <returns>成功触发返回 true。</returns>
    public bool DebugTriggerEnding(GameEnding ending)
    {
        if (_currentState == GameFlowState.GameEnd)
        {
            Debug.LogWarning("[GameFlowDebug] 游戏已经处于结局状态，无法重复触发");
            return false;
        }

        StopAllCoroutines();
        PrepareDebugFlowInterruption();
        SetState(GameFlowState.GameEnd);
        _isGameRunning = false;

        HandleModeUnlock(GetDebugSanityForEnding(ending));
        GalleryManager.Instance?.MarkEndingUnlocked(ending);
        OnGameEnding?.Invoke(ending);
        OnGameEnd?.Invoke(_currentDay);

        Debug.Log($"[GameFlowDebug] 已触发指定结局：{ending}");
        return true;
    }

    /// <summary>
    /// 测试工具：指定下一位顾客。
    /// </summary>
    /// <param name="customer">下一位顾客配置。</param>
    /// <returns>成功设置返回 true。</returns>
    public bool DebugSetNextCustomer(CustomerSO customer)
    {
        if (CustomerManager.Instance == null)
        {
            Debug.LogWarning("[GameFlowDebug] CustomerManager 不存在，无法指定下一位顾客");
            return false;
        }

        bool success = CustomerManager.Instance.DebugSetNextCustomer(customer, out bool appended);
        if (success && appended)
        {
            _remainingCustomers = Mathf.Max(_remainingCustomers, 0) + 1;
        }

        return success;
    }

    /// <summary>
    /// 测试工具：查询当前是否允许直接结算当前订单。
    /// </summary>
    public bool CanDebugCompleteCurrentOrder()
    {
        return _currentState == GameFlowState.CustomerEntering
            || _currentState == GameFlowState.CustomerTalking
            || _currentState == GameFlowState.WaitingForCraft;
    }

    /// <summary>
    /// 测试工具：创建指定档位的评分数据。
    /// </summary>
    /// <param name="feedbackLevel">评价档位，0=差评，1=中评，2=好评，3=Perfect。</param>
    private CoffeeScoringData CreateDebugScoringData(int feedbackLevel)
    {
        int normalizedFeedback = Mathf.Clamp(feedbackLevel, 0, 2);
        bool isPerfect = feedbackLevel >= 3;
        float score = feedbackLevel switch
        {
            <= 0 => 25f,
            1 => 65f,
            2 => 85f,
            _ => 100f
        };

        CoffeeQuality quality = isPerfect
            ? CoffeeQuality.Perfect
            : normalizedFeedback switch
            {
                2 => CoffeeQuality.Acceptable,
                1 => CoffeeQuality.Acceptable,
                _ => CoffeeQuality.Terrible
            };

        return new CoffeeScoringData
        {
            coffeeMatchScore = score,
            liquidMatchScore = score,
            toppingMatchScore = score,
            volumeMatchScore = score,
            cupAdaptationScore = score,
            finalScore = score,
            qualityLevel = quality,
            feedbackLevel = normalizedFeedback,
            scoringDetail = $"测试工具强制结算：{BuildFeedbackLabel(feedbackLevel)}"
        };
    }

    /// <summary>
    /// 测试工具：记录指定档位的日统计数据。
    /// </summary>
    /// <param name="feedbackLevel">归一化后的评价档位。</param>
    private void RecordDebugOrderStatistics(int feedbackLevel)
    {
        _todayCustomersServed++;

        switch (Mathf.Clamp(feedbackLevel, 0, 2))
        {
            case 2:
                _todaySatisfiedCount++;
                break;
            case 1:
                _todayNeutralCount++;
                break;
            default:
                _todayDissatisfiedCount++;
                break;
        }
    }

    /// <summary>
    /// 测试工具：中断当前流程前清理可能残留的对话、订单和顾客状态。
    /// </summary>
    private void PrepareDebugFlowInterruption()
    {
        CacheReferences();
        SetState(GameFlowState.None);
        DialogueUIManager.Instance?.ForceClearDialogue();
        _orderTicketController?.ResetTicket();
        _customerDisplayUI?.HidePatienceBar();
        CoffeeCraftManager.Instance?.DebugResetCraftState();
        OrderManager.Instance?.ResetOrder();
        CustomerManager.Instance?.DebugClearCurrentCustomer();
        ClearCurrentOrderSession();
        ViewSwitchManager.Instance?.SetCanSwitch(false);
        ViewSwitchManager.Instance?.ShowView(GameViewType.Bar);
    }

    /// <summary>
    /// 测试工具：获取评价档位显示名。
    /// </summary>
    /// <param name="feedbackLevel">评价档位，0=差评，1=中评，2=好评，3=Perfect。</param>
    private string BuildFeedbackLabel(int feedbackLevel)
    {
        return feedbackLevel switch
        {
            <= 0 => "差评",
            1 => "中评",
            2 => "好评",
            _ => "Perfect"
        };
    }

    /// <summary>
    /// 测试工具：为指定结局提供符合正式阈值判断的理智值。
    /// </summary>
    /// <param name="ending">需要模拟的结局。</param>
    private float GetDebugSanityForEnding(GameEnding ending)
    {
        return ending switch
        {
            GameEnding.Good => _goodEndingThreshold,
            GameEnding.Return => _returnEndingThreshold,
            _ => Mathf.Max(0f, _returnEndingThreshold - 1f)
        };
    }
#endif

    /// <summary>
    /// 进入等待制作阶段
    /// </summary>
    private void StartWaitingForCraft()
    {
        CustomerOrderSlotData currentSlot = GetCurrentOrderSlot();
        if (currentSlot == null || currentSlot.requirementData == null)
        {
            Debug.LogError("[GameFlow] 当前订单会话为空，无法进入制作阶段");
            return;
        }

        SyncCurrentOrderFields(currentSlot);
        SetState(GameFlowState.WaitingForCraft);

        // 顾客开始等待
        CustomerManager.Instance.StartWaiting();

        // 允许视图切换（玩家可以去制作界面）
        ViewSwitchManager.Instance.SetCanSwitch(true);

        // 初始化制作系统
        CoffeeCraftManager.Instance.StartNewCraft(currentSlot.requirementData);

        if (_showDebugLog)
        {
            Debug.Log("[GameFlow] 等待玩家制作咖啡...");
        }
    }

    /// <summary>
    /// 玩家提交咖啡（由外部调用，如提交按钮）
    /// </summary>
    public void OnPlayerSubmitCoffee()
    {
        if (_currentState != GameFlowState.WaitingForCraft) return;

        if (!CoffeeCraftManager.Instance.CanSubmit())
        {
            if (_showDebugLog)
            {
                Debug.Log("[GameFlow] 咖啡尚未完成，无法提交");
            }
            return;
        }

        CacheReferences();

        // 提交咖啡，获取数据
        CoffeeData coffeeData = CoffeeCraftManager.Instance.SubmitCoffee();

        // 禁止视图切换
        ViewSwitchManager.Instance.SetCanSwitch(false);

        // 切换回吧台
        ViewSwitchManager.Instance.ShowView(GameViewType.Bar);

        // 进入评分阶段
        StartScoring(coffeeData);
    }

    /// <summary>
    /// 开始评分
    /// </summary>
    private void StartScoring(CoffeeData coffeeData)
    {
        CustomerOrderSlotData currentSlot = GetCurrentOrderSlot();
        OrderSO currentOrder = currentSlot != null ? currentSlot.orderSO : _currentOrderSO;
        if (currentOrder == null)
        {
            Debug.LogError("[GameFlow] 当前订单为空，无法评分");
            return;
        }

        currentSlot?.TrySubmitCoffee(coffeeData);
        SyncCurrentOrderFields(currentSlot);
        SetState(GameFlowState.Scoring);

        // 提交订单
        OrderManager.Instance.SubmitOrder(currentOrder, coffeeData);

        // 计算评分
        CoffeeScoringData scoringData = ScoringManager.Instance.CalculateScore(
            currentOrder, coffeeData, _gameModeConfig.gameMode
        );

        currentSlot?.TryCompleteScoring(scoringData);
        SyncCurrentOrderFields(currentSlot);

        if (_showDebugLog)
        {
            Debug.Log($"[GameFlow] 评分完成: {scoringData.finalScore}分, 等级: {scoringData.qualityLevel}, 反馈: {scoringData.feedbackLevel}");
        }

        bool isCustomerOrderGroupSession = IsCurrentCustomerOrderGroupSession();
        if (isCustomerOrderGroupSession && TrySelectNextWaitingOrderSlot())
        {
            if (_showDebugLog)
            {
                Debug.Log("[GameFlow] 顾客组仍有未完成订单，继续等待下一杯制作");
            }

            StartWaitingForCraft();
            return;
        }

        if (isCustomerOrderGroupSession)
        {
            scoringData = BuildCurrentOrderSessionScoringData(scoringData);
            SyncCurrentOrderFields(currentSlot);
        }

        RefreshCurrentOrderSessionTickets(false);
        StartCoroutine(FinalizeScoredOrderAfterTicketRefresh(scoringData));
    }

    /// <summary>
    /// 等待小票完成态刷新到画面后，再进入订单结算流程。
    /// </summary>
    /// <param name="scoringData">最终用于结算的评分数据。</param>
    private IEnumerator FinalizeScoredOrderAfterTicketRefresh(CoffeeScoringData scoringData)
    {
        yield return null;
        FinalizeScoredOrder(scoringData);
    }

    /// <summary>
    /// 完成一次订单接待的统计、日志、理智值变化和顾客反馈流程。
    /// </summary>
    /// <param name="scoringData">用于本次结算的评分数据。</param>
    private void FinalizeScoredOrder(CoffeeScoringData scoringData)
    {
        if (scoringData == null)
        {
            Debug.LogError("[GameFlow] 评分数据为空，无法完成订单结算");
            return;
        }

        // 记录统计
        _customerDisplayUI?.HidePatienceBar();
        _todayCustomersServed++;
        switch (scoringData.feedbackLevel)
        {
            case 2: _todaySatisfiedCount++; break;
            case 1: _todayNeutralCount++; break;
            case 0: _todayDissatisfiedCount++; break;
        }

        // 发布提交订单和评级日志
        ActionLogBus.Log($"提交订单");
        string feedbackLabel = scoringData.feedbackLevel switch
        {
            2 => "好评",
            1 => "中评",
            _ => "差评"
        };
        Color feedbackColor = scoringData.feedbackLevel switch
        {
            2 => Color.green,
            1 => Color.yellow,
            _ => Color.red
        };
        ActionLogBus.Log($"订单评级：{feedbackLabel}", feedbackColor);

        // 应用理智值变化
        SanityManager.Instance.ApplyFeedbackSanityChange(scoringData.feedbackLevel);

        // 显示反馈
        StartCustomerFeedback(scoringData);
    }

    /// <summary>
    /// 聚合当前顾客组全部订单的评分结果。
    /// </summary>
    /// <param name="fallbackScoringData">无法聚合时使用的兜底评分。</param>
    /// <returns>顾客组统一结算评分。</returns>
    private CoffeeScoringData BuildCurrentOrderSessionScoringData(CoffeeScoringData fallbackScoringData)
    {
        if (_currentOrderSession == null || _currentOrderSession.SlotCount <= 1)
        {
            return fallbackScoringData;
        }

        int validCount = 0;
        float coffeeMatchScore = 0f;
        float liquidMatchScore = 0f;
        float toppingMatchScore = 0f;
        float volumeMatchScore = 0f;
        float cupAdaptationScore = 0f;
        float finalScore = 0f;
        int feedbackLevel = 2;
        CoffeeQuality qualityLevel = CoffeeQuality.Perfect;
        bool hasOverflow = false;
        bool hasMissingComponents = false;
        float overflowSanityLoss = 0f;

        foreach (CustomerOrderSlotData slot in _currentOrderSession.orderSlots)
        {
            CoffeeScoringData scoringData = slot?.scoringData;
            if (scoringData == null)
            {
                continue;
            }

            validCount++;
            coffeeMatchScore += scoringData.coffeeMatchScore;
            liquidMatchScore += scoringData.liquidMatchScore;
            toppingMatchScore += scoringData.toppingMatchScore;
            volumeMatchScore += scoringData.volumeMatchScore;
            cupAdaptationScore += scoringData.cupAdaptationScore;
            finalScore += scoringData.finalScore;
            feedbackLevel = Mathf.Min(feedbackLevel, scoringData.feedbackLevel);
            qualityLevel = GetLowerCoffeeQuality(qualityLevel, scoringData.qualityLevel);
            hasOverflow |= scoringData.hasOverflow;
            hasMissingComponents |= scoringData.hasMissingComponents;
            overflowSanityLoss += scoringData.overflowSanityLoss;
        }

        if (validCount == 0)
        {
            return fallbackScoringData;
        }

        return new CoffeeScoringData
        {
            coffeeMatchScore = coffeeMatchScore / validCount,
            liquidMatchScore = liquidMatchScore / validCount,
            toppingMatchScore = toppingMatchScore / validCount,
            volumeMatchScore = volumeMatchScore / validCount,
            cupAdaptationScore = cupAdaptationScore / validCount,
            finalScore = finalScore / validCount,
            qualityLevel = qualityLevel,
            feedbackLevel = feedbackLevel,
            hasOverflow = hasOverflow,
            overflowSanityLoss = overflowSanityLoss,
            hasMissingComponents = hasMissingComponents,
            scoringDetail = $"顾客组订单统一结算：{validCount} 张订单，最终档位取最低评价"
        };
    }

    /// <summary>
    /// 获取两个品质等级中较低的等级。
    /// </summary>
    /// <param name="left">左侧品质等级。</param>
    /// <param name="right">右侧品质等级。</param>
    /// <returns>较低品质等级。</returns>
    private CoffeeQuality GetLowerCoffeeQuality(CoffeeQuality left, CoffeeQuality right)
    {
        return (CoffeeQuality)Mathf.Min((int)left, (int)right);
    }

    /// <summary>
    /// 顾客反馈阶段
    /// </summary>
    private void StartCustomerFeedback(CoffeeScoringData scoringData)
    {
        SetState(GameFlowState.CustomerFeedback);

        _currentScoringData = scoringData;

        int cappedLevel = CustomerManager.Instance.FinishOrderAndShowFeedback(scoringData.feedbackLevel);

        DialogueUIManager.Instance?.ForceClearDialogue();

        string feedbackText = CustomerManager.Instance.GetRandomFeedbackText(cappedLevel);

        // 说话前先切换表情
        CustomerManager.Instance.SetFeedbackState(cappedLevel);

        if (string.IsNullOrEmpty(feedbackText))
        {
            TryGrantCollectibleThenLeave();
            return;
        }

        DialogueUIManager.Instance.ShowDialogue(feedbackText, () =>
        {
            string specialText = CustomerManager.Instance.GetRandomSpecialFeedbackText();

            if (!string.IsNullOrEmpty(specialText) && cappedLevel >= 1)
            {
                DialogueUIManager.Instance.ShowDialogue(specialText, () =>
                {
                    TryGrantCollectibleThenLeave();
                });
            }
            else
            {
                TryGrantCollectibleThenLeave();
            }
        });
    }

    /// <summary>
    /// 尝试授予收集物，然后进入离开阶段
    /// </summary>
    private void TryGrantCollectibleThenLeave()
    {
        var customer = CustomerManager.Instance?.CurrentCustomer;

        if (_currentScoringData != null && _currentScoringData.qualityLevel == CoffeeQuality.Perfect)
        {
            GalleryManager.Instance?.MarkCharacterPerfected(customer);
        }

        TryGrantIngredientRewards(
            customer,
            out List<LiquidSO> unlockedLiquids,
            out List<ToppingSO> unlockedToppings);

        ShowIngredientUnlockPopupIfNeeded(unlockedLiquids, unlockedToppings, () =>
        {
            TryGrantCollectibleRewardThenLeave(customer);
        });
    }

    /// <summary>
    /// 尝试授予收集物，然后进入离开阶段。
    /// </summary>
    private void TryGrantCollectibleRewardThenLeave(CustomerSO customer)
    {
        if (CollectibleManager.Instance != null && _currentScoringData != null)
        {
            CollectibleSO obtained = CollectibleManager.Instance.TryObtainCollectible(
                customer, _currentScoringData.qualityLevel);

            if (obtained != null && _collectibleNotifyUI != null)
            {
                ActionLogBus.Log($"获得新的收集物：{obtained.collectibleName}", Color.cyan);
                _collectibleNotifyUI.Show(obtained, () =>
                {
                    StartCustomerLeaving();
                });
                return;
            }
        }

        StartCustomerLeaving();
    }

    /// <summary>
    /// 应用当天开始时解锁的辅助液和小料。
    /// </summary>
    private void ApplyDayIngredientUnlocks(
        DayCustomerConfigSO dayConfig,
        out List<LiquidSO> unlockedLiquids,
        out List<ToppingSO> unlockedToppings)
    {
        unlockedLiquids = new List<LiquidSO>();
        unlockedToppings = new List<ToppingSO>();

        if (dayConfig == null || IngredientUnlockManager.Instance == null)
        {
            return;
        }

        UnlockConfiguredLiquids(dayConfig.unlockedLiquidsOnDayStart, unlockedLiquids);
        UnlockConfiguredToppings(dayConfig.unlockedToppingsOnDayStart, unlockedToppings);

        if ((unlockedLiquids.Count > 0 || unlockedToppings.Count > 0) && _showDebugLog)
        {
            Debug.Log($"[GameFlow] 第{_currentDay}天解锁材料：辅助液 {unlockedLiquids.Count} 个，小料 {unlockedToppings.Count} 个");
        }

        IngredientUnlockManager.Instance.LogCurrentUnlocks($"第{_currentDay}天开始后");
    }

    /// <summary>
    /// Perfect 接待特殊顾客后发放材料解锁奖励。
    /// </summary>
    private void TryGrantIngredientRewards(
        CustomerSO customer,
        out List<LiquidSO> unlockedLiquids,
        out List<ToppingSO> unlockedToppings)
    {
        unlockedLiquids = new List<LiquidSO>();
        unlockedToppings = new List<ToppingSO>();

        if (customer == null || _currentScoringData == null)
        {
            return;
        }

        if (_currentScoringData.qualityLevel != CoffeeQuality.Perfect)
        {
            return;
        }

        if (IngredientUnlockManager.Instance == null)
        {
            Debug.LogWarning("[GameFlow] IngredientUnlockManager 未找到，无法发放材料解锁奖励");
            return;
        }

        SpecialCustomerProfileSO specialProfile = customer.specialProfile;
        if (specialProfile == null)
        {
            return;
        }

        UnlockConfiguredLiquids(specialProfile.rewardLiquidsOnPerfect, unlockedLiquids);
        UnlockConfiguredToppings(specialProfile.rewardToppingsOnPerfect, unlockedToppings);

        if (unlockedLiquids.Count > 0 || unlockedToppings.Count > 0)
        {
            ActionLogBus.Log($"解锁新材料：辅助液 {unlockedLiquids.Count} 个，小料 {unlockedToppings.Count} 个", Color.cyan);
            IngredientUnlockManager.Instance.LogCurrentUnlocks($"Perfect 接待 {customer.customerName} 后");
        }
    }

    /// <summary>
    /// 解锁配置中的辅助液，并记录本次新解锁的条目。
    /// </summary>
    private void UnlockConfiguredLiquids(IEnumerable<LiquidSO> liquids, List<LiquidSO> newlyUnlockedLiquids)
    {
        if (liquids == null || newlyUnlockedLiquids == null || IngredientUnlockManager.Instance == null)
        {
            return;
        }

        foreach (LiquidSO liquid in liquids)
        {
            if (liquid == null || IngredientUnlockManager.Instance.IsLiquidUnlocked(liquid))
            {
                continue;
            }

            if (IngredientUnlockManager.Instance.UnlockLiquid(liquid))
            {
                newlyUnlockedLiquids.Add(liquid);
            }
        }
    }

    /// <summary>
    /// 解锁配置中的小料，并记录本次新解锁的条目。
    /// </summary>
    private void UnlockConfiguredToppings(IEnumerable<ToppingSO> toppings, List<ToppingSO> newlyUnlockedToppings)
    {
        if (toppings == null || newlyUnlockedToppings == null || IngredientUnlockManager.Instance == null)
        {
            return;
        }

        foreach (ToppingSO topping in toppings)
        {
            if (topping == null || IngredientUnlockManager.Instance.IsToppingUnlocked(topping))
            {
                continue;
            }

            if (IngredientUnlockManager.Instance.UnlockTopping(topping))
            {
                newlyUnlockedToppings.Add(topping);
            }
        }
    }

    /// <summary>
    /// 有新材料时显示解锁提示；没有提示UI时直接继续流程。
    /// </summary>
    private void ShowIngredientUnlockPopupIfNeeded(
        IReadOnlyList<LiquidSO> unlockedLiquids,
        IReadOnlyList<ToppingSO> unlockedToppings,
        Action onComplete)
    {
        int liquidCount = unlockedLiquids?.Count ?? 0;
        int toppingCount = unlockedToppings?.Count ?? 0;
        if (liquidCount + toppingCount <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        CacheReferences();

        if (_ingredientUnlockNotifyUI == null)
        {
            Debug.LogWarning("[GameFlow] 未找到 IngredientUnlockNotifyUI，跳过材料解锁提示弹窗");
            onComplete?.Invoke();
            return;
        }

        _ingredientUnlockNotifyUI.Show(unlockedLiquids, unlockedToppings, onComplete);
    }

    /// <summary>
    /// 顾客离开阶段
    /// </summary>
    private void StartCustomerLeaving()
    {
        SetState(GameFlowState.CustomerLeaving);

        // 本单结束时再做一次兜底重置，避免下位顾客复用上一单的小票状态
        _orderTicketController?.ResetTicket();
        DialogueUIManager.Instance?.ForceClearDialogue();

        CustomerManager.Instance.Leave();
    }

    /// <summary>
    /// 顾客离开完成回调
    /// </summary>
    private void OnCustomerLeft(CustomerSO customer)
    {
        if (_currentState != GameFlowState.CustomerLeaving) return;

        _remainingCustomers--;

        if (_showDebugLog)
            Debug.Log($"[GameFlow] 顾客已离开，剩余: {_remainingCustomers}");

        // 重置订单
        OrderManager.Instance.ResetOrder();
        ClearCurrentOrderSession();

        // 有动画组件：等退场动画完成后再推进
        // 无动画组件：直接推进
        if (_customerDisplayUI == null)
            ProceedAfterCustomerLeft();
    }

    /// <summary>
    /// 退场动画完成回调 — 此时顾客已离屏，推进到下一位或日结算
    /// </summary>
    private void OnCustomerExitAnimationComplete()
    {
        if (_currentState != GameFlowState.CustomerLeaving) return;
        ProceedAfterCustomerLeft();
    }

    /// <summary>
    /// 退场完成后的流程推进（生成下一位顾客或进入日结算）
    /// </summary>
    private void ProceedAfterCustomerLeft()
    {
        if (_remainingCustomers > 0)
            StartCoroutine(DelayedSpawnNext());
        else
            StartDayEnd();
    }

    /// <summary>
    /// 延迟生成下一位顾客（退场动画已完成，此处仅作短暂间隔）
    /// </summary>
    private IEnumerator DelayedSpawnNext()
    {
        yield return new WaitForSeconds(0.3f);
        SpawnNextCustomer();
    }

    /// <summary>
    /// 当天结束，进入日结算
    /// </summary>
    private void StartDayEnd()
    {
        SetState(GameFlowState.DayEnd);
        TutorialEventBus.Publish("DayEnd");

        if (_showDebugLog)
            Debug.Log($"[GameFlow] === 第 {_currentDay} 天结束 ===");

        DaySettlementData settlementData = CollectDaySettlementData();
        DaySettlementManager.Instance.ShowSettlement(settlementData, null);
    }

    /// <summary>
    /// 收集当天结算数据
    /// </summary>
    private DaySettlementData CollectDaySettlementData()
    {
        float sanityEnd = SanityManager.Instance.CurrentSanity;

        var data = new DaySettlementData
        {
            dayNumber = _currentDay,
            sanityStart = _todaySanityStart,
            sanityEnd = sanityEnd,
            sanityChange = sanityEnd - _todaySanityStart,
            customersServed = _todayCustomersServed,
            satisfiedCount = _todaySatisfiedCount,
            neutralCount = _todayNeutralCount,
            dissatisfiedCount = _todayDissatisfiedCount,
            sanityChangeEntries = new List<SanityChangeEntry>()
        };

        // 从SanityManager获取当天的变化记录
        var history = SanityManager.Instance.ChangeHistory;
        if (history != null)
        {
            foreach (var record in history)
            {
                data.sanityChangeEntries.Add(new SanityChangeEntry(record.reason, record.delta));
            }
        }

        return data;
    }

    /// <summary>
    /// 日结算管理器触发的下一天事件
    /// </summary>
    private void OnNextDayFromSettlement(int nextDay)
    {
        if (!_isGameRunning) return;

        // 已到最后一天，触发结局
        if (_currentDay >= _gameModeConfig.totalDays)
        {
            EndGame();
            return;
        }

        // 结算结束，恢复非主轨道BGM
        AudioManager.Instance?.ResumeSecondaryBgms();

        StartNewDay();
    }

    /// <summary>
    /// 理智值归零回调
    /// </summary>
    private void OnSanityDepleted()
    {
        if (!_isGameRunning) return;

        if (_showDebugLog)
        {
            Debug.Log("[GameFlow] 理智值归零！游戏立即结束");
        }

        // 立即结束游戏
        EndGame();
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    private void EndGame()
    {
        SetState(GameFlowState.GameEnd);
        _isGameRunning = false;

        float finalSanity = SanityManager.Instance.CurrentSanity;
        GameEnding ending = DetermineEnding(finalSanity);

        HandleModeUnlock(finalSanity);
        GalleryManager.Instance?.MarkEndingUnlocked(ending);

        OnGameEnding?.Invoke(ending);
        OnGameEnd?.Invoke(_currentDay);

        if (_showDebugLog)
            Debug.Log($"[GameFlow] === 游戏结束 === 天数: {_currentDay}, 最终理智值: {finalSanity:F1}, 结局: {ending}");
    }

    /// <summary>
    /// 根据理智值判定结局
    /// 0~60 迷失结局，60~90 回归结局，90~100 好结局
    /// </summary>
    private GameEnding DetermineEnding(float sanity)
    {
        if (sanity >= _goodEndingThreshold) return GameEnding.Good;
        if (sanity >= _returnEndingThreshold) return GameEnding.Return;
        return GameEnding.Lost;
    }

    /// <summary>
    /// 根据结局判定处理模式解锁
    /// </summary>
    private void HandleModeUnlock(float finalSanity)
    {
        if (GameManager.Instance == null || _gameModeConfig == null) return;

        GameEnding ending = DetermineEnding(finalSanity);

        switch (_gameModeConfig.gameMode)
        {
            case GameMode.Tutorial:
                // 教学完成 → 直接解锁普通模式
                GameManager.Instance.MarkTutorialCompleted();
                break;

            case GameMode.Normal:
                break;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器调试：强制开始游戏
    /// </summary>
    [ContextMenu("测试：强制开始游戏")]
    private void DebugStartGame()
    {
        if (_gameModeConfig != null)
        {
            StartGame(_gameModeConfig);
        }
        else
        {
            Debug.LogError("[GameFlow] 请先在Inspector中设置GameModeConfigSO");
        }
    }

    /// <summary>
    /// 编辑器调试：强制结束当天
    /// </summary>
    [ContextMenu("测试：强制结束当天")]
    private void DebugEndDay()
    {
        _remainingCustomers = 0;
        StartDayEnd();
    }

    /// <summary>
    /// 编辑器调试：强制提交咖啡
    /// </summary>
    [ContextMenu("测试：强制提交咖啡")]
    private void DebugSubmitCoffee()
    {
        OnPlayerSubmitCoffee();
    }
#endif
}
