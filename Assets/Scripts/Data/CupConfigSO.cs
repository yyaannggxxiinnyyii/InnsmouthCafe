using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 杯型配置数据
    /// 定义每种杯型的容量参数
    /// </summary>
    [CreateAssetMenu(fileName = "CupConfig", menuName = "InnsmouthCafe/Config/Cup Config", order = 1)]
    public class CupConfigSO : ScriptableObject
    {
        [Header("杯型信息")]
        [Tooltip("杯型类型")]
        public CupType cupType;

        [Tooltip("杯型名称")]
        public string cupName;

        [Header("容量参数")]
        [Tooltip("标准容量（毫升）- 推荐填充量")]
        public float standardVolume;

        [Tooltip("最大容量（毫升）- 超过此值会溢出")]
        public float maxVolume;

        [Header("咖啡豆参数")]
        [Tooltip("目标豆重（克）- 该杯型推荐的咖啡豆用量")]
        public float targetBeanWeight;

        [Header("UI显示")]
        [Tooltip("杯型图标")]
        public Sprite cupIcon;

        [Tooltip("杯型描述")]
        [TextArea(2, 4)]
        public string description;

        /// <summary>
        /// 咖啡液容量（毫升）
        /// 自动计算公式：目标豆重 × 5ml
        /// </summary>
        public float CoffeeLiquidVolume
        {
            get { return targetBeanWeight * 5f; }
        }

        /// <summary>
        /// 辅助液目标总量（毫升）
        /// 计算公式：标准容量 - 咖啡液容量
        /// </summary>
        public float TargetLiquidAmount
        {
            get { return standardVolume - CoffeeLiquidVolume; }
        }

        /// <summary>
        /// 危险区容量（毫升）
        /// 计算公式：最大容量 - 标准容量
        /// </summary>
        public float DangerZoneVolume
        {
            get { return maxVolume - standardVolume; }
        }

        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 确保最大容量大于标准容量
            if (maxVolume <= standardVolume)
            {
                Debug.LogWarning($"[{cupName}] 最大容量必须大于标准容量！");
            }

            // 确保咖啡液容量不超过标准容量
            if (CoffeeLiquidVolume >= standardVolume)
            {
                Debug.LogWarning($"[{cupName}] 咖啡液容量（{CoffeeLiquidVolume}ml）不应超过标准容量（{standardVolume}ml）！");
            }
        }
    }
}
