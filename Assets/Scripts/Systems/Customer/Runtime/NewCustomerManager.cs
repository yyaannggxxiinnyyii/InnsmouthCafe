using System;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Business;
using InnsmouthCafe.Scoring;
using InnsmouthCafe.Patience;
using InnsmouthCafe.Persistence;
using InnsmouthCafe.Progression;
using InnsmouthCafe.Shop;
using InnsmouthCafe.CoffeeCraft;
using InnsmouthCafe.Explore;

namespace InnsmouthCafe.Customer
{
    /// <summary>
    /// 新顾客管理器（多顾客机制）。
    /// 支持多个顾客同时在场等待，按队列顺序服务。
    /// 使用 Singleton 自动创建实例，无需手动添加到场景。
    /// </summary>
    public class NewCustomerManager : Singleton<NewCustomerManager>
    {
        [Header("顾客队列配置")]
        [Tooltip("顾客生成间隔（秒），从店铺等级读取")]
        [SerializeField] private float _customerSpawnInterval = 100f;

        [Header("调试信息 - 只读")]
        [Tooltip("当前在场的所有顾客实例")]
        [SerializeField] private List<CustomerInstance> _activeCustomers = new List<CustomerInstance>();

        [Tooltip("当前正在服务的顾客索引")]
        [SerializeField] private int _currentServingIndex = 0;

        [Tooltip("今日队列已生成到第几位")]
        [SerializeField] private int _todayQueueIndex = 0;

        [Tooltip("本晚已提出的订单数")]
        [SerializeField] private int _ordersGeneratedTonight = 0;

        [Tooltip("今晚已提交并完成的订单数")]
        [SerializeField] private int _ordersCompletedTonight = 0;

        [Tooltip("今日顾客队列（从配置生成）")]
        [SerializeField] private List<CustomerSO> _todayQueue = new List<CustomerSO>();

        [Header("测试行为")]
        [Tooltip("启用后自动接受或拒绝订单；正式场景应关闭")]
        [SerializeField] private bool _autoAcceptOrderForTest;

        /// <summary>
        /// 下一位顾客的生成时间（Time.time）。
        /// </summary>
        private float _nextSpawnTime;

        /// <summary>
        /// 营业阶段是否激活。
        /// </summary>
        private bool _isBusinessPhaseActive;
        private bool _isSubscribedToCoffeeCraft;

        /// <summary>
        /// 当前在场的所有顾客。
        /// </summary>
        public IReadOnlyList<CustomerInstance> ActiveCustomers => _activeCustomers;

        /// <summary>
        /// 当前正在服务的顾客实例。
        /// </summary>
        public CustomerInstance CurrentServingCustomer =>
            _currentServingIndex < _activeCustomers.Count ? _activeCustomers[_currentServingIndex] : null;

        /// <summary>
        /// 当前正在服务的顾客索引。
        /// </summary>
        public int CurrentServingIndex => _currentServingIndex;

        /// <summary>
        /// 本晚已提出的订单数。
        /// </summary>
        public int OrdersGeneratedTonight => _ordersGeneratedTonight;

        /// <summary>
        /// 今晚已提交完成的订单数。
        /// </summary>
        public int OrdersCompletedTonight => _ordersCompletedTonight;

        /// <summary>
        /// 当前正在处理的顾客订单会话。
        /// </summary>
        public CustomerOrderSessionData CurrentOrderSession => CurrentServingCustomer?.orderSession;

        /// <summary>
        /// 顾客生成事件（顾客进店时触发）。
        /// </summary>
        public event Action<CustomerInstance> OnCustomerSpawned;

        /// <summary>
        /// 顾客接受订单事件。
        /// </summary>
        public event Action<OrderSO> OnOrderAccepted;

        /// <summary>
        /// 顾客接受订单后生成的小票会话事件。
        /// </summary>
        public event Action<CustomerOrderSessionData> OnOrderSessionAccepted;

        /// <summary>
        /// 当前订单会话中的小票状态发生变化时触发。
        /// </summary>
        public event Action<CustomerOrderSessionData> OnOrderSessionChanged;

        /// <summary>
        /// 玩家切换当前要制作的订单时触发。
        /// </summary>
        public event Action<OrderSO> OnOrderSelected;

        /// <summary>
        /// 顾客状态变化事件。
        /// </summary>
        public event Action<CustomerInstance, CustomerState> OnCustomerStateChanged;

        /// <summary>
        /// 顾客离开事件。
        /// </summary>
        public event Action<CustomerInstance> OnCustomerLeft;

        /// <summary>
        /// 当前服务顾客切换事件。
        /// </summary>
        public event Action<CustomerInstance> OnCurrentServingCustomerChanged;

        private void OnEnable()
        {
            SubscribeToCoffeeCraft();

            // 监听耐心值耗尽事件
            if (CustomerPatienceManager.Instance != null)
            {
                CustomerPatienceManager.Instance.OnPatienceExhausted += HandlePatienceExhausted;
            }
        }

        private void OnDisable()
        {
            if (_isSubscribedToCoffeeCraft && NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCoffeeSubmitted -= HandleOrderSubmitted;
                _isSubscribedToCoffeeCraft = false;
            }

            if (CustomerPatienceManager.Instance != null)
            {
                CustomerPatienceManager.Instance.OnPatienceExhausted -= HandlePatienceExhausted;
            }
        }

