using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Persistence;

namespace InnsmouthCafe.Shop
{
    /// <summary>
    /// 店铺升级管理器。
    /// 负责管理店铺等级、读取等级配置、提供升级接口。
    /// </summary>
    public class ShopUpgradeManager : Singleton<ShopUpgradeManager>
    {
        [Header("店铺等级配置")]
        [Tooltip("所有店铺等级配置（按等级排序）")]
        [SerializeField] private List<ShopLevelConfigSO> _levelConfigs = new List<ShopLevelConfigSO>();

        [Header("调试信息 - 只读")]
        [Tooltip("当前店铺等级")]
        [SerializeField] private int _currentLevel = 1;

        /// <summary>
        /// 店铺升级事件。
        /// 参数：新等级。
        /// </summary>
        public event System.Action<int> OnShopUpgraded;

        /// <summary>
        /// 当前店铺等级。
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// 当前等级配置。
        /// </summary>
        public ShopLevelConfigSO CurrentLevelConfig => GetLevelConfig(_currentLevel);

        private void Start()
        {
            // 从存档读取店铺等级
            LoadShopLevel();
        }

        /// <summary>
        /// 从存档加载店铺等级。
        /// </summary>
        private void LoadShopLevel()
        {
            if (SaveSlotService.Instance?.CurrentSave != null)
            {
                _currentLevel = SaveSlotService.Instance.CurrentSave.shopLevel;
                Debug.Log($"[ShopUpgrade] 加载店铺等级：{_currentLevel}");
            }
            else
            {
                _currentLevel = 1;
                Debug.LogWarning("[ShopUpgrade] 存档不存在，使用默认等级 1");
            }
        }

        /// <summary>
        /// 获取指定等级的配置。
        /// </summary>
        /// <param name="level">等级（1-5）。</param>
        /// <returns>等级配置，如果找不到返回null。</returns>
        public ShopLevelConfigSO GetLevelConfig(int level)
        {
            if (_levelConfigs == null || _levelConfigs.Count == 0)
            {
                Debug.LogError("[ShopUpgrade] 店铺等级配置列表为空");
                return null;
            }

            foreach (var config in _levelConfigs)
            {
                if (config != null && config.level == level)
                {
                    return config;
                }
            }

            Debug.LogWarning($"[ShopUpgrade] 找不到等级 {level} 的配置");
            return null;
        }

        /// <summary>
        /// 尝试升级店铺。
        /// </summary>
        /// <returns>true表示升级成功，false表示升级失败。</returns>
        public bool TryUpgradeShop()
        {
            // 检查是否已达到最高等级
            if (_currentLevel >= 5)
            {
                Debug.LogWarning("[ShopUpgrade] 已达到最高等级");
                return false;
            }

            var currentConfig = GetLevelConfig(_currentLevel);
            if (currentConfig == null)
            {
                Debug.LogError("[ShopUpgrade] 当前等级配置不存在");
                return false;
            }

            // 检查金币是否足够
            if (SaveSlotService.Instance?.CurrentSave != null)
            {
                int currentCash = SaveSlotService.Instance.CurrentSave.dailyCash;
                if (currentCash < currentConfig.upgradeCost)
                {
                    Debug.Log($"[ShopUpgrade] 金币不足：需要 {currentConfig.upgradeCost}，当前 {currentCash}");
                    return false;
                }
            }

            // TODO: 检查其他升级条件（章节、收集度等）

            // 执行升级
            return UpgradeShop();
        }

        /// <summary>
        /// 执行店铺升级（内部方法）。
        /// </summary>
        /// <returns>true表示升级成功。</returns>
        private bool UpgradeShop()
        {
            var currentConfig = GetLevelConfig(_currentLevel);
            if (currentConfig == null)
            {
                return false;
            }

            // 扣除金币
            if (SaveSlotService.Instance?.CurrentSave != null)
            {
                SaveSlotService.Instance.CurrentSave.dailyCash -= currentConfig.upgradeCost;
            }

            // 升级
            _currentLevel++;

            // 保存到存档
            if (SaveSlotService.Instance?.CurrentSave != null)
            {
                SaveSlotService.Instance.CurrentSave.shopLevel = _currentLevel;
                SaveSlotService.Instance.SaveCurrent();
            }

            var newConfig = GetLevelConfig(_currentLevel);
            Debug.Log($"[ShopUpgrade] 店铺升级成功：{_currentLevel} ({newConfig?.levelName})");

            // 触发事件
            OnShopUpgraded?.Invoke(_currentLevel);

            return true;
        }

        /// <summary>
        /// 获取当前营业时间（秒）。
        /// </summary>
        /// <returns>营业时间（秒）。</returns>
        public int GetBusinessTimeSeconds()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.businessTimeSeconds : 480; // 默认8分钟
        }

        /// <summary>
        /// 获取顾客来访间隔（秒）。
        /// </summary>
        /// <returns>来访间隔（秒）。</returns>
        public float GetCustomerSpawnInterval()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.customerSpawnInterval : 100f; // 默认100秒
        }

        /// <summary>
        /// 获取订单价格倍率。
        /// </summary>
        /// <returns>价格倍率（例如 1.5 = 1.5倍）。</returns>
        public float GetOrderPriceMultiplier()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.orderPriceMultiplier : 1.0f; // 默认原价
        }

        /// <summary>
        /// 获取单晚最大订单数。
        /// </summary>
        /// <returns>最大订单数。</returns>
        public int GetMaxOrdersPerNight()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.maxOrdersPerNight : 6; // 默认6杯
        }

        /// <summary>
        /// 获取基础耐心加成（百分比）。
        /// </summary>
        /// <returns>耐心加成（例如 0.1 = +10%）。</returns>
        public float GetBasePatienceBonus()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.basePatienceBonus : 0f;
        }

        /// <summary>
        /// 获取最大等待客容量。
        /// </summary>
        /// <returns>容量数量。</returns>
        public int GetMaxWaitingCustomers()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.maxWaitingCustomers : 3;
        }

        /// <summary>
        /// 获取普通装饰槽位数量。
        /// </summary>
        /// <returns>槽位数量。</returns>
        public int GetNormalDecorationSlots()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.normalDecorationSlots : 3;
        }

        /// <summary>
        /// 获取Boss装饰槽位数量。
        /// </summary>
        /// <returns>槽位数量。</returns>
        public int GetBossDecorationSlots()
        {
            var config = CurrentLevelConfig;
            return config != null ? config.bossDecorationSlots : 0;
        }
    }
}
