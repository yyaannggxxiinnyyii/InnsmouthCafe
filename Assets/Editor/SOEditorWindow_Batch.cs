using System.Collections.Generic;
using System.IO;
using System.Linq;
using InnsmouthCafe.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SOEditorWindow — 批量操作部分
/// </summary>
public partial class SOEditorWindow
{
    // ── 批量新建面板状态 ───────────────────────────────────────────────────
    private string _batchPrefix = "New_";
    private int _batchCount = 5;
    private string _batchSavePath = "Assets/Resources/SO";

    // ── 批量添加目标 ──────────────────────────────────────────────────────
    private OrderPoolSO _targetPool;
    private DayCustomerConfigSO _targetDayConfig;
    private int _targetDayConfigSlot; // 0=normalPool 1=extraCustomers 2=fixedCustomers

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>底部批量操作栏（固定在窗口底部）</summary>
    internal void DrawBatchActionBar(Rect rect)
    {
        GUILayout.BeginArea(rect);
        var boxStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(6, 6, 4, 4) };
        EditorGUILayout.BeginHorizontal(boxStyle);

        GUILayout.Label($"已选中 {SelectedGuids.Count} 个", GUILayout.Width(80));

        if (_currentTab == Tab.Order)
            DrawOrderBatchActions();
        else if (_currentTab == Tab.Customer)
            DrawCustomerBatchActions();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("批量复制", EditorStyles.miniButton, GUILayout.Width(60)))
            BatchDuplicate();

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("批量删除", EditorStyles.miniButton, GUILayout.Width(60)))
            BatchDelete();
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("取消选择", EditorStyles.miniButton, GUILayout.Width(60)))
            SelectedGuids.Clear();

        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ── 订单批量操作 ──────────────────────────────────────────────────────
    private void DrawOrderBatchActions()
    {
        GUILayout.Label("添加到订单池:", GUILayout.Width(70));
        _targetPool = (OrderPoolSO)EditorGUILayout.ObjectField(
            _targetPool, typeof(OrderPoolSO), false, GUILayout.Width(160));

        GUI.enabled = _targetPool != null;
        if (GUILayout.Button("执行添加", EditorStyles.miniButton, GUILayout.Width(60)))
            BatchAddOrdersToPool();
        GUI.enabled = true;
    }

    private void BatchAddOrdersToPool()
    {
        if (_targetPool == null) return;

        var so = new SerializedObject(_targetPool);
        var listProp = so.FindProperty("orders");

        // 收集已有的 GUID 避免重复
        var existingPaths = new HashSet<string>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (elem != null) existingPaths.Add(AssetDatabase.GetAssetPath(elem));
        }

        int added = 0;
        foreach (var guid in SelectedGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (existingPaths.Contains(path)) continue;
            var order = AssetDatabase.LoadAssetAtPath<OrderSO>(path);
            if (order == null) continue;

            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = order;
            added++;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"已向「{_targetPool.name}」添加 {added} 个订单。", "OK");
    }

    // ── 顾客批量操作 ──────────────────────────────────────────────────────
    private static readonly string[] DayConfigSlotLabels =
        { "普通顾客池", "额外顾客", "固定顾客" };

    private void DrawCustomerBatchActions()
    {
        GUILayout.Label("添加到每日配置:", GUILayout.Width(80));
        _targetDayConfig = (DayCustomerConfigSO)EditorGUILayout.ObjectField(
            _targetDayConfig, typeof(DayCustomerConfigSO), false, GUILayout.Width(150));

        _targetDayConfigSlot = EditorGUILayout.Popup(
            _targetDayConfigSlot, DayConfigSlotLabels, GUILayout.Width(80));

        GUI.enabled = _targetDayConfig != null;
        if (GUILayout.Button("执行添加", EditorStyles.miniButton, GUILayout.Width(60)))
            BatchAddCustomersToDayConfig();
        GUI.enabled = true;
    }

    private void BatchAddCustomersToDayConfig()
    {
        if (_targetDayConfig == null) return;

        var so = new SerializedObject(_targetDayConfig);
        string propName = _targetDayConfigSlot switch
        {
            0 => "normalCustomerPool",
            1 => "extraCustomers",
            _ => "fixedCustomers"
        };
        var listProp = so.FindProperty(propName);

        var existingPaths = new HashSet<string>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (elem != null) existingPaths.Add(AssetDatabase.GetAssetPath(elem));
        }

        int added = 0;
        foreach (var guid in SelectedGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (existingPaths.Contains(path)) continue;
            var customer = AssetDatabase.LoadAssetAtPath<CustomerSO>(path);
            if (customer == null) continue;

            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = customer;
            added++;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成",
            $"已向「{_targetDayConfig.name}」的{DayConfigSlotLabels[_targetDayConfigSlot]}添加 {added} 个顾客。", "OK");
    }

    // ── 批量复制 / 删除 ───────────────────────────────────────────────────
    private void BatchDuplicate()
    {
        int count = 0;
        foreach (var guid in SelectedGuids.ToList())
        {
            string src = AssetDatabase.GUIDToAssetPath(guid);
            string dst = AssetDatabase.GenerateUniqueAssetPath(src);
            if (AssetDatabase.CopyAsset(src, dst)) count++;
        }
        AssetDatabase.SaveAssets();
        RefreshAssets();
        EditorUtility.DisplayDialog("完成", $"已复制 {count} 个资源。", "OK");
    }

    private void BatchDelete()
    {
        if (!EditorUtility.DisplayDialog("确认删除",
            $"将永久删除选中的 {SelectedGuids.Count} 个资源，无法撤销。确定吗？", "删除", "取消"))
            return;

        foreach (var guid in SelectedGuids.ToList())
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        SelectedGuids.Clear();
        _selectedAsset = null;
        if (_cachedEditor != null) { DestroyImmediate(_cachedEditor); _cachedEditor = null; }
        AssetDatabase.SaveAssets();
        RefreshAssets();
    }

    // ── 批量新建面板 ──────────────────────────────────────────────────────
    internal void DrawBatchCreatePanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("批量新建", EditorStyles.boldLabel);

        _batchPrefix = EditorGUILayout.TextField("名称前缀", _batchPrefix);
        _batchCount  = EditorGUILayout.IntSlider("数量", _batchCount, 1, 20);

        EditorGUILayout.BeginHorizontal();
        _batchSavePath = EditorGUILayout.TextField("保存路径", _batchSavePath);
        if (GUILayout.Button("…", GUILayout.Width(24)))
        {
            string picked = EditorUtility.OpenFolderPanel("选择保存目录",
                _batchSavePath.Replace("Assets/", ""), "");
            if (!string.IsNullOrEmpty(picked))
            {
                // 转为相对路径
                int idx = picked.IndexOf("Assets/");
                if (idx >= 0) _batchSavePath = picked.Substring(idx);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button($"创建 {_batchCount} 个", EditorStyles.miniButton))
            ExecuteBatchCreate();

        EditorGUILayout.EndVertical();
    }

    private void ExecuteBatchCreate()
    {
        if (!Directory.Exists(_batchSavePath))
            Directory.CreateDirectory(_batchSavePath);

        System.Type type = _currentTab == Tab.Order ? typeof(OrderSO) : typeof(CustomerSO);
        int created = 0;
        for (int i = 1; i <= _batchCount; i++)
        {
            var asset = ScriptableObject.CreateInstance(type);
            string name = $"{_batchPrefix}{i:D2}";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{_batchSavePath}/{name}.asset");
            AssetDatabase.CreateAsset(asset, path);
            created++;
        }
        AssetDatabase.SaveAssets();
        ShowBatchCreate = false;
        RefreshAssets();
        EditorUtility.DisplayDialog("完成", $"已创建 {created} 个资源。", "OK");
    }
}
