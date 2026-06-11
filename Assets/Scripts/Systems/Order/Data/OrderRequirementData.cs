using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡液需求数据
    /// 定义单段咖啡液的需求（豆种+研磨度+容量）
    /// </summary>
    [System.Serializable]
    public class CoffeeRequirementData
    {
        [Header("咖啡豆")]
        [Tooltip("咖啡豆配置")]
        public BeanSO bean;

        [Header("研磨度")]
        [Tooltip("研磨程度")]
        public GrindType grindType;

        [Header("目标容量")]
        [Tooltip("目标咖啡液容量（ml），必须是25ml的倍数")]
        public int targetVolume;
    }

    /// <summary>
    /// 辅助液需求数据
    /// 定义单种辅助液的需求（液体+容量）
    /// </summary>
    [System.Serializable]
    public class LiquidRequirementData
    {
        [Header("辅助液")]
        [Tooltip("辅助液配置")]
        public LiquidSO liquid;

        [Header("目标容量")]
        [Tooltip("目标辅助液容量（ml）")]
        public int targetVolume;
    }

    /// <summary>
    /// 小料需求数据
    /// 定义小料需求（仅指定具体小料）
    /// </summary>
    [System.Serializable]
    public class ToppingRequirementData
    {
        [Header("指定小料")]
        [Tooltip("要求的具体小料列表")]
        public List<ToppingSO> requiredToppings;
    }

    /// <summary>
    /// 订单需求数据（运行时）
    /// 由OrderSO在运行时转换而来
    /// 用于传递给CoffeeCraftManager进行制作
    /// </summary>
    [System.Serializable]
    public class OrderRequirementData
    {
        [Header("订单信息")]
        [Tooltip("订单名称")]
        public string orderName;

        [Tooltip("订单描述")]
        public string orderDescription;

        [Header("目标容量")]
        [Tooltip("目标总容量（咖啡液 + 辅助液总和，ml）")]
        public float targetTotalVolume;

        [Header("推荐容器")]
        [Tooltip("推荐杯子最小容量（ml）")]
        public float recommendedCupCapacity;

        [Header("咖啡液需求")]
        [Tooltip("所有咖啡液需求列表")]
        public List<CoffeeRequirementData> coffeeRequirements = new();

        [Header("辅助液需求")]
        [Tooltip("所有辅助液需求列表")]
        public List<LiquidRequirementData> liquidRequirements = new();

        [Header("小料需求")]
        [Tooltip("小料需求")]
        public ToppingRequirementData toppingRequirement = new();

        [Header("难度配置")]
        [Tooltip("订单难度等级（1-5）")]
        public int difficultyLevel = 1;
    }

    /// <summary>
    /// 顾客组订单槽状态，描述单张小票在一次顾客组接待中的完成进度。
    /// </summary>
    public enum CustomerOrderSlotState
    {
        /// <summary>
        /// 未初始化。
        /// </summary>
        None,

        /// <summary>
        /// 等待玩家提交咖啡。
        /// </summary>
        WaitingForSubmission,

        /// <summary>
        /// 已提交咖啡，等待评分或反馈汇总。
        /// </summary>
        Submitted,

        /// <summary>
        /// 已完成评分，可参与顾客组统一反馈。
        /// </summary>
        Completed
    }

    /// <summary>
    /// 顾客组内单个订单成员配置，例如双头鲨的左头、右头或珊瑚夫人的迷弟。
    /// </summary>
    [System.Serializable]
    public class CustomerOrderParticipantConfig
    {
        [Header("成员信息")]
        [Tooltip("成员唯一标识；为空时运行时会按列表索引生成")]
        public string participantId;

        [Tooltip("成员显示名称，例如左头、右头、迷弟A")]
        public string displayName;

        [Tooltip("该成员在订单小票上显示的头像")]
        public Sprite ordererAvatarSprite;

        [Header("订单池")]
        [Tooltip("该成员可抽取的订单池列表")]
        public List<CustomerOrderPoolEntry> orderPoolEntries = new List<CustomerOrderPoolEntry>();

        [Header("点单文本")]
        [TextArea(2, 4)]
        [Tooltip("该成员点单时使用的对白；为空时使用订单自身点单文本")]
        public List<string> orderDialogueTexts = new List<string>();
    }

    /// <summary>
    /// 顾客组内单张订单小票的运行时数据，保存订单、提交咖啡与评分结果。
    /// </summary>
    [System.Serializable]
    public class CustomerOrderSlotData
    {
        [Header("槽位信息")]
        [Tooltip("订单槽唯一ID")]
        public string slotId;

        [Tooltip("订单归属显示名称")]
        public string displayName;

        [Tooltip("订单者在小票上显示的头像")]
        public Sprite ordererAvatarSprite;

        [Tooltip("订单槽状态")]
        public CustomerOrderSlotState state = CustomerOrderSlotState.None;

        [Header("订单数据")]
        [Tooltip("订单配置")]
        public OrderSO orderSO;

        [Tooltip("订单运行时需求数据")]
        public OrderRequirementData requirementData;

        [Header("提交结果")]
        [Tooltip("玩家提交的咖啡数据")]
        public CoffeeData submittedCoffee;

        [Tooltip("该订单的评分结果")]
        public CoffeeScoringData scoringData;

        /// <summary>
        /// 是否仍在等待玩家提交咖啡。
        /// </summary>
        public bool IsWaitingForSubmission => state == CustomerOrderSlotState.WaitingForSubmission;

        /// <summary>
        /// 是否已经提交咖啡但还未完成最终评分。
        /// </summary>
        public bool IsSubmitted => state == CustomerOrderSlotState.Submitted;

        /// <summary>
        /// 是否已经完成评分。
        /// </summary>
        public bool IsCompleted => state == CustomerOrderSlotState.Completed;

        /// <summary>
        /// 使用订单配置初始化槽位。
        /// </summary>
        /// <param name="newSlotId">订单槽唯一ID。</param>
        /// <param name="newDisplayName">订单归属显示名称。</param>
        /// <param name="newOrderSO">订单配置。</param>
        /// <param name="newOrdererAvatarSprite">订单者在小票上显示的头像。</param>
        public void Initialize(string newSlotId, string newDisplayName, OrderSO newOrderSO, Sprite newOrdererAvatarSprite = null)
        {
            slotId = newSlotId;
            displayName = newDisplayName;
            ordererAvatarSprite = newOrdererAvatarSprite;
            orderSO = newOrderSO;
            requirementData = newOrderSO != null ? newOrderSO.ToData() : null;
            submittedCoffee = null;
            scoringData = null;
            state = newOrderSO != null
                ? CustomerOrderSlotState.WaitingForSubmission
                : CustomerOrderSlotState.None;
        }

        /// <summary>
        /// 设置该订单槽在小票上显示的订单者头像。
        /// </summary>
        /// <param name="newOrdererAvatarSprite">需要显示的订单者头像。</param>
        public void SetOrdererAvatar(Sprite newOrdererAvatarSprite)
        {
            ordererAvatarSprite = newOrdererAvatarSprite;
        }

        /// <summary>
        /// 获取该订单槽的小票头像，未设置时返回指定备用头像。
        /// </summary>
        /// <param name="fallbackSprite">订单槽未配置头像时使用的备用头像。</param>
        /// <returns>最终用于小票显示的头像。</returns>
        public Sprite GetOrdererAvatar(Sprite fallbackSprite = null)
        {
            return ordererAvatarSprite != null
                ? ordererAvatarSprite
                : fallbackSprite;
        }

        /// <summary>
        /// 记录玩家提交的咖啡，并进入已提交状态。
        /// </summary>
        /// <param name="coffeeData">需要绑定到该订单槽的咖啡数据。</param>
        /// <returns>提交成功返回 true。</returns>
        public bool TrySubmitCoffee(CoffeeData coffeeData)
        {
            if (coffeeData == null || state != CustomerOrderSlotState.WaitingForSubmission)
            {
                return false;
            }

            submittedCoffee = coffeeData.Clone();
            state = CustomerOrderSlotState.Submitted;
            return true;
        }

        /// <summary>
        /// 记录评分结果，并进入完成状态。
        /// </summary>
        /// <param name="newScoringData">该订单槽的评分结果。</param>
        /// <returns>记录成功返回 true。</returns>
        public bool TryCompleteScoring(CoffeeScoringData newScoringData)
        {
            if (newScoringData == null || state == CustomerOrderSlotState.None)
            {
                return false;
            }

            scoringData = newScoringData;
            state = CustomerOrderSlotState.Completed;
            return true;
        }

        /// <summary>
        /// 清空提交与评分结果，恢复为等待提交状态。
        /// </summary>
        public void ResetRuntimeResult()
        {
            submittedCoffee = null;
            scoringData = null;
            state = orderSO != null
                ? CustomerOrderSlotState.WaitingForSubmission
                : CustomerOrderSlotState.None;
        }
    }

    /// <summary>
    /// 单次顾客组接待的运行时订单会话，统一管理一个主顾客下的多张订单小票。
    /// </summary>
    [System.Serializable]
    public class CustomerOrderSessionData
    {
        [Header("会话信息")]
        [Tooltip("订单会话唯一ID")]
        public string sessionId;

        [Tooltip("本次接待的主顾客")]
        public CustomerSO primaryCustomer;

        [Header("订单槽")]
        [Tooltip("本次顾客组接待内的所有订单槽")]
        public List<CustomerOrderSlotData> orderSlots = new List<CustomerOrderSlotData>();

        [Tooltip("当前选中的订单槽索引")]
        public int selectedSlotIndex = -1;

        /// <summary>
        /// 当前选中的订单槽。
        /// </summary>
        public CustomerOrderSlotData SelectedSlot
        {
            get
            {
                if (selectedSlotIndex < 0 || selectedSlotIndex >= orderSlots.Count)
                {
                    return null;
                }

                return orderSlots[selectedSlotIndex];
            }
        }

        /// <summary>
        /// 当前订单槽数量。
        /// </summary>
        public int SlotCount => orderSlots.Count;

        /// <summary>
        /// 是否存在等待提交的订单槽。
        /// </summary>
        public bool HasWaitingSlot => GetFirstWaitingSlot() != null;

        /// <summary>
        /// 是否所有订单槽都已经提交过咖啡。
        /// </summary>
        public bool IsAllSubmitted
        {
            get
            {
                if (orderSlots.Count == 0)
                {
                    return false;
                }

                foreach (CustomerOrderSlotData slot in orderSlots)
                {
                    if (slot == null || slot.state == CustomerOrderSlotState.WaitingForSubmission)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 是否所有订单槽都完成评分。
        /// </summary>
        public bool IsAllCompleted
        {
            get
            {
                if (orderSlots.Count == 0)
                {
                    return false;
                }

                foreach (CustomerOrderSlotData slot in orderSlots)
                {
                    if (slot == null || !slot.IsCompleted)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 初始化顾客组订单会话。
        /// </summary>
        /// <param name="newSessionId">会话唯一ID。</param>
        /// <param name="newPrimaryCustomer">本次接待的主顾客。</param>
        public void Initialize(string newSessionId, CustomerSO newPrimaryCustomer)
        {
            sessionId = newSessionId;
            primaryCustomer = newPrimaryCustomer;
            orderSlots.Clear();
            selectedSlotIndex = -1;
        }

        /// <summary>
        /// 添加一个订单槽，并在没有选中项时自动选中第一张小票。
        /// </summary>
        /// <param name="slot">需要加入会话的订单槽。</param>
        public void AddSlot(CustomerOrderSlotData slot)
        {
            if (slot == null)
            {
                return;
            }

            orderSlots.Add(slot);
            if (selectedSlotIndex < 0)
            {
                selectedSlotIndex = 0;
            }
        }

        /// <summary>
        /// 按索引选择订单槽。
        /// </summary>
        /// <param name="slotIndex">订单槽索引。</param>
        /// <returns>选择成功返回 true。</returns>
        public bool TrySelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= orderSlots.Count)
            {
                return false;
            }

            selectedSlotIndex = slotIndex;
            return true;
        }

        /// <summary>
        /// 按订单槽ID选择订单槽。
        /// </summary>
        /// <param name="slotId">订单槽唯一ID。</param>
        /// <returns>选择成功返回 true。</returns>
        public bool TrySelectSlot(string slotId)
        {
            for (int i = 0; i < orderSlots.Count; i++)
            {
                if (orderSlots[i] != null && orderSlots[i].slotId == slotId)
                {
                    selectedSlotIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 按订单槽ID获取订单槽。
        /// </summary>
        /// <param name="slotId">订单槽唯一ID。</param>
        /// <returns>找到时返回订单槽，否则返回 null。</returns>
        public CustomerOrderSlotData GetSlot(string slotId)
        {
            foreach (CustomerOrderSlotData slot in orderSlots)
            {
                if (slot != null && slot.slotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取第一张仍在等待提交的订单槽。
        /// </summary>
        /// <returns>存在等待提交的小票时返回订单槽，否则返回 null。</returns>
        public CustomerOrderSlotData GetFirstWaitingSlot()
        {
            foreach (CustomerOrderSlotData slot in orderSlots)
            {
                if (slot != null && slot.IsWaitingForSubmission)
                {
                    return slot;
                }
            }

            return null;
        }

        /// <summary>
        /// 自动选择第一张等待提交的小票。
        /// </summary>
        /// <returns>选择成功返回 true。</returns>
        public bool SelectFirstWaitingSlot()
        {
            for (int i = 0; i < orderSlots.Count; i++)
            {
                if (orderSlots[i] != null && orderSlots[i].IsWaitingForSubmission)
                {
                    selectedSlotIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 清空会话数据。
        /// </summary>
        public void Clear()
        {
            sessionId = string.Empty;
            primaryCustomer = null;
            orderSlots.Clear();
            selectedSlotIndex = -1;
        }
    }
}
