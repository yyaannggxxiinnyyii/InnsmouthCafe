using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 辅助液段数据
    /// 记录单种辅助液的添加量
    /// 同种辅助液分多次加入时会合并到同一段
    /// </summary>
    [System.Serializable]
    public class LiquidSegmentData
    {
        [Header("液体类型")]
        [Tooltip("辅助液类型")]
        public LiquidType liquidType;

        [Header("液体量")]
        [Tooltip("实际倒入的毫升数（不四舍五入）")]
        public float amountMl;
    }
}
