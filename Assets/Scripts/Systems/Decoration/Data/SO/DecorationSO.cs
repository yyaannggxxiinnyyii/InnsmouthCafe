using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 装饰物类型。
    /// </summary>
    public enum DecorationType
    {
        Normal = 0,  // 普通装饰物（购买获得，占普通槽位）
        Boss = 1     // Boss装饰物（Boss奖励，占独立Boss槽位）
    }

    /// <summary>
    /// 装饰物加成类型。
    /// </summary>
    public enum DecorationBonusType
    {
        CustomerPatience = 0,   // 顾客耐心加成（百分比，0.05 = +5%）
        BusinessTime = 1,       // 营业时间加成（秒，30 = +30秒）
        QualityTolerance = 2,   // 咖啡品质容错加成（百分比，0.1 = +10%）
        MaxEnergy = 3           // 探索精力上限加成（点数，20 = +20点）
    }

    /// <summary>
    /// 单条装饰物加成配置。
    /// 一个装饰物可以携带多条不同类型的加成。
    /// </summary>
    [System.Serializable]
    public class DecorationBonusEntry
    {
        [Tooltip("加成类型")]
        public DecorationBonusType bonusType = DecorationBonusType.CustomerPatience;

        [Tooltip("加成数值；百分比类填 0.05 表示 +5%，营业时间填秒数，精力上限填点数")]
        public float bonusValue = 0f;
    }

    /// <summary>
    /// 装饰物配置数据。
    /// 定义单个装饰物的属性、美观度贡献和额外加成。
    /// </summary>
    [CreateAssetMenu(fileName = "Decoration_", menuName = "InnsmouthCafe/Config/Decoration", order = 21)]
    public class DecorationSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("装饰物唯一ID")]
        public string decorationId;

        [Tooltip("装饰物名称")]
        public string decorationName;

        [Tooltip("装饰物类型；Boss 装饰物占用独立的 Boss 槽位")]
        public DecorationType decorationType = DecorationType.Normal;

        [Header("视觉")]
        [Tooltip("装饰物图标（商店与管理界面使用）")]
        public Sprite icon;

        [Tooltip("装饰物预制体（安装后在场景槽位实例化）")]
        public GameObject prefab;

        [Header("美观度贡献")]
        [Tooltip("提供的美观度数值")]
        public int beautyValue = 10;

        [Header("额外加成（可选）")]
        [Tooltip("装饰物携带的加成列表；留空表示纯美观装饰物")]
        public List<DecorationBonusEntry> bonuses = new List<DecorationBonusEntry>();

        [Header("获取方式")]
        [Tooltip("购买价格；0 表示不可购买")]
        public int purchasePrice = 0;

        [Tooltip("来源 Boss ID；Boss 装饰物必填")]
        public string sourceBossId = "";

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("装饰物描述")]
        public string description = "";

        /// <summary>
        /// 是否携带任何额外加成。
        /// </summary>
        public bool HasBonus => bonuses != null && bonuses.Count > 0;

        /// <summary>
        /// 获取指定类型的加成总值；同类型多条加成会累加。
        /// </summary>
        /// <param name="type">需要查询的加成类型。</param>
        /// <returns>加成总值；没有该类型加成时返回 0。</returns>
        public float GetBonusValue(DecorationBonusType type)
        {
            if (bonuses == null)
            {
                return 0f;
            }

            float total = 0f;
            foreach (DecorationBonusEntry entry in bonuses)
            {
                if (entry != null && entry.bonusType == type)
                {
                    total += entry.bonusValue;
                }
            }

            return total;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (decorationType == DecorationType.Normal
                && purchasePrice <= 0
                && string.IsNullOrEmpty(sourceBossId))
            {
                Debug.LogWarning($"[Decoration] {decorationName} 是普通装饰物但未设置购买价格。");
            }

            if (decorationType == DecorationType.Boss && string.IsNullOrEmpty(sourceBossId))
            {
                Debug.LogWarning($"[Decoration] {decorationName} 是 Boss 装饰物但未设置来源 Boss ID。");
            }

            if (bonuses == null)
            {
                return;
            }

            foreach (DecorationBonusEntry entry in bonuses)
            {
                if (entry != null && Mathf.Approximately(entry.bonusValue, 0f))
                {
                    Debug.LogWarning($"[Decoration] {decorationName} 存在加成数值为 0 的 {entry.bonusType} 条目。");
                }
            }
        }
#endif
    }
}
