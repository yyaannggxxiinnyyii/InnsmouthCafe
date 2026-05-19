
/// <summary>
/// 游戏内所有音效的唯一标识枚举。
/// 新增音效时：① 在此处添加枚举值；② 在 AudioManager Inspector 中配对 AudioClip。
/// </summary>
public enum SoundId
{
    // ── 通用UI音效 ──────────────────────────────────────────

    /// <summary>通用按钮点击音效。</summary>
    ButtonClick,

    /// <summary>时间线控制按钮音效（播放/暂停/快进等）。</summary>
    TimelineButton,

    /// <summary>可交互线索高亮/发现音效。</summary>
    ClueInteractable,

    /// <summary>线索墙点击线索音效。</summary>
    ClueWallClick,

    // ── 咖啡制作音效 ──────────────────────────────────────────

    /// <summary>选择杯子音效。</summary>
    CoffeeCupSelect,

    /// <summary>取豆音效（每次5g）。</summary>
    CoffeeBeanAdd,

    /// <summary>倒掉豆子音效。</summary>
    CoffeeBeanClear,

    /// <summary>研磨咖啡豆音效。</summary>
    CoffeeGrind,

    /// <summary>开始萃取音效。</summary>
    CoffeeExtractionStart,

    /// <summary>萃取完成音效。</summary>
    CoffeeExtractionComplete,

    /// <summary>倒掉咖啡粉音效。</summary>
    CoffeePowderClear,

    /// <summary>开始倒辅助液音效（循环）。</summary>
    CoffeeLiquidPourStart,

    /// <summary>停止倒辅助液音效。</summary>
    CoffeeLiquidPourStop,

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
