using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 订单池配置数据
    /// 管理一组订单集合，用于订单分类（简单、中等、困难、特殊顾客专属等）
    /// 池内订单等概率随机
    /// </summary>
    [CreateAssetMenu(fileName = "OrderPool_", menuName = "InnsmouthCafe/Config/Order Pool", order = 7)]
    public class OrderPoolSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("订单池唯一ID")]
        public string poolId;

        [Tooltip("订单池显示名称")]
        public string poolName;

        [Header("订单列表")]
        [Tooltip("订单池中的订单列表")]
        public List<OrderSO> orders = new List<OrderSO>();

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 校验逻辑将在Task 4.2中实现
        }
#endif
    }
}