        private void Update()
        {
            SubscribeToCoffeeCraft();

            if (!_isBusinessPhaseActive)
            {
                return;
            }

            // 委托给 CustomerPatienceManager 更新所有顾客耐心值
            CustomerPatienceManager.Instance.UpdateAllCustomersPatience(_activeCustomers, Time.deltaTime);

            // 自动生成新顾客
            TrySpawnNextCustomerAuto();
        }

        /// <summary>
        /// 绑定新咖啡制作管理器的提交事件，兼容管理器运行时创建顺序。
        /// </summary>
        private void SubscribeToCoffeeCraft()
        {
            if (_isSubscribedToCoffeeCraft || NewCoffeeCraftManager.Instance == null)
            {
                return;
            }

            NewCoffeeCraftManager.Instance.OnCoffeeSubmitted += HandleOrderSubmitted;
            _isSubscribedToCoffeeCraft = true;
        }

        /// <summary>
        /// 根据当前地图和已进入区域生成本晚顾客队列。
        /// </summary>
        public void GenerateTodayQueue()
        {
            _todayQueue.Clear();
            _todayQueueIndex = 0;

            MapConfigSO currentMap = ChapterProgressService.Instance.CurrentMap;
            if (currentMap == null || currentMap.areas == null)
            {
                Debug.LogError("[NewCustomerManager] 当前地图或区域配置为空，无法生成顾客队列");
                return;
            }

            GameSaveData saveData = SaveSlotService.Instance.CurrentSave;
            if (saveData == null || saveData.visitedAreaIds == null)
            {
                Debug.LogWarning("[NewCustomerManager] 当前没有有效存档或已进入区域记录");
                return;
            }

            HashSet<string> visitedAreaIds = new HashSet<string>(saveData.visitedAreaIds);
            HashSet<CustomerSO> availableCustomers = new HashSet<CustomerSO>();

            foreach (AreaConfigSO area in currentMap.areas)
            {
                if (area == null || !visitedAreaIds.Contains(area.AreaId)
                    || area.NormalCustomers == null)
                {
                    continue;
                }

                foreach (CustomerSO customer in area.NormalCustomers)
                {
                    if (customer != null)
                    {
                        availableCustomers.Add(customer);
                    }
                }
            }

            _todayQueue.AddRange(availableCustomers);
            ShuffleQueue(_todayQueue);

            int maxCustomersTonight = ShopUpgradeManager.Instance != null
                ? ShopUpgradeManager.Instance.GetMaxOrdersPerNight()
                : _todayQueue.Count;
            if (_todayQueue.Count > maxCustomersTonight)
            {
                _todayQueue.RemoveRange(
                    maxCustomersTonight,
                    _todayQueue.Count - maxCustomersTonight);
            }

            Debug.Log($"[NewCustomerManager] 根据已进入区域生成本晚顾客队列，共 {_todayQueue.Count} 位顾客");
        }

        /// <summary>
        /// 开始营业阶段（启动顾客自动生成）。
        /// </summary>
        public void StartBusinessPhase()
        {
            _isBusinessPhaseActive = true;
            _activeCustomers.Clear();
            _currentServingIndex = 0;
            _ordersGeneratedTonight = 0;
            _ordersCompletedTonight = 0;

            // 从店铺等级读取顾客生成间隔
            if (ShopUpgradeManager.Instance != null)
            {
                _customerSpawnInterval = ShopUpgradeManager.Instance.GetCustomerSpawnInterval();
                Debug.Log($"[NewCustomerManager] 从店铺等级读取顾客生成间隔：{_customerSpawnInterval}秒");
            }

            _nextSpawnTime = Time.time + 5f; // 开店5秒后第一位顾客到达

            Debug.Log("[NewCustomerManager] 开始营业阶段");
        }

        /// <summary>
        /// 结束营业阶段。
        /// </summary>
        public void EndBusinessPhase()
        {
            _isBusinessPhaseActive = false;

            Debug.Log("[NewCustomerManager] 结束营业阶段");
        }

        /// <summary>
        /// 按间隔自动生成下一位顾客。
        /// </summary>
        private void TrySpawnNextCustomerAuto()
        {
            // 检查是否还接受新顾客
            if (!BusinessTimerManager.Instance.IsAcceptingNewCustomers)
            {
                return;
            }

            // 检查是否达到同时等待上限
            int maxWaitingCustomers = ShopUpgradeManager.Instance != null
                ? ShopUpgradeManager.Instance.GetMaxWaitingCustomers()
                : 3;

            int maxOrdersPerNight = ShopUpgradeManager.Instance != null
                ? ShopUpgradeManager.Instance.GetMaxOrdersPerNight()
                : 6;

            if (_ordersGeneratedTonight >= maxOrdersPerNight)
            {
                return;
            }

            if (_activeCustomers.Count >= maxWaitingCustomers)
            {
                return; // 场上顾客已满，不生成新顾客
            }

            // 检查是否到达生成时间
            if (Time.time < _nextSpawnTime)
            {
                return;
            }

            // 检查队列是否还有顾客
            if (_todayQueueIndex >= _todayQueue.Count)
            {
                return;
            }

            // 生成新顾客
            SpawnCustomer(_todayQueue[_todayQueueIndex]);
            _todayQueueIndex++;

            // 计算下一位顾客的生成时间
            _nextSpawnTime = Time.time + _customerSpawnInterval;
        }

