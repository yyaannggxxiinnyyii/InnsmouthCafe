using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 特殊顾客效果基类，定义特殊顾客在每日流程中可应用的规则变化。
    /// </summary>
    public abstract class SpecialCustomerEffectSO : ScriptableObject
    {
        /// <summary>
        /// 应用特殊顾客效果。
        /// </summary>
        public abstract void Apply(SpecialCustomerEffectContext context);
    }
}
