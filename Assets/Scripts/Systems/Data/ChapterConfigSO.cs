using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Explore;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 章节配置（ChapterConfigSO）。
    /// 替代旧的「7 天结局」，是地图内的推进目标。
    /// 通关判定 = 达成经营目标（营业额、顾客数等），而非「活过 7 天」。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Map/Chapter Config", fileName = "ChapterConfig")]
    public class ChapterConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("章节唯一标识符")]
        public string chapterId;

        [Tooltip("章节显示名称")]
        public string chapterName;

        [Tooltip("章节描述")]
        [TextArea(3, 5)]
        public string description;

        [Tooltip("章节排序（数字越小越靠前）")]
        public int sortOrder;

        [Header("通关目标")]
        [Tooltip("通关条件列表（需全部达成）")]
        public List<ChapterGoal> goals = new List<ChapterGoal>();

        [Header("奖励")]
        [Tooltip("通关奖励金币")]
        public int rewardMoney;

        [Tooltip("通关奖励材料")]
        public List<RewardMaterialEntry> rewardMaterials = new List<RewardMaterialEntry>();

        [Tooltip("解锁的探索地区")]
        public List<AreaConfigSO> unlockAreas = new List<AreaConfigSO>();

        [Tooltip("解锁的下一章节")]
        public ChapterConfigSO nextChapter;

        /// <summary>章节目标</summary>
        [System.Serializable]
        public class ChapterGoal
        {
            [Tooltip("目标类型")]
            public GoalType type;

            [Tooltip("目标值（如：累计营业额 1000，服务顾客 50 人）")]
            public int targetValue;

            [Tooltip("目标描述（显示给玩家）")]
            public string description;
        }

        /// <summary>目标类型枚举</summary>
        public enum GoalType
        {
            TotalRevenue,        // 累计营业额
            ServedCustomers,     // 服务顾客总数
            PerfectOrders,       // 完美订单数
            CollectedMaterials,  // 采集材料总数
            ReachSanityLevel,    // 达到理智上限等级
            UnlockArea           // 解锁指定探索地区
        }

        /// <summary>奖励材料条目</summary>
        [System.Serializable]
        public class RewardMaterialEntry
        {
            [Tooltip("材料 ID")]
            public string materialId;

            [Tooltip("奖励数量")]
            public int count;
        }
    }
}
