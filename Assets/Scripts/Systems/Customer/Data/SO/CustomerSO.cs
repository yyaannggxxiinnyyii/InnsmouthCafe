using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Explore;

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

        [Header("区域归属")]
        [Tooltip("顾客所属的区域列表；玩家进入任意所属区域后，该顾客加入经营顾客池")]
        public List<AreaConfigSO> belongAreas = new List<AreaConfigSO>();

        [Header("顾客额外订单")]
        [Tooltip("顾客自身额外指定的订单，不受区域普通订单池限制")]
        public List<OrderSO> extraOrders = new List<OrderSO>();

        [Tooltip("顾客自身额外指定的订单池，不受区域普通订单池限制")]
        public List<CustomerOrderPoolEntry> extraOrderPoolEntries =
            new List<CustomerOrderPoolEntry>();

        [Header("立绘")]
        [Tooltip("顾客头像（用于环形耐心条中心图标）")]
        public Sprite avatarSprite;

        [Tooltip("普通单订单小票上显示的订单者头像；启用顾客组时改用顾客组成员的订单头像")]
        public Sprite ordererAvatarSprite;

        [Tooltip("正常/等待状态立绘")]
        public Sprite normalSprite;

        [Tooltip("不耐烦状态立绘（为空则使用 normalSprite）")]
        public Sprite impatientSprite;

        [Tooltip("愤怒状态立绘（为空则使用 normalSprite）")]
        public Sprite angrySprite;

        [Tooltip("开心状态立绘（为空则使用 normalSprite）")]
        public Sprite happySprite;

        [Header("耐心值配置")]
        [Tooltip("最大耐心值（数值型，例如 100）")]
        public float maxPatienceValue = 100f;

        [Tooltip("基础耐心降低值（每秒消耗的基础值，例如 1.0 表示每秒掉 1 点）")]
        public float basePatienceDrainRate = 1.0f;

        [Tooltip("【已弃用】基础耐心时间（秒），新系统请使用 maxPatienceValue")]
        [HideInInspector]
        public float basePatienceTime = 60f;

        [Header("订单生成策略")]
        [Tooltip("无法完成订单的概率（0-1），用于挑战玩家备货")]
        [Range(0f, 1f)]
        public float impossibleOrderProbability = 0.2f;

        [Tooltip("允许拒绝订单的最大次数")]
        [Range(1, 5)]
        public int maxOrderRejections = 2;

        [Tooltip("每次拒绝订单降低的星级数")]
        [Range(0f, 2f)]
        public float rejectionPenaltyStars = 0.5f;

        [Header("订单池配置")]
        [Tooltip("顾客可抽取的订单池列表（带权重）")]
        public List<CustomerOrderPoolEntry> orderPoolEntries = new List<CustomerOrderPoolEntry>();

        [Header("特殊顾客")]
        [Tooltip("特殊顾客配置；为空表示普通顾客")]
        public SpecialCustomerProfileSO specialProfile;

        [Header("角色图鉴")]
        [TextArea(3, 5)]
        [Tooltip("遇到顾客后显示的角色介绍")]
        public string galleryCharacterDescription;

        [TextArea(3, 5)]
        [Tooltip("遇到顾客后显示的角色特性说明")]
        public string galleryEffectDescription;

        [Header("进店对话")]
        [TextArea(3, 5)]
        [Tooltip("顾客进店时的开场白列表（随机一条）")]
        public List<string> enterDialogueTexts = new List<string>();

        [HideInInspector]
        [Tooltip("该顾客是否携带收集物（完美接待后获得）")]
        public bool hasCollectible;

        [HideInInspector]
        [Tooltip("顾客携带的收集物配置（hasCollectible为true时有效）")]
        public CollectibleSO collectible;

        [HideInInspector]
        [Tooltip("好评/Perfect 接待后解锁的辅助液列表")]
        public List<LiquidSO> rewardLiquidsOnPerfect = new List<LiquidSO>();

        [HideInInspector]
        [Tooltip("好评/Perfect 接待后解锁的小料列表")]
        public List<ToppingSO> rewardToppingsOnPerfect = new List<ToppingSO>();

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

        [TextArea(3, 5)]
        [Tooltip("特殊评价文本列表（随机一条）")]
        public List<string> SpecialFeedbackTexts = new List<string>();

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
