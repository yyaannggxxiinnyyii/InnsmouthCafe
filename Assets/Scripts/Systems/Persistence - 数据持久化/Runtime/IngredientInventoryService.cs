using System;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.Progression;
using UnityEngine;

namespace InnsmouthCafe.Persistence
{
    /// <summary>
    /// 当前存档的小料与辅助液库存服务。
    /// 材料配置来自 ToppingSO/LiquidSO，数量与解锁状态写入当前存档。
    /// </summary>
    [DisallowMultipleComponent]
    public class IngredientInventoryService : MonoBehaviour
    {
        private static IngredientInventoryService _instance;

        /// <summary>
        /// 获取材料库存服务单例。
        /// </summary>
        public static IngredientInventoryService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject serviceObject = new GameObject(nameof(IngredientInventoryService));
                    _instance = serviceObject.AddComponent<IngredientInventoryService>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 当前存档材料发生变化时触发。
        /// </summary>
        public event Action OnInventoryChanged;

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

        /// <summary>
        /// 查询辅助液是否已在当前存档解锁。
        /// </summary>
        public bool IsLiquidUnlocked(LiquidSO liquid)
        {
            return liquid != null
                && HasId(SaveSlotService.Instance.CurrentSave?.unlockedLiquidIds, liquid.liquidId);
        }

        /// <summary>
        /// 查询小料是否已在当前存档解锁。
        /// </summary>
        public bool IsToppingUnlocked(ToppingSO topping)
        {
            return topping != null
                && HasId(SaveSlotService.Instance.CurrentSave?.unlockedToppingIds, topping.toppingId);
        }

        /// <summary>
        /// 解锁辅助液并通知订阅者。
        /// </summary>
        public bool UnlockLiquid(LiquidSO liquid)
        {
            if (liquid == null || string.IsNullOrWhiteSpace(liquid.liquidId))
            {
                return false;
            }

            GameSaveData saveData = GetCurrentSave();
            if (saveData == null || HasId(saveData.unlockedLiquidIds, liquid.liquidId))
            {
                return false;
            }

            saveData.unlockedLiquidIds.Add(liquid.liquidId);
            TaskProgressService.Instance.RecordResourceUnlocked(liquid.liquidId, true);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 解锁小料并通知订阅者。
        /// </summary>
        public bool UnlockTopping(ToppingSO topping)
        {
            if (topping == null || string.IsNullOrWhiteSpace(topping.toppingId))
            {
                return false;
            }

            GameSaveData saveData = GetCurrentSave();
            if (saveData == null || HasId(saveData.unlockedToppingIds, topping.toppingId))
            {
                return false;
            }

            saveData.unlockedToppingIds.Add(topping.toppingId);
            TaskProgressService.Instance.RecordResourceUnlocked(topping.toppingId, false);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 获取辅助液在当前存档中的数量。
        /// </summary>
        public int GetLiquidAmount(LiquidSO liquid)
        {
            return liquid == null ? 0 : GetAmount(IngredientKind.Liquid, liquid.liquidId);
        }

        /// <summary>
        /// 获取小料在当前存档中的数量。
        /// </summary>
        public int GetToppingAmount(ToppingSO topping)
        {
            return topping == null ? 0 : GetAmount(IngredientKind.Topping, topping.toppingId);
        }

        /// <summary>
        /// 增加辅助液数量。
        /// </summary>
        public bool AddLiquid(LiquidSO liquid, int amount)
        {
            return liquid != null
                && AddAmount(IngredientKind.Liquid, liquid.liquidId, amount);
        }

        /// <summary>
        /// 增加小料数量。
        /// </summary>
        public bool AddTopping(ToppingSO topping, int amount)
        {
            return topping != null
                && AddAmount(IngredientKind.Topping, topping.toppingId, amount);
        }

        /// <summary>
        /// 尝试消耗辅助液数量。
        /// </summary>
        public bool TryConsumeLiquid(LiquidSO liquid, int amount)
        {
            return liquid != null
                && TryConsumeAmount(IngredientKind.Liquid, liquid.liquidId, amount);
        }

        /// <summary>
        /// 尝试消耗小料数量。
        /// </summary>
        public bool TryConsumeTopping(ToppingSO topping, int amount)
        {
            return topping != null
                && TryConsumeAmount(IngredientKind.Topping, topping.toppingId, amount);
        }

        /// <summary>
        /// 获取指定材料数量，供订单和制作系统按 ID 查询。
        /// </summary>
        public int GetAmount(IngredientKind ingredientKind, string itemId)
        {
            IngredientInventoryEntryData entry = FindEntry(ingredientKind, itemId);
            return entry?.amount ?? 0;
        }

        /// <summary>
        /// 增加指定材料数量。
        /// </summary>
        public bool AddAmount(IngredientKind ingredientKind, string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            GameSaveData saveData = GetCurrentSave();
            if (saveData == null)
            {
                return false;
            }

            IngredientInventoryEntryData entry = FindEntry(saveData, ingredientKind, itemId);
            if (entry == null)
            {
                entry = new IngredientInventoryEntryData
                {
                    ingredientKind = ingredientKind,
                    itemId = itemId,
                    amount = 0
                };
                saveData.ingredientInventory.Add(entry);
            }

            entry.amount = Mathf.Max(0, entry.amount + amount);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 尝试消耗指定材料数量。
        /// </summary>
        public bool TryConsumeAmount(IngredientKind ingredientKind, string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return false;
            }

            GameSaveData saveData = GetCurrentSave();
            IngredientInventoryEntryData entry = FindEntry(saveData, ingredientKind, itemId);
            if (entry == null || entry.amount < amount)
            {
                return false;
            }

            entry.amount -= amount;
            if (entry.amount == 0)
            {
                saveData.ingredientInventory.Remove(entry);
            }

            NotifyChanged();
            return true;
        }

        private GameSaveData GetCurrentSave()
        {
            GameSaveData saveData = SaveSlotService.Instance.CurrentSave;
            EnsureCollections(saveData);
            return saveData;
        }

        /// <summary>
        /// 确保新字段在加载旧版本存档时也有可写的集合实例。
        /// </summary>
        private void EnsureCollections(GameSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            if (saveData.ingredientInventory == null)
            {
                saveData.ingredientInventory = new List<IngredientInventoryEntryData>();
            }

            if (saveData.unlockedLiquidIds == null)
            {
                saveData.unlockedLiquidIds = new List<string>();
            }

            if (saveData.unlockedToppingIds == null)
            {
                saveData.unlockedToppingIds = new List<string>();
            }
        }

        private IngredientInventoryEntryData FindEntry(
            IngredientKind ingredientKind,
            string itemId)
        {
            return FindEntry(GetCurrentSave(), ingredientKind, itemId);
        }

        private IngredientInventoryEntryData FindEntry(
            GameSaveData saveData,
            IngredientKind ingredientKind,
            string itemId)
        {
            if (saveData?.ingredientInventory == null)
            {
                return null;
            }

            return saveData.ingredientInventory.Find(entry =>
                entry != null
                && entry.ingredientKind == ingredientKind
                && entry.itemId == itemId);
        }

        private bool HasId(List<string> ids, string itemId)
        {
            return ids != null && !string.IsNullOrWhiteSpace(itemId) && ids.Contains(itemId);
        }

        private void NotifyChanged()
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
