using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 载具配置（VehicleConfigSO）。
    /// 定义：探索时使用的载具（船只），影响移动速度和理智抗性。
    /// 与探索系统对齐。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Explore/Vehicle Config", fileName = "VehicleConfig")]
    public class VehicleConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("载具唯一标识符")]
        public string vehicleId;

        [Tooltip("载具显示名称")]
        public string vehicleName;

        [Tooltip("载具描述")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("载具图标")]
        public Sprite icon;

        [Header("载具类型")]
        [Tooltip("载具类型（Basic=基础船, Advanced=进阶船, Speed=速度船）")]
        public VehicleType type;

        [Header("属性")]
        [Tooltip("当前等级（0-5）")]
        public int level = 0;

        [Tooltip("移动速度加成（倍率，1.0 = 无加成）")]
        public float speedMultiplier = 1.0f;

        [Tooltip("理智消耗抗性（0-1，0.2 = 减少 20% 消耗）")]
        [Range(0f, 1f)]
        public float sanityResistance = 0f;

        [Header("升级")]
        [Tooltip("升级到下一级所需金币")]
        public int upgradeCost = 100;

        [Tooltip("下一级载具配置（null = 已满级）")]
        public VehicleConfigSO nextLevelVehicle;

        [Header("购买")]
        [Tooltip("购买价格（初次获得）")]
        public int purchasePrice = 500;

        [Tooltip("是否初始拥有")]
        public bool isInitial = false;
    }

    /// <summary>载具类型枚举</summary>
    public enum VehicleType
    {
        Basic = 0,      // 基础船（平衡）
        Advanced = 1,   // 进阶船（高抗性）
        Speed = 2       // 速度船（高速度）
    }
}
