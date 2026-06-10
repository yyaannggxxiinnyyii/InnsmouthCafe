using System;
using UnityEngine;
using InnsmouthCafe.Data;

/// <summary>
/// 订单管理器
/// 负责订单生成、订单池选择、避免重复订单
/// </summary>
public class OrderManager : Singleton<OrderManager>
{
    [Header("默认订单池")]
    [SerializeField]
    [Tooltip("兜底订单池（当顾客无可用订单池时使用）")]
    private OrderPoolSO _defaultOrderPool;

    [Header("当前状态")]
    [SerializeField]
    [Tooltip("当前订单")]
    private OrderSO _currentOrder;

    [SerializeField]
    [Tooltip("上一个订单（用于避免重复）")]
    private OrderSO _lastOrder;

    [SerializeField]
    [Tooltip("当前顾客")]
    private CustomerSO _currentCustomer;

    [SerializeField]
    [Tooltip("预生成订单")]
    private OrderSO _preparedOrder;

    [SerializeField]
    [Tooltip("预生成订单对应顾客")]
    private CustomerSO _preparedCustomer;

    /// <summary>
    /// 订单生成事件（订单SO）
    /// </summary>
    public event Action<OrderSO> OnOrderGenerated;

    /// <summary>
    /// 订单提交事件（订单SO，咖啡数据）
    /// </summary>
    public event Action<OrderSO, CoffeeData> OnOrderSubmitted;

    /// <summary>
    /// 当前订单
    /// </summary>
    public OrderSO CurrentOrder => _currentOrder;

    /// <summary>
    /// 当前顾客
    /// </summary>
    public CustomerSO CurrentCustomer => _currentCustomer;

    /// <summary>
    /// 为顾客选择订单池
    /// </summary>
    /// <param name="customer">顾客配置</param>
    /// <returns>选中的订单池，失败返回null</returns>
    private OrderPoolSO SelectOrderPool(CustomerSO customer)
    {
        if (customer == null)
        {
            Debug.LogError("[Order] SelectOrderPool: 顾客配置为空");
            return null;
        }

        if (customer.orderPoolEntries == null || customer.orderPoolEntries.Count == 0)
        {
            Debug.LogWarning($"[Order] 顾客 {customer.customerName} 没有配置订单池，使用默认订单池");
            return GetDefaultOrderPool(customer.customerName);
        }

        // 过滤有效的订单池条目
        var validEntries = new System.Collections.Generic.List<CustomerOrderPoolEntry>();
        int totalWeight = 0;

        foreach (var entry in customer.orderPoolEntries)
        {
            // 检查订单池是否有效
            if (entry.orderPool == null)
            {
                Debug.LogWarning($"[Order] 顾客 {customer.customerName} 的订单池条目中存在空引用");
                continue;
            }

            // 检查权重是否有效
            if (entry.weight <= 0)
            {
                continue;
            }

            // 检查订单池是否有订单
            if (entry.orderPool.orders == null || entry.orderPool.orders.Count == 0)
            {
                Debug.LogWarning($"[Order] 订单池 {entry.orderPool.poolName} 没有订单");
                continue;
            }

            if (!HasFulfillableOrder(entry.orderPool))
            {
                Debug.LogWarning($"[Order] 订单池 {entry.orderPool.poolName} 没有当前已解锁材料可完成的订单");
                continue;
            }

            validEntries.Add(entry);
            totalWeight += entry.weight;
        }

        // 如果没有有效的订单池，使用默认订单池
        if (validEntries.Count == 0 || totalWeight <= 0)
        {
            Debug.LogWarning($"[Order] 顾客 {customer.customerName} 没有有效的订单池，使用默认订单池");
            return GetDefaultOrderPool(customer.customerName);
        }

        // 按权重随机选择订单池
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var entry in validEntries)
        {
            currentWeight += entry.weight;
            if (randomValue < currentWeight)
            {
                Debug.Log($"[Order] 为顾客 {customer.customerName} 选择了订单池 {entry.orderPool.poolName}");
                return entry.orderPool;
            }
        }