        /// <summary>
        /// 生成一位顾客。
        /// </summary>
        /// <param name="customerSO">顾客配置。</param>
        private void SpawnCustomer(CustomerSO customerSO)
        {
            if (customerSO == null)
            {
                Debug.LogError("[NewCustomerManager] 顾客配置为空，无法生成");
                return;
            }

            var instance = new CustomerInstance
            {
                customerSO = customerSO,
                currentState = CustomerState.Entering,
                maxPatience = customerSO.maxPatienceValue,
                currentPatience = customerSO.maxPatienceValue,
                basePatienceDrain = customerSO.basePatienceDrainRate,
                enterTime = Time.time,
                remainingOrderAttempts = customerSO.maxOrderRejections,
                rejectionCount = 0,
                accumulatedRejectionPenalty = 0f
            };

            _activeCustomers.Add(instance);

            ChangeCustomerState(instance, CustomerState.Queuing);

            OnCustomerSpawned?.Invoke(instance);

            Debug.Log($"[NewCustomerManager] 顾客进店：{customerSO.customerName}，当前在场顾客数：{_activeCustomers.Count}");

            // 首位顾客进入队列后自动开始服务，后续顾客由前一位离场时推进。
            if (CurrentServingCustomer == instance)
            {
                Invoke(nameof(StartServingCurrentCustomer), 0.5f);
            }
        }

        /// <summary>
        /// 开始服务当前顾客（进入思考状态，准备生成订单）。
        /// </summary>
        public void StartServingCurrentCustomer()
        {
            var customer = CurrentServingCustomer;
            if (customer == null)
            {
                Debug.LogWarning("[NewCustomerManager] 没有可服务的顾客");
                return;
            }

            // 进入思考状态
            ChangeCustomerState(customer, CustomerState.Thinking);

            Debug.Log($"[NewCustomerManager] 开始服务顾客：{customer.customerSO.customerName}，顾客开始思考订单...");

            // 延迟1-2秒后提出订单（模拟思考时间）
            float thinkingTime = UnityEngine.Random.Range(1f, 2f);
            Invoke(nameof(ProposeOrderForCurrentCustomer), thinkingTime);
        }

        /// <summary>
        /// 为当前顾客提出订单。
        /// </summary>
        private void ProposeOrderForCurrentCustomer()
        {
            var customer = CurrentServingCustomer;
            if (customer == null || customer.currentState != CustomerState.Thinking)
            {
                return;
            }

            int maxOrdersPerNight = ShopUpgradeManager.Instance != null
                ? ShopUpgradeManager.Instance.GetMaxOrdersPerNight()
                : 6;

            if (_ordersGeneratedTonight >= maxOrdersPerNight)
            {
                Debug.Log($"[NewCustomerManager] 本晚订单数已达上限：{maxOrdersPerNight}，顾客离店");
                ChangeCustomerState(customer, CustomerState.Leaving);
                Invoke(nameof(CurrentCustomerLeave), 1f);
                return;
            }

            // 生成订单（智能概率）
            CustomerOrderSessionData session = GenerateOrderSession(customer);

            if (session == null || session.SelectedSlot == null)
            {
                Debug.LogError($"[NewCustomerManager] 无法为顾客 {customer.customerSO.customerName} 生成任何订单（订单池为空或配置错误）");
                // 顾客失望离开
                ChangeCustomerState(customer, CustomerState.Angry);
                Invoke(nameof(CurrentCustomerLeave), 1f);
                return;
            }

            customer.proposedOrderSession = session;
            customer.proposedOrder = session.SelectedSlot.orderSO;

            // 切换到订单已提出状态
            ChangeCustomerState(customer, CustomerState.OrderProposed);

            Debug.Log($"[NewCustomerManager] 顾客 {customer.customerSO.customerName} 提出订单：{customer.proposedOrder.orderName}");

            if (_autoAcceptOrderForTest)
            {
                Invoke(nameof(AutoAcceptOrderForTest), 0.5f);
            }
        }

        /// <summary>
        /// 自动接受订单（测试用，后续由玩家UI触发）。
        /// </summary>
        private void AutoAcceptOrderForTest()
        {
            var customer = CurrentServingCustomer;
            if (customer == null || customer.currentState != CustomerState.OrderProposed)
            {
                return;
            }

            // 【测试】随机决定接受或拒绝
            bool accept = UnityEngine.Random.value > 0.3f; // 70%接受，30%拒绝

            if (accept)
            {
                AcceptOrder();
            }
            else
            {
                RejectOrder();
            }
        }

        /// <summary>
        /// 玩家接受订单（由UI按钮调用）。
        /// </summary>
        public void AcceptOrder()
        {
            var customer = CurrentServingCustomer;
            if (customer == null || customer.currentState != CustomerState.OrderProposed)
            {
                Debug.LogWarning("[NewCustomerManager] 当前没有等待接受的订单");
                return;
            }

            int maxOrdersPerNight = ShopUpgradeManager.Instance != null
                ? ShopUpgradeManager.Instance.GetMaxOrdersPerNight()
                : 6;

            if (_ordersGeneratedTonight >= maxOrdersPerNight)
            {
                Debug.Log($"[NewCustomerManager] 本晚订单数已达上限：{maxOrdersPerNight}，无法接受新订单");
                ChangeCustomerState(customer, CustomerState.Leaving);
                Invoke(nameof(CurrentCustomerLeave), 1f);
                return;
            }

            // 将提出的订单设为当前订单
            customer.currentOrder = customer.proposedOrder;
            customer.proposedOrder = null;
            customer.orderSession = customer.proposedOrderSession;
            customer.proposedOrderSession = null;
            _ordersGeneratedTonight++;

            // 切换到等待制作状态
            ChangeCustomerState(customer, CustomerState.Waiting);

            OnOrderAccepted?.Invoke(customer.currentOrder);
            OnOrderSessionAccepted?.Invoke(customer.orderSession);

            Debug.Log($"[NewCustomerManager] 玩家接受订单：{customer.currentOrder.orderName}，开始制作");
        }

