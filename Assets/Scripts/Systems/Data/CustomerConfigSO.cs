using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 顾客配置（CustomerConfigSO）。
    /// 定义：单个顾客的基础信息、外观、特性。
    /// 替代旧的顾客数据结构。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Customer/Customer Config", fileName = "CustomerConfig")]
    public class CustomerConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("顾客唯一标识符")]
        public string customerId;

        [Tooltip("顾客显示名称")]
        public string customerName;

        [Tooltip("顾客描述/背景故事")]
        [TextArea(3, 5)]
        public string description;

        [Header("外观")]
        [Tooltip("顾客立绘/头像")]
        public Sprite portrait;

        [Tooltip("顾客预制体（场景中的表现）")]
        public GameObject prefab;

        [Header("属性")]
        [Tooltip("是否为特殊顾客（Boss）")]
        public bool isSpecial = false;

        [Tooltip("耐心值（秒，超时会离开）")]
        public float patience = 60f;

        [Tooltip("小费倍率（1.0 = 正常小费，1.5 = 150% 小费）")]
        public float tipMultiplier = 1.0f;

        [Header("订单偏好")]
        [Tooltip("偏好的咖啡类型（空 = 无偏好）")]
        public string[] preferredCoffeeTypes = new string[0];

        [Tooltip("偏好的材料（空 = 无偏好）")]
        public string[] preferredMaterials = new string[0];

        [Header("对话")]
        [Tooltip("问候语（进店时）")]
        [TextArea(2, 3)]
        public string greetingDialogue;

        [Tooltip("满意对话（订单完美）")]
        [TextArea(2, 3)]
        public string satisfiedDialogue;

        [Tooltip("不满对话（订单失败/超时）")]
        [TextArea(2, 3)]
        public string dissatisfiedDialogue;
    }
}
