using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 顾客池配置（CustomerPoolSO）。
    /// 定义：某个地图中会出现哪些顾客。
    /// 分为普通顾客池和特殊顾客池（Boss）。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Map/Customer Pool", fileName = "CustomerPool")]
    public class CustomerPoolSO : ScriptableObject
    {
        [Header("顾客池信息")]
        [Tooltip("顾客池名称")]
        public string poolName;

        [Tooltip("顾客池描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("顾客列表")]
        [Tooltip("该池中的顾客配置")]
        public List<CustomerEntry> customers = new List<CustomerEntry>();

        /// <summary>顾客条目</summary>
        [System.Serializable]
        public class CustomerEntry
        {
            [Tooltip("顾客配置 SO")]
            public CustomerConfigSO customer;

            [Tooltip("出现权重（越高越容易出现）")]
            [Min(1)]
            public int weight = 1;

            [Tooltip("解锁条件：需要通关的章节索引（-1 = 无条件）")]
            public int unlockChapterIndex = -1;
        }

        /// <summary>根据权重随机抽取一个顾客</summary>
        public CustomerConfigSO GetRandomCustomer()
        {
            if (customers == null || customers.Count == 0) return null;

            // 计算总权重
            int totalWeight = 0;
            foreach (var entry in customers)
            {
                // TODO: 对接章节系统后，检查 unlockChapterIndex
                if (entry.customer != null)
                {
                    totalWeight += entry.weight;
                }
            }

            if (totalWeight == 0) return null;

            // 随机抽取
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in customers)
            {
                if (entry.customer != null)
                {
                    currentWeight += entry.weight;
                    if (randomValue < currentWeight)
                    {
                        return entry.customer;
                    }
                }
            }

            return null;
        }
    }
}
