using System.Collections.Generic;

/// <summary>
/// 咖啡教学目标 ID 常量，集中维护流程与场景目标之间的约定。
/// </summary>
public static class CoffeeTutorialTargets
{
    public const string Bar = "bar";
    public const string Zhangyu = "zhangyu";
    public const string Shoujiwu = "shoujiwu";
    public const string Customer = "customer";
    public const string OrderTicket_1 = "order_ticket_1";
    public const string OrderTicket_2 = "order_ticket_2";
    public const string SwitchNext = "switch_next";
    public const string Cup = "cup";
    public const string Bean = "bean";
    public const string Grinder_1 = "grinder_1";
    public const string Grinder_2 = "grinder_2";
    public const string Grinder_3 = "grinder_3";
    public const string Extract_0 = "extract_0";
    public const string Extract = "extract";
    public const string Lajitong = "lajitong";
    public const string Liquid_1 = "liquid_1";
    public const string Liquid_2 = "liquid_2";
    public const string Topping = "topping";
    public const string Submit = "submit";
}

/// <summary>
/// 咖啡制作教学流程定义，按主路径创建首日教学步骤。
/// </summary>
public static class CoffeeTutorialFlow
{
    /// <summary>
    /// 构建教学模式第一天的咖啡制作主流程步骤。
    /// </summary>
    public static List<TutorialStep> Build()
    {
        return new List<TutorialStep>
            {
                TutorialStep.Say("我在海里捡到你，作为报答你需要替我经营5天咖啡馆，之后便可放你离开。"),
                TutorialStep.Say("接下来我会手把手带你做完第一杯咖啡,好好记住流程。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Bar, "这里是吧台。客人会在这里出现并点单。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Zhangyu, "这是章鱼小老板，每天开始时他会给你一些信息。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Shoujiwu, "这里是收集物架，接待了特殊客人之后有概率获得对应的收集物"),
                TutorialStep.ReleaseGate(TutorialGateKey.BeforeFirstCustomerEnter),
                TutorialStep.WaitForEvent(TutorialEvents.CustomerArrived),
                TutorialStep.Highlight(CoffeeTutorialTargets.Customer, "这是顾客。顾客点单后会等待你的咖啡，动作太慢会影响耐心。"),
                TutorialStep.ReleaseGate(TutorialGateKey.BeforeFirstCustomerDialogue),
                TutorialStep.WaitForEvent(TutorialEvents.CustomerTicketDetailShown),
                TutorialStep.Highlight(CoffeeTutorialTargets.OrderTicket_1, "这是小票。小票记录了这杯咖啡的详细需求。"),
                TutorialStep.ReleaseGate(TutorialGateKey.BeforeFirstTicketAutoCollapse),
                TutorialStep.Highlight(CoffeeTutorialTargets.OrderTicket_2, "可以点击小票 或者 按下Q键来展开查看详细小票。"),
                TutorialStep.RequireClick(CoffeeTutorialTargets.SwitchNext,"点击切换按钮切换场景，也可以通过按键 A、D快速切换场景。",TutorialEvents.ViewSwitchCraftBase),
                TutorialStep.RequireClick(CoffeeTutorialTargets.Cup, "先选择一个杯子。", TutorialEvents.CupSelected),
                TutorialStep.RequireClick(CoffeeTutorialTargets.Bean, "选取一种咖啡豆，可以一下取出多次咖啡豆。", TutorialEvents.BeanAdded),
                TutorialStep.Highlight(CoffeeTutorialTargets.Grinder_1, "这里研磨机，选完咖啡豆之后可以进行研磨。"),
                TutorialStep.RequireClick(CoffeeTutorialTargets.Grinder_2, "点击研磨柄，开始研磨，每次点击，研磨程度都会增加。", TutorialEvents.GrindSelected),
                TutorialStep.Highlight(CoffeeTutorialTargets.Grinder_3, "这里会显示当前研磨机中咖啡豆的研磨度。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Extract_0, "这里萃取机，研磨好咖啡豆之后进行咖啡液的萃取。"),
                TutorialStep.RequireClick(CoffeeTutorialTargets.Extract, "点击手柄开始萃取咖啡液。", TutorialEvents.ExtractionStarted),
                TutorialStep.WaitForEvent(TutorialEvents.ExtractionCompleted),
                TutorialStep.Say("这样就萃取完成了，期间如果你有操作失误的地方，例如：选错豆子、研磨度错误、萃取错误，都可以在提交前进行修改。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Lajitong, "点击垃圾桶即可丢掉现有材料重新开始制作。"),
                TutorialStep.RequireClick(CoffeeTutorialTargets.SwitchNext,"点击切换按钮，进入加液和小料界面。",TutorialEvents.ViewSwitchCraftMix),
                TutorialStep.Say("这个场景用来添加咖啡辅助液和风味小料。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Liquid_1, "这里是鲸鱼奶、水母汁，还有冰块。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Liquid_2, "这里是海草汁，以及深海水。"),
                TutorialStep.Highlight(CoffeeTutorialTargets.Topping, "这些是小料，拖拽小料到杯口绿色显示区域即可添加小料。"),
                TutorialStep.Say("咖啡制作完成后，返回吧台界面，点击提交订单。"),
                TutorialStep.FreePlayUntilEvent(TutorialEvents.CoffeeSubmit),
                TutorialStep.Say("很好，第一杯咖啡已经交给客人。后面的订单就交给你了。")
            };
    }
}
