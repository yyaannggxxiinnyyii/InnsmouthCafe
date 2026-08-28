namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 资源点采集方式。决定资源点需要开采还是可以直接拾取。
    /// </summary>
    public enum ResourceNodeType
    {
        Harvest = 0,  // 需要工具并持续采集
        Pickup = 1    // 不需要工具，按下 F 立即拾取
    }
}
