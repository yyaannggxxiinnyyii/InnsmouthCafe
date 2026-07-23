using System;
using System.Collections.Generic;
using System.Text;
using InnsmouthCafe.Data;
using UnityEngine;

/// <summary>
/// 材料解锁管理器，维护持久化的辅助液和小料解锁状态。
/// </summary>
public class IngredientUnlockManager : Singleton<IngredientUnlockManager>
{
    [Header("运行时观察")]
    [SerializeField]
    [Tooltip("当前已解锁辅助液列表，仅用于 Play Mode 调试观察")]
    private List<LiquidSO> _debugUnlockedLiquids = new List<LiquidSO>();

    [SerializeField]
    [Tooltip("当前已解锁小料列表，仅用于 Play Mode 调试观察")]
    private List<ToppingSO> _debugUnlockedToppings = new List<ToppingSO>();

    [Header("调试")]
    [SerializeField]
    [Tooltip("是否显示材料解锁调试日志")]
    private bool _showDebugLog = true;

    private readonly HashSet<LiquidSO> _unlockedLiquids = new HashSet<LiquidSO>();
    private readonly HashSet<ToppingSO> _unlockedToppings = new HashSet<ToppingSO>();

    private const string LiquidResourcePath = "SO/辅助液SO";
    private const string ToppingResourcePath = "SO/小料SO";
    private const string LiquidPrefKeyPrefix = "Ingredient_Liquid_";
    private const string ToppingPrefKeyPrefix = "Ingredient_Topping_";

