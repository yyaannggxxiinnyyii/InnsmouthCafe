using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.Utils
{
    /// <summary>
    /// OrderManager测试工具
    /// 用于测试订单生成逻辑
    /// </summary>
    public class OrderManagerTester : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField]
        [Tooltip("OrderManager引用")]
        private OrderManager _orderManager;

        [SerializeField]
        [Tooltip("测试用顾客列表")]
        private CustomerSO[] _testCustomers;

        [Header("测试参数")]
        [SerializeField]
        [Tooltip("连续生成订单次数")]
        private int _testCount = 10;

        private void Start()
        {
            if (_orderManager == null)
            {
                _orderManager = FindObjectOfType<OrderManager>();
                if (_orderManager == null)
                {
                    Debug.LogError("[OrderTester] 场景中没有找到OrderManager");
                    return;
                }
            }

            // 订阅事件
            _orderManager.OnOrderGenerated += OnOrderGenerated;
            _orderManager.OnOrderSubmitted += OnOrderSubmitted;
        }

        private void OnDestroy()
        {
            if (_orderManager != null)
            {
                _orderManager.OnOrderGenerated -= OnOrderGenerated;
                _orderManager.OnOrderSubmitted -= OnOrderSubmitted;
            }
        }

        /// <summary>
        /// 订单生成事件回调
        /// </summary>
        private void OnOrderGenerated(OrderSO order)
        {
            Debug.Log($"<color=green>[OrderTester] 订单生成事件触发: {order.orderName}</color>");
        }

        /// <summary>
        /// 订单提交事件回调
        /// </summary>
        private void OnOrderSubmitted(OrderSO order, CoffeeData coffeeData)
        {
            Debug.Log($"<color=cyan>[OrderTester] 订单提交事件触发: {order.orderName}, 咖啡总量: {coffeeData.currentTotalVolume}ml</color>");
        }

        [ContextMenu("测试1: 为单个顾客生成订单")]
        private void Test1_GenerateSingleOrder()
        {
            if (_testCustomers == null || _testCustomers.Length == 0)
            {
                Debug.LogError("[OrderTester] 没有配置测试顾客");
                return;
            }

            CustomerSO customer = _testCustomers[0];
            Debug.Log($"<color=yellow>========== 测试1: 为顾客 {customer.customerName} 生成订单 ==========</color>");

            OrderSO order = _orderManager.GenerateOrderForCustomer(customer);
            if (order != null)
            {
                string dialogue = _orderManager.GetRandomOrderDialogue(order);
                Debug.Log($"<color=green>✓ 订单生成成功: {order.orderName}</color>");
                Debug.Log($"<color=green>✓ 点单文本: {dialogue}</color>");
            }
            else
            {
                Debug.LogError("✗ 订单生成失败");
            }
        }

        [ContextMenu("测试2: 连续生成订单（测试避免重复）")]
        private void Test2_GenerateMultipleOrders()
        {
            if (_testCustomers == null || _testCustomers.Length == 0)
            {
                Debug.LogError("[OrderTester] 没有配置测试顾客");
                return;
            }

            CustomerSO customer = _testCustomers[0];
            Debug.Log($"<color=yellow>========== 测试2: 为顾客 {customer.customerName} 连续生成 {_testCount} 个订单 ==========</color>");

            var orderCounts = new System.Collections.Generic.Dictionary<string, int>();

            for (int i = 0; i < _testCount; i++)
            {
                OrderSO order = _orderManager.GenerateOrderForCustomer(customer);
                if (order != null)
                {
                    if (!orderCounts.ContainsKey(order.orderName))
                    {
                        orderCounts[order.orderName] = 0;
                    }
                    orderCounts[order.orderName]++;

                    Debug.Log($"<color=green>第{i + 1}次: {order.orderName}</color>");
                }
                else
                {
                    Debug.LogError($"第{i + 1}次生成失败");
                }
            }

            // 统计结果
            Debug.Log($"<color=cyan>========== 统计结果 ==========</color>");
            foreach (var kvp in orderCounts)
            {
                float percentage = (kvp.Value / (float)_testCount) * 100f;
                Debug.Log($"<color=cyan>{kvp.Key}: {kvp.Value}次 ({percentage:F1}%)</color>");
            }
        }

        [ContextMenu("测试3: 测试所有顾客")]
        private void Test3_TestAllCustomers()
        {
            if (_testCustomers == null || _testCustomers.Length == 0)
            {
                Debug.LogError("[OrderTester] 没有配置测试顾客");
                return;
            }

            Debug.Log($"<color=yellow>========== 测试3: 测试所有顾客 ==========</color>");

            foreach (var customer in _testCustomers)
            {
                if (customer == null) continue;

                Debug.Log($"<color=cyan>--- 测试顾客: {customer.customerName} ---</color>");

                OrderSO order = _orderManager.GenerateOrderForCustomer(customer);
                if (order != null)
                {
                    string dialogue = _orderManager.GetRandomOrderDialogue(order);
                    Debug.Log($"<color=green>✓ 订单: {order.orderName}</color>");
                    Debug.Log($"<color=green>✓ 点单文本: {dialogue}</color>");
                }
                else
                {
                    Debug.LogError($"✗ 顾客 {customer.customerName} 订单生成失败");
                }
            }
        }

        [ContextMenu("测试4: 测试评价文本")]
        private void Test4_TestFeedbackTexts()
        {
            if (_testCustomers == null || _testCustomers.Length == 0)
            {
                Debug.LogError("[OrderTester] 没有配置测试顾客");
                return;
            }

            CustomerSO customer = _testCustomers[0];
            Debug.Log($"<color=yellow>========== 测试4: 测试顾客 {customer.customerName} 的评价文本 ==========</color>");

            // 测试满意评价
            Debug.Log("<color=green>--- 满意评价 ---</color>");
            for (int i = 0; i < 3; i++)
            {
                string feedback = _orderManager.GetRandomFeedback(customer, 2);
                Debug.Log($"<color=green>{i + 1}. {feedback}</color>");
            }

            // 测试一般评价
            Debug.Log("<color=yellow>--- 一般评价 ---</color>");
            for (int i = 0; i < 3; i++)
            {
                string feedback = _orderManager.GetRandomFeedback(customer, 1);
                Debug.Log($"<color=yellow>{i + 1}. {feedback}</color>");
            }

            // 测试不满意评价
            Debug.Log("<color=red>--- 不满意评价 ---</color>");
            for (int i = 0; i < 3; i++)
            {
                string feedback = _orderManager.GetRandomFeedback(customer, 0);
                Debug.Log($"<color=red>{i + 1}. {feedback}</color>");
            }
        }

        [ContextMenu("测试5: 测试订单重置")]
        private void Test5_TestResetOrder()
        {
            Debug.Log($"<color=yellow>========== 测试5: 测试订单重置 ==========</color>");

            if (_testCustomers == null || _testCustomers.Length == 0)
            {
                Debug.LogError("[OrderTester] 没有配置测试顾客");
                return;
            }

            // 生成订单
            CustomerSO customer = _testCustomers[0];
            OrderSO order = _orderManager.GenerateOrderForCustomer(customer);
            Debug.Log($"<color=green>生成订单: {order?.orderName}</color>");
            Debug.Log($"<color=green>当前订单: {_orderManager.CurrentOrder?.orderName}</color>");
            Debug.Log($"<color=green>当前顾客: {_orderManager.CurrentCustomer?.customerName}</color>");

            // 重置订单
            _orderManager.ResetOrder();
            Debug.Log($"<color=cyan>重置后 - 当前订单: {(_orderManager.CurrentOrder == null ? "null" : _orderManager.CurrentOrder.orderName)}</color>");
            Debug.Log($"<color=cyan>重置后 - 当前顾客: {(_orderManager.CurrentCustomer == null ? "null" : _orderManager.CurrentCustomer.customerName)}</color>");
        }

        [ContextMenu("测试6: 权重随机测试（100次）")]
        private void Test6_TestWeightedRandom()
        {
            if (_testCustomers == null || _testCustomers.Length == 0)
            {
                Debug.LogError("[OrderTester] 没有配置测试顾客");
                return;
            }

            Debug.Log($"<color=yellow>========== 测试6: 权重随机测试（100次）==========</color>");

            var poolCounts = new System.Collections.Generic.Dictionary<string, int>();
            int totalTests = 100;

            for (int i = 0; i < totalTests; i++)
            {
                foreach (var customer in _testCustomers)
                {
                    if (customer == null) continue;

                    OrderSO order = _orderManager.GenerateOrderForCustomer(customer);
                    if (order != null)
                    {
                        // 通过订单ID前缀判断来自哪个池
                        string poolName = "未知池";
                        if (order.orderId.StartsWith("Simple_") || order.orderId.StartsWith("Easy_"))
                        {
                            poolName = "简单池";
                        }
                        else if (order.orderId.StartsWith("Medium_") || order.orderId.StartsWith("Normal_"))
                        {
                            poolName = "普通池";
                        }
                        else if (order.orderId.StartsWith("Hard_"))
                        {
                            poolName = "困难池";
                        }

                        string key = $"{customer.customerName} - {poolName}";
                        if (!poolCounts.ContainsKey(key))
                        {
                            poolCounts[key] = 0;
                        }
                        poolCounts[key]++;
                    }
                }
            }

            // 统计结果
            Debug.Log($"<color=cyan>========== 统计结果（每个顾客{totalTests}次）==========</color>");
            foreach (var kvp in poolCounts)
            {
                float percentage = (kvp.Value / (float)totalTests) * 100f;
                Debug.Log($"<color=cyan>{kvp.Key}: {kvp.Value}次 ({percentage:F1}%)</color>");
            }
        }
    }
}
