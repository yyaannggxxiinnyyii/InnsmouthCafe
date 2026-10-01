using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 游戏界面类型
    /// </summary>
    public enum GameViewType
    {
        /// <summary>
        /// 吧台接单界面
        /// </summary>
        Bar,

        /// <summary>
        /// 制作界面1：基础咖啡制作
        /// </summary>
        CraftBase,

        /// <summary>
        /// 制作界面2：调味与完成
        /// </summary>
        CraftMix
    }

    /// <summary>
    /// 杯型
    /// </summary>
    public enum CupType
    {
        /// <summary>
        /// 小杯 - 标准容量200ml，最大容量250ml，豆重10g
        /// </summary>
        Small,

        /// <summary>
        /// 中杯 - 标准容量300ml，最大容量350ml，豆重15g
        /// </summary>
        Medium
    }

    /// <summary>
    /// 研磨程度
    /// </summary>
    public enum GrindType
    {
        /// <summary>
        /// 粗磨 - 粗粝、清爽
        /// </summary>
        Coarse,

        /// <summary>
        /// 细磨 - 平衡、标准
        /// </summary>
        Fine,

        /// <summary>
        /// 精磨 - 浓郁、强烈
        /// </summary>
        ExtraFine
    }

    /// <summary>
    /// 小料标签（属性）
    /// </summary>
    public enum ToppingTag
    {
        /// <summary>
        /// 甜
        /// </summary>
        Sweet,

        /// <summary>
        /// 苦
        /// </summary>
        Bitter,

        /// <summary>
        /// 咸
        /// </summary>
        Salty,

        /// <summary>
        /// 脆
        /// </summary>
        Crispy,

        /// <summary>
        /// 爆珠
        /// </summary>
        Popping,

        /// <summary>
        /// 异香
        /// </summary>
        StrangeAroma
    }

    /// <summary>
    /// 游戏结局类型
    /// </summary>
    public enum GameEnding
    {
        /// <summary>迷失结局 — 理智值 0~60</summary>
        Lost,

        /// <summary>回归结局 — 理智值 60~90</summary>
        Return,

        /// <summary>好结局 — 理智值 90~100</summary>
        Good
    }

    /// <summary>
    /// 游戏模式
    /// </summary>
    public enum GameMode
    {
        /// <summary>
        /// 教学模式 - 固定流程，容错率高
        /// </summary>
        Tutorial = 0,

        /// <summary>
        /// 普通模式 - 标准挑战
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 困难模式 - 提高经营与制作难度
        /// </summary>
        Hard = 2,

        /// <summary>
        /// 无尽模式 - 持续经营直至满足结束条件
        /// </summary>
        Endless = 3
    }

    /// <summary>
    /// 咖啡品质等级
    /// </summary>
    public enum CoffeeQuality
    {
        /// <summary>
        /// 糟糕 - 0-49分 → 顾客不满意
        /// </summary>
        Terrible,

        /// <summary>
        /// 合格 - 50-89分 → 顾客一般
        /// </summary>
        Acceptable,

        /// <summary>
        /// 完美 - 90-100分 → 顾客满意 + 触发收集物
        /// </summary>
        Perfect
    }

}
