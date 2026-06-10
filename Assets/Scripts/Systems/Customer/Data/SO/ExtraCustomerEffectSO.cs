using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 特殊顾客效果：当天额外追加指定顾客到队列末尾。
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_ExtraCustomer_", menuName = "InnsmouthCafe/Config/Special Effects/Extra Customers", order = 20)]
    public class ExtraCustomerEffectSO : SpecialCustomerEffectSO
    {
        [Header("额外顾客")]
        [Tooltip("追加到当天队列末尾的顾客列表")]
        public List<CustomerSO> extraCustomers = new List<CustomerSO>();

        /// <summary>
        /// 将配置中的额外顾客追加到当天队列。
        /// </summary>
        public override void Apply(SpecialCustomerEffectContext context)
        {
            context?.AddExtraCustomers(extraCustomers);
        }
    }
}