        /// <summary>
        /// 选择当前顾客的一张未完成小票作为制作目标。
        /// </summary>
        /// <param name="slotId">需要选择的订单槽ID。</param>
        /// <returns>选择成功返回 true。</returns>
        public bool TrySelectOrderSlot(string slotId)
        {
            CustomerInstance customer = CurrentServingCustomer;
            CustomerOrderSessionData session = customer?.orderSession;
            if (customer == null || session == null || customer.currentState != CustomerState.Waiting)
            {
                return false;
            }

            CustomerOrderSlotData slot = session.GetSlot(slotId);
            if (slot == null || !slot.IsWaitingForSubmission)
            {
                return false;
            }

            session.TrySelectSlot(slotId);
            customer.currentOrder = slot.orderSO;
            OnOrderSelected?.Invoke(customer.currentOrder);
            return true;
        }

        /// <summary>
        /// 玩家拒绝订单（由UI按钮调用）。
        /// </summary>
        public void RejectOrder()
        {
            var customer = CurrentServingCustomer;
            if (customer == null || customer.currentState != CustomerState.OrderProposed)
            {
                Debug.LogWarning("[NewCustomerManager] 当前没有等待拒绝的订单");
                return;
            }

            customer.rejectionCount++;
            customer.remainingOrderAttempts--;
            customer.accumulatedRejectionPenalty += customer.customerSO.rejectionPenaltyStars;

            Debug.Log($"[NewCustomerManager] 玩家拒绝订单：{customer.proposedOrder.orderName}，剩余机会：{customer.remainingOrderAttempts}，累计惩罚：-{customer.accumulatedRejectionPenalty}星");

            customer.proposedOrder = null;

            // 检查是否还有机会
            if (customer.remainingOrderAttempts > 0)
            {
                // 还有机会，重新思考
                Debug.Log($"[NewCustomerManager] 顾客 {customer.customerSO.customerName} 重新思考订单...");
                ChangeCustomerState(customer, CustomerState.Thinking);

                // 耐心值降低10%（拒绝惩罚）
                customer.currentPatience *= 0.9f;

                // 延迟1秒后重新提出订单
                Invoke(nameof(ProposeOrderForCurrentCustomer), 1f);
            }
            else
            {
                // 机会用尽，顾客愤怒离开
                Debug.Log($"[NewCustomerManager] 顾客 {customer.customerSO.customerName} 被拒绝{customer.rejectionCount}次，愤怒离开：\"店里什么都没有还开什么咖啡店！\"");
                ChangeCustomerState(customer, CustomerState.Angry);

                // 直接给差评
                customer.feedbackLevel = 0;

                // 延迟1秒后离开
                Invoke(nameof(CurrentCustomerLeave), 1f);
            }
        }

        /// <summary>
        /// 使用智能策略生成订单。
        /// </summary>
        /// <param name="customer">顾客实例。</param>
        /// <returns>生成的订单，如果无法生成则返回null。</returns>
        private OrderSO GenerateOrderWithStrategy(CustomerInstance customer)
        {
            // 1. 获取顾客的所有可能订单
            List<OrderSO> allPossibleOrders = GetAllPossibleOrders(customer.customerSO);

            if (allPossibleOrders == null || allPossibleOrders.Count == 0)
            {
                Debug.LogWarning($"[NewCustomerManager] 顾客 {customer.customerSO.customerName} 没有配置任何订单池");
                return null;
            }

            // 2. 分类：可完成 vs 不可完成
            List<OrderSO> fulfillableOrders = new List<OrderSO>();
            List<OrderSO> impossibleOrders = new List<OrderSO>();

            foreach (var order in allPossibleOrders)
            {
                if (CanFulfillOrder(order))
                {
                    fulfillableOrders.Add(order);
                }
                else
                {
                    impossibleOrders.Add(order);
                }
            }

            Debug.Log($"[NewCustomerManager] 订单分类：可完成 {fulfillableOrders.Count} 个，不可完成 {impossibleOrders.Count} 个");

            // 3. 根据配置的概率决定生成策略
            float roll = UnityEngine.Random.value;

            if (roll < customer.customerSO.impossibleOrderProbability)
            {
                // 挑战模式：优先从不可完成订单中选
                if (impossibleOrders.Count > 0)
                {
                    OrderSO order = impossibleOrders[UnityEngine.Random.Range(0, impossibleOrders.Count)];
                    Debug.Log($"[NewCustomerManager] 挑战模式（概率{customer.customerSO.impossibleOrderProbability * 100}%）：生成不可完成订单 {order.orderName}");
                    return order;
                }
                else
                {
                    // 兜底：没有不可完成订单，从可完成中选
                    Debug.Log($"[NewCustomerManager] 挑战模式但没有不可完成订单，改为从可完成订单中选");
                }
            }

            // 友好模式或兜底：从可完成订单中选
            if (fulfillableOrders.Count > 0)
            {
                OrderSO order = fulfillableOrders[UnityEngine.Random.Range(0, fulfillableOrders.Count)];
                Debug.Log($"[NewCustomerManager] 友好模式：生成可完成订单 {order.orderName}");
                return order;
            }
            else
            {
                // 兜底：所有订单都不可完成，随机选一个
                if (impossibleOrders.Count > 0)
                {
                    OrderSO order = impossibleOrders[UnityEngine.Random.Range(0, impossibleOrders.Count)];
                    Debug.LogWarning($"[NewCustomerManager] 所有订单都不可完成，随机选择 {order.orderName}");
                    return order;
                }
            }

            return null;
        }

