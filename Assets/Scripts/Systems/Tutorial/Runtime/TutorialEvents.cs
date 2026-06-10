
/// <summary>
/// 教学事件 Key 集中定义
/// </summary>
public static class TutorialEvents
{
    public const string DayStart = "DayStart";
    public const string DayEnd = "DayEnd";
    public const string CustomerEnter = "CustomerEnter";
    public const string CustomerArrived = "CustomerArrived";
    public const string CustomerReadyToTalk = "CustomerReadyToTalk";
    public const string CustomerTicketDetailShown = "CustomerTicketDetailShown";
    public const string CustomerTicketShown = "CustomerTicketShown";
    public const string FirstBarToCraftBaseSwitchComplete = "FirstBarToCraftBaseSwitchComplete";
    public const string FirstCraftMixSwitchComplete = "FirstCraftMixSwitchComplete";
    public const string CustomerLeave = "CustomerLeave";
    public const string OrderGenerated = "OrderGenerated";
    public const string CoffeeSubmit = "CoffeeSubmit";
    public const string ViewSwitchBar = "ViewSwitchBar";
    public const string ViewSwitchCraftBase = "ViewSwitchCraftBase";
    public const string ViewSwitchCraftMix = "ViewSwitchCraftMix";

    /// <summary>小章鱼说完教学模式开场台词后触发（仅教学模式第一天）</summary>
    public const string OctopusOpeningComplete = "OctopusOpeningComplete";

    /// <summary>教学模式下首次选择任意杯子时触发</summary>
    public const string FirstCupSelected = "FirstCupSelected";
    public const string CupSelected = "CupSelected";
    public const string BeanAdded = "BeanAdded";
    public const string GrindSelected = "GrindSelected";
    public const string ExtractionStarted = "ExtractionStarted";
    public const string ExtractionCompleted = "ExtractionCompleted";
    public const string LiquidAdded = "LiquidAdded";
    public const string ToppingAdded = "ToppingAdded";
}
