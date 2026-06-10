using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 当前豆批次数据
    /// 记录玩家当前正在准备的豆批次（萃取前的临时状态）
    /// </summary>
    [System.Serializable]
    public class CurrentBeanBatchData
    {
        [Header("豆种信息")]
        [Tooltip("当前选择的豆配置（null表示未选择）")]
        public BeanSO bean;

        [Header("豆量信息")]
        [Tooltip("当前批次豆量（克）")]
        public float beanGram;

        [Header("研磨信息")]
        [Tooltip("当前选择的研磨程度（null表示未选择）")]
        public GrindType? grindType;

        /// <summary>
        /// 清空当前批次数据
        /// </summary>
        public void Clear()
        {
            bean = null;
            beanGram = 0f;
            grindType = null;
        }

        /// <summary>
        /// 检查是否可以萃取
        /// </summary>
        public bool CanExtract()
        {
            return bean != null && beanGram > 0f && grindType.HasValue;
        }
    }
}
