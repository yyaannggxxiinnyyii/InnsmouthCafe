using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 地图解锁条件。
    /// 定义：玩家需要满足什么条件才能解锁某张地图。
    /// </summary>
    [System.Serializable]
    public class MapUnlockRequirement
    {
        [Header("前置地图")]
        [Tooltip("需要先通关的地图（null = 无前置，直接解锁）")]
        public MapConfigSO prerequisiteMap;

        [Tooltip("前置地图需要通关的章节索引（-1 = 通关所有章节）")]
        public int prerequisiteChapterIndex = -1;

        [Header("其他条件")]
        [Tooltip("需要的最低金币")]
        public int requiredMoney = 0;

        [Tooltip("需要的理智上限等级")]
        public int requiredSanityLevel = 0;

        [Tooltip("需要拥有的工具类型和等级")]
        public ToolRequirement[] requiredTools = new ToolRequirement[0];

        /// <summary>工具需求</summary>
        [System.Serializable]
        public class ToolRequirement
        {
            [Tooltip("工具类型")]
            public ToolType toolType;

            [Tooltip("最低等级")]
            public int minLevel;
        }

        /// <summary>检查是否满足解锁条件（预留接口，存档系统对接时实现）</summary>
        public bool IsMet()
        {
            // TODO: 对接存档系统后，从存档中读取玩家状态判断
            // 现在先返回 true（测试用）
            return true;
        }
    }
}
