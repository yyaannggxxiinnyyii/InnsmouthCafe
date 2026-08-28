using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Explore;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 地图配置（MapConfigSO）。
    /// 长期经营架构的核心数据：一张地图 = 一个经营存档的「世界」。
    /// 定义：这张地图有什么顾客、订单、探索地区、章节目标。
    /// 替代：旧的 GameModeConfigSO（7天模式）。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Map/Map Config", fileName = "MapConfig")]
    public class MapConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("地图唯一标识符")]
        public string mapId;

        [Tooltip("地图显示名称")]
        public string mapName;

        [Tooltip("地图描述")]
        [TextArea(3, 5)]
        public string description;

        [Tooltip("地图缩略图")]
        public Sprite thumbnail;

        [Header("初始状态")]
        [Tooltip("初始金币")]
        public int initialMoney = 100;

        [Tooltip("初始理智值上限")]
        public float initialMaxSanity = 100f;

        [Tooltip("初始拥有的材料（材料ID + 数量）")]
        public List<InitialMaterialEntry> initialMaterials = new List<InitialMaterialEntry>();

        [Tooltip("初始拥有的工具（工具类型 + 等级）")]
        public List<InitialToolEntry> initialTools = new List<InitialToolEntry>();

        [Tooltip("初始拥有的载具")]
        public List<VehicleConfigSO> initialVehicles = new List<VehicleConfigSO>();

        [Header("经营内容")]
        [Tooltip("该地图的普通顾客池")]
        public CustomerPoolSO customerPool;

        [Tooltip("该地图的特殊顾客池（Boss）")]
        public CustomerPoolSO specialCustomerPool;

        [Tooltip("该地图的订单池")]
        public OrderPoolSO orderPool;

        [Header("探索内容")]
        [Tooltip("该地图包含的探索地区（可复用其他地图的地区）")]
        public List<AreaConfigSO> areas = new List<AreaConfigSO>();

        [Header("章节系统")]
        [Tooltip("章节节点列表（通关目标）")]
        public List<ChapterConfigSO> chapters = new List<ChapterConfigSO>();

        [Header("解锁条件")]
        [Tooltip("解锁该地图的前置条件")]
        public MapUnlockRequirement unlockRequirement;

        /// <summary>初始材料条目</summary>
        [System.Serializable]
        public class InitialMaterialEntry
        {
            [Tooltip("材料 ID")]
            public string materialId;

            [Tooltip("初始数量")]
            public int count;
        }

        /// <summary>初始工具条目</summary>
        [System.Serializable]
        public class InitialToolEntry
        {
            [Tooltip("工具类型（Hook=稿子, Clip=夹子, Shovel=铲子）")]
            public ToolType toolType;

            [Tooltip("初始等级（0-5）")]
            public int level;
        }
    }

    /// <summary>工具类型枚举（与探索系统对齐）</summary>
    public enum ToolType
    {
        None = 0,
        Hook = 1,   // 稿子（用于硬质资源）
        Clip = 2,   // 夹子（用于活体资源）
        Shovel = 3  // 铲子（用于沙泥资源）
    }
}
