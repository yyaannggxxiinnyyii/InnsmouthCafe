using System;
using System.Collections.Generic;
using System.Linq;
using InnsmouthCafe.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// InnsmouthCafe SO 资源编辑器
/// 菜单：InnsmouthCafe / SO 编辑器  (快捷键 Ctrl+Shift+E)
/// </summary>
public partial class SOEditorWindow : EditorWindow
{
    // ── 枚举 ──────────────────────────────────────────────────────────────
    internal enum Tab { Order, OrderPool, Customer, DayConfig, GameMode }

    // ── 持久状态 ──────────────────────────────────────────────────────────
    private Tab _currentTab = Tab.Order;
    private string _searchFilter = "";
    private Vector2 _leftScroll;
    private Vector2 _rightScroll;

    // ── 资源列表 ──────────────────────────────────────────────────────────
    internal struct AssetEntry
    {
        public string Guid;
        public string Path;
        public string Name;
    }
    internal List<AssetEntry> CurrentAssets = new();

    // ── 选中状态 ──────────────────────────────────────────────────────────
    private ScriptableObject _selectedAsset;
    private Editor _cachedEditor;
    internal readonly HashSet<string> SelectedGuids = new();

    // ── 批量新建弹窗状态（由 Batch partial 管理） ─────────────────────────
    internal bool ShowBatchCreate;

