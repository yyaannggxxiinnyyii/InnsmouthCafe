using System;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Customer;
using InnsmouthCafe.Decoration;
using InnsmouthCafe.Shop;
using DecorationBonusType = InnsmouthCafe.Data.DecorationBonusType;
// 旧系统 InnsmouthCafe.Data 中存在同名 CustomerState，这里显式指向新系统版本
using CustomerState = InnsmouthCafe.Customer.CustomerState;

namespace InnsmouthCafe.Patience
{
    /// <summary>
    /// 顾客耐心值管理器。
    /// 负责计算和更新所有顾客的耐心值，支持多种倍率叠加。
    /// 公式：每秒消耗 = 基础降低值 × 阶段倍率 × 其他倍率。
    /// </summary>
    public class CustomerPatienceManager : Singleton<CustomerPatienceManager>
    {
        [Header("阶段耐心值消耗倍率")]
        [Tooltip("排队阶段耐心值消耗倍率")]
        [SerializeField] private float _queuingPatienceMultiplier = 0.2f;

        [Tooltip("制作阶段耐心值消耗倍率")]
        [SerializeField] private float _waitingPatienceMultiplier = 1.0f;

        [Header("调试信息 - 只读")]
        [Tooltip("当前店铺倍率（从 ShopUpgradeManager 读取）")]
        [SerializeField] private float _shopPatienceMultiplier = 1.0f;

        [Tooltip("当前装饰倍率（从 DecorationManager 读取）")]
        [SerializeField] private float _decorationPatienceMultiplier = 1.0f;

        /// <summary>
        /// 耐心值耗尽事件。
        /// 参数：耐心耗尽的顾客实例。
        /// </summary>
        public event Action<CustomerInstance> OnPatienceExhausted;

        private void Start()
        {
            RefreshMultipliers();

            // 装饰变化后重新读取加成
            if (DecorationManager.Instance != null)
            {
                DecorationManager.Instance.OnDecorationsChanged += RefreshMultipliers;
            }

            // 店铺升级后重新读取加成
            if (ShopUpgradeManager.Instance != null)
            {
                ShopUpgradeManager.Instance.OnShopUpgraded += HandleShopUpgraded;
            }
        }

        private void OnDestroy()
        {
            if (DecorationManager.Instance != null)
            {
                DecorationManager.Instance.OnDecorationsChanged -= RefreshMultipliers;
            }

            if (ShopUpgradeManager.Instance != null)
            {
                ShopUpgradeManager.Instance.OnShopUpgraded -= HandleShopUpgraded;
            }
        }

        /// <summary>
        /// 店铺升级后刷新耐心倍率。
        /// </summary>
        /// <param name="newLevel">升级后的店铺等级。</param>
        private void HandleShopUpgraded(int newLevel)
        {
            RefreshMultipliers();
        }

        /// <summary>
        /// 更新店铺耐心倍率。
        /// 店铺加成为正向收益，转换为更低的消耗倍率。
        /// </summary>
        private void UpdateShopPatienceMultiplier()
        {
            if (ShopUpgradeManager.Instance == null)
            {
                return;
            }

            float bonus = ShopUpgradeManager.Instance.GetBasePatienceBonus();
            _shopPatienceMultiplier = Mathf.Max(0.1f, 1.0f - bonus);
            Debug.Log($"[CustomerPatience] 店铺耐心加成：+{bonus * 100:F0}%，消耗倍率：{_shopPatienceMultiplier:F2}");
        }

        /// <summary>
        /// 更新装饰物耐心倍率。
        /// 加成来自 DecorationManager 汇总的 CustomerPatience 加成。
        /// </summary>
        private void UpdateDecorationPatienceMultiplier()
        {
            if (DecorationManager.Instance == null)
            {
                return;
            }

            float bonus = DecorationManager.Instance.GetTotalBonus(DecorationBonusType.CustomerPatience);
            _decorationPatienceMultiplier = Mathf.Max(0.1f, 1.0f - bonus);
            Debug.Log($"[CustomerPatience] 装饰耐心加成：+{bonus * 100:F0}%，消耗倍率：{_decorationPatienceMultiplier:F2}");
        }

        /// <summary>
        /// 更新所有顾客的耐心值。
        /// </summary>
        /// <param name="customers">顾客实例列表。</param>
        /// <param name="deltaTime">时间增量。</param>
        public void UpdateAllCustomersPatience(List<CustomerInstance> customers, float deltaTime)
        {
            if (customers == null || customers.Count == 0)
            {
                return;
            }

            foreach (var customer in customers)
            {
                if (customer != null)
                {
                    UpdateCustomerPatience(customer, deltaTime);
                }
            }
        }

        /// <summary>
        /// 更新单个顾客的耐心值。
        /// </summary>
        /// <param name="customer">顾客实例。</param>
        /// <param name="deltaTime">时间增量。</param>
        private void UpdateCustomerPatience(CustomerInstance customer, float deltaTime)
        {
            // 计算耐心消耗速率
            float drainRate = CalculatePatienceDrainRate(customer);

            // 更新耐心值
            customer.currentPatience -= drainRate * deltaTime;
            customer.currentPatience = Mathf.Max(0, customer.currentPatience);

            // 触发耐心耗尽事件
            if (customer.currentPatience <= 0 && customer.currentState != CustomerState.Angry)
            {
                OnPatienceExhausted?.Invoke(customer);
            }
        }

        /// <summary>
        /// 计算顾客的耐心消耗速率。
        /// 公式：基础降低值 × 阶段倍率 × 店铺倍率 × 装饰倍率。
        /// </summary>
        /// <param name="customer">顾客实例。</param>
        /// <returns>耐心消耗速率（点/秒）。</returns>
        public float CalculatePatienceDrainRate(CustomerInstance customer)
        {
            float baseRate = customer.basePatienceDrain;
            float stageMultiplier = GetStageMultiplier(customer.currentState);
            float shopMultiplier = _shopPatienceMultiplier;
            float decorationMultiplier = _decorationPatienceMultiplier;

            return baseRate * stageMultiplier * shopMultiplier * decorationMultiplier;
        }

        /// <summary>
        /// 根据顾客状态获取阶段耐心值消耗倍率。
        /// </summary>
        /// <param name="state">顾客状态。</param>
        /// <returns>阶段倍率。</returns>
        private float GetStageMultiplier(CustomerState state)
        {
            switch (state)
            {
                case CustomerState.Queuing:
                    return _queuingPatienceMultiplier; // 排队阶段：慢速消耗

                case CustomerState.Waiting:
                    return _waitingPatienceMultiplier; // 制作阶段：正常消耗

                default:
                    return 0f; // 其他状态不消耗
            }
        }

        /// <summary>
        /// 获取顾客的剩余耐心比例（0-1）。
        /// </summary>
        /// <param name="customer">顾客实例。</param>
        /// <returns>剩余耐心比例。</returns>
        public float GetPatienceRatio(CustomerInstance customer)
        {
            if (customer.maxPatience <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(customer.currentPatience / customer.maxPatience);
        }

        /// <summary>
        /// 刷新所有倍率（店铺升级或装饰变化后自动调用）。
        /// </summary>
        public void RefreshMultipliers()
        {
            UpdateShopPatienceMultiplier();
            UpdateDecorationPatienceMultiplier();
        }
    }
}
