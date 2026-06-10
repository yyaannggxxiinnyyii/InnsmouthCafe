using System.Collections.Generic;
using System.IO;
using InnsmouthCafe.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SOEditorWindow — 资源管理工具方法
/// </summary>
public partial class SOEditorWindow
{
    // ── 默认保存路径映射 ──────────────────────────────────────────────────
    private static readonly Dictionary<Tab, string> DefaultSavePaths = new()
    {
        { Tab.Order,     "Assets/Resources/SO/订单SO/简单订单" },
        { Tab.OrderPool, "Assets/Resources/SO/订单池SO" },
        { Tab.Customer,  "Assets/Resources/SO/顾客SO" },
        { Tab.DayConfig, "Assets/Resources/SO/每日顾客SO" },
        { Tab.GameMode,  "Assets/Resources/SO/游戏模式SO" },
    };

    // ── 资源类型映射 ──────────────────────────────────────────────────────
    private static System.Type GetSOType(Tab tab) => tab switch
    {
        Tab.Order     => typeof(OrderSO),
        Tab.OrderPool => typeof(OrderPoolSO),
        Tab.Customer  => typeof(CustomerSO),
        Tab.DayConfig => typeof(DayCustomerConfigSO),
        Tab.GameMode  => typeof(GameModeConfigSO),
        _             => null
    };

    private static string GetTypeFilter(Tab tab) => tab switch
    {
        Tab.Order     => "t:OrderSO",
        Tab.OrderPool => "t:OrderPoolSO",
        Tab.Customer  => "t:CustomerSO",
        Tab.DayConfig => "t:DayCustomerConfigSO",
        Tab.GameMode  => "t:GameModeConfigSO",
        _             => ""
    };

    // ── 刷新资源列表 ──────────────────────────────────────────────────────
    internal void RefreshAssets()
    {
        CurrentAssets.Clear();
        string filter = GetTypeFilter(_currentTab);
        if (string.IsNullOrEmpty(filter)) return;

        string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            CurrentAssets.Add(new AssetEntry { Guid = guid, Path = path, Name = name });
        }

        // 按名称排序
        CurrentAssets.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));

        // 清理失效的批量选择
        SelectedGuids.RemoveWhere(g => !System.Array.Exists(guids, x => x == g));

        Repaint();
    }

    // ── 新建单个资源 ──────────────────────────────────────────────────────
    internal void CreateNewAsset()
    {
        var type = GetSOType(_currentTab);
        if (type == null) return;

        string defaultPath = DefaultSavePaths.TryGetValue(_currentTab, out var p) ? p : "Assets/Resources/SO";
        string savePath = EditorUtility.SaveFilePanelInProject(
            $"新建 {type.Name}",
            $"New_{type.Name}",
            "asset",
            "选择保存位置",
            defaultPath);

        if (string.IsNullOrEmpty(savePath)) return;

        var asset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();
        RefreshAssets();
        SelectAsset(savePath);
    }

    // ── 复制当前选中资源 ──────────────────────────────────────────────────
    internal void DuplicateSelected()
    {
        if (_selectedAsset == null) return;

        string src = AssetDatabase.GetAssetPath(_selectedAsset);
        string dst = AssetDatabase.GenerateUniqueAssetPath(src);
        if (!AssetDatabase.CopyAsset(src, dst)) return;

        AssetDatabase.SaveAssets();
        RefreshAssets();
        SelectAsset(dst);
    }

    // ── 删除当前选中资源 ──────────────────────────────────────────────────
    internal void DeleteSelected()
    {
        if (_selectedAsset == null) return;

        string name = _selectedAsset.name;
        if (!EditorUtility.DisplayDialog("确认删除",
            $"将永久删除「{name}」，无法撤销。确定吗？", "删除", "取消"))
            return;

        string path = AssetDatabase.GetAssetPath(_selectedAsset);
        _selectedAsset = null;
        if (_cachedEditor != null) { DestroyImmediate(_cachedEditor); _cachedEditor = null; }

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.SaveAssets();
        RefreshAssets();
    }
}
