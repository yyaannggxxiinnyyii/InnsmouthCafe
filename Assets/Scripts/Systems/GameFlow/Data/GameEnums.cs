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
        Tutorial,

        /// <summary>
        /// 普通模式 - 标准挑战
        /// </summary>
        Normal
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
    /// 顾客状态
    /// </summary>
    public enum CustomerState
    {
        /// <summary>
        /// 未生成
        /// </summary>
        None,

        /// <summary>
        /// 进入中
        /// </summary>
        Entering,

        /// <summary>
        /// 对话中
        /// </summary>
        Talking,

        /// <summary>
        /// 等待中
        /// </summary>
        Waiting,

        /// <summary>
        /// 愤怒
        /// </summary>
        Angry,

        /// <summary>
        /// 反馈中
        /// </summary>
        Feedback,

        /// <summary>
        /// 开心
        /// </summary>
        Happy,

        /// <summary>
        /// 疑惑（一般评价）
        /// </summary>
        Neutral,

        /// <summary>
        /// 离开中
        /// </summary>
        Leaving
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

    /// <summary>
    /// 制作主状态（V0.2）
    /// </summary>
    public enum CraftMainState
    {
        /// <summary>
        /// 未开始
        /// </summary>
        None,

        /// <summary>
        /// 制作中
        /// </summary>
        Crafting,

        /// <summary>
        /// 已提交
        /// </summary>
        Submitted,

        /// <summary>
        /// 失败（溢出等）
        /// </summary>
        Failed
    }

    /// <summary>
    /// 制作模块状态（V0.2）
    /// </summary>
    public enum CraftModuleState
    {
        /// <summary>
        /// 选择杯子
        /// </summary>
        CupSelect,

        /// <summary>
        /// 选择豆子
        /// </summary>
        BeanSelect,

        /// <summary>
        /// 选择研磨
        /// </summary>
        GrindSelect,

        /// <summary>
        /// 萃取
        /// </summary>
        Extract,

        /// <summary>
        /// 添加辅助液
        /// </summary>
        LiquidAdd,

        /// <summary>
        /// 添加小料
        /// </summary>
        ToppingAdd,

        /// <summary>
        /// 查看/提交
        /// </summary>
        Review
    }

    /// <summary>
    /// 游戏流程状态
    /// </summary>
    public enum GameFlowState
    {
        /// <summary>
        /// 未开始
        /// </summary>
        None,

        /// <summary>
        /// 等待开始（延迟中）
        /// </summary>
        WaitingToStart,

        /// <summary>
        /// 顾客进入
        /// </summary>
        CustomerEntering,

        /// <summary>
        /// 顾客对话（点单）
        /// </summary>
        CustomerTalking,

        /// <summary>
        /// 等待制作（玩家制作咖啡中）
        /// </summary>
        WaitingForCraft,

        /// <summary>
        /// 评分中
        /// </summary>
        Scoring,

        /// <summary>
        /// 顾客反馈
        /// </summary>
        CustomerFeedback,

        /// <summary>
        /// 顾客离开
        /// </summary>
        CustomerLeaving,

        /// <summary>
        /// 当天结束
        /// </summary>
        DayEnd,

        /// <summary>
        /// 游戏结束
        /// </summary>
        GameEnd
    }
}
