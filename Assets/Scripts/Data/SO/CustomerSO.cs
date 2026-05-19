using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 顾客配置数据
    /// 定义顾客的基础信息、耐心时间、订单池配置、对话文本
    /// </summary>
    [CreateAssetMenu(fileName = "Customer_", menuName = "InnsmouthCafe/Config/Customer", order = 8)]
    public class CustomerSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("顾客唯一ID")]
        public string customerId;

        [Tooltip("顾客显示名称")]
        public string customerName;

        [Header("耐心时间")]
        [Tooltip("基础耐心时间（秒）")]
        public float basePatienceTime = 60f;

        [Header("订单池配置")]
        [Tooltip("顾客可抽取的订单池列表（带权重）")]
        public List<CustomerOrderPoolEntry> orderPoolEntries = new List<CustomerOrderPoolEntry>();

        [Header("进店对话")]
        [TextArea(3, 5)]
        [Tooltip("顾客进店时的开场白列表（随机一条）")]
        public List<string> enterDialogueTexts = new List<string>();

        [Header("评价文本")]
        [TextArea(3, 5)]
        [Tooltip("满意时的评价文本列表（随机一条）")]
        public List<string> satisfiedFeedbackTexts = new List<string>();

        [TextArea(3, 5)]
        [Tooltip("一般时的评价文本列表（随机一条）")]
        public List<string> neutralFeedbackTexts = new List<string>();

        [TextArea(3, 5)]
        [Tooltip("不满意时的评价文本列表（随机一条）")]
        public List<string> dissatisfiedFeedbackTexts = new List<string>();

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 校验逻辑将在Task 4.3中实现
        }
#endif
    }
}
