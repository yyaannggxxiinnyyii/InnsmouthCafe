using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 顾客订单池条目
    /// 定义顾客可抽取的订单池及其权重
    /// 支持基于权重的随机选择
    /// </summary>
    [System.Serializable]
    public class CustomerOrderPoolEntry
    {
        [Header("订单池")]
        [Tooltip("订单池配置")]
        public OrderPoolSO orderPool;

        [Header("权重")]
        [Range(0, 100)]
        [Tooltip("抽取权重（0=不参与，1-100=权重值）")]
        public int weight = 1;
    }
}
