using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 特殊顾客效果上下文，向效果配置暴露当天顾客队列和全局修正入口。
    /// </summary>
    public class SpecialCustomerEffectContext
    {
        private readonly List<CustomerSO> _todayQueue;

        /// <summary>
        /// 初始化特殊顾客效果上下文。
        /// </summary>
        public SpecialCustomerEffectContext(List<CustomerSO> todayQueue)
        {
            _todayQueue = todayQueue;
            PatienceMultiplier = 1f;
        }

        /// <summary>
        /// 当天所有顾客的耐心倍率。
        /// </summary>
        public float PatienceMultiplier { get; private set; }

        /// <summary>
        /// 向当天队列末尾追加额外顾客。
        /// </summary>
        public void AddExtraCustomers(IEnumerable<CustomerSO> customers)
        {
            if (customers == null || _todayQueue == null)
            {
                return;
            }

            foreach (CustomerSO customer in customers)
            {
                if (customer != null)
                {
                    _todayQueue.Add(customer);
                }
            }
        }

        /// <summary>
        /// 叠乘当天所有顾客的耐心倍率。
        /// </summary>
        public void MultiplyPatience(float multiplier)
        {
            if (multiplier <= 0f)
            {
                Debug.LogWarning($"[SpecialCustomerEffect] 耐心倍率必须大于 0，已忽略：{multiplier}");
                return;
            }

            PatienceMultiplier *= multiplier;
        }
    }
}