    /// <summary>材料解锁状态变化事件。</summary>
    public event Action OnUnlockStateChanged;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);
        LoadPersistedUnlocks();
    }

    /// <summary>
    /// 查询辅助液是否有持久化解锁记录。
    /// </summary>
    public static bool HasLiquidUnlockRecord(LiquidSO liquid)
    {
        if (liquid == null || string.IsNullOrEmpty(liquid.liquidId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(GetLiquidPrefKey(liquid), 0) == 1;
    }

    /// <summary>
    /// 查询小料是否有持久化解锁记录。
    /// </summary>
    public static bool HasToppingUnlockRecord(ToppingSO topping)
    {
        if (topping == null || string.IsNullOrEmpty(topping.toppingId))
        {
            return false;
        }

        return PlayerPrefs.GetInt(GetToppingPrefKey(topping), 0) == 1;
    }

    /// <summary>
    /// 清除全部材料解锁记录，供清除游戏进度时调用。
    /// </summary>
    public static void ClearAllUnlocks()
    {
        foreach (LiquidSO liquid in Resources.LoadAll<LiquidSO>(LiquidResourcePath))
        {
            if (liquid != null && !string.IsNullOrEmpty(liquid.liquidId))
            {
                PlayerPrefs.DeleteKey(GetLiquidPrefKey(liquid));
            }
        }

        foreach (ToppingSO topping in Resources.LoadAll<ToppingSO>(ToppingResourcePath))
        {
            if (topping != null && !string.IsNullOrEmpty(topping.toppingId))
            {
                PlayerPrefs.DeleteKey(GetToppingPrefKey(topping));
            }
        }

        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.LoadPersistedUnlocks();
            Instance.OnUnlockStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// 查询辅助液是否已解锁。
    /// </summary>
    public bool IsLiquidUnlocked(LiquidSO liquid)
    {
        return HasLiquidUnlockRecord(liquid);
    }

    /// <summary>
    /// 查询小料是否已解锁。
    /// </summary>
    public bool IsToppingUnlocked(ToppingSO topping)
    {
        return HasToppingUnlockRecord(topping);
    }

    /// <summary>
    /// 解锁单个辅助液。
    /// </summary>
    public bool UnlockLiquid(LiquidSO liquid)
    {
        if (liquid == null || string.IsNullOrEmpty(liquid.liquidId))
        {
            return false;
        }

        bool added = !HasLiquidUnlockRecord(liquid);
        if (added)
        {
            PlayerPrefs.SetInt(GetLiquidPrefKey(liquid), 1);
            PlayerPrefs.Save();
            _unlockedLiquids.Add(liquid);
            RefreshDebugLists();
            LogUnlock($"辅助液：{liquid.liquidName}");
            OnUnlockStateChanged?.Invoke();
        }

        return added;
    }

    /// <summary>
    /// 解锁单个小料。
    /// </summary>
    public bool UnlockTopping(ToppingSO topping)
    {
        if (topping == null || string.IsNullOrEmpty(topping.toppingId))
        {
            return false;
        }

        bool added = !HasToppingUnlockRecord(topping);
        if (added)
        {
            PlayerPrefs.SetInt(GetToppingPrefKey(topping), 1);
            PlayerPrefs.Save();
            _unlockedToppings.Add(topping);
            RefreshDebugLists();
            LogUnlock($"小料：{topping.toppingName}");
            OnUnlockStateChanged?.Invoke();
        }

        return added;
    }

    /// <summary>
    /// 批量解锁辅助液。
    /// </summary>
    public int UnlockLiquids(IEnumerable<LiquidSO> liquids)
    {
        int unlockCount = 0;
        if (liquids == null)
        {
            return unlockCount;
        }

        foreach (LiquidSO liquid in liquids)
        {
            if (UnlockLiquid(liquid))
            {
                unlockCount++;
            }
        }

        return unlockCount;
    }

    /// <summary>
    /// 批量解锁小料。
    /// </summary>
    public int UnlockToppings(IEnumerable<ToppingSO> toppings)
    {
        int unlockCount = 0;
        if (toppings == null)
        {
            return unlockCount;
        }

        foreach (ToppingSO topping in toppings)
        {
            if (UnlockTopping(topping))
            {
                unlockCount++;
            }
        }

        return unlockCount;
    }

    /// <summary>
    /// 判断订单需求中所有辅助液和小料是否均已解锁。
    /// </summary>
    public bool CanFulfillOrder(OrderSO order)
    {
        if (order == null)
        {
            return false;
        }

        if (order.ignoreIngredientUnlockFilter)
        {
            return true;
        }

        if (order.liquidRequirements != null)
        {
            foreach (LiquidRequirementData requirement in order.liquidRequirements)
            {
                if (requirement != null && !IsLiquidUnlocked(requirement.liquid))
                {
                    return false;
                }
            }
        }

        List<ToppingSO> requiredToppings = order.toppingRequirement?.requiredToppings;
        if (requiredToppings != null)
        {
            foreach (ToppingSO topping in requiredToppings)
            {
                if (topping != null && !IsToppingUnlocked(topping))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 输出材料解锁日志。
    /// </summary>
    private void LogUnlock(string itemName)
    {
        if (!_showDebugLog)
        {
            return;
        }

        Debug.Log($"[IngredientUnlock] 解锁{itemName}");
    }

    /// <summary>
    /// 输出当前已解锁材料的完整列表，便于 Play Mode 排查每日解锁状态。
    /// </summary>
    public void LogCurrentUnlocks(string context)
    {
        if (!_showDebugLog)
        {
            return;
        }

        Debug.Log($"[IngredientUnlock] {context} 当前已解锁材料：{BuildUnlockSummary()}");
    }

    /// <summary>
    /// 构建当前已解锁材料摘要文本。
    /// </summary>
    private string BuildUnlockSummary()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("辅助液[");
        AppendLiquidNames(builder);
        builder.Append("] 小料[");
        AppendToppingNames(builder);
        builder.Append("]");
        return builder.ToString();
    }

    /// <summary>
    /// 追加已解锁辅助液名称。
    /// </summary>
    private void AppendLiquidNames(StringBuilder builder)
    {
        bool hasAny = false;
        foreach (LiquidSO liquid in _unlockedLiquids)
        {
            if (liquid == null)
            {
                continue;
            }

            if (hasAny)
            {
                builder.Append(", ");
            }

            builder.Append(liquid.liquidName);
            hasAny = true;
        }

        if (!hasAny)
        {
            builder.Append("无");
        }
    }

    /// <summary>
    /// 追加已解锁小料名称。
    /// </summary>
    private void AppendToppingNames(StringBuilder builder)
    {
        bool hasAny = false;
        foreach (ToppingSO topping in _unlockedToppings)
        {
            if (topping == null)
            {
                continue;
            }

            if (hasAny)
            {
                builder.Append(", ");
            }

            builder.Append(topping.toppingName);
            hasAny = true;
        }

        if (!hasAny)
        {
            builder.Append("无");
        }
    }

    /// <summary>
    /// 刷新 Inspector 中用于观察的已解锁材料列表。
    /// </summary>
    private void RefreshDebugLists()
    {
        _debugUnlockedLiquids.Clear();
        _debugUnlockedLiquids.AddRange(_unlockedLiquids);

        _debugUnlockedToppings.Clear();
        _debugUnlockedToppings.AddRange(_unlockedToppings);
    }

    /// <summary>
    /// 从持久化记录恢复运行时缓存，供启动和清除进度后刷新调试状态。
    /// </summary>
    private void LoadPersistedUnlocks()
    {
        _unlockedLiquids.Clear();
        _unlockedToppings.Clear();

        foreach (LiquidSO liquid in Resources.LoadAll<LiquidSO>(LiquidResourcePath))
        {
            if (HasLiquidUnlockRecord(liquid))
            {
                _unlockedLiquids.Add(liquid);
            }
        }

        foreach (ToppingSO topping in Resources.LoadAll<ToppingSO>(ToppingResourcePath))
        {
            if (HasToppingUnlockRecord(topping))
            {
                _unlockedToppings.Add(topping);
            }
        }

        RefreshDebugLists();
    }

    /// <summary>
    /// 获取辅助液解锁记录的存档键名。
    /// </summary>
    private static string GetLiquidPrefKey(LiquidSO liquid)
    {
        return $"{LiquidPrefKeyPrefix}{liquid.liquidId}";
    }

    /// <summary>
    /// 获取小料解锁记录的存档键名。
    /// </summary>
    private static string GetToppingPrefKey(ToppingSO topping)
    {
        return $"{ToppingPrefKeyPrefix}{topping.toppingId}";
    }
}
