using System;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 游戏基础平衡配置SO。
    /// 集中存放不随游戏模式变化的玩法基础数值。
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayBalanceConfig", menuName = "InnsmouthCafe/Config/Gameplay Balance Config", order = 13)]
    public class GameplayBalanceConfigSO : ScriptableObject
    {
        [Header("顾客压力")]
        [Tooltip("顾客耐心与愤怒状态的基础规则")]
        [SerializeField] private CustomerBalanceSettings _customer = new CustomerBalanceSettings();

        [Header("理智规则")]
        [Tooltip("理智初始值、评价变化量与等级判定阈值")]
        [SerializeField] private SanityBalanceSettings _sanity = new SanityBalanceSettings();

        [Header("咖啡制作")]
        [Tooltip("取豆、倒液、萃取与制作失误惩罚的基础规则")]
        [SerializeField] private CoffeeCraftBalanceSettings _coffeeCraft = new CoffeeCraftBalanceSettings();

        [Header("评分规则")]
        [Tooltip("订单评分权重、容错范围与评分档位规则")]
        [SerializeField] private ScoringBalanceSettings _scoring = new ScoringBalanceSettings();

        [Header("结局规则")]
        [Tooltip("根据最终理智值判定结局的阈值")]
        [SerializeField] private EndingBalanceSettings _ending = new EndingBalanceSettings();

        [Header("小章鱼互动")]
        [Tooltip("拍击小章鱼随机事件的触发概率、权重与理智变化")]
        [SerializeField] private OctopusBalanceSettings _octopus = new OctopusBalanceSettings();

        /// <summary>顾客压力基础参数。</summary>
        public CustomerBalanceSettings Customer => _customer;

        /// <summary>理智基础参数。</summary>
        public SanityBalanceSettings Sanity => _sanity;

        /// <summary>咖啡制作基础参数。</summary>
        public CoffeeCraftBalanceSettings CoffeeCraft => _coffeeCraft;

        /// <summary>订单评分基础参数。</summary>
        public ScoringBalanceSettings Scoring => _scoring;

        /// <summary>结局基础参数。</summary>
        public EndingBalanceSettings Ending => _ending;

        /// <summary>小章鱼互动基础参数。</summary>
        public OctopusBalanceSettings Octopus => _octopus;
    }

    /// <summary>顾客压力基础参数。</summary>
    [Serializable]
    public class CustomerBalanceSettings
    {
        [Tooltip("愤怒状态下每秒损失的基础理智值")]
        [Min(0f)] public float angrySanityLossPerSecond = 0.1f;

        [Tooltip("剩余耐心值降至此比例时进入不耐烦阶段")]
        [Range(0f, 1f)] public float stageOnePatienceRatio = 0.6f;

        [Tooltip("剩余耐心值降至此比例时进入更紧张阶段")]
        [Range(0f, 1f)] public float stageTwoPatienceRatio = 0.3f;
    }

    /// <summary>理智基础参数。</summary>
    [Serializable]
    public class SanityBalanceSettings
    {
        [Tooltip("开始新游戏时的理智值")]
        [Min(0f)] public float initialSanity = 100f;

        [Tooltip("理智值允许达到的最小值")]
        [Min(0f)] public float minSanity = 0f;

        [Tooltip("理智值允许达到的最大值")]
        [Min(0f)] public float maxSanity = 100f;

        [Tooltip("顾客评价满意时增加的基础理智值")]
        public float satisfiedReward = 3f;

        [Tooltip("顾客评价一般时变化的基础理智值")]
        public float neutralChange = 0f;

        [Tooltip("顾客评价不满意时减少的基础理智值，应填写负数")]
        public float dissatisfiedPenalty = -5f;

        [Tooltip("理智值达到此值时判定为高理智")]
        [Range(0f, 100f)] public float highThreshold = 80f;

        [Tooltip("理智值达到此值时判定为中等理智")]
        [Range(0f, 100f)] public float mediumThreshold = 50f;

        [Tooltip("理智值达到此值时判定为低理智")]
        [Range(0f, 100f)] public float lowThreshold = 20f;
    }

    /// <summary>咖啡制作基础参数。</summary>
    [Serializable]
    public class CoffeeCraftBalanceSettings
    {
        [Tooltip("每次取豆操作加入的咖啡豆重量（克）")]
        [Min(0f)] public float beanPerClick = 5f;

        [Tooltip("单批次允许加入的最大咖啡豆重量（克）")]
        [Min(0f)] public float maxBeanPerBatch = 20f;

        [Tooltip("每克咖啡豆可萃取出的咖啡液体积（毫升）")]
        [Min(0f)] public float beanToLiquidRatio = 5f;

        [Tooltip("慢速倒入辅助液的速度（毫升/秒）")]
        [Min(0f)] public float slowPourSpeed = 35f;

        [Tooltip("快速倒入辅助液的速度（毫升/秒）")]
        [Min(0f)] public float fastPourSpeed = 90f;

        [Tooltip("累计溢出达到此体积时结算一次理智惩罚（毫升）")]
        [Min(0.01f)] public float overflowPenaltyInterval = 25f;

        [Tooltip("每次溢出惩罚结算减少的基础理智值")]
        [Min(0f)] public float overflowSanityPenalty = 0.2f;

        [Tooltip("咖啡萃取速度（毫升/秒）")]
        [Min(0f)] public float extractionSpeed = 20f;

        [Tooltip("倒掉未研磨咖啡豆时减少的基础理智值")]
        [Min(0f)] public float clearBeansSanityPenalty = 0.2f;

        [Tooltip("倒掉咖啡粉时减少的基础理智值")]
        [Min(0f)] public float clearPowderSanityPenalty = 0.3f;

        [Tooltip("倒掉整杯咖啡时减少的基础理智值")]
        [Min(0f)] public float clearWholeCoffeeSanityPenalty = 0.4f;
    }

    /// <summary>订单评分基础参数。</summary>
    [Serializable]
    public class ScoringBalanceSettings
    {
        [Tooltip("咖啡液匹配分在总分中的权重")]
        [Range(0f, 1f)] public float coffeeMatchWeight = 0.3f;

        [Tooltip("辅助液匹配分在总分中的权重")]
        [Range(0f, 1f)] public float liquidMatchWeight = 0.3f;

        [Tooltip("小料匹配分在总分中的权重")]
        [Range(0f, 1f)] public float toppingMatchWeight = 0.1f;

        [Tooltip("总量匹配分在总分中的权重")]
        [Range(0f, 1f)] public float volumeMatchWeight = 0.3f;

        [Tooltip("杯子填充比例适配分在总分中的权重")]
        [Range(0f, 1f)] public float cupAdaptationWeight = 0f;

        [Tooltip("未使用模式配置时，辅助液与总量可获得满分的允许误差百分比")]
        [Range(0f, 50f)] public float defaultErrorMargin = 10f;

        [Tooltip("杯子填充比例低于此值时开始扣除适配分")]
        [Range(0f, 1f)] public float optimalFillRatioLow = 0.4f;

        [Tooltip("杯子填充比例高于此值时开始扣除适配分")]
        [Range(0f, 1f)] public float optimalFillRatioHigh = 1f;

        [Tooltip("未使用模式配置时，达到合格评价所需的最低分数")]
        [Range(0f, 100f)] public float acceptableScoreThreshold = 70f;

        [Tooltip("未使用模式配置时，达到Perfect评价所需的最低分数")]
        [Range(0f, 100f)] public float perfectScoreThreshold = 90f;

        [Tooltip("发生溢出后最终评分乘以的系数")]
        [Range(0f, 1f)] public float overflowScoreMultiplier = 0.9f;
    }

    /// <summary>结局基础参数。</summary>
    [Serializable]
    public class EndingBalanceSettings
    {
        [Tooltip("最终理智值达到此值时进入好结局")]
        [Range(0f, 100f)] public float goodEndingThreshold = 90f;

        [Tooltip("最终理智值达到此值时进入回归结局，低于此值进入迷失结局")]
        [Range(0f, 100f)] public float returnEndingThreshold = 60f;
    }

    /// <summary>小章鱼互动基础参数。</summary>
    [Serializable]
    public class OctopusBalanceSettings
    {
        [Tooltip("每次拍击小章鱼触发随机事件的概率")]
        [Range(0f, 1f)] public float squishEventChance = 0.3f;

        [Tooltip("随机事件中播放台词的相对权重")]
        [Min(0f)] public float lineWeight = 5f;

        [Tooltip("随机事件中增加理智的相对权重")]
        [Min(0f)] public float sanityGainWeight = 2f;

        [Tooltip("随机事件增加的基础理智值")]
        [Min(0f)] public float sanityGainAmount = 1f;

        [Tooltip("随机事件中减少理智的相对权重")]
        [Min(0f)] public float sanityLossWeight = 1f;

        [Tooltip("随机事件减少的基础理智值")]
        [Min(0f)] public float sanityLossAmount = 0.2f;
    }
}
