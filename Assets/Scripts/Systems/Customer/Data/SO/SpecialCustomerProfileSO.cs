using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 特殊顾客配置数据，集中定义特殊顾客的奖励与后续可扩展玩法规则。
    /// </summary>
    [CreateAssetMenu(fileName = "SpecialCustomerProfile_", menuName = "InnsmouthCafe/Config/Special Customer Profile", order = 11)]
    public class SpecialCustomerProfileSO : ScriptableObject
    {
        [Header("特殊客人播报")]
        [TextArea(2, 4)]
        [Tooltip("特殊客人真正入场后，由小章鱼播报的文案；留空则不播报")]
        public string specialCustomerAnnouncementLine;

        [Header("每日效果")]
        [Tooltip("特殊顾客进入当天队列后，在每日开店前应用的效果列表")]
        public List<SpecialCustomerEffectSO> dayStartEffects = new List<SpecialCustomerEffectSO>();

        [Header("顾客组订单")]
        [Tooltip("是否启用顾客组多订单；关闭时按普通单订单顾客处理")]
        public bool useCustomerOrderGroup;

        [Tooltip("顾客组内每个订单成员配置；为空时按普通单订单顾客处理")]
        public List<CustomerOrderParticipantConfig> orderParticipants = new List<CustomerOrderParticipantConfig>();

        [TextArea(2, 4)]
        [Tooltip("顾客组所有订单完成后的统一反馈文本；为空时使用顾客默认评价文本")]
        public List<string> groupCompletedFeedbackTexts = new List<string>();

        [Header("收集物奖励")]
        [Tooltip("Perfect 接待后获得的收集物；为空则不发放收集物")]
        public CollectibleSO collectibleRewardOnPerfect;

        [Header("材料奖励")]
        [Tooltip("Perfect 接待后解锁的辅助液列表")]
        public List<LiquidSO> rewardLiquidsOnPerfect = new List<LiquidSO>();

        [Tooltip("Perfect 接待后解锁的小料列表")]
        public List<ToppingSO> rewardToppingsOnPerfect = new List<ToppingSO>();
    }
}
