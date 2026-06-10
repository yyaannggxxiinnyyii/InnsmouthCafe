using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 每日顾客配置数据
    ///
    /// 队列构成规则：
    ///   1. 固定队列（fixedQueue）按顺序优先出场
    ///   2. 随机普通池（randomNormalPool）抽 randomNormalCount 个，防重复
    ///   3. 随机特殊池（randomSpecialPool）抽 randomSpecialCount 个，防重复
    ///   4. 步骤2、3的结果混合后随机排序，追加在固定队列之后
    ///
    /// 今日总顾客数 = fixedQueue.Count + randomNormalCount + randomSpecialCount
    /// 无需手动填写总人数，由配置自动计算。
    /// </summary>
    [CreateAssetMenu(fileName = "DayCustomerConfig_", menuName = "InnsmouthCafe/Config/DayCustomerConfig", order = 9)]
    public class DayCustomerConfigSO : ScriptableObject
    {
        [Header("固定顾客队列")]
        [Tooltip("按顺序出场的固定顾客（普通或特殊均可），优先于随机顾客出场。教学模式直接填此列表即可。")]
        public List<CustomerSO> fixedQueue = new List<CustomerSO>();

        [Header("随机普通顾客池")]
        [Tooltip("普通顾客候选池，从中不重复随机抽取（池不足时允许重复并输出警告）")]
        public List<CustomerSO> randomNormalPool = new List<CustomerSO>();

        [Tooltip("从普通池中随机抽取的顾客数量")]
        [Min(0)]
        public int randomNormalCount = 0;

        [Header("随机特殊顾客池")]
        [Tooltip("特殊顾客候选池，从中不重复随机抽取（池不足时允许重复并输出警告）")]
        public List<CustomerSO> randomSpecialPool = new List<CustomerSO>();

        [Tooltip("从特殊池中随机抽取的顾客数量")]
        [Min(0)]
        public int randomSpecialCount = 0;

        [Header("当天材料解锁")]
        [Tooltip("当天开始时解锁的辅助液列表")]
        public List<LiquidSO> unlockedLiquidsOnDayStart = new List<LiquidSO>();

        [Tooltip("当天开始时解锁的小料列表")]
        public List<ToppingSO> unlockedToppingsOnDayStart = new List<ToppingSO>();

        /// <summary>今日顾客总数（只读，由配置自动计算）</summary>
        public int TotalCustomerCount =>
            (fixedQueue?.Count ?? 0) + randomNormalCount + randomSpecialCount;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (randomNormalCount > 0 && (randomNormalPool == null || randomNormalPool.Count == 0))
                Debug.LogWarning($"[DayCustomerConfigSO] 配置了 randomNormalCount={randomNormalCount} 但普通顾客池为空: {name}", this);

            if (randomSpecialCount > 0 && (randomSpecialPool == null || randomSpecialPool.Count == 0))
                Debug.LogWarning($"[DayCustomerConfigSO] 配置了 randomSpecialCount={randomSpecialCount} 但特殊顾客池为空: {name}", this);

            if (TotalCustomerCount == 0)
                Debug.LogWarning($"[DayCustomerConfigSO] 今日顾客总数为 0，请检查配置: {name}", this);
        }
#endif
    }
}