        /// <summary>
        /// 为当前顾客生成普通订单或顾客组订单会话。
        /// </summary>
        /// <param name="customer">当前顾客实例。</param>
        /// <returns>生成成功返回订单会话。</returns>
        private CustomerOrderSessionData GenerateOrderSession(CustomerInstance customer)
        {
            if (customer?.customerSO == null)
            {
                return null;
            }

            CustomerOrderSessionData session = new CustomerOrderSessionData();
            session.Initialize($"new-{customer.customerSO.customerId}-{Time.frameCount}", customer.customerSO);

            SpecialCustomerProfileSO profile = customer.customerSO.specialProfile;
            if (profile != null && profile.useCustomerOrderGroup
                && profile.orderParticipants != null && profile.orderParticipants.Count > 0)
            {
                for (int i = 0; i < profile.orderParticipants.Count; i++)
                {
                    CustomerOrderParticipantConfig participant = profile.orderParticipants[i];
                    if (participant == null)
                    {
                        continue;
                    }

                    OrderSO order = GenerateOrderFromPools(
                        customer,
                        participant.orderPoolEntries,
                        $"{customer.customerSO.customerName}-{participant.displayName}");
                    if (order == null)
                    {
                        continue;
                    }

                    string participantName = string.IsNullOrWhiteSpace(participant.displayName)
                        ? $"成员{i + 1}"
                        : participant.displayName;
                    CustomerOrderSlotData slot = new CustomerOrderSlotData();
                    slot.Initialize(
                        $"{session.sessionId}-{i}",
                        participantName,
                        order,
                        participant.ordererAvatarSprite);
                    session.AddSlot(slot);
                }
            }
            else
            {
                OrderSO order = GenerateOrderWithStrategy(customer);
                if (order != null)
                {
                    CustomerOrderSlotData slot = new CustomerOrderSlotData();
                    slot.Initialize(
                        $"{session.sessionId}-0",
                        customer.customerSO.customerName,
                        order,
                        customer.customerSO.ordererAvatarSprite);
                    session.AddSlot(slot);
                }
            }

            return session.SlotCount > 0 ? session : null;
        }

        /// <summary>
        /// 从指定订单池按当前顾客的订单策略生成一张订单。
        /// </summary>
        /// <param name="customer">当前顾客实例。</param>
        /// <param name="poolEntries">候选订单池。</param>
        /// <param name="ownerName">订单归属名称。</param>
        /// <returns>生成成功返回订单。</returns>
        private OrderSO GenerateOrderFromPools(
            CustomerInstance customer,
            List<CustomerOrderPoolEntry> poolEntries,
            string ownerName)
        {
            List<OrderSO> allOrders = GetAllPossibleOrders(poolEntries);
            if (allOrders.Count == 0)
            {
                Debug.LogWarning($"[NewCustomerManager] {ownerName} 没有配置有效订单池");
                return null;
            }

            List<OrderSO> fulfillableOrders = new List<OrderSO>();
            List<OrderSO> impossibleOrders = new List<OrderSO>();
            foreach (OrderSO order in allOrders)
            {
                if (CanFulfillOrder(order))
                {
                    fulfillableOrders.Add(order);
                }
                else
                {
                    impossibleOrders.Add(order);
                }
            }

            bool chooseImpossible = UnityEngine.Random.value < customer.customerSO.impossibleOrderProbability;
            List<OrderSO> candidates = chooseImpossible && impossibleOrders.Count > 0
                ? impossibleOrders
                : fulfillableOrders.Count > 0 ? fulfillableOrders : impossibleOrders;
            return candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : null;
        }

        /// <summary>
        /// 获取顾客的所有可能订单（从所有订单池中收集）。
        /// </summary>
        /// <param name="customerSO">顾客配置。</param>
        /// <returns>所有可能的订单列表。</returns>
        private List<OrderSO> GetAllPossibleOrders(CustomerSO customerSO)
        {
            List<OrderSO> allOrders = new List<OrderSO>();
            if (customerSO == null)
            {
                return allOrders;
            }

            GameSaveData saveData = SaveSlotService.Instance.CurrentSave;
            HashSet<string> visitedAreaIds = saveData?.visitedAreaIds != null
                ? new HashSet<string>(saveData.visitedAreaIds)
                : new HashSet<string>();

            if (customerSO.belongAreas != null)
            {
                foreach (AreaConfigSO area in customerSO.belongAreas)
                {
                    if (area == null || !visitedAreaIds.Contains(area.AreaId)
                        || area.OrderPool == null || area.OrderPool.orders == null)
                    {
                        continue;
                    }

                    AddOrdersWithoutDuplicates(allOrders, area.OrderPool.orders);
                }
            }

            AddOrdersWithoutDuplicates(allOrders, customerSO.extraOrders);
            AddOrdersFromPoolEntries(allOrders, customerSO.extraOrderPoolEntries);
            return allOrders;
        }

