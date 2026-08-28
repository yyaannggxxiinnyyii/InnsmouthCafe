using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 采集点模板配置（设计文档 §5）。采集点完全自足，采集条件由自身定义，
    /// 不依赖地区。程序生成时克隆该模板得到 N 个实例。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Explore/Resource Node Config", fileName = "ResourceNodeConfig")]
    public class ResourceNodeConfig : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("资源唯一 ID")]
        public string resourceId = "resource_1";

        [Tooltip("资源显示名")]
        public string resourceName = "未命名资源";

        [Tooltip("资源点采集方式：需要开采，或直接按 F 拾取")]
        public ResourceNodeType nodeType = ResourceNodeType.Harvest;

        [Header("采集条件")]
        [Tooltip("采集该点所需工具类型")]
        public ToolType harvestToolType = ToolType.Pickaxe;

        [Tooltip("采集该点所需工具等级（0-5）。等级隐含可采集地区。")]
        [Range(0, 5)]
        public int harvestToolLevel = 0;

        [Header("采集消耗")]
        [Tooltip("采集完成瞬间额外消耗的理智值")]
        public float harvestSanityCost = 5f;

        [Tooltip("按住采集到完成的总耗时（秒）")]
        public float harvestDuration = 1.2f;

        [Header("产出（单次产量）")]
        [Tooltip("采集一次获得的资源类型 + 数量列表")]
        public HarvestYield[] yields = new HarvestYield[]
        {
            new HarvestYield("resource_1", 1)
        };

        [Header("外观")]
        [Tooltip("采集点显示精灵；为空时使用默认色块绘制")]
        public Sprite sprite;
    }
}