        // 理论上不会到这里，但作为保险返回第一个有效订单池
        Debug.LogWarning("[Order] 权重随机选择失败，返回第一个有效订单池");
        return validEntries[0].orderPool;
    }

    /// <summary>
    /// 从订单池中随机一个订单
    /// </summary>
    /// <param name="pool">订单池</param>
    /// <returns>随机的订单，失败返回null</returns>
    private OrderSO GetRandomOrderFromPool(OrderPoolSO pool)
    {
        if (pool == null)
        {
            Debug.LogError("[Order] GetRandomOrderFromPool: 订单池为空");
            return null;
        }

        if (pool.orders == null || pool.orders.Count == 0)
        {
            Debug.LogError($"[Order] 订单池 {pool.poolName} 没有订单");
            return null;
        }

        // 过滤掉空订单和当前材料无法完成的订单
        var validOrders = new System.Collections.Generic.List<OrderSO>();
        foreach (var order in pool.orders)
        {
            if (IsOrderFulfillable(order))
            {
                validOrders.Add(order);
            }
        }

        if (validOrders.Count == 0)
        {
            Debug.LogError($"[Order] 订单池 {pool.poolName} 没有有效订单");
            return null;
        }

        // 如果只有一个订单，直接返回
        if (validOrders.Count == 1)
        {
            Debug.Log($"[Order] 订单池 {pool.poolName} 只有一个订单，返回 {validOrders[0].orderName}");
            return validOrders[0];
        }

        // 尝试避免与上一单相同
        var candidateOrders = new System.Collections.Generic.List<OrderSO>();
        foreach (var order in validOrders)
        {
            if (order != _lastOrder)
            {
                candidateOrders.Add(order);
            }
        }

        // 如果所有订单都是上一单（理论上不可能，因为validOrders.Count > 1），则从所有订单中随机
        if (candidateOrders.Count == 0)
        {
            Debug.LogWarning($"[Order] 订单池 {pool.poolName} 中所有订单都是上一单，从所有订单中随机");
            candidateOrders = validOrders;
        }

        // 等概率随机选择
        int randomIndex = UnityEngine.Random.Range(0, candidateOrders.Count);
        OrderSO selectedOrder = candidateOrders[randomIndex];

        Debug.Log($"[Order] 从订单池 {pool.poolName} 中随机选择了订单 {selectedOrder.orderName}");
        return selectedOrder;
    }

    /// <summary>
    /// 获取默认订单池，并确保其中存在当前可完成订单。
    /// </summary>
    private OrderPoolSO GetDefaultOrderPool(string customerName)
    {
        if (_defaultOrderPool == null || _defaultOrderPool.orders == null || _defaultOrderPool.orders.Count == 0)
        {
            Debug.LogError("[Order] 默认订单池无效，无法生成订单");
            return null;
        }

        if (!HasFulfillableOrder(_defaultOrderPool))
        {
            Debug.LogError($"[Order] 默认订单池没有当前已解锁材料可完成的订单，顾客：{customerName}");
            return null;
        }

        return _defaultOrderPool;
    }

    /// <summary>
    /// 判断订单池中是否存在当前已解锁材料可完成的订单。
    /// </summary>
    private bool HasFulfillableOrder(OrderPoolSO pool)
    {
        if (pool == null || pool.orders == null)
        {
            return false;
        }

        foreach (OrderSO order in pool.orders)
        {
            if (IsOrderFulfillable(order))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断订单是否可被当前已解锁材料完成；没有解锁管理器时保留旧行为。
    /// </summary>
    private bool IsOrderFulfillable(OrderSO order)
    {
        if (order == null)
        {
            return false;
        }

        return IngredientUnlockManager.Instance == null
               || IngredientUnlockManager.Instance.CanFulfillOrder(order);
    }

    /// <summary>
    /// 预生成顾客订单，不触发订单生成事件
    /// </summary>
    /// <param name="customer">顾客配置</param>
    /// <returns>预生成的订单，失败返回null</returns>
    public OrderSO PrepareOrderForCustomer(CustomerSO customer)
    {
        if (customer == null)
        {
            Debug.LogError("[Order] PrepareOrderForCustomer: 顾客配置为空");
            return null;
        }

        _preparedOrder = null;
        _preparedCustomer = null;

        Debug.Log($"[Order] 开始为顾客 {customer.customerName} 预生成订单");

        OrderPoolSO selectedPool = SelectOrderPool(customer);
        if (selectedPool == null)
        {
            Debug.LogError($"[Order] 为顾客 {customer.customerName} 选择订单池失败");
            return null;
        }

        OrderSO selectedOrder = GetRandomOrderFromPool(selectedPool);
        if (selectedOrder == null)
        {
            Debug.LogError($"[Order] 从订单池 {selectedPool.poolName} 中随机订单失败");
            return null;
        }

        _preparedOrder = selectedOrder;
        _preparedCustomer = customer;

        Debug.Log($"[Order] 成功预生成顾客 {customer.customerName} 的订单 {selectedOrder.orderName}");
        return selectedOrder;
    }

    /// <summary>
    /// 确认预生成订单并触发订单生成事件
    /// </summary>
    /// <returns>确认后的订单，失败返回null</returns>
    public OrderSO ConfirmPreparedOrder()
    {
        if (_preparedOrder == null || _preparedCustomer == null)
        {
            Debug.LogError("[Order] ConfirmPreparedOrder: 没有可确认的预生成订单");
            return null;
        }

        _lastOrder = _currentOrder;
        _currentOrder = _preparedOrder;
        _currentCustomer = _preparedCustomer;

        OrderSO confirmedOrder = _preparedOrder;
        _preparedOrder = null;
        _preparedCustomer = null;

        Debug.Log($"[Order] 成功确认订单 {confirmedOrder.orderName}");

        OnOrderGenerated?.Invoke(confirmedOrder);
        TutorialEventBus.Publish("OrderGenerated");
        ActionLogBus.Log($"新顾客下单");

        return confirmedOrder;
    }

    /// <summary>
    /// 为顾客生成订单
    /// </summary>
    /// <param name="customer">顾客配置</param>
    /// <returns>生成的订单，失败返回null</returns>
    public OrderSO GenerateOrderForCustomer(CustomerSO customer)
    {
        OrderSO selectedOrder = PrepareOrderForCustomer(customer);
        if (selectedOrder == null)
        {
            return null;
        }

        return ConfirmPreparedOrder();
    }

    /// <summary>
    /// 获取订单的随机点单文本
    /// </summary>
    /// <param name="order">订单配置</param>
    /// <returns>点单文本</returns>
    public string GetRandomOrderDialogue(OrderSO order)
    {
        if (order == null)
        {
            Debug.LogWarning("[Order] GetRandomOrderDialogue: 订单为空");
            return "我想要一杯咖啡。";
        }

        if (order.orderDialogueTexts == null || order.orderDialogueTexts.Count == 0)
        {
            Debug.LogWarning($"[Order] 订单 {order.orderName} 没有配置点单文本");
            return $"我想要一杯{order.orderName}。";
        }

        // 随机选择一条点单文本
        int randomIndex = UnityEngine.Random.Range(0, order.orderDialogueTexts.Count);
        string dialogue = order.orderDialogueTexts[randomIndex];

        Debug.Log($"[Order] 订单 {order.orderName} 的点单文本: {dialogue}");
        return dialogue;
    }

    /// <summary>
    /// 获取顾客的随机评价文本
    /// </summary>
    /// <param name="customer">顾客配置</param>
    /// <param name="feedbackLevel">评价档位（0=不满意，1=一般，2=满意）</param>
    /// <returns>评价文本</returns>
    public string GetRandomFeedback(CustomerSO customer, int feedbackLevel)
    {
        if (customer == null)
        {
            Debug.LogWarning("[Order] GetRandomFeedback: 顾客为空");
            return "嗯...";
        }

        System.Collections.Generic.List<string> feedbackList = null;
        string defaultFeedback = "";

        // 根据评价档位选择对应列表
        switch (feedbackLevel)
        {
            case 0: // 不满意
                feedbackList = customer.dissatisfiedFeedbackTexts;
                defaultFeedback = "这不是我想要的。";
                break;
            case 1: // 一般
                feedbackList = customer.neutralFeedbackTexts;
                defaultFeedback = "还行吧。";
                break;
            case 2: // 满意
                feedbackList = customer.satisfiedFeedbackTexts;
                defaultFeedback = "不错，谢谢。";
                break;
            default:
                Debug.LogWarning($"[Order] 无效的评价档位: {feedbackLevel}，使用一般评价");
                feedbackList = customer.neutralFeedbackTexts;
                defaultFeedback = "还行吧。";
                break;
        }

        // 如果列表为空，返回默认文本
        if (feedbackList == null || feedbackList.Count == 0)
        {
            Debug.LogWarning($"[Order] 顾客 {customer.customerName} 的评价档位 {feedbackLevel} 没有配置文本");
            return defaultFeedback;
        }

        // 随机选择一条评价文本
        int randomIndex = UnityEngine.Random.Range(0, feedbackList.Count);
        string feedback = feedbackList[randomIndex];

        Debug.Log($"[Order] 顾客 {customer.customerName} 的评价（档位{feedbackLevel}）: {feedback}");
        return feedback;
    }

    /// <summary>
    /// 提交订单
    /// </summary>
    /// <param name="order">订单配置</param>
    /// <param name="coffeeData">咖啡成品数据</param>
    public void SubmitOrder(OrderSO order, CoffeeData coffeeData)
    {
        if (order == null)
        {
            Debug.LogError("[Order] SubmitOrder: 订单为空");
            return;
        }

        if (coffeeData == null)
        {
            Debug.LogError("[Order] SubmitOrder: 咖啡数据为空");
            return;
        }

        Debug.Log($"[Order] 提交订单 {order.orderName}，咖啡总量: {coffeeData.currentTotalVolume}ml");

        // 触发订单提交事件
        OnOrderSubmitted?.Invoke(order, coffeeData);
        TutorialEventBus.Publish("CoffeeSubmit");
    }

    /// <summary>
    /// 重置当前订单
    /// </summary>
    public void ResetOrder()
    {
        Debug.Log("[Order] 重置当前订单");

        _currentOrder = null;
        _currentCustomer = null;
        _preparedOrder = null;
        _preparedCustomer = null;
        // 注意：不清空 _lastOrder，用于避免重复订单
    }
}
