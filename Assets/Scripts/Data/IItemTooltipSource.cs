namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 可用于悬停提示的通用物品信息接口
    /// </summary>
    public interface IItemTooltipSource
    {
        string TooltipTitle { get; }
        string TooltipDescription { get; }
    }
}
