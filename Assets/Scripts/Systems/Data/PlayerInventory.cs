using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 玩家库存管理器（PlayerInventory）。
    /// 提供材料、金币、工具、载具、理智值的增减接口。
    /// 所有操作都直接修改当前存档（SaveManager.CurrentSave）。
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        private static PlayerInventory _instance;
        public static PlayerInventory Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerInventory");
                    _instance = go.AddComponent<PlayerInventory>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private SaveData CurrentSave => SaveManager.Instance.CurrentSave;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region 金币

        /// <summary>获取当前金币</summary>
        public int GetMoney()
        {
            return CurrentSave?.money ?? 0;
        }

        /// <summary>增加金币</summary>
        public bool AddMoney(int amount)
        {
            if (CurrentSave == null) return false;
            if (amount < 0)
            {
                Debug.LogWarning("[PlayerInventory] 金币增加量不能为负");
                return false;
            }

            CurrentSave.money += amount;
            Debug.Log($"[PlayerInventory] 金币 +{amount}，当前：{CurrentSave.money}");
            return true;
        }

        /// <summary>减少金币</summary>
        public bool RemoveMoney(int amount)
        {
            if (CurrentSave == null) return false;
            if (amount < 0)
            {
                Debug.LogWarning("[PlayerInventory] 金币减少量不能为负");
                return false;
            }

            if (CurrentSave.money < amount)
            {
                Debug.LogWarning($"[PlayerInventory] 金币不足，需要 {amount}，当前 {CurrentSave.money}");
                return false;
            }

            CurrentSave.money -= amount;
            Debug.Log($"[PlayerInventory] 金币 -{amount}，当前：{CurrentSave.money}");
            return true;
        }

        /// <summary>设置金币</summary>
        public void SetMoney(int amount)
        {
            if (CurrentSave == null) return;
            CurrentSave.money = Mathf.Max(0, amount);
        }

        #endregion

        #region 材料

        /// <summary>获取材料数量</summary>
        public int GetMaterialCount(string materialId)
        {
            if (CurrentSave == null) return 0;

            var dict = CurrentSave.materials.ToDictionary();
            return dict.ContainsKey(materialId) ? dict[materialId] : 0;
        }

        /// <summary>增加材料</summary>
        public bool AddMaterial(string materialId, int count)
        {
            if (CurrentSave == null) return false;
            if (string.IsNullOrEmpty(materialId) || count <= 0) return false;

            var dict = CurrentSave.materials.ToDictionary();
            if (dict.ContainsKey(materialId))
            {
                dict[materialId] += count;
            }
            else
            {
                dict[materialId] = count;
            }
            CurrentSave.materials.FromDictionary(dict);

            Debug.Log($"[PlayerInventory] 材料 {materialId} +{count}，当前：{dict[materialId]}");
            return true;
        }

        /// <summary>减少材料</summary>
        public bool RemoveMaterial(string materialId, int count)
        {
            if (CurrentSave == null) return false;
            if (string.IsNullOrEmpty(materialId) || count <= 0) return false;

            var dict = CurrentSave.materials.ToDictionary();
            if (!dict.ContainsKey(materialId) || dict[materialId] < count)
            {
                Debug.LogWarning($"[PlayerInventory] 材料 {materialId} 不足，需要 {count}，当前 {(dict.ContainsKey(materialId) ? dict[materialId] : 0)}");
                return false;
            }

            dict[materialId] -= count;
            if (dict[materialId] <= 0)
            {
                dict.Remove(materialId);
            }
            CurrentSave.materials.FromDictionary(dict);

            Debug.Log($"[PlayerInventory] 材料 {materialId} -{count}");
            return true;
        }

        #endregion

        #region 理智值

        /// <summary>获取当前理智值</summary>
        public float GetCurrentSanity()
        {
            return CurrentSave?.currentSanity ?? 0f;
        }

        /// <summary>获取理智值上限</summary>
        public float GetMaxSanity()
        {
            return CurrentSave?.maxSanity ?? 100f;
        }

        /// <summary>增加理智值</summary>
        public bool AddSanity(float amount)
        {
            if (CurrentSave == null) return false;

            CurrentSave.currentSanity = Mathf.Min(CurrentSave.currentSanity + amount, CurrentSave.maxSanity);
            return true;
        }

        /// <summary>减少理智值</summary>
        public bool RemoveSanity(float amount)
        {
            if (CurrentSave == null) return false;

            CurrentSave.currentSanity = Mathf.Max(CurrentSave.currentSanity - amount, 0f);
            return true;
        }

        /// <summary>设置理智值</summary>
        public void SetCurrentSanity(float value)
        {
            if (CurrentSave == null) return;
            CurrentSave.currentSanity = Mathf.Clamp(value, 0f, CurrentSave.maxSanity);
        }

        /// <summary>设置理智值上限</summary>
        public void SetMaxSanity(float value)
        {
            if (CurrentSave == null) return;
            CurrentSave.maxSanity = Mathf.Max(0f, value);
            CurrentSave.currentSanity = Mathf.Min(CurrentSave.currentSanity, CurrentSave.maxSanity);
        }

        #endregion

        #region 工具

        /// <summary>获取工具等级</summary>
        public int GetToolLevel(ToolType toolType)
        {
            if (CurrentSave == null) return 0;

            var dict = CurrentSave.toolLevels.ToDictionary();
            return dict.ContainsKey(toolType) ? dict[toolType] : 0;
        }

        /// <summary>升级工具</summary>
        public bool UpgradeTool(ToolType toolType)
        {
            if (CurrentSave == null) return false;

            var dict = CurrentSave.toolLevels.ToDictionary();
            int currentLevel = dict.ContainsKey(toolType) ? dict[toolType] : 0;

            if (currentLevel >= 5)
            {
                Debug.LogWarning($"[PlayerInventory] 工具 {toolType} 已达最大等级");
                return false;
            }

            dict[toolType] = currentLevel + 1;
            CurrentSave.toolLevels.FromDictionary(dict);

            Debug.Log($"[PlayerInventory] 工具 {toolType} 升级到 Lv.{dict[toolType]}");
            return true;
        }

        /// <summary>设置工具等级</summary>
        public void SetToolLevel(ToolType toolType, int level)
        {
            if (CurrentSave == null) return;

            level = Mathf.Clamp(level, 0, 5);
            var dict = CurrentSave.toolLevels.ToDictionary();
            dict[toolType] = level;
            CurrentSave.toolLevels.FromDictionary(dict);
        }

        #endregion

        #region 载具

        /// <summary>是否拥有载具</summary>
        public bool HasVehicle(string vehicleId)
        {
            if (CurrentSave == null) return false;
            return CurrentSave.ownedVehicles.Contains(vehicleId);
        }

        /// <summary>添加载具</summary>
        public bool AddVehicle(string vehicleId)
        {
            if (CurrentSave == null) return false;
            if (string.IsNullOrEmpty(vehicleId)) return false;

            if (CurrentSave.ownedVehicles.Contains(vehicleId))
            {
                Debug.LogWarning($"[PlayerInventory] 已拥有载具 {vehicleId}");
                return false;
            }

            CurrentSave.ownedVehicles.Add(vehicleId);
            Debug.Log($"[PlayerInventory] 获得载具 {vehicleId}");
            return true;
        }

        /// <summary>移除载具</summary>
        public bool RemoveVehicle(string vehicleId)
        {
            if (CurrentSave == null) return false;

            bool removed = CurrentSave.ownedVehicles.Remove(vehicleId);
            if (removed)
            {
                Debug.Log($"[PlayerInventory] 移除载具 {vehicleId}");
            }
            return removed;
        }

        #endregion

        #region 统计数据

        /// <summary>增加累计营业额</summary>
        public void AddRevenue(int amount)
        {
            if (CurrentSave == null) return;
            CurrentSave.totalRevenue += amount;
        }

        /// <summary>增加服务顾客数</summary>
        public void AddServedCustomer()
        {
            if (CurrentSave == null) return;
            CurrentSave.totalServedCustomers++;
        }

        /// <summary>增加完美订单数</summary>
        public void AddPerfectOrder()
        {
            if (CurrentSave == null) return;
            CurrentSave.totalPerfectOrders++;
        }

        /// <summary>增加采集材料数</summary>
        public void AddCollectedMaterial(int count = 1)
        {
            if (CurrentSave == null) return;
            CurrentSave.totalCollectedMaterials += count;
        }

        #endregion
    }
}