        /// <summary>
        /// 将订单加入列表并去除重复引用。
        /// </summary>
        private void AddOrdersWithoutDuplicates(List<OrderSO> target, List<OrderSO> orders)
        {
            if (orders == null)
            {
                return;
            }

            foreach (OrderSO order in orders)
            {
                if (order != null && !target.Contains(order))
                {
                    target.Add(order);
                }
            }
        }

        /// <summary>
        /// 从顾客额外订单池中收集订单并去除重复引用。
        /// </summary>
        private void AddOrdersFromPoolEntries(
            List<OrderSO> target,
            List<CustomerOrderPoolEntry> poolEntries)
        {
            if (poolEntries == null)
            {
                return;
            }

            foreach (CustomerOrderPoolEntry poolEntry in poolEntries)
            {
                if (poolEntry == null || poolEntry.orderPool == null
                    || poolEntry.weight <= 0)
                {
                    continue;
                }

                AddOrdersWithoutDuplicates(target, poolEntry.orderPool.orders);
            }
        }

        /// <summary>
        /// 随机打乱本晚候选顾客队列。
        /// </summary>
        private void ShuffleQueue(List<CustomerSO> queue)
        {
            for (int i = queue.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                CustomerSO customer = queue[i];
                queue[i] = queue[swapIndex];
                queue[swapIndex] = customer;
            }
        }

        /// <summary>
        /// 收集指定订单池中的所有订单。
        /// </summary>
        /// <param name="poolEntries">候选订单池。</param>
        /// <returns>去重后的订单列表。</returns>
        private List<OrderSO> GetAllPossibleOrders(List<CustomerOrderPoolEntry> poolEntries)
        {
            List<OrderSO> allOrders = new List<OrderSO>();
            if (poolEntries == null || poolEntries.Count == 0)
            {
                return allOrders;
            }

            foreach (CustomerOrderPoolEntry poolEntry in poolEntries)
            {
                if (poolEntry == null || poolEntry.orderPool == null || poolEntry.weight <= 0)
                {
                    continue;
                }

                if (poolEntry.orderPool.orders != null)
                {
                    foreach (OrderSO order in poolEntry.orderPool.orders)
                    {
                        if (order != null && !allOrders.Contains(order))
                        {
                            allOrders.Add(order);
                        }
                    }
                }
            }

            return allOrders;
        }

        /// <summary>
        /// 生成可完成的订单（材料足够）。
        /// 【已废弃】改用 GenerateOrderWithStrategy。
        /// </summary>
        [System.Obsolete("Use GenerateOrderWithStrategy instead")]
        private OrderSO GenerateFulfillableOrder(CustomerSO customerSO)
        {
            // 保留方法避免编译错误，但不再使用
            return null;
        }

        /// <summary>
        /// 检查订单是否可完成（材料足够）。
        /// </summary>
        /// <param name="order">订单配置。</param>
        /// <returns>true表示材料足够，false表示材料不足。</returns>
        private bool CanFulfillOrder(OrderSO order)
        {
            if (order == null)
            {
                return false;
            }

            if (IngredientInventoryService.Instance == null)
            {
                // 库存服务不存在，默认返回 true（测试模式）
                Debug.LogWarning("[NewCustomerManager] IngredientInventoryService 不存在，无法检查材料");
                return true;
            }

            // 检查辅助液需求
            if (order.liquidRequirements != null)
            {
                foreach (var liquidReq in order.liquidRequirements)
                {
                    if (liquidReq?.liquid == null)
                    {
                        continue;
                    }

                    string liquidId = liquidReq.liquid.liquidId;
                    int requiredAmount = liquidReq.targetVolume;

                    int availableAmount = IngredientInventoryService.Instance.GetAmount(IngredientKind.Liquid, liquidId);

                    if (availableAmount < requiredAmount)
                    {
                        Debug.Log($"[NewCustomerManager] 材料不足：{liquidReq.liquid.liquidName}，需要 {requiredAmount}ml，库存 {availableAmount}ml");
                        return false;
                    }
                }
            }

            // 检查小料需求
            if (order.toppingRequirement?.requiredToppings != null)
            {
                foreach (var topping in order.toppingRequirement.requiredToppings)
                {
                    if (topping == null)
                    {
                        continue;
                    }

                    string toppingId = topping.toppingId;
                    int requiredAmount = 1; // 小料通常需要1份

                    int availableAmount = IngredientInventoryService.Instance.GetAmount(IngredientKind.Topping, toppingId);

                    if (availableAmount < requiredAmount)
                    {
                        Debug.Log($"[NewCustomerManager] 材料不足：{topping.toppingName}，需要 {requiredAmount}份，库存 {availableAmount}份");
                        return false;
                    }
                }
            }

            // 所有材料检查通过
            return true;
        }

        /// <summary>
        /// 完成当前顾客的订单（切换到 Feedback 状态）。
        /// </summary>
        public void CompleteCurrentCustomerOrder(int feedbackLevel)
        {
            var customer = CurrentServingCustomer;
            if (customer == null)
            {
                Debug.LogWarning("[NewCustomerManager] 没有可完成订单的顾客");
                return;
            }

            customer.feedbackLevel = feedbackLevel;

            ChangeCustomerState(customer, CustomerState.Feedback);

            Debug.Log($"[NewCustomerManager] 完成订单：{customer.customerSO.customerName}，评价档位：{feedbackLevel}");
        }

