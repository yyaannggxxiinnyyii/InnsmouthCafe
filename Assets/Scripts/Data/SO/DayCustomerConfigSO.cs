using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 每日顾客配置数据
    /// 从顾客配置池生成当天的顾客队列
    /// </summary>
    [CreateAssetMenu(fileName = "DayCustomerConfig_", menuName = "InnsmouthCafe/Config/DayCustomerConfig", order = 9)]
    public class DayCustomerConfigSO : ScriptableObject
    {
        [Header("顾客数量配置")]
        [Tooltip("当天基础顾客数量")]
        public int baseCustomerCount;

        [Header("抽卡池：普通顾客")]
        [Tooltip("普通顾客池")]
        public List<CustomerSO> normalCustomerPool = new List<CustomerSO>();

        [Header("追加顾客层")]
        [Tooltip("额外的特殊顾客列表（不计入基础总人数）")]
        public List<CustomerSO> extraCustomers = new List<CustomerSO>();

        [Header("固定顾客配置")]
        [Tooltip("如果是教学模式或固定剧情天数，可以直接在此配置所有固定顾客，顺序不变")]
        public List<CustomerSO> fixedCustomers = new List<CustomerSO>();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (baseCustomerCount < 0)
            {
                Debug.LogError($"[DayCustomerConfigSO Error] baseCustomerCount 必须大于或者等于0: {name}", this);
            }
            if (fixedCustomers == null || fixedCustomers.Count == 0)
            {
                if (normalCustomerPool == null || normalCustomerPool.Count == 0)
                {
                    Debug.LogWarning($"[DayCustomerConfigSO Warning] 既没有配置固定顾客，也没有配置普通顾客池: {name}", this);
                }
            }
            if (baseCustomerCount > 0 && (normalCustomerPool == null || normalCustomerPool.Count == 0) && (fixedCustomers == null || fixedCustomers.Count == 0))
            {
                Debug.LogError($"[DayCustomerConfigSO Error] 配置了基础数量但是未提供普通顾客池: {name}", this);
            }
        }
#endif
    }
}
