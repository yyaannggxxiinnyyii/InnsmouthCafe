using System;
using InnsmouthCafe.Data;
using UnityEngine;

/// <summary>
/// 图鉴管理器，负责永久记录角色图鉴解锁状态和红点提示状态。
/// </summary>
public class GalleryManager : Singleton<GalleryManager>
{
    private const string CharacterEncounteredPrefix = "Gallery_Character_Encountered_";
    private const string CharacterPerfectedPrefix = "Gallery_Character_Perfected_";
    private const string EndingUnlockedPrefix = "Gallery_Ending_Unlocked_";
    private const string CharacterUnreadKey = "Gallery_Unread_Character";
    private const string CollectibleUnreadKey = "Gallery_Unread_Collectible";
    private const string EndingUnreadKey = "Gallery_Unread_Ending";
    private const string GalleryGenerationKey = "Gallery_Generation";

    /// <summary>
    /// 图鉴状态变化事件，用于刷新图鉴 UI 和红点。
    /// </summary>
    public event Action OnGalleryChanged;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
        {
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 标记顾客已遇到，并设置角色图鉴红点。
    /// </summary>
    public bool MarkCharacterEncountered(CustomerSO customer)
    {
        if (!CanUseCustomerForGallery(customer))
        {
            return false;
        }

        string key = BuildCharacterKey(CharacterEncounteredPrefix, customer);
        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            return false;
        }

        PlayerPrefs.SetInt(key, 1);
        SetCharacterUnread();
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
        Debug.Log($"[Gallery] 解锁角色图鉴：{customer.customerName}");
        return true;
    }

    /// <summary>
    /// 标记顾客已 Perfect 接待，并解锁完整角色图鉴信息。
    /// </summary>
    public bool MarkCharacterPerfected(CustomerSO customer)
    {
        if (!CanUseCustomerForGallery(customer))
        {
            return false;
        }

        MarkCharacterEncountered(customer);

        string key = BuildCharacterKey(CharacterPerfectedPrefix, customer);
        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            return false;
        }

