using System.Collections.Generic;
using System.Linq;
using InnsmouthCafe.Data;
using UnityEditor;
using UnityEngine;

/// <summary>
/// OrderSO 自定义 Inspector
/// 咖啡液/辅助液/小料区域用快捷卡片替代原生字段
/// </summary>
[CustomEditor(typeof(OrderSO))]
public class OrderSOEditor : Editor
{
    // ── 缓存的 SO 列表 ────────────────────────────────────────────────────
    private List<BeanSO>    _allBeans    = new();
    private List<LiquidSO>  _allLiquids  = new();
    private List<ToppingSO> _allToppings = new();

    // ── 图标纹理缓存 ──────────────────────────────────────────────────────
    private readonly Dictionary<Object, Texture2D> _iconCache = new();

    // ── 研磨度标签 ────────────────────────────────────────────────────────
    private static readonly string[] GrindLabels = { "粗", "中", "细" };
    private static readonly GrindType[] GrindValues =
        { GrindType.Coarse, GrindType.Fine, GrindType.ExtraFine };

    // ── 快捷模式开关（持久化到 EditorPrefs）────────────────────────────────
    private bool _quickMode;
    private const string QuickModeKey = "InnsmouthCafe_OrderSOEditor_QuickMode";

    // ── 样式缓存 ──────────────────────────────────────────────────────────
    private GUIStyle _cardSelectedStyle;
    private GUIStyle _cardNormalStyle;
    private bool _stylesReady;

    // ── SerializedProperty 缓存 ───────────────────────────────────────────
    private SerializedProperty _propOrderId;
    private SerializedProperty _propOrderName;
    private SerializedProperty _propDialogues;
    private SerializedProperty _propCoffeeReqs;
    private SerializedProperty _propLiquidReqs;
    private SerializedProperty _propToppingReq;

    // ─────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _quickMode = EditorPrefs.GetBool(QuickModeKey, true);

        _propOrderId    = serializedObject.FindProperty("orderId");
        _propOrderName  = serializedObject.FindProperty("orderName");
        _propDialogues  = serializedObject.FindProperty("orderDialogueTexts");
        _propCoffeeReqs = serializedObject.FindProperty("coffeeRequirements");
        _propLiquidReqs = serializedObject.FindProperty("liquidRequirements");
        _propToppingReq = serializedObject.FindProperty("toppingRequirement");

