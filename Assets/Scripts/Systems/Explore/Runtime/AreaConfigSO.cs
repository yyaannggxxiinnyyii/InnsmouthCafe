using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 区域配置数据。
    /// 区域范围由场景 Tilemap 决定，本配置只描述区域身份、内容和探索软限制。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Explore/Area Config", fileName = "AreaConfig")]
    public class AreaConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("区域唯一 ID")]
        [SerializeField] private string areaId = "area_1";

        [Tooltip("区域显示名")]
        [SerializeField] private string areaName = "未命名区域";

        [TextArea(3, 5)]
        [Tooltip("区域描述，用于地图、章节或区域信息界面")]
        [SerializeField] private string description;

        [Tooltip("区域缩略图或地图标记图")]
        [SerializeField] private Sprite thumbnail;

        [Header("Tilemap 区域标记")]
        [Tooltip("属于本区域的 Tile。运行时会在场景 Tilemap 上按 Tile 类型判定玩家所在区域。")]
        [SerializeField] private TileBase[] areaTiles = new TileBase[0];

        [Header("探索参数")]
        [Tooltip("该区域理智消耗速率（理智/秒）")]
        [Min(0f)]
        [SerializeField] private float energyDrainPerSecond = 0.18f;

        [Tooltip("推荐使用的工具类型，仅用于提示和任务判断，不作为进入条件")]
        [SerializeField] private ToolType recommendedToolType = ToolType.None;

        [Tooltip("推荐工具等级，仅用于提示和任务判断，不作为进入条件")]
        [Range(0, 5)]
        [SerializeField] private int recommendedToolLevel;

        [Tooltip("推荐理智值，仅用于提示和任务判断，不作为进入条件")]
        [Min(0f)]
        [SerializeField] private float recommendedSanity;

        [Header("区域经营内容")]
        [Tooltip("该区域可能出现的普通顾客列表")]
        [SerializeField] private List<CustomerSO> normalCustomers = new List<CustomerSO>();

        [Tooltip("该区域使用的普通订单池")]
        [SerializeField] private OrderPoolSO orderPool;

        [Tooltip("该区域对应的特殊来宾")]
        [SerializeField] private CustomerSO specialCustomer;

        [Header("区域采集内容")]
        [Tooltip("该区域可以生成的资源点配置")]
        [SerializeField] private ResourceNodeConfig[] resourceNodes = new ResourceNodeConfig[0];

        /// <summary>区域唯一 ID。</summary>
        public string AreaId => areaId;

        /// <summary>区域显示名。</summary>
        public string AreaName => areaName;

        /// <summary>区域描述。</summary>
        public string Description => description;

        /// <summary>区域缩略图或地图标记图。</summary>
        public Sprite Thumbnail => thumbnail;

        /// <summary>区域理智消耗速率。</summary>
        public float EnergyDrainPerSecond => energyDrainPerSecond;

        /// <summary>区域推荐工具类型。</summary>
        public ToolType RecommendedToolType => recommendedToolType;

        /// <summary>区域推荐工具等级。</summary>
        public int RecommendedToolLevel => recommendedToolLevel;

        /// <summary>区域推荐理智值。</summary>
        public float RecommendedSanity => recommendedSanity;

        /// <summary>区域可能出现的普通顾客列表。</summary>
        public IReadOnlyList<CustomerSO> NormalCustomers => normalCustomers;

        /// <summary>区域普通订单池。</summary>
        public OrderPoolSO OrderPool => orderPool;

        /// <summary>区域对应的特殊来宾。</summary>
        public CustomerSO SpecialCustomer => specialCustomer;

        /// <summary>区域可生成的资源点配置。</summary>
        public ResourceNodeConfig[] ResourceNodes => resourceNodes;

        /// <summary>
        /// 判断指定 Tile 是否属于本区域。
        /// 区域的实际空间范围由调用方提供的场景 Tilemap 决定。
        /// </summary>
        public bool ContainsTile(TileBase tile)
        {
            if (tile == null || areaTiles == null || areaTiles.Length == 0)
            {
                return false;
            }

            foreach (TileBase areaTile in areaTiles)
            {
                if (areaTile == tile)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