        PlayerPrefs.SetInt(key, 1);
        SetCharacterUnread();
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
        Debug.Log($"[Gallery] 完整解锁角色图鉴：{customer.customerName}");
        return true;
    }

    /// <summary>
    /// 查询顾客是否已遇到。
    /// </summary>
    public bool IsCharacterEncountered(CustomerSO customer)
    {
        if (!CanUseCustomerForGallery(customer))
        {
            return false;
        }

        return PlayerPrefs.GetInt(BuildCharacterKey(CharacterEncounteredPrefix, customer), 0) == 1;
    }

    /// <summary>
    /// 查询顾客是否已 Perfect 接待。
    /// </summary>
    public bool IsCharacterPerfected(CustomerSO customer)
    {
        if (!CanUseCustomerForGallery(customer))
        {
            return false;
        }

        return PlayerPrefs.GetInt(BuildCharacterKey(CharacterPerfectedPrefix, customer), 0) == 1;
    }

    /// <summary>
    /// 标记收集物图鉴有新内容，并设置收集物图鉴红点。
    /// </summary>
    public bool MarkCollectibleObtained(CollectibleSO collectible)
    {
        if (collectible == null)
        {
            return false;
        }

        SetCollectibleUnread();
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
        Debug.Log($"[Gallery] 解锁收集物图鉴：{collectible.collectibleName}");
        return true;
    }

    /// <summary>
    /// 标记指定结局已收集，并设置结局图鉴红点。
    /// </summary>
    public bool MarkEndingUnlocked(GameEnding ending)
    {
        string key = BuildEndingKey(ending);
        if (PlayerPrefs.GetInt(key, 0) == 1)
        {
            return false;
        }

        PlayerPrefs.SetInt(key, 1);
        SetEndingUnread();
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
        Debug.Log($"[Gallery] 解锁结局图鉴：{ending}");
        return true;
    }

    /// <summary>
    /// 查询指定结局是否已收集。
    /// </summary>
    public bool IsEndingUnlocked(GameEnding ending)
    {
        return PlayerPrefs.GetInt(BuildEndingKey(ending), 0) == 1;
    }

    /// <summary>
    /// 查询角色图鉴是否有未读更新。
    /// </summary>
    public bool HasCharacterUnread()
    {
        return PlayerPrefs.GetInt(CharacterUnreadKey, 0) == 1;
    }

    /// <summary>
    /// 查询收集物图鉴是否有未读更新。
    /// </summary>
    public bool HasCollectibleUnread()
    {
        return PlayerPrefs.GetInt(CollectibleUnreadKey, 0) == 1;
    }

    /// <summary>
    /// 查询结局图鉴是否有未读更新。
    /// </summary>
    public bool HasEndingUnread()
    {
        return PlayerPrefs.GetInt(EndingUnreadKey, 0) == 1;
    }

    /// <summary>
    /// 查询图鉴按钮是否需要显示红点。
    /// </summary>
    public bool HasAnyUnread()
    {
        return HasCharacterUnread() || HasCollectibleUnread() || HasEndingUnread();
    }

    /// <summary>
    /// 清除角色图鉴红点。
    /// </summary>
    public void ClearCharacterUnread()
    {
        if (!HasCharacterUnread())
        {
            return;
        }

        PlayerPrefs.DeleteKey(CharacterUnreadKey);
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
    }

    /// <summary>
    /// 清除收集物图鉴红点。
    /// </summary>
    public void ClearCollectibleUnread()
    {
        if (!HasCollectibleUnread())
        {
            return;
        }

        PlayerPrefs.DeleteKey(CollectibleUnreadKey);
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
    }

    /// <summary>
    /// 清除结局图鉴红点。
    /// </summary>
    public void ClearEndingUnread()
    {
        if (!HasEndingUnread())
        {
            return;
        }

        PlayerPrefs.DeleteKey(EndingUnreadKey);
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
    }

    /// <summary>
    /// 重置所有图鉴进度。
    /// </summary>
    public void ResetAllGalleryData()
    {
        PlayerPrefs.SetInt(GalleryGenerationKey, GetGalleryGeneration() + 1);
        PlayerPrefs.DeleteKey(CharacterUnreadKey);
        PlayerPrefs.DeleteKey(CollectibleUnreadKey);
        PlayerPrefs.DeleteKey(EndingUnreadKey);
        PlayerPrefs.Save();
        OnGalleryChanged?.Invoke();
        Debug.Log("[Gallery] 图鉴进度已重置");
    }

    /// <summary>
    /// 设置角色图鉴未读状态。
    /// </summary>
    private void SetCharacterUnread()
    {
        PlayerPrefs.SetInt(CharacterUnreadKey, 1);
    }

    /// <summary>
    /// 设置收集物图鉴未读状态。
    /// </summary>
    private void SetCollectibleUnread()
    {
        PlayerPrefs.SetInt(CollectibleUnreadKey, 1);
    }

    /// <summary>
    /// 设置结局图鉴未读状态。
    /// </summary>
    private void SetEndingUnread()
    {
        PlayerPrefs.SetInt(EndingUnreadKey, 1);
    }

    /// <summary>
    /// 判断顾客是否可以进入角色图鉴。
    /// </summary>
    private bool CanUseCustomerForGallery(CustomerSO customer)
    {
        return customer != null;
    }

    /// <summary>
    /// 构建角色图鉴 PlayerPrefs 键名。
    /// </summary>
    private string BuildCharacterKey(string prefix, CustomerSO customer)
    {
        string id = string.IsNullOrEmpty(customer.customerId) ? customer.name : customer.customerId;
        return $"{prefix}{GetGalleryGeneration()}_{id}";
    }

    /// <summary>
    /// 构建结局图鉴 PlayerPrefs 键名。
    /// </summary>
    private string BuildEndingKey(GameEnding ending)
    {
        return $"{EndingUnlockedPrefix}{GetGalleryGeneration()}_{ending}";
    }

    /// <summary>
    /// 获取当前图鉴存档代际；重置图鉴时递增该值，让旧键自然失效。
    /// </summary>
    private int GetGalleryGeneration()
    {
        return PlayerPrefs.GetInt(GalleryGenerationKey, 0);
    }
}
