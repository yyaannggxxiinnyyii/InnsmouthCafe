using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using InnsmouthCafe.Customer;
using InnsmouthCafe.UI;

namespace InnsmouthCafe.Editor
{
    /// <summary>
    /// 为新订单小票系统生成场景层级、条目预制体并完成基础引用绑定。
    /// </summary>
    public static class NewOrderTicketFrameworkGenerator
    {
        private const string RootName = "NewOrderTicketRoot | 小票系统";
        private const string ScenePath = "Assets/Scenes/GameScene_DioramaPrototype.unity";
        private const string PrefabDirectory = "Assets/Prefabs/UI/NewOrderTicket";

        /// <summary>
        /// 在当前打开的场景中生成新小票系统框架。
        /// </summary>
        [MenuItem("Innsmouth Cafe/新系统/生成新小票UI框架")]
        public static void Generate()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null && !Application.isPlaying)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                root = GameObject.Find(RootName);
            }

            if (root == null)
            {
                Debug.LogError($"[NewOrderTicketUI] 未找到场景对象：{RootName}");
                return;
            }

            EnsureDirectory(PrefabDirectory);

            Transform listRoot = GetOrCreateRect(root.transform, "TicketListRoot | 小票列表");
            ConfigureListRoot(listRoot);

            Transform detailPanel = GetOrCreateRect(root.transform, "OrderTicketDetailPanel | 小票详情");
            ConfigurePanelRoot(detailPanel);
            Transform content = GetOrCreateRect(detailPanel, "TicketContent | 详情内容");
            ConfigureContent(content);
            Transform coffeeRoot = GetOrCreateRect(content, "CoffeeRequirementsRoot | 咖啡需求");
            Transform liquidRoot = GetOrCreateRect(content, "LiquidRequirementsRoot | 辅助液需求");
            Transform toppingRoot = GetOrCreateRect(content, "ToppingRequirementsRoot | 小料需求");
            ConfigureVerticalGroup(coffeeRoot);
            ConfigureVerticalGroup(liquidRoot);
            ConfigureVerticalGroup(toppingRoot);

            Transform closeButton = GetOrCreateRect(detailPanel, "CloseButton | 收起");
            ConfigureButton(closeButton, "×");

            OrderTicketPreviewItemUI previewPrefab = CreatePreviewPrefab();
            if (previewPrefab == null)
            {
                return;
            }

            GameObject coffeePrefab = CreateCoffeeRequirementPrefab();
            GameObject liquidPrefab = CreateRequirementPrefab<LiquidRequirementItemUI>(
                "LiquidRequirementItem | 辅助液需求条目", "LiquidIcon", "容量");
            GameObject toppingPrefab = CreateRequirementPrefab<ToppingRequirementItemUI>(
                "ToppingRequirementItem | 小料需求条目", "ToppingIcon", "小料名称");

            OrderTicketDetailPanelUI panel = detailPanel.GetComponent<OrderTicketDetailPanelUI>();
            if (panel == null)
            {
                panel = detailPanel.gameObject.AddComponent<OrderTicketDetailPanelUI>();
            }

            SetObjectReference(panel, "_coffeeRequirementsContainer", coffeeRoot);
            SetObjectReference(panel, "_liquidRequirementsContainer", liquidRoot);
            SetObjectReference(panel, "_toppingRequirementsContainer", toppingRoot);
            SetObjectReference(panel, "_coffeeRequirementItemPrefab", coffeePrefab);
            SetObjectReference(panel, "_liquidRequirementItemPrefab", liquidPrefab);
            SetObjectReference(panel, "_toppingRequirementItemPrefab", toppingPrefab);
            SetObjectReference(panel, "_detailCanvasGroup", detailPanel.GetComponent<CanvasGroup>());
            SetObjectReference(panel, "_detailRect", content);
            SetObjectReference(panel, "_closeButton", closeButton.GetComponent<Button>());

            NewOrderTicketController controller = root.GetComponent<NewOrderTicketController>();
            if (controller == null)
            {
                controller = root.AddComponent<NewOrderTicketController>();
            }

            SetObjectReference(controller, "_ticketListRoot", listRoot);
            SetObjectReference(controller, "_ticketPreviewPrefab", previewPrefab);
            SetObjectReference(controller, "_detailPanel", panel);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[NewOrderTicketUI] 新小票UI框架生成完成，请在场景中检查布局。");
        }

        private static Transform GetOrCreateRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ConfigureListRoot(Transform target)
        {
            RectTransform rect = (RectTransform)target;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(40f, 0f);
            rect.sizeDelta = new Vector2(180f, 420f);
            AddComponentIfMissing<VerticalLayoutGroup>(target.gameObject).spacing = 12f;
            AddComponentIfMissing<ContentSizeFitter>(target.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigurePanelRoot(Transform target)
        {
            RectTransform rect = (RectTransform)target;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(520f, 620f);
            Image image = AddComponentIfMissing<Image>(target.gameObject);
            image.color = new Color(0.93f, 0.86f, 0.72f, 0.98f);
            AddComponentIfMissing<CanvasGroup>(target.gameObject);
        }

        private static void ConfigureContent(Transform target)
        {
            RectTransform rect = (RectTransform)target;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -32f);
            rect.sizeDelta = new Vector2(-48f, 520f);
            VerticalLayoutGroup layout = AddComponentIfMissing<VerticalLayoutGroup>(target.gameObject);
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddComponentIfMissing<ContentSizeFitter>(target.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureVerticalGroup(Transform target)
        {
            VerticalLayoutGroup layout = AddComponentIfMissing<VerticalLayoutGroup>(target.gameObject);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            AddComponentIfMissing<ContentSizeFitter>(target.gameObject).verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureButton(Transform target, string label)
        {
            RectTransform rect = (RectTransform)target;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-12f, -12f);
            rect.sizeDelta = new Vector2(42f, 42f);
            Button button = AddComponentIfMissing<Button>(target.gameObject);
            Image image = AddComponentIfMissing<Image>(target.gameObject);
            image.color = new Color(0.2f, 0.16f, 0.12f, 0.9f);
            GameObject textObject = GetOrCreateRect(target, "Label").gameObject;
            TextMeshProUGUI text = AddComponentIfMissing<TextMeshProUGUI>(textObject);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 24f;
            text.color = Color.white;
            button.targetGraphic = image;
        }

        private static OrderTicketPreviewItemUI CreatePreviewPrefab()
        {
            string path = $"{PrefabDirectory}/OrderTicketPreviewItem.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject item = existing != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(existing)
                : new GameObject("OrderTicketPreviewItem | 小票缩略", typeof(RectTransform));
            item.transform.SetParent(null);
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect == null)
            {
                Object.DestroyImmediate(item);
                Debug.LogError("[NewOrderTicketUI] 现有 OrderTicketPreviewItem 预制体缺少 RectTransform，请删除该预制体后重新生成。", item);
                return null;
            }
            rect.sizeDelta = new Vector2(180f, 92f);
            Image background = AddComponentIfMissing<Image>(item);
            background.color = new Color(0.96f, 0.91f, 0.8f, 1f);
            Button button = AddComponentIfMissing<Button>(item);
            button.targetGraphic = background;
            AddImageChild(item.transform, "ParticipantAvatar | 顾客头像", new Vector2(54f, 54f), new Vector2(-52f, 0f));
            AddImageChild(item.transform, "SelectedMarker | 当前订单", new Vector2(24f, 24f), new Vector2(62f, 22f));
            AddImageChild(item.transform, "CompletedMarker | 完成品质", new Vector2(30f, 30f), new Vector2(62f, -24f));
            OrderTicketPreviewItemUI component = AddComponentIfMissing<OrderTicketPreviewItemUI>(item);
            SetObjectReference(component, "_selectButton", button);
            SetObjectReference(component, "_participantAvatarImage", item.transform.Find("ParticipantAvatar | 顾客头像").GetComponent<Image>());
            SetObjectReference(component, "_selectedMarker", item.transform.Find("SelectedMarker | 当前订单").GetComponent<Image>());
            SetObjectReference(component, "_completedMarker", item.transform.Find("CompletedMarker | 完成品质").GetComponent<Image>());
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, path);
            Object.DestroyImmediate(item);
            return prefab.GetComponent<OrderTicketPreviewItemUI>();
        }

        private static GameObject CreateRequirementPrefab<T>(string name, string iconName, string textName) where T : Component
        {
            string fileName = name.Split('|')[0].Trim().Replace(' ', '_') + ".prefab";
            string path = $"{PrefabDirectory}/{fileName}";
            GameObject item = new GameObject(name, typeof(RectTransform));
            ((RectTransform)item.transform).sizeDelta = new Vector2(460f, 64f);
            AddImageChild(item.transform, iconName, new Vector2(48f, 48f), new Vector2(-195f, 0f));
            Transform textTransform = GetOrCreateRect(item.transform, textName);
            RectTransform textRect = (RectTransform)textTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(-165f, 0f);
            textRect.offsetMax = new Vector2(0f, 0f);
            TextMeshProUGUI text = AddComponentIfMissing<TextMeshProUGUI>(textTransform.gameObject);
            text.text = "需求";
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 20f;
            AddComponentIfMissing<T>(item);
            Component component = item.GetComponent<T>();
            SetObjectReference(component, GetFieldName<T>("Icon"), item.transform.Find(iconName).GetComponent<Image>());
            SetObjectReference(component, GetFieldName<T>("Text"), text);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, path);
            Object.DestroyImmediate(item);
            return prefab;
        }

        private static GameObject CreateCoffeeRequirementPrefab()
        {
            string path = $"{PrefabDirectory}/CoffeeRequirementItem.prefab";
            GameObject item = new GameObject("CoffeeRequirementItem | 咖啡需求条目", typeof(RectTransform));
            ((RectTransform)item.transform).sizeDelta = new Vector2(460f, 64f);
            AddImageChild(item.transform, "BeanIcon", new Vector2(48f, 48f), new Vector2(-195f, 0f));
            TextMeshProUGUI grindText = AddTextChild(item.transform, "GrindText | 研磨", "研磨");
            TextMeshProUGUI volumeText = AddTextChild(item.transform, "VolumeText | 容量", "容量");
            AddComponentIfMissing<CoffeeRequirementItemUI>(item);
            CoffeeRequirementItemUI component = item.GetComponent<CoffeeRequirementItemUI>();
            SetObjectReference(component, "_beanIcon", item.transform.Find("BeanIcon").GetComponent<Image>());
            SetObjectReference(component, "_grindText", grindText);
            SetObjectReference(component, "_volumeText", volumeText);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, path);
            Object.DestroyImmediate(item);
            return prefab;
        }

        private static TextMeshProUGUI AddTextChild(Transform parent, string name, string value)
        {
            Transform child = GetOrCreateRect(parent, name);
            RectTransform rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(-165f, 0f);
            rect.offsetMax = new Vector2(0f, 0f);
            TextMeshProUGUI text = AddComponentIfMissing<TextMeshProUGUI>(child.gameObject);
            text.text = value;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = 20f;
            return text;
        }

        private static string GetFieldName<T>(string suffix) where T : Component
        {
            if (typeof(T) == typeof(CoffeeRequirementItemUI)) return suffix == "Icon" ? "_beanIcon" : "_volumeText";
            if (typeof(T) == typeof(LiquidRequirementItemUI)) return suffix == "Icon" ? "_liquidIcon" : "_volumeText";
            return suffix == "Icon" ? "_toppingIcon" : "_nameText";
        }

        private static Image AddImageChild(Transform parent, string name, Vector2 size, Vector2 position)
        {
            Transform child = GetOrCreateRect(parent, name);
            RectTransform rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return AddComponentIfMissing<Image>(child.gameObject);
        }

        private static T AddComponentIfMissing<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private static void SetObjectReference(Object target, string property, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty propertyValue = serializedObject.FindProperty(property);
            if (propertyValue == null)
            {
                Debug.LogWarning($"[NewOrderTicketUI] 未找到序列化字段：{target.GetType().Name}.{property}");
                return;
            }

            propertyValue.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
