using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.UI;

namespace InnsmouthCafe.Managers
{
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

        [Header("小章鱼NPC")]
        [SerializeField]
        [Tooltip("小章鱼NPC控制器（每天开门前播放开场对话）")]
        private OctopusNPCController _octopusNPC;

        [Header("调试")]
        [SerializeField]
        [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

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
        /// 当天剩余顾客数
        /// </summary>
        private int _remainingCustomers = 0;

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

            // 重置理智值
            SanityManager.Instance.ResetSanity();

            // 清空结算历史
            DaySettlementManager.Instance.ClearHistory();

            OnGameStart?.Invoke();

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
                CustomerManager.Instance.OnCustomerLeft    += OnCustomerLeft;
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
                _customerDisplayUI.OnExitAnimationComplete  += OnCustomerExitAnimationComplete;
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
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeEvents()
        {
            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerSpawned -= OnCustomerSpawned;
                CustomerManager.Instance.OnCustomerLeft    -= OnCustomerLeft;
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
                _customerDisplayUI.OnExitAnimationComplete  -= OnCustomerExitAnimationComplete;
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

            // 生成当天顾客队列
            CustomerManager.Instance.GenerateTodayQueue(dayConfig, _gameModeConfig.gameMode);
            _remainingCustomers = dayConfig.baseCustomerCount + dayConfig.extraCustomers.Count;
            if (dayConfig.fixedCustomers.Count > 0)
            {
                _remainingCustomers = dayConfig.fixedCustomers.Count + dayConfig.extraCustomers.Count;
            }

            // 切换到吧台视图
            ViewSwitchManager.Instance.ShowView(GameViewType.Bar);
            ViewSwitchManager.Instance.SetCanSwitch(false);

            OnDayStart?.Invoke(_currentDay);
            TutorialEventBus.Publish("DayStart");

            if (_showDebugLog)
            {
                Debug.Log($"[GameFlow] === 第 {_currentDay} 天开始 === 顾客数: {_remainingCustomers}");
            }

            // 小章鱼开场对话，说完后再生成第一位顾客
            if (_octopusNPC != null)
            {
                bool isTutorial = _gameModeConfig.gameMode == GameMode.Tutorial;
                _octopusNPC.PlayDailyOpening(isTutorial, dayConfig, SpawnNextCustomer);
            }
            else
            {
                SpawnNextCustomer();
            }
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

            // 播放客人进店音效
            AudioManager.Instance?.PlaySfx(SoundId.CustomerEnter);

            ActionLogBus.Log($"客人进店");

            // 进入顾客流程时，先禁止切换，直到订单确认后再开启
            ViewSwitchManager.Instance.SetCanSwitch(false);

            // 先强制清空残留对话，再由入场动画完成事件触发进店对白
            DialogueUIManager.Instance?.ForceClearDialogue();

            // 没有动画组件时，直接进入进店对白
            if (_customerDisplayUI == null)
            {
                StartCustomerEnterDialogue(customer);
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
                StartCustomerEnterDialogue(customer);
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

                _currentOrderData = _currentOrderSO.ToData();
                StartWaitingForCraft();
            });
        }

        /// <summary>
        /// 进入等待制作阶段
        /// </summary>
        private void StartWaitingForCraft()
        {
            SetState(GameFlowState.WaitingForCraft);

            // 顾客开始等待
            CustomerManager.Instance.StartWaiting();

            // 允许视图切换（玩家可以去制作界面）
            ViewSwitchManager.Instance.SetCanSwitch(true);

            // 初始化制作系统
            CoffeeCraftManager.Instance.StartNewCraft(_currentOrderData);

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

            // 提交后立即隐藏并重置小票
            if (_orderTicketController != null)
            {
                _orderTicketController.ResetTicket();
            }
            else if (_showDebugLog)
            {
                Debug.LogWarning("[GameFlow] 未找到 OrderTicketController，无法自动隐藏小票");
            }

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
            SetState(GameFlowState.Scoring);

            // 提交订单
            OrderManager.Instance.SubmitOrder(_currentOrderSO, coffeeData);

            // 计算评分
            CoffeeScoringData scoringData = ScoringManager.Instance.CalculateScore(
                _currentOrderSO, coffeeData, _gameModeConfig.gameMode
            );

            if (_showDebugLog)
            {
                Debug.Log($"[GameFlow] 评分完成: {scoringData.finalScore}分, 等级: {scoringData.qualityLevel}, 反馈: {scoringData.feedbackLevel}");
            }

            // 记录统计
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
        /// 顾客反馈阶段
        /// </summary>
        private void StartCustomerFeedback(CoffeeScoringData scoringData)
        {
            SetState(GameFlowState.CustomerFeedback);

            _currentScoringData = scoringData;

            int cappedLevel = CustomerManager.Instance.FinishOrderAndShowFeedback(scoringData.feedbackLevel);

            DialogueUIManager.Instance?.ForceClearDialogue();

            string feedbackText = CustomerManager.Instance.GetRandomFeedbackText(cappedLevel);

            if (string.IsNullOrEmpty(feedbackText))
            {
                TryGrantCollectibleThenLeave();
                return;
            }

            DialogueUIManager.Instance.ShowDialogue(feedbackText, () =>
            {
                if (cappedLevel >= 2)
                {
                    CustomerManager.Instance.SetHappy();
                }

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
            if (CollectibleManager.Instance != null && _currentScoringData != null)
            {
                var customer = CustomerManager.Instance?.CurrentCustomer;
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
            _currentOrderSO   = null;
            _currentOrderData = null;
            _currentScoringData = null;

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
            if (sanity >= 90f) return GameEnding.Good;
            if (sanity >= 60f) return GameEnding.Return;
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
                    GameManager.Instance.MarkTutorialCompleted();
                    break;

                case GameMode.Beginner:
                    // 回归或好结局 → 解锁普通模式
                    if (ending == GameEnding.Return || ending == GameEnding.Good)
                        GameManager.Instance.UnlockMode(GameMode.Normal);
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
}
