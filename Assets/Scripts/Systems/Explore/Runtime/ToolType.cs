namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 采集工具类型。设计文档 §6：只有「稿子 / 夹子 / 铲子」三种工具。
    /// 采集点用「工具类型 + 工具等级」做采集条件，工具不区分资源种类。
    /// </summary>
    public enum ToolType
    {
        None = 0,          // 无工具
        Pickaxe = 1,       // 稿子：采集矿类资源
        Clipper = 2,       // 夹子：采集甲壳类资源
        Shovel = 3         // 铲子：采集水下软植类资源
    }
}
