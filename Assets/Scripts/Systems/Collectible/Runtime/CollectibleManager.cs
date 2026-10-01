using System;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.UI;

/// <summary>
/// 收集物管理器
/// 负责收集物的获取判定、持久化存储和前台展示控制
/// </summary>
public class CollectibleManager : Singleton<CollectibleManager>
{
    [Header("前台展示配置")]
    [SerializeField]
    [Tooltip("收集物与前台展示物的绑定列表")]
    private List<CollectibleDisplayBinding> _displayBindings = new List<CollectibleDisplayBinding>();

    [Header("调试")]
    [SerializeField]
    [Tooltip("是否显示调试日志")]
    private bool _showDebugLog = true;

    /// <summary>PlayerPrefs 键名前缀</summary>
    private const string PrefKeyPrefix = "Collectible_";

    /// <summary>重置标记键名（跨场景通信用）</summary>
    private const string PrefKeyResetFlag = "Collectible_ResetFlag";

    /// <summary>收集物配置所在的 Resources 路径</summary>
    private const string CollectibleResourcePath = "SO/收集物SO";

    /// <summary>收集物获得事件，参数为获得的收集物SO</summary>
    public event Action<CollectibleSO> OnCollectibleObtained;

    /// <summary>获得提示弹窗（懒查找，允许场景中未挂载）</summary>
    private CollectibleNotifyUI _notifyUI;

    private void Start()
    {
        // 检查是否有待执行的重置标记（由主菜单场景的 GameManager 设置）
        if (PlayerPrefs.GetInt(PrefKeyResetFlag, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PrefKeyResetFlag);
            ResetAllCollectibles();
        }
        else
        {
            RefreshAllDisplays();
        }
    }

    /// <summary>
    /// 尝试获取收集物（由 NewCustomerManager 在顾客组订单全部 Perfect 后调用）
    /// </summary>
    /// <param name="customer">当前顾客配置</param>
    /// <param name="qualityLevel">咖啡品质等级</param>
    /// <returns>获得的收集物SO，无则返回null</returns>
    public CollectibleSO TryObtainCollectible(CustomerSO customer, CoffeeQuality qualityLevel)
    {
        if (customer == null) return null;
        if (customer.specialProfile == null) return null;
        if (customer.specialProfile.collectibleRewardOnPerfect == null) return null;
        if (qualityLevel != CoffeeQuality.Perfect) return null;

        CollectibleSO collectible = customer.specialProfile.collectibleRewardOnPerfect;

        // 已拥有则不重复获取
        if (HasCollectible(collectible))
        {
            if (_showDebugLog)
                Debug.Log($"[Collectible] 已拥有收集物: {collectible.collectibleName}，跳过");
            return null;
        }

        // 持久化存储
        PlayerPrefs.SetInt(GetPrefKey(collectible), 1);
        PlayerPrefs.Save();

        // 刷新对应展示物
        RefreshDisplay(collectible);

        if (_showDebugLog)
            Debug.Log($"[Collectible] 获得收集物: {collectible.collectibleName}");

        GalleryManager.Instance?.MarkCollectibleObtained(collectible);

        // 弹出获得提示：Show 内部会暂停游戏时间，点确认后恢复
        if (_notifyUI == null)
        {
            _notifyUI = FindObjectOfType<CollectibleNotifyUI>();
        }

        if (_notifyUI != null)
        {
            _notifyUI.Show(collectible, null);
        }

        OnCollectibleObtained?.Invoke(collectible);
        return collectible;
    }

    /// <summary>
    /// 查询是否已拥有指定收集物
    /// </summary>
    public bool HasCollectible(CollectibleSO collectible)
    {
        if (collectible == null) return false;
        return PlayerPrefs.GetInt(GetPrefKey(collectible), 0) == 1;
    }

    /// <summary>
    /// 刷新所有前台展示物的显隐状态
    /// </summary>
    public void RefreshAllDisplays()
    {
        foreach (var binding in _displayBindings)
        {
            if (binding.collectible == null || binding.displayObject == null)
                continue;

            binding.displayObject.SetActive(HasCollectible(binding.collectible));
        }
    }

    /// <summary>
    /// 刷新指定收集物的前台展示
    /// </summary>
    private void RefreshDisplay(CollectibleSO collectible)
    {
        foreach (var binding in _displayBindings)
        {
            if (binding.collectible == collectible && binding.displayObject != null)
            {
                binding.displayObject.SetActive(true);
                break;
            }
        }
    }

    /// <summary>
    /// 获取收集物的 PlayerPrefs 键名
    /// </summary>
    private string GetPrefKey(CollectibleSO collectible)
    {
        return PrefKeyPrefix + collectible.collectibleId;
    }

    /// <summary>
    /// 重置所有收集物（清除持久化数据并隐藏前台展示）
    /// </summary>
    public void ResetAllCollectibles()
    {
        // 必须按资源全量清键，不能只遍历 _displayBindings：
        // 前台展示绑定允许为空，旧实现此时一件收集物也删不掉，重置形同虚设。
        CollectibleSO[] allCollectibles = Resources.LoadAll<CollectibleSO>(CollectibleResourcePath);
        int cleared = 0;

        foreach (CollectibleSO collectible in allCollectibles)
        {
            if (collectible == null || string.IsNullOrEmpty(collectible.collectibleId))
            {
                continue;
            }

            PlayerPrefs.DeleteKey(GetPrefKey(collectible));
            cleared++;
        }

        PlayerPrefs.DeleteKey(PrefKeyResetFlag);
        PlayerPrefs.Save();
        RefreshAllDisplays();

        if (_showDebugLog)
            Debug.Log($"[Collectible] 所有收集物已重置（共 {cleared} 件）");
    }

#if UNITY_EDITOR
    /// <summary>编辑器调试：重置所有收集物</summary>
    [ContextMenu("调试：重置所有收集物")]
    private void DebugResetAll()
    {
        ResetAllCollectibles();
    }
#endif
}

/// <summary>
/// 收集物与前台展示物的绑定
/// </summary>
[Serializable]
public class CollectibleDisplayBinding
{
    [Tooltip("收集物配置")]
    public CollectibleSO collectible;

    [Tooltip("前台展示的 GameObject（默认隐藏，获得后显示）")]
    public GameObject displayObject;
}
