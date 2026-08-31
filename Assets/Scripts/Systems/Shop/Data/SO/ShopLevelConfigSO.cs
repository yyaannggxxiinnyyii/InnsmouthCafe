using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 店铺等级配置数据。
    /// 定义单个店铺等级的所有属性和加成。
    /// </summary>
    [CreateAssetMenu(fileName = "ShopLevel_", menuName = "InnsmouthCafe/Config/Shop Level", order = 20)]
    public class ShopLevelConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("店铺等级（1-5）")]
        [Range(1, 5)]
        public int level = 1;

        [Tooltip("等级名称")]
        public string levelName = "简陋的咖啡馆";

        [Header("营业能力")]
        [Tooltip("营业时间（秒），例如 480 = 8分钟")]
        public int businessTimeSeconds = 480;

        [Tooltip("顾客来访间隔（秒）")]
        [Range(60, 120)]
        public float customerSpawnInterval = 100f;

        [Tooltip("订单价格倍率，例如 1.0 = 原价，1.5 = 1.5倍")]
        [Range(1f, 3f)]
        public float orderPriceMultiplier = 1.0f;

        [Tooltip("基础耐心加成（百分比），例如 0.1 = +10%")]
        [Range(0f, 0.5f)]
        public float basePatienceBonus = 0f;

        [Tooltip("同时等待客容量")]
        [Range(2, 4)]
        public int maxWaitingCustomers = 3;

        [Tooltip("单晚最大订单数（红线，不可超过10）")]
        [Range(5, 10)]
        public int maxOrdersPerNight = 6;

        [Header("装饰槽位")]
        [Tooltip("普通装饰槽位数量")]
        [Range(3, 12)]
        public int normalDecorationSlots = 3;

        [Tooltip("Boss装饰槽位数量")]
        [Range(0, 4)]
        public int bossDecorationSlots = 0;

        [Header("升级需求")]
        [Tooltip("升级到下一级所需金币")]
        public int upgradeCost = 0;

        [Tooltip("升级到下一级所需章节ID（可选）")]
        public string requiredChapterId = "";

        [Tooltip("升级到下一级所需收集度（可选）")]
        [Range(0, 100)]
        public int requiredCollectionPercent = 0;

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("等级描述文本")]
        public string description = "";
    }
}