        /// <summary>
        /// 当前顾客离开（移除并切换到下一位）。
        /// </summary>
        public void CurrentCustomerLeave()
        {
            var customer = CurrentServingCustomer;
            if (customer == null)
            {
                Debug.LogWarning("[NewCustomerManager] 没有可离开的顾客");
                return;
            }

            ChangeCustomerState(customer, CustomerState.Leaving);

            _activeCustomers.RemoveAt(_currentServingIndex);

            OnCustomerLeft?.Invoke(customer);

            // 不增加 _currentServingIndex，因为移除后索引自动对准下一位

            // 通知UI更新当前服务顾客
            OnCurrentServingCustomerChanged?.Invoke(CurrentServingCustomer);

            Debug.Log($"[NewCustomerManager] 顾客离开：{customer.customerSO.customerName}，剩余顾客数：{_activeCustomers.Count}");

            // 自动开始服务下一位顾客
            if (CurrentServingCustomer != null)
            {
                Debug.Log($"[NewCustomerManager] 自动开始服务下一位顾客：{CurrentServingCustomer.customerSO.customerName}");
                Invoke(nameof(StartServingCurrentCustomer), 0.5f); // 延迟0.5秒，让UI有时间更新
            }
            else
            {
                Debug.Log("[NewCustomerManager] 队列中没有更多顾客");
            }
        }

        /// <summary>
        /// 处理耐心值耗尽事件。
        /// </summary>
        /// <param name="customer">耐心耗尽的顾客。</param>
        private void HandlePatienceExhausted(CustomerInstance customer)
        {
            if (customer.currentState == CustomerState.Angry)
            {
                return; // 已经处理过了
            }

            ChangeCustomerState(customer, CustomerState.Angry);

            Debug.Log($"[NewCustomerManager] 顾客 {customer.customerSO.customerName} 耐心耗尽，愤怒离开");

            // 延迟1秒后让顾客离开
            if (customer == CurrentServingCustomer)
            {
                Invoke(nameof(CurrentCustomerLeave), 1f);
            }
        }

        /// <summary>
        /// 处理订单提交事件（玩家提交咖啡）。
        /// </summary>
        /// <param name="order">提交的订单。</param>
        /// <param name="coffee">咖啡成品数据。</param>
        private void HandleOrderSubmitted(OrderSO order, CoffeeData coffee)
        {
            var customer = CurrentServingCustomer;
            CustomerOrderSessionData session = customer?.orderSession;
            if (customer == null || session == null)
            {
                Debug.LogWarning("[NewCustomerManager] 订单提交时没有当前服务顾客");
                return;
            }

            // 检查订单是否匹配
            if (customer.currentOrder != order)
            {
                Debug.LogWarning($"[NewCustomerManager] 订单不匹配：当前顾客订单={customer.currentOrder?.orderName}，提交订单={order.orderName}");
                return;
            }

            CustomerOrderSlotData slot = FindWaitingOrderSlot(session, order);
            if (slot == null || !slot.TrySubmitCoffee(coffee))
            {
                Debug.LogWarning($"[NewCustomerManager] 订单小票不可提交：{order.orderName}");
                return;
            }

            // 计算评分（应用拒绝惩罚）
            float patienceRatio = CustomerPatienceManager.Instance.GetPatienceRatio(customer);
            int beautyScore = 0; // 传入0，让评分系统自动从 DecorationManager 读取

            var result = NewScoringManager.Instance.CalculateScore(
                order,
                coffee,
                patienceRatio,
                beautyScore
            );

            // 应用拒绝惩罚
            result.starRating -= customer.accumulatedRejectionPenalty;
            result.starRating = Mathf.Max(0, result.starRating);

            Debug.Log($"[NewCustomerManager] 订单评分完成：原始{result.starRating + customer.accumulatedRejectionPenalty:F1}星，拒绝惩罚-{customer.accumulatedRejectionPenalty:F1}星，最终{result.starRating:F1}星");


            // 映射星级到反馈档位（0-2）
            int feedbackLevel = MapStarsToFeedbackLevel(result.starRating);

            CoffeeScoringData scoringData = new CoffeeScoringData
            {
                finalScore = result.starRating * 20f,
                qualityLevel = result.starRating >= 4f
                    ? CoffeeQuality.Perfect
                    : result.starRating >= 2.5f
                        ? CoffeeQuality.Acceptable
                        : CoffeeQuality.Terrible,
                feedbackLevel = feedbackLevel,
                scoringDetail = result.feedbackText
            };
            slot.TryCompleteScoring(scoringData);

            _ordersCompletedTonight++;
            customer.currentOrder = null;
            OnOrderSessionChanged?.Invoke(session);

            if (session.IsAllCompleted)
            {
                CompleteCurrentCustomerOrder(feedbackLevel);
                TryGrantCollectibleReward(customer, session);
                Invoke(nameof(CurrentCustomerLeave), 2f);
            }
        }

