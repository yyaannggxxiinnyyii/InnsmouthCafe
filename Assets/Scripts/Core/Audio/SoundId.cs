
/// <summary>
/// 游戏内所有音效的唯一标识枚举。
/// 新增音效时：① 在此处添加枚举值；② 在 AudioManager Inspector 中配对 AudioClip。
/// </summary>
public enum SoundId
{
    /// <summary>通用按钮点击音效。</summary>
    ButtonClick,

    /// <summary>选择杯子音效。</summary>
    CoffeeCupSelect,

    /// <summary>取豆音效（每次5g）。</summary>
    CoffeeBeanAdd,

    /// <summary>倒掉豆子音效。</summary>
    CoffeeBeanClear,

    /// <summary>研磨咖啡豆音效。</summary>
    CoffeeGrind,

    /// <summary>萃取咖啡豆音效。</summary>
    CoffeeExtraction,

    /// <summary>倒掉咖啡粉音效。</summary>
    CoffeePowderClear,

    /// <summary>开始倒辅助液音效（循环）。</summary>
    CoffeeLiquidPourStart,

    /// <summary>添加小料音效。</summary>
    CoffeeToppingAdd,

    /// <summary>移除小料音效。</summary>
    CoffeeToppingRemove,

    /// <summary>咖啡溢出音效。</summary>
    CoffeeOverflow,

    /// <summary>倒掉整杯咖啡音效。</summary>
    CoffeeClearWhole,

    /// <summary>提交咖啡音效。</summary>
    CoffeeSubmit,

    /// <summary>理智值下降音效。</summary>
    SanityDecrease,
}
