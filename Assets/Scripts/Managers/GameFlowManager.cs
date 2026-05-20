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
        /// 游戏结束事件
        /// </summary>
        public event Action<int> OnGameEnd;

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

            // 如果Inspector中配置了GameModeConfig，延迟自动开始（测试用）
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

            if (_showDebugLog)
            {
                Debug.Log($"[GameFlow] === 第 {_currentDay} 天开始 === 顾客数: {_remainingCustomers}");
            }

            // 生成第一位顾客
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
            {
                Debug.Log($"[GameFlow] 顾客到达: {customer.customerName}");
            }

            // 进入对话阶段
            StartCustomerDialogue(customer);
        }

        /// <summary>
        /// 开始顾客对话（点单）
        /// </summary>
        private void StartCustomerDialogue(CustomerSO customer)
        {
            SetState(GameFlowState.CustomerTalking);

            // 生成订单
            _currentOrderSO = OrderManager.Instance.GenerateOrderForCustomer(customer);
            _currentOrderData = _currentOrderSO.ToData();

            // 获取点单对话
            string orderDialogue = OrderManager.Instance.GetRandomOrderDialogue(_currentOrderSO);

            // 顾客开始说话
            CustomerManager.Instance.StartTalking();

            // 显示对话气泡
            DialogueUIManager.Instance.ShowDialogue(orderDialogue, () =>
            {
                // 对话完成，进入等待制作阶段
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

            // 顾客显示反馈表情/动画
            CustomerManager.Instance.FinishOrderAndShowFeedback(scoringData.feedbackLevel);

            // 显示反馈对话（从CustomerManager获取反馈文本）
            string feedbackText = CustomerManager.Instance.GetRandomFeedbackText(scoringData.feedbackLevel);

            if (!string.IsNullOrEmpty(feedbackText))
            {
                DialogueUIManager.Instance.ShowDialogue(feedbackText, () =>
                {
                    // 反馈对话完成，顾客离开
                    StartCustomerLeaving();
                });
            }
            else
            {
                // 没有反馈文本，直接离开
                StartCustomerLeaving();
            }
        }

        /// <summary>
        /// 顾客离开阶段
        /// </summary>
        private void StartCustomerLeaving()
        {
            SetState(GameFlowState.CustomerLeaving);
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
            {
                Debug.Log($"[GameFlow] 顾客已离开，剩余: {_remainingCustomers}");
            }

            // 重置订单
            OrderManager.Instance.ResetOrder();
            _currentOrderSO = null;
            _currentOrderData = null;

            // 检查是否还有顾客
            if (_remainingCustomers > 0)
            {
                // 还有顾客，延迟一小段时间后生成下一位
                StartCoroutine(DelayedSpawnNext());
            }
            else
            {
                // 当天顾客全部完成，进入日结算
                StartDayEnd();
            }
        }

        /// <summary>
        /// 延迟生成下一位顾客
        /// </summary>
        private IEnumerator DelayedSpawnNext()
        {
            yield return new WaitForSeconds(1f);
            SpawnNextCustomer();
        }

        /// <summary>
        /// 当天结束，进入日结算
        /// </summary>
        private void StartDayEnd()
        {
            SetState(GameFlowState.DayEnd);

            if (_showDebugLog)
            {
                Debug.Log($"[GameFlow] === 第 {_currentDay} 天结束 ===");
            }

            // 收集当天结算数据
            DaySettlementData settlementData = CollectDaySettlementData();

            // 显示结算界面
            DaySettlementManager.Instance.ShowSettlement(settlementData, () =>
            {
                // 结算回调：检查游戏是否结束
                CheckGameEnd();
            });
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
        /// 检查游戏是否结束
        /// </summary>
        private void CheckGameEnd()
        {
            if (_currentDay >= _gameModeConfig.totalDays)
            {
                // 达到总天数，游戏结束
                EndGame();
            }
            else
            {
                // 还有天数，等待日结算的下一天事件
                // （由DaySettlementManager的OnNextDayStart触发）
            }
        }

        /// <summary>
        /// 日结算管理器触发的下一天事件
        /// </summary>
        private void OnNextDayFromSettlement(int nextDay)
        {
            if (!_isGameRunning) return;

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

            OnGameEnd?.Invoke(_currentDay);

            if (_showDebugLog)
            {
                float finalSanity = SanityManager.Instance.CurrentSanity;
                Debug.Log($"[GameFlow] === 游戏结束 === 天数: {_currentDay}, 最终理智值: {finalSanity:F1}");
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
