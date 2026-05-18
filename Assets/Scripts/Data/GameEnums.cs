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
    /// 咖啡豆类型
    /// </summary>
    public enum BeanType
    {
        /// <summary>
        /// 普通豆 - 基础豆种
        /// </summary>
        Normal,

        /// <summary>
        /// 阿拉比卡豆 - 香气型豆种
        /// </summary>
        Arabica,

        /// <summary>
        /// 罗布斯塔豆 - 浓烈型豆种
        /// </summary>
        Robusta
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
        Superfine
    }

    /// <summary>
    /// 辅助液类型
    /// </summary>
    public enum LiquidType
    {
        /// <summary>
        /// 热水 - 稀释、美式
        /// </summary>
        HotWater,

        /// <summary>
        /// 牛奶 - 奶咖、柔和口味
        /// </summary>
        Milk,

        /// <summary>
        /// 奶泡 - 泡沫、顶部口感
        /// </summary>
        Foam,

        /// <summary>
        /// 冰水 - 冰咖啡、清爽口味
        /// </summary>
        IceWater
    }

    /// <summary>
    /// 小料类型
    /// </summary>
    public enum ToppingType
    {
        /// <summary>
        /// 焦糖碎 - 甜/脆
        /// </summary>
        CaramelCrumbs,

        /// <summary>
        /// 巧克力粉 - 苦
        /// </summary>
        ChocolatePowder,

        /// <summary>
        /// 海星糖 - 咸/脆
        /// </summary>
        StarfishSugar,

        /// <summary>
        /// 眼球爆珠 - 爆珠/异香
        /// </summary>
        EyeballBoba,

        /// <summary>
        /// 月尘粉 - 异香
        /// </summary>
        MoonDust,

        /// <summary>
        /// 黑盐 - 咸/苦
        /// </summary>
        BlackSalt
    }

    /// <summary>
    /// 小料属性
    /// </summary>
    public enum ToppingAttribute
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
        Boba,

        /// <summary>
        /// 异香
        /// </summary>
        ExoticAroma
    }

    /// <summary>
    /// 游戏模式
    /// </summary>
    public enum GameMode
    {
        /// <summary>
        /// 教学模式 - 固定流程
        /// </summary>
        Tutorial,

        /// <summary>
        /// 新手模式 - 低难度随机
        /// </summary>
        Beginner,

        /// <summary>
        /// 普通模式 - 标准挑战
        /// </summary>
        Normal,

        /// <summary>
        /// 无尽模式 - 无限经营
        /// </summary>
        Endless
    }

    /// <summary>
    /// 咖啡品质等级
    /// </summary>
    public enum CoffeeQuality
    {
        /// <summary>
        /// 糟糕 - 0-39分
        /// </summary>
        Terrible,

        /// <summary>
        /// 不合格 - 40-59分
        /// </summary>
        Poor,

        /// <summary>
        /// 合格 - 60-74分
        /// </summary>
        Acceptable,

        /// <summary>
        /// 优秀 - 75-89分
        /// </summary>
        Excellent,

        /// <summary>
        /// 完美 - 90-100分
        /// </summary>
        Perfect
    }

    /// <summary>
    /// 时间阶段
    /// </summary>
    public enum TimeStage
    {
        /// <summary>
        /// 第一阶段 - 顾客耐心充足
        /// </summary>
        StageOne,

        /// <summary>
        /// 第二阶段 - 正常等待
        /// </summary>
        StageTwo,

        /// <summary>
        /// 第三阶段 - 顾客不耐烦
        /// </summary>
        StageThree
    }

    /// <summary>
    /// 订单状态
    /// </summary>
    public enum OrderState
    {
        /// <summary>
        /// 空闲
        /// </summary>
        Idle,

        /// <summary>
        /// 顾客进入
        /// </summary>
        CustomerEnter,

        /// <summary>
        /// 顾客对话
        /// </summary>
        CustomerDialogue,

        /// <summary>
        /// 订单已接收
        /// </summary>
        OrderReceived,

        /// <summary>
        /// 选择杯型
        /// </summary>
        CupSelect,

        /// <summary>
        /// 选择咖啡豆
        /// </summary>
        BeanSelect,

        /// <summary>
        /// 选择研磨程度
        /// </summary>
        GrindSelect,

        /// <summary>
        /// 萃取中
        /// </summary>
        Extraction,

        /// <summary>
        /// 添加辅助液
        /// </summary>
        LiquidAdd,

        /// <summary>
        /// 添加小料
        /// </summary>
        ToppingAdd,

        /// <summary>
        /// 准备提交
        /// </summary>
        ReadyToSubmit,

        /// <summary>
        /// 已提交
        /// </summary>
        Submitted,

        /// <summary>
        /// 评分中
        /// </summary>
        Scoring,

        /// <summary>
        /// 顾客反馈
        /// </summary>
        CustomerFeedback,

        /// <summary>
        /// 订单结束
        /// </summary>
        OrderEnd
    }

    /// <summary>
    /// 咖啡制作状态机
    /// 用于管理单杯咖啡的制作流程状态
    /// </summary>
    public enum CraftState
    {
        /// <summary>
        /// 尚未开始制作
        /// </summary>
        None,

        /// <summary>
        /// 等待选择杯型
        /// </summary>
        CupSelect,

        /// <summary>
        /// 等待取豆（可混豆）
        /// </summary>
        BeanSelect,

        /// <summary>
        /// 等待选择研磨程度
        /// </summary>
        GrindSelect,

        /// <summary>
        /// 萃取动画播放中（禁止操作）
        /// </summary>
        Extracting,

        /// <summary>
        /// 已完成萃取，可进入调味阶段
        /// </summary>
        Extracted,

        /// <summary>
        /// 可添加辅助液
        /// </summary>
        LiquidAdd,

        /// <summary>
        /// 可添加小料
        /// </summary>
        ToppingAdd,

        /// <summary>
        /// 可提交咖啡
        /// </summary>
        ReadyToSubmit,

        /// <summary>
        /// 已提交，数据已锁定
        /// </summary>
        Submitted,

        /// <summary>
        /// 当前制作失败（如溢出），必须倒掉重做
        /// </summary>
        Failed
    }
}
