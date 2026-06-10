/// <summary>
/// 教学步骤类型，决定步骤展示方式与推进条件。
/// </summary>
public enum TutorialStepKind
{
    Say,
    Highlight,
    RequireClick,
    RequireAreaClick,
    WaitForEvent,
    FreePlayUntilEvent,
    ReleaseGate
}

/// <summary>
/// 单个教学步骤的数据定义，由程序内流程构建并交给 TutorialRunner 执行。
/// </summary>
public sealed class TutorialStep
{
    /// <summary>步骤类型。</summary>
    public TutorialStepKind Kind { get; private set; }

    /// <summary>目标 ID，用于查找场景中的 TutorialTarget。</summary>
    public string TargetId { get; private set; }

    /// <summary>步骤展示文案。</summary>
    public string Text { get; private set; }

    /// <summary>等待的业务事件 Key。</summary>
    public string EventKey { get; private set; }

    /// <summary>需要放行的教学门控节点。</summary>
    public TutorialGateKey GateKey { get; private set; }

    /// <summary>
    /// 创建纯说明步骤，玩家点击任意区域后推进。
    /// </summary>
    public static TutorialStep Say(string text)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.Say,
            Text = text
        };
    }

    /// <summary>
    /// 创建高亮说明步骤，玩家点击任意区域后推进。
    /// </summary>
    public static TutorialStep Highlight(string targetId, string text)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.Highlight,
            TargetId = targetId,
            Text = text
        };
    }

    /// <summary>
    /// 创建强制点击步骤。
    /// </summary>
    public static TutorialStep RequireClick(string targetId, string text, string waitForEventKey = null)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.RequireClick,
            TargetId = targetId,
            Text = text,
            EventKey = waitForEventKey
        };
    }

    /// <summary>
    /// 创建强制区域点击步骤，玩家必须点中目标高亮区域。
    /// </summary>
    public static TutorialStep RequireAreaClick(string targetId, string text)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.RequireAreaClick,
            TargetId = targetId,
            Text = text
        };
    }

    /// <summary>
    /// 创建等待业务事件步骤，用于萃取完成这类非点击流程。
    /// </summary>
    public static TutorialStep WaitForEvent(string eventKey)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.WaitForEvent,
            EventKey = eventKey
        };
    }

    /// <summary>
    /// 创建自由操作等待步骤，隐藏引导 UI 并等待真实业务事件后继续。
    /// </summary>
    public static TutorialStep FreePlayUntilEvent(string eventKey)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.FreePlayUntilEvent,
            EventKey = eventKey
        };
    }

    /// <summary>
    /// 创建放行教学门控步骤，用于让业务流程继续推进。
    /// </summary>
    public static TutorialStep ReleaseGate(TutorialGateKey gateKey)
    {
        return new TutorialStep
        {
            Kind = TutorialStepKind.ReleaseGate,
            GateKey = gateKey
        };
    }
}