        RefreshSOLists();
    }

    private void RefreshSOLists()
    {
        _allBeans    = LoadAll<BeanSO>();
        _allLiquids  = LoadAll<LiquidSO>();
        _allToppings = LoadAll<ToppingSO>();
        _iconCache.Clear();
    }

    private static List<T> LoadAll<T>() where T : ScriptableObject
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" })
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(x => x != null)
            .OrderBy(x => x.name)
            .ToList();
    }

    // ─────────────────────────────────────────────────────────────────────
    public override void OnInspectorGUI()
    {
        InitStyles();
        serializedObject.Update();

        // ── 模式切换开关 ──────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.FlexibleSpace();
        var modeLabel = _quickMode ? "⚡ 快捷模式（切换为原生）" : "📋 原生模式（切换为快捷）";
        if (GUILayout.Button(modeLabel, EditorStyles.toolbarButton, GUILayout.Width(180)))
        {
            _quickMode = !_quickMode;
            EditorPrefs.SetBool(QuickModeKey, _quickMode);
        }
        EditorGUILayout.EndHorizontal();

        // ── 原生模式：直接用默认 Inspector ───────────────────────────────
        if (!_quickMode)
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // ── 快捷模式 ──────────────────────────────────────────────────────
        DrawSectionHeader("基础信息");
        EditorGUILayout.PropertyField(_propOrderId);
        EditorGUILayout.PropertyField(_propOrderName);
        EditorGUILayout.Space(4);

        DrawSectionHeader("点单文本");
        EditorGUILayout.PropertyField(_propDialogues, true);
        EditorGUILayout.Space(4);

        DrawSectionHeader("咖啡液需求");
        DrawCoffeeSection();
        EditorGUILayout.Space(4);

        DrawSectionHeader("辅助液需求");
        DrawLiquidSection();
        EditorGUILayout.Space(4);

        DrawSectionHeader("小料需求");
        DrawToppingSection();

        serializedObject.ApplyModifiedProperties();
    }

    // ── 咖啡液区域 ────────────────────────────────────────────────────────
    private void DrawCoffeeSection()
    {
        if (_allBeans.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到任何 BeanSO 资源", MessageType.Warning);
            return;
        }

        foreach (var bean in _allBeans)
        {
            int existingIdx = FindCoffeeIndex(bean);
            bool isSelected = existingIdx >= 0;

            DrawCardBegin(isSelected);

            // 图标 + 名称行（点击切换选中）
            EditorGUILayout.BeginHorizontal();
            var icon = GetIcon(bean.icon);
            if (icon != null)
                GUILayout.Label(icon, GUILayout.Width(36), GUILayout.Height(36));

            EditorGUILayout.BeginVertical();
            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            GUILayout.Label(bean.beanName ?? bean.name, nameStyle);

            // 选中/取消 按钮
            var toggleLabel = isSelected ? "✓ 已选中（点击取消）" : "点击选中";
            var toggleStyle = new GUIStyle(EditorStyles.miniButton);
            if (isSelected) toggleStyle.normal.textColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(130)))
            {
                if (isSelected)
                    RemoveCoffeeEntry(existingIdx);
                else
                    AddCoffeeEntry(bean);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                existingIdx = FindCoffeeIndex(bean);
                isSelected = existingIdx >= 0;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // 选中后显示研磨度 + 容量
            if (isSelected && existingIdx >= 0)
            {
                EditorGUILayout.Space(4);
                var entryProp = _propCoffeeReqs.GetArrayElementAtIndex(existingIdx);
                var grindProp  = entryProp.FindPropertyRelative("grindType");
                var volumeProp = entryProp.FindPropertyRelative("targetVolume");

                // 研磨度单选按钮组
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("研磨度", GUILayout.Width(48));
                // 找到当前枚举值在 GrindValues 中的索引
                int grindIdx = System.Array.IndexOf(GrindValues, (GrindType)grindProp.enumValueIndex);
                if (grindIdx < 0) grindIdx = 0;

                for (int g = 0; g < GrindLabels.Length; g++)
                {
                    bool active = grindIdx == g;
                    var btnStyle = active
                        ? new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Bold, normal = { textColor = Color.white } }
                        : EditorStyles.miniButtonMid;
                    var oldBg = GUI.backgroundColor;
                    if (active) GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                    if (GUILayout.Button(GrindLabels[g], btnStyle, GUILayout.Width(36)))
                    {
                        grindProp.enumValueIndex = (int)GrindValues[g];
                    }
                    GUI.backgroundColor = oldBg;
                }
                EditorGUILayout.EndHorizontal();

                // 容量步进
                DrawVolumeControl(volumeProp, 25, 25, 400);
            }

            DrawCardEnd();
            EditorGUILayout.Space(2);
        }
    }

    // ── 辅助液区域 ────────────────────────────────────────────────────────
    private void DrawLiquidSection()
    {
        if (_allLiquids.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到任何 LiquidSO 资源", MessageType.Warning);
            return;
        }

        foreach (var liquid in _allLiquids)
        {
            int existingIdx = FindLiquidIndex(liquid);
            bool isSelected = existingIdx >= 0;

            DrawCardBegin(isSelected);

            // 图标 + 名称行
            EditorGUILayout.BeginHorizontal();
            var icon = GetIcon(liquid.icon);
            if (icon != null)
                GUILayout.Label(icon, GUILayout.Width(36), GUILayout.Height(36));

            EditorGUILayout.BeginVertical();
            GUILayout.Label(liquid.liquidName ?? liquid.name, new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });

            var toggleLabel = isSelected ? "✓ 已选中（点击取消）" : "点击选中";
            var toggleStyle = new GUIStyle(EditorStyles.miniButton);
            if (isSelected) toggleStyle.normal.textColor = new Color(0.2f, 0.8f, 0.3f);
            if (GUILayout.Button(toggleLabel, toggleStyle, GUILayout.Width(130)))
            {
                if (isSelected)
                    RemoveLiquidEntry(existingIdx);
                else
                    AddLiquidEntry(liquid);
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                existingIdx = FindLiquidIndex(liquid);
                isSelected = existingIdx >= 0;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // 选中后显示容量控制（步进 25/50/100）
            if (isSelected && existingIdx >= 0)
            {
                EditorGUILayout.Space(4);
                var entryProp  = _propLiquidReqs.GetArrayElementAtIndex(existingIdx);
                var volumeProp = entryProp.FindPropertyRelative("targetVolume");
                DrawVolumeControlMultiStep(volumeProp);
            }

            DrawCardEnd();
            EditorGUILayout.Space(2);
        }
    }

    // ── 小料区域 ──────────────────────────────────────────────────────────
    private void DrawToppingSection()
    {
        if (_allToppings.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到任何 ToppingSO 资源", MessageType.Warning);
            return;
        }

        var requiredProp = _propToppingReq.FindPropertyRelative("requiredToppings");

        EditorGUILayout.BeginHorizontal();
        foreach (var topping in _allToppings)
        {
            bool isSelected = IsToppingSelected(requiredProp, topping);
            DrawToppingToggle(requiredProp, topping, isSelected);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToppingToggle(SerializedProperty listProp, ToppingSO topping, bool isSelected)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(64));

        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = isSelected ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);

        var icon = GetIcon(topping.icon);
        var content = icon != null ? new GUIContent(icon) : new GUIContent(topping.toppingName ?? topping.name);
        if (GUILayout.Button(content, GUILayout.Width(56), GUILayout.Height(56)))
        {
            if (isSelected)
                RemoveToppingEntry(listProp, topping);
            else
                AddToppingEntry(listProp, topping);
        }
        GUI.backgroundColor = oldBg;

        var labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };
        GUILayout.Label(topping.toppingName ?? topping.name, labelStyle, GUILayout.Width(56));

        EditorGUILayout.EndVertical();
    }

    // ── 容量步进控件（单一步长，左键+右键-）────────────────────────────────
    private static void DrawVolumeControl(SerializedProperty volumeProp, int step, int min, int max)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("容量", GUILayout.Width(48));
        GUILayout.Label($"{volumeProp.intValue} ml", EditorStyles.boldLabel, GUILayout.Width(60));

        // 用 Event.current.button 在按钮回调内判断左/右键
        // 注意：右键在 Inspector 里会被 Unity 拦截弹出菜单，改用 Ctrl+点击 减少
        if (GUILayout.Button($"+{step}", EditorStyles.miniButtonLeft, GUILayout.Width(36)))
        {
            volumeProp.intValue = Mathf.Clamp(volumeProp.intValue + step, min, max);
            GUI.changed = true;
        }
        if (GUILayout.Button($"-{step}", EditorStyles.miniButtonRight, GUILayout.Width(36)))
        {
            volumeProp.intValue = Mathf.Clamp(volumeProp.intValue - step, min, max);
            GUI.changed = true;
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── 容量步进控件（多步长 25/50/100，各自 +/- 按钮）──────────────────
    private static void DrawVolumeControlMultiStep(SerializedProperty volumeProp)
    {
        int[] steps = { 25, 50, 100 };
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("容量", GUILayout.Width(48));
        GUILayout.Label($"{volumeProp.intValue} ml", EditorStyles.boldLabel, GUILayout.Width(60));

        foreach (int step in steps)
        {
            if (GUILayout.Button($"+{step}", EditorStyles.miniButtonLeft, GUILayout.Width(38)))
            {
                volumeProp.intValue = Mathf.Max(0, volumeProp.intValue + step);
                GUI.changed = true;
            }
            if (GUILayout.Button($"-{step}", EditorStyles.miniButtonRight, GUILayout.Width(38)))
            {
                volumeProp.intValue = Mathf.Max(0, volumeProp.intValue - step);
                GUI.changed = true;
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    // ── 卡片样式辅助 ──────────────────────────────────────────────────────
    private void DrawCardBegin(bool selected)
    {
        var style = selected ? _cardSelectedStyle : _cardNormalStyle;
        EditorGUILayout.BeginVertical(style);
    }

    private static void DrawCardEnd()
    {
        EditorGUILayout.EndVertical();
    }

    private static void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(2);
        var rect = EditorGUILayout.GetControlRect(false, 20);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            padding = new RectOffset(6, 0, 2, 0),
            normal  = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };
        EditorGUI.LabelField(rect, title, style);
        EditorGUILayout.Space(2);
    }

    // ── 查找 / 增删 咖啡液条目 ────────────────────────────────────────────
    private int FindCoffeeIndex(BeanSO bean)
    {
        for (int i = 0; i < _propCoffeeReqs.arraySize; i++)
        {
            var beanProp = _propCoffeeReqs.GetArrayElementAtIndex(i).FindPropertyRelative("bean");
            if (beanProp.objectReferenceValue == bean) return i;
        }
        return -1;
    }

    private void AddCoffeeEntry(BeanSO bean)
    {
        _propCoffeeReqs.arraySize++;
        var newEntry = _propCoffeeReqs.GetArrayElementAtIndex(_propCoffeeReqs.arraySize - 1);
        newEntry.FindPropertyRelative("bean").objectReferenceValue = bean;
        newEntry.FindPropertyRelative("grindType").enumValueIndex  = 0;
        newEntry.FindPropertyRelative("targetVolume").intValue     = 25;
    }

    private void RemoveCoffeeEntry(int index)
    {
        _propCoffeeReqs.DeleteArrayElementAtIndex(index);
    }

    // ── 查找 / 增删 辅助液条目 ────────────────────────────────────────────
    private int FindLiquidIndex(LiquidSO liquid)
    {
        for (int i = 0; i < _propLiquidReqs.arraySize; i++)
        {
            var liqProp = _propLiquidReqs.GetArrayElementAtIndex(i).FindPropertyRelative("liquid");
            if (liqProp.objectReferenceValue == liquid) return i;
        }
        return -1;
    }

    private void AddLiquidEntry(LiquidSO liquid)
    {
        _propLiquidReqs.arraySize++;
        var newEntry = _propLiquidReqs.GetArrayElementAtIndex(_propLiquidReqs.arraySize - 1);
        newEntry.FindPropertyRelative("liquid").objectReferenceValue = liquid;
        newEntry.FindPropertyRelative("targetVolume").intValue       = 25;
    }

    private void RemoveLiquidEntry(int index)
    {
        _propLiquidReqs.DeleteArrayElementAtIndex(index);
    }

    // ── 查找 / 增删 小料条目 ──────────────────────────────────────────────
    private static bool IsToppingSelected(SerializedProperty listProp, ToppingSO topping)
    {
        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == topping)
                return true;
        }
        return false;
    }

    private static void AddToppingEntry(SerializedProperty listProp, ToppingSO topping)
    {
        listProp.arraySize++;
        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = topping;
    }

    private static void RemoveToppingEntry(SerializedProperty listProp, ToppingSO topping)
    {
        for (int i = listProp.arraySize - 1; i >= 0; i--)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == topping)
            {
                listProp.DeleteArrayElementAtIndex(i);
                return;
            }
        }
    }

    // ── 图标纹理获取 ──────────────────────────────────────────────────────
    private Texture2D GetIcon(Sprite sprite)
    {
        if (sprite == null) return null;
        if (_iconCache.TryGetValue(sprite, out var cached)) return cached;

        Texture2D tex;
        var srcTex = sprite.texture;

        // 整张纹理就是这个 sprite，直接用
        if (sprite.rect.width  == srcTex.width &&
            sprite.rect.height == srcTex.height)
        {
            tex = srcTex;
        }
        else if (srcTex.isReadable)
        {
            // 裁剪 sprite 区域到独立纹理
            var rect = sprite.textureRect;
            tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
            tex.SetPixels(srcTex.GetPixels(
                (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height));
            tex.Apply();
        }
        else
        {
            // 纹理未开启 Read/Write，退回整张纹理
            tex = srcTex;
        }

        _iconCache[sprite] = tex;
        return tex;
    }

    // ── 样式初始化 ────────────────────────────────────────────────────────
    private void InitStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _cardNormalStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin  = new RectOffset(0, 0, 2, 2)
        };

        _cardSelectedStyle = new GUIStyle(_cardNormalStyle);
        _cardSelectedStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.7f, 0.25f));
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
