using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 特殊顾客效果：按倍率影响当天所有顾客的基础耐心时间。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_PatienceModifier_", menuName = "InnsmouthCafe/Config/Special Effects/Patience Modifier", order = 21)]
    public class PatienceModifierEffectSO : SpecialCustomerEffectSO
    {
        [Header("耐心倍率")]
        [SerializeField]
        [Tooltip("当天所有顾客基础耐心值的倍率")]
        private float _patienceMultiplier = 1f;

        /// <summary>
        /// 叠乘当天所有顾客的耐心倍率。
        /// </summary>
        public override void Apply(SpecialCustomerEffectContext context)
        {
            context?.MultiplyPatience(_patienceMultiplier);
        }
    }
}