        /// <summary>
        /// 顾客整场订单全部达成完美时，授予其特殊配置对应的收集物。
        /// 口径与旧流程一致：整场品质取所有订单中的最低档，故需每张订单都是 Perfect。
        /// </summary>
        /// <param name="customer">已完成整场订单的顾客。</param>
        /// <param name="session">该顾客的订单会话。</param>
        private void TryGrantCollectibleReward(CustomerInstance customer, CustomerOrderSessionData session)
        {
            if (CollectibleManager.Instance == null || customer?.customerSO == null ||
                session?.orderSlots == null || session.orderSlots.Count == 0)
            {
                return;
            }

            foreach (CustomerOrderSlotData slot in session.orderSlots)
            {
                if (slot?.scoringData == null || slot.scoringData.qualityLevel != CoffeeQuality.Perfect)
                {
                    return;
                }
            }

            CollectibleSO obtained = CollectibleManager.Instance.TryObtainCollectible(
                customer.customerSO, CoffeeQuality.Perfect);

            if (obtained != null)
            {
                ActionLogBus.Log($"获得新的收集物：{obtained.collectibleName}", Color.cyan);
            }
        }

        /// <summary>
        /// 查找当前订单会话中等待提交的指定订单槽。
        /// </summary>
        /// <param name="session">当前订单会话。</param>
        /// <param name="order">需要查找的订单。</param>
        /// <returns>找到时返回订单槽，否则返回 null。</returns>
        private CustomerOrderSlotData FindWaitingOrderSlot(
            CustomerOrderSessionData session,
            OrderSO order)
        {
            if (session?.orderSlots == null || order == null)
            {
                return null;
            }

            foreach (CustomerOrderSlotData slot in session.orderSlots)
            {
                if (slot != null && slot.orderSO == order && slot.IsWaitingForSubmission)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 将星级映射到反馈档位。
        /// </summary>
        /// <param name="stars">星级（0-5）。</param>
        /// <returns>反馈档位（0=差评，1=一般，2=好评）。</returns>
        private int MapStarsToFeedbackLevel(float stars)
        {
            if (stars >= 4.0f)
                return 2; // 好评

            if (stars >= 2.5f)
                return 1; // 一般

            return 0; // 差评
        }

        /// <summary>
        /// 改变顾客状态。
        /// </summary>
        /// <param name="customer">顾客实例。</param>
        /// <param name="newState">新状态。</param>
        private void ChangeCustomerState(CustomerInstance customer, CustomerState newState)
        {
            customer.currentState = newState;

            OnCustomerStateChanged?.Invoke(customer, newState);
        }

        /// <summary>
        /// 从顾客池中不重复随机抽取指定数量。
        /// 若池子数量不足，抽空后重新放回并继续抽取。
        /// </summary>
        /// <param name="pool">顾客池。</param>
        /// <param name="count">需要抽取的数量。</param>
        /// <returns>抽取的顾客列表。</returns>
        private List<CustomerSO> DrawFromPool(List<CustomerSO> pool, int count)
        {
            var result = new List<CustomerSO>();
            if (pool == null || pool.Count == 0)
            {
                return result;
            }

            var available = new List<CustomerSO>(pool);
            for (int i = 0; i < count; i++)
            {
                if (available.Count == 0)
                {
                    available.AddRange(pool); // 池子抽空后重新放回
                }

                int idx = UnityEngine.Random.Range(0, available.Count);
                result.Add(available[idx]);
                available.RemoveAt(idx);
            }

            return result;
        }
    }

    /// <summary>
    /// 顾客实例（运行时数据）。
    /// 包含顾客配置、当前状态、耐心值等运行时信息。
    /// </summary>
    [System.Serializable]
    public class CustomerInstance
    {
        [Tooltip("顾客配置")]
        public CustomerSO customerSO;

        [Tooltip("当前状态")]
        public CustomerState currentState;

        [Tooltip("当前耐心值")]
        public float currentPatience;

        [Tooltip("最大耐心值（从 CustomerSO.maxPatienceValue 读取）")]
        public float maxPatience;

        [Tooltip("基础耐心降低值（从 CustomerSO.basePatienceDrainRate 读取）")]
        public float basePatienceDrain;

        [Tooltip("进店时间（Time.time）")]
        public float enterTime;

        [Tooltip("反馈评价档位（0=差评，1=一般，2=好评）")]
        public int feedbackLevel;

        [Tooltip("当前订单（生成后保存）")]
        public OrderSO currentOrder;

        [Tooltip("当前顾客已接受的订单会话")]
        public CustomerOrderSessionData orderSession;

        [Tooltip("当前提出的订单（等待玩家选择）")]
        public OrderSO proposedOrder;

        [Tooltip("当前提出但尚未接受的订单会话")]
        public CustomerOrderSessionData proposedOrderSession;

        [Tooltip("剩余订单申请次数")]
        public int remainingOrderAttempts;

        [Tooltip("已拒绝订单次数")]
        public int rejectionCount;

        [Tooltip("因拒绝累计的评分惩罚（星）")]
        public float accumulatedRejectionPenalty;
    }

    /// <summary>
    /// 顾客状态枚举。
    /// </summary>
    public enum CustomerState
    {
        None = 0,
        Entering = 1,    // 进店中
        Queuing = 2,     // 排队中
        Talking = 3,     // 对话中
        Thinking = 4,    // 思考订单中（新增）
        OrderProposed = 5, // 订单已提出，等待玩家选择（新增）
        Waiting = 6,     // 等待制作中
        Feedback = 7,    // 反馈中
        Happy = 8,       // 满意
        Neutral = 9,     // 一般
        Angry = 10,      // 愤怒
        Leaving = 11     // 离开中
    }
}
