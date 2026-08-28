using System;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 采集单次产出（设计文档 §5.2 harvestYield）。
    /// 资源产物用 resourceId 标识，便于后续对接材料库存系统。
    /// </summary>
    [Serializable]
    public struct HarvestYield
    {
        public string resourceId;   // 资源唯一 ID
        public int amount;          // 单次采集获得的资源数量

        public HarvestYield(string resourceId, int amount)
        {
            this.resourceId = resourceId;
            this.amount = amount;
        }
    }
}