    // ── 样式缓存 ──────────────────────────────────────────────────────────
    private GUIStyle _selectedItemStyle;
    private GUIStyle _normalItemStyle;
    private bool _stylesInitialized;

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("InnsmouthCafe/SO 编辑器 %#e")]
    public static void Open()
    {
        var w = GetWindow<SOEditorWindow>("SO 编辑器");
        w.minSize = new Vector2(960, 580);
        w.Show();
    }

    private void OnEnable() => RefreshAssets();

    private void OnDisable()
    {
        if (_cachedEditor != null)
        {
            DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }
    }

    private void OnGUI()
    {
        InitStyles();

        // 所有区域全部用绝对 Rect，避免 GUILayout 流式布局干扰
        float toolbarH  = 22f;
        float batchBarH = (SelectedGuids.Count > 0) ? 28f : 0f;
        float leftW     = 270f;
        float divW      = 2f;
        float panelY    = toolbarH;
        float panelH    = position.height - toolbarH - batchBarH;

        DrawToolbar   (new Rect(0,          0,      position.width,            toolbarH));
        DrawLeftPanel (new Rect(0,          panelY, leftW,                     panelH));
        DrawDivider   (new Rect(leftW,      panelY, divW,                      panelH));
        DrawRightPanel(new Rect(leftW+divW, panelY, position.width-leftW-divW, panelH));

        if (SelectedGuids.Count > 0)
            DrawBatchActionBar(new Rect(0, position.height - batchBarH, position.width, batchBarH));
    }

    // ── 工具栏 ────────────────────────────────────────────────────────────
    private void DrawToolbar(Rect rect)
    {
        GUILayout.BeginArea(rect);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        string[] labels = { "订单 SO", "订单池 SO", "顾客 SO", "每日配置 SO", "游戏模式 SO" };
        for (int i = 0; i < labels.Length; i++)
        {
            bool active  = (int)_currentTab == i;
            bool clicked = GUILayout.Toggle(active, labels[i], EditorStyles.toolbarButton, GUILayout.Width(90));
            if (clicked && !active)
            {
                _currentTab = (Tab)i;
                ClearSelection();
                ShowBatchCreate = false;
                RefreshAssets();
            }
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("↺ 刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            RefreshAssets();

        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ── 左侧面板（手动 Rect 布局，确保底部按钮始终可见）────────────────────
    private void DrawLeftPanel(Rect totalRect)
    {
        bool canBatch = _currentTab is Tab.Order or Tab.Customer;

        // 计算底部按钮区域高度
        float bottomH = 24f; // 新建按钮行
        if (ShowBatchCreate && canBatch) bottomH += _batchPanelHeight;

        // 顶部搜索栏 20px
        float searchH = 20f;
        Rect searchRect = new Rect(totalRect.x, totalRect.y, totalRect.width, searchH);

        // 批量选择行 20px（仅 canBatch）
        float batchBarH = canBatch ? 20f : 0f;
        Rect batchBarRect = new Rect(totalRect.x, searchRect.yMax, totalRect.width, batchBarH);

        // 列表区域：剩余高度 - 底部
        float listY = batchBarRect.yMax;
        float listH = totalRect.height - searchH - batchBarH - bottomH - 4f;
        Rect listRect = new Rect(totalRect.x, listY, totalRect.width, Mathf.Max(listH, 20f));

        // 底部按钮区域
        Rect bottomRect = new Rect(totalRect.x, listRect.yMax + 2f, totalRect.width, bottomH);

        // ── 搜索栏 ────────────────────────────────────────────────────────
        GUILayout.BeginArea(searchRect);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        var newFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
        if (newFilter != _searchFilter) _searchFilter = newFilter;
        if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(18))) _searchFilter = "";
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();

        // ── 批量选择控件 ──────────────────────────────────────────────────
        if (canBatch)
        {
            GUILayout.BeginArea(batchBarRect);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (GUILayout.Button("全选", EditorStyles.miniButtonLeft, GUILayout.Width(38)))
                SelectedGuids.UnionWith(CurrentAssets.Select(a => a.Guid));
            if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(38)))
                SelectedGuids.Clear();
            GUILayout.Label($"已选 {SelectedGuids.Count} / {CurrentAssets.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ── 资源列表（带滚动条）──────────────────────────────────────────
        GUILayout.BeginArea(listRect);
        _leftScroll = GUILayout.BeginScrollView(_leftScroll);

        var filtered = string.IsNullOrEmpty(_searchFilter)
            ? CurrentAssets
            : CurrentAssets.Where(a => a.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        foreach (var entry in filtered)
        {
            EditorGUILayout.BeginHorizontal();
            if (canBatch)
            {
                bool was = SelectedGuids.Contains(entry.Guid);
                bool now = EditorGUILayout.Toggle(was, GUILayout.Width(16));
                if (now != was)
                {
                    if (now) SelectedGuids.Add(entry.Guid);
                    else SelectedGuids.Remove(entry.Guid);
                }
            }
            bool isMain = _selectedAsset != null
                && AssetDatabase.GetAssetPath(_selectedAsset) == entry.Path;
            var style = isMain ? _selectedItemStyle : _normalItemStyle;
            if (GUILayout.Button(entry.Name, style, GUILayout.ExpandWidth(true)))
                SelectAsset(entry.Path);
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // ── 底部按钮区域 ──────────────────────────────────────────────────
        GUILayout.BeginArea(bottomRect);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 新建", EditorStyles.miniButtonLeft))
            CreateNewAsset();
        if (canBatch)
        {
            var batchLabel = ShowBatchCreate ? "▲ 批量新建" : "▼ 批量新建";
            if (GUILayout.Button(batchLabel, EditorStyles.miniButtonRight))
                ShowBatchCreate = !ShowBatchCreate;
        }
        EditorGUILayout.EndHorizontal();

        if (ShowBatchCreate && canBatch)
            DrawBatchCreatePanel();

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    // 批量新建面板展开时的高度（用于 Rect 计算）
    private float _batchPanelHeight => 80f;

    // ── 分隔线 ────────────────────────────────────────────────────────────
    private static void DrawDivider(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.4f));
    }

    // ── 右侧面板 ──────────────────────────────────────────────────────────
    private void DrawRightPanel(Rect rightRect)
    {
        GUILayout.BeginArea(rightRect);

        if (_selectedAsset == null)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("← 从左侧选择一个资源进行编辑", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }
        else
        {
            // 顶部操作栏
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(_selectedAsset.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(40)))
                EditorGUIUtility.PingObject(_selectedAsset);
            if (GUILayout.Button("复制", EditorStyles.toolbarButton, GUILayout.Width(40)))
                DuplicateSelected();
            if (GUILayout.Button("删除", EditorStyles.toolbarButton, GUILayout.Width(40)))
                DeleteSelected();
            EditorGUILayout.EndHorizontal();

            // Inspector（带滚动条）
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            if (_cachedEditor == null || _cachedEditor.target != _selectedAsset)
            {
                if (_cachedEditor != null) DestroyImmediate(_cachedEditor);
                _cachedEditor = Editor.CreateEditor(_selectedAsset);
            }
            _cachedEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    // ── 内部工具 ──────────────────────────────────────────────────────────
    internal void SelectAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (asset == _selectedAsset) return;
        _selectedAsset = asset;
        if (_cachedEditor != null) { DestroyImmediate(_cachedEditor); _cachedEditor = null; }
        Selection.activeObject = asset;
        Repaint();
    }

    internal void ClearSelection()
    {
        SelectedGuids.Clear();
        _selectedAsset = null;
        if (_cachedEditor != null) { DestroyImmediate(_cachedEditor); _cachedEditor = null; }
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _normalItemStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(4, 4, 2, 2)
        };

        _selectedItemStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(4, 4, 2, 2),
            normal = { textColor = new Color(0.3f, 0.7f, 1f) }
        };
    }
}
