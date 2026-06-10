using System.Collections.Generic;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 图鉴面板UI，负责页签切换、红点提示和角色图鉴列表刷新。
    /// </summary>
    public class GalleryPanelUI : MonoBehaviour
    {
        [Header("入口")]
        [SerializeField]
        [Tooltip("打开图鉴按钮")]
        private Button _openButton;

        [SerializeField]
        [Tooltip("图鉴入口红点")]
        private GameObject _entryRedDot;

        [Header("面板")]
        [SerializeField]
        [Tooltip("图鉴面板 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        [Tooltip("关闭按钮")]
        private Button _closeButton;

        [Header("页签按钮")]
        [SerializeField]
        [Tooltip("角色页签按钮")]
        private Button _characterTabButton;

        [SerializeField]
        [Tooltip("收集物页签按钮")]
        private Button _collectibleTabButton;

        [SerializeField]
        [Tooltip("结局页签按钮")]
        private Button _endingTabButton;

        [SerializeField]
        [Tooltip("资源页签按钮")]
        private Button _ingredientTabButton;

        [Header("页签面板")]
        [SerializeField]
        [Tooltip("角色页签面板 CanvasGroup")]
        private CanvasGroup _characterPanel;

        [SerializeField]
        [Tooltip("收集物页签面板 CanvasGroup")]
        private CanvasGroup _collectiblePanel;

        [SerializeField]
        [Tooltip("结局页签面板 CanvasGroup")]
        private CanvasGroup _endingPanel;

        [SerializeField]
        [Tooltip("资源页签面板 CanvasGroup")]
        private CanvasGroup _ingredientPanel;

        [Header("角色图鉴")]
        [SerializeField]
        [Tooltip("角色图鉴候选顾客列表")]
        private List<CustomerSO> _characterEntries = new List<CustomerSO>();

        [SerializeField]
        [Tooltip("角色条目父节点")]
        private Transform _characterContentRoot;

        [SerializeField]
        [Tooltip("角色条目预制体")]
        private GalleryCharacterItemUI _characterItemPrefab;

        [SerializeField]
        [Tooltip("角色详情面板")]
        private GalleryCharacterDetailPanelUI _characterDetailPanel;

        [SerializeField]
        [Tooltip("角色立绘详情面板")]
        private GalleryCharacterPortraitPanelUI _characterPortraitPanel;

        [Header("收集物图鉴")]
        [SerializeField]
        [Tooltip("收集物图鉴候选配置列表；为空时从 Resources/SO/收集物SO 自动加载")]
        private List<CollectibleSO> _collectibleEntries = new List<CollectibleSO>();

        [SerializeField]
        [Tooltip("收集物条目父节点；为空时在收集物页下运行时创建")]
        private Transform _collectibleContentRoot;

        [SerializeField]
        [Tooltip("收集物条目预制体；为空时运行时创建基础条目")]
        private GalleryCollectibleItemUI _collectibleItemPrefab;

        [SerializeField]
        [Tooltip("收集物详情面板；为空时运行时创建基础详情面板")]
        private GalleryCollectibleDetailPanelUI _collectibleDetailPanel;

        [Header("资源图鉴")]
        [SerializeField]
        [Tooltip("辅助液图鉴候选配置列表；为空时从 Resources/SO/辅助液SO 自动加载")]
        private List<LiquidSO> _liquidEntries = new List<LiquidSO>();

        [SerializeField]
        [Tooltip("小料图鉴候选配置列表；为空时从 Resources/SO/小料SO 自动加载")]
        private List<ToppingSO> _toppingEntries = new List<ToppingSO>();

        [SerializeField]
        [Tooltip("辅助液条目父节点，对应辅助液 ScrollRect 的 Content")]
        private Transform _liquidContentRoot;

        [SerializeField]
        [Tooltip("小料条目父节点，对应小料 ScrollRect 的 Content")]
        private Transform _toppingContentRoot;

        [SerializeField]
        [Tooltip("资源条目预制体；为空时运行时创建基础条目")]
        private GalleryIngredientItemUI _ingredientItemPrefab;

        [Header("结局图鉴")]
        [SerializeField]
        [Tooltip("结局配置SO；为空时从 Resources/SO/结局SO 自动加载第一个配置")]
        private EndingConfigSO _endingConfigSO;

        [SerializeField]
        [Tooltip("结局条目父节点")]
        private Transform _endingContentRoot;

        [SerializeField]
        [Tooltip("结局条目预制体；为空时运行时创建基础条目")]
        private GalleryEndingItemUI _endingItemPrefab;

        [SerializeField]
        [Tooltip("结局重播面板；局外图鉴需要绑定 EndingPanelUI")]
        private EndingPanelUI _endingReplayPanel;

        [SerializeField]
        [Tooltip("是否允许从图鉴重播结局；局内图鉴保持关闭，局外图鉴打开")]
        private bool _allowEndingReplay;

        private readonly List<GalleryCharacterItemUI> _characterItems = new List<GalleryCharacterItemUI>();
        private readonly List<GalleryCollectibleItemUI> _collectibleItems = new List<GalleryCollectibleItemUI>();
        private readonly List<GalleryIngredientItemUI> _liquidItems = new List<GalleryIngredientItemUI>();
        private readonly List<GalleryIngredientItemUI> _toppingItems = new List<GalleryIngredientItemUI>();
        private readonly List<GalleryEndingItemUI> _endingItems = new List<GalleryEndingItemUI>();
        private readonly List<GameEnding> _unlockedEndingEntries = new List<GameEnding>();

        private const string CollectibleResourcePath = "SO/收集物SO";
        private const string CharacterResourcePath = "SO/顾客SO";
        private const string LiquidResourcePath = "SO/辅助液SO";
        private const string ToppingResourcePath = "SO/小料SO";
        private const string EndingResourcePath = "SO/结局SO";
        private const string CollectiblePrefKeyPrefix = "Collectible_";
        private const string EndingPrefKeyPrefix = "Gallery_Ending_Unlocked_";
        private const string GalleryGenerationKey = "Gallery_Generation";

        private void Awake()
        {
            _openButton?.onClick.AddListener(Show);
            _closeButton?.onClick.AddListener(Hide);
            _characterTabButton?.onClick.AddListener(ShowCharacterTab);
            _collectibleTabButton?.onClick.AddListener(ShowCollectibleTab);
            _endingTabButton?.onClick.AddListener(ShowEndingTab);
            _ingredientTabButton?.onClick.AddListener(ShowIngredientTab);

            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (GalleryManager.Instance != null)
            {
                GalleryManager.Instance.OnGalleryChanged += RefreshRedDot;
            }

            if (IngredientUnlockManager.Instance != null)
            {
                IngredientUnlockManager.Instance.OnUnlockStateChanged += RefreshIngredientLists;
            }

            RefreshRedDot();
        }

        private void OnDisable()
        {
            if (GalleryManager.Instance != null)
            {
                GalleryManager.Instance.OnGalleryChanged -= RefreshRedDot;
            }

            if (IngredientUnlockManager.Instance != null)
            {
                IngredientUnlockManager.Instance.OnUnlockStateChanged -= RefreshIngredientLists;
            }
        }

        /// <summary>
        /// 显示图鉴面板，并默认打开角色页签。
        /// </summary>
        public void Show()
        {
            SetPanelVisible(true);
            ShowCharacterTab();
        }

        /// <summary>
        /// 隐藏图鉴面板。
        /// </summary>
        public void Hide()
        {
            SetPanelVisible(false);
            _characterDetailPanel?.Hide();
            _characterPortraitPanel?.Hide();
            _collectibleDetailPanel?.Hide();
        }

        /// <summary>
        /// 显示角色页签。
        /// </summary>
        public void ShowCharacterTab()
        {
            SetActiveTab(_characterPanel);
            RefreshCharacterList();
            GalleryManager.Instance?.ClearCharacterUnread();
            RefreshRedDot();
        }

        /// <summary>
        /// 显示收集物页签。
        /// </summary>
        public void ShowCollectibleTab()
        {
            SetActiveTab(_collectiblePanel);
            RefreshCollectibleList();
            GalleryManager.Instance?.ClearCollectibleUnread();
            RefreshRedDot();
        }

        /// <summary>
        /// 显示结局页签。
        /// </summary>
        public void ShowEndingTab()
        {
            SetActiveTab(_endingPanel);
            RefreshEndingList();
            GalleryManager.Instance?.ClearEndingUnread();
            RefreshRedDot();
        }

        /// <summary>
        /// 设置图鉴是否允许重播结局；主菜单局外图鉴打开，局内图鉴关闭。
        /// </summary>
        public void SetEndingReplayEnabled(bool enabled)
        {
            _allowEndingReplay = enabled;
            RefreshEndingList();
        }

        /// <summary>
        /// 显示资源页签。
        /// </summary>
        public void ShowIngredientTab()
        {
            SetActiveTab(_ingredientPanel);
            RefreshIngredientLists();
        }

        /// <summary>
        /// 刷新角色图鉴列表。
        /// </summary>
        private void RefreshCharacterList()
        {
            EnsureCharacterEntriesLoaded();
            EnsureCharacterItems();

            for (int i = 0; i < _characterItems.Count; i++)
            {
                CustomerSO customer = i < _characterEntries.Count ? _characterEntries[i] : null;
                bool isEncountered = GalleryManager.Instance != null && GalleryManager.Instance.IsCharacterEncountered(customer);
                bool isPerfected = GalleryManager.Instance != null && GalleryManager.Instance.IsCharacterPerfected(customer);
                _characterItems[i].Configure(customer, isEncountered, isPerfected, ShowCharacterDetail);
            }
        }

        /// <summary>
        /// 在未手动配置角色列表时，从 Resources 自动加载全部顾客配置。
        /// </summary>
        private void EnsureCharacterEntriesLoaded()
        {
            if (_characterEntries.Count > 0)
            {
                return;
            }

            _characterEntries.AddRange(Resources.LoadAll<CustomerSO>(CharacterResourcePath));
            _characterEntries.Sort(CompareCustomers);
        }

        /// <summary>
        /// 确保角色条目实例数量与配置列表一致。
        /// </summary>
        private void EnsureCharacterItems()
        {
            if (_characterContentRoot == null || _characterItemPrefab == null)
            {
                return;
            }

            while (_characterItems.Count < _characterEntries.Count)
            {
                GalleryCharacterItemUI item = Instantiate(_characterItemPrefab, _characterContentRoot);
                _characterItems.Add(item);
            }

            for (int i = 0; i < _characterItems.Count; i++)
            {
                _characterItems[i].gameObject.SetActive(i < _characterEntries.Count);
            }
        }

        /// <summary>
        /// 刷新收集物图鉴列表。
        /// </summary>
        private void RefreshCollectibleList()
        {
            EnsureCollectibleEntriesLoaded();
            EnsureCollectibleItems();

            for (int i = 0; i < _collectibleItems.Count; i++)
            {
                CollectibleSO collectible = i < _collectibleEntries.Count ? _collectibleEntries[i] : null;
                bool isObtained = IsCollectibleObtained(collectible);
                _collectibleItems[i].Configure(collectible, isObtained, ShowCollectibleDetail);
            }
        }

        /// <summary>
        /// 在未手动配置收集物列表时，从 Resources 自动加载全部收集物配置。
        /// </summary>
        private void EnsureCollectibleEntriesLoaded()
        {
            if (_collectibleEntries.Count > 0)
            {
                return;
            }

            _collectibleEntries.AddRange(Resources.LoadAll<CollectibleSO>(CollectibleResourcePath));
            _collectibleEntries.Sort(CompareCollectibles);
        }

        /// <summary>
        /// 确保收集物条目实例数量与配置列表一致。
        /// </summary>
        private void EnsureCollectibleItems()
        {
            EnsureCollectibleContentRoot();

            if (_collectibleContentRoot == null)
            {
                return;
            }

            while (_collectibleItems.Count < _collectibleEntries.Count)
            {
                GalleryCollectibleItemUI item = CreateCollectibleItem(_collectibleContentRoot);
                _collectibleItems.Add(item);
            }

            for (int i = 0; i < _collectibleItems.Count; i++)
            {
                _collectibleItems[i].gameObject.SetActive(i < _collectibleEntries.Count);
            }
        }

        /// <summary>
        /// 确保收集物列表父节点存在；未绑定时创建带 GridLayoutGroup 的根节点。
        /// </summary>
        private void EnsureCollectibleContentRoot()
        {
            if (_collectibleContentRoot != null || _collectiblePanel == null)
            {
                return;
            }

            GameObject rootObject = new GameObject("收集物图鉴根节点", typeof(RectTransform), typeof(GridLayoutGroup));
            rootObject.transform.SetParent(_collectiblePanel.transform, false);
            RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            GridLayoutGroup gridLayout = rootObject.GetComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(36, 0, 36, 0);
            gridLayout.cellSize = new Vector2(250f, 350f);
            gridLayout.spacing = new Vector2(22f, 22f);

            _collectibleContentRoot = rootObject.transform;
        }

        /// <summary>
        /// 创建收集物条目实例，优先使用预制体，未配置时创建基础运行时条目。
        /// </summary>
        private GalleryCollectibleItemUI CreateCollectibleItem(Transform parent)
        {
            if (_collectibleItemPrefab != null)
            {
                return Instantiate(_collectibleItemPrefab, parent);
            }

            GameObject itemObject = new GameObject(
                "收集物图鉴条目",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(GalleryCollectibleItemUI));
            itemObject.transform.SetParent(parent, false);

            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(250f, 350f);

            Image background = itemObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.85f);

            Button button = itemObject.GetComponent<Button>();
            button.targetGraphic = background;

            Image iconImage = CreateRuntimeImage(
                "收集物图标",
                itemObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(190f, 190f),
                new Vector2(0f, 44f));

            TextMeshProUGUI nameText = CreateRuntimeText(
                "收集物名",
                itemObject.transform,
                "???",
                28f,
                TextAlignmentOptions.Center);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(210f, 70f);
            nameRect.anchoredPosition = new Vector2(0f, -120f);

            GalleryCollectibleItemUI item = itemObject.GetComponent<GalleryCollectibleItemUI>();
            item.UseRuntimeReferences(nameText, iconImage, button);
            return item;
        }

        /// <summary>
        /// 打开收集物详情面板。
        /// </summary>
        private void ShowCollectibleDetail(CollectibleSO collectible)
        {
            EnsureCollectibleDetailPanel();
            _collectibleDetailPanel?.Show(collectible);
            _characterDetailPanel?.Hide();
            _characterPortraitPanel?.Hide();
        }

        /// <summary>
        /// 确保收集物详情面板存在；未绑定时创建基础运行时详情面板。
        /// </summary>
        private void EnsureCollectibleDetailPanel()
        {
            if (_collectibleDetailPanel != null || _collectiblePanel == null)
            {
                return;
            }

            GameObject panelObject = new GameObject(
                "收集物详情面板",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(GalleryCollectibleDetailPanelUI));
            panelObject.transform.SetParent(_collectiblePanel.transform, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 420f);
            panelRect.anchoredPosition = Vector2.zero;

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.96f, 0.95f, 0.9f, 0.98f);

            CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
            Image iconImage = CreateRuntimeImage(
                "收集物详情图标",
                panelObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(180f, 180f),
                new Vector2(-190f, 38f));

            TextMeshProUGUI nameText = CreateRuntimeText(
                "收集物详情名",
                panelObject.transform,
                string.Empty,
                34f,
                TextAlignmentOptions.Left);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(320f, 64f);
            nameRect.anchoredPosition = new Vector2(120f, 126f);

            TextMeshProUGUI descriptionText = CreateRuntimeText(
                "收集物详情描述",
                panelObject.transform,
                string.Empty,
                24f,
                TextAlignmentOptions.TopLeft);
            RectTransform descriptionRect = descriptionText.GetComponent<RectTransform>();
            descriptionRect.sizeDelta = new Vector2(340f, 220f);
            descriptionRect.anchoredPosition = new Vector2(130f, -12f);

            Button closeButton = CreateRuntimeButton(
                "关闭收集物详情",
                panelObject.transform,
                "关闭",
                new Vector2(120f, 48f),
                new Vector2(240f, -166f));

            _collectibleDetailPanel = panelObject.GetComponent<GalleryCollectibleDetailPanelUI>();
            _collectibleDetailPanel.UseRuntimeReferences(
                canvasGroup,
                closeButton,
                iconImage,
                nameText,
                descriptionText);
        }

        /// <summary>
        /// 查询收集物是否已获得；场景中无 CollectibleManager 时回退到 PlayerPrefs。
        /// </summary>
        private bool IsCollectibleObtained(CollectibleSO collectible)
        {
            if (collectible == null)
            {
                return false;
            }

            if (CollectibleManager.Instance != null)
            {
                return CollectibleManager.Instance.HasCollectible(collectible);
            }

            return PlayerPrefs.GetInt($"{CollectiblePrefKeyPrefix}{collectible.collectibleId}", 0) == 1;
        }

        /// <summary>
        /// 按收集物ID和名称排序，保证自动加载列表顺序稳定。
        /// </summary>
        private int CompareCollectibles(CollectibleSO left, CollectibleSO right)
        {
            string leftKey = left == null ? string.Empty : $"{left.collectibleId}_{left.collectibleName}";
            string rightKey = right == null ? string.Empty : $"{right.collectibleId}_{right.collectibleName}";
            return string.CompareOrdinal(leftKey, rightKey);
        }

        /// <summary>
        /// 按顾客ID和名称排序，保证自动加载列表顺序稳定。
        /// </summary>
        private int CompareCustomers(CustomerSO left, CustomerSO right)
        {
            string leftKey = left == null ? string.Empty : $"{left.customerId}_{left.customerName}";
            string rightKey = right == null ? string.Empty : $"{right.customerId}_{right.customerName}";
            return string.CompareOrdinal(leftKey, rightKey);
        }

        /// <summary>
        /// 刷新资源图鉴中的辅助液和小料列表。
        /// </summary>
        private void RefreshIngredientLists()
        {
            EnsureIngredientEntriesLoaded();
            RefreshLiquidList();
            RefreshToppingList();
        }

        /// <summary>
        /// 在未手动配置资源列表时，从 Resources 自动加载辅助液和小料配置。
        /// </summary>
        private void EnsureIngredientEntriesLoaded()
        {
            if (_liquidEntries.Count == 0)
            {
                _liquidEntries.AddRange(Resources.LoadAll<LiquidSO>(LiquidResourcePath));
                _liquidEntries.Sort(CompareLiquids);
            }

            if (_toppingEntries.Count == 0)
            {
                _toppingEntries.AddRange(Resources.LoadAll<ToppingSO>(ToppingResourcePath));
                _toppingEntries.Sort(CompareToppings);
            }
        }

        /// <summary>
        /// 刷新辅助液资源图鉴列表。
        /// </summary>
        private void RefreshLiquidList()
        {
            if (!EnsureIngredientItems(_liquidItems, _liquidEntries.Count, _liquidContentRoot, "辅助液"))
            {
                return;
            }

            for (int i = 0; i < _liquidItems.Count; i++)
            {
                LiquidSO liquid = i < _liquidEntries.Count ? _liquidEntries[i] : null;
                bool isUnlocked = IsLiquidUnlocked(liquid);
                _liquidItems[i].Configure(
                    liquid != null ? liquid.liquidName : string.Empty,
                    liquid != null ? liquid.icon : null,
                    isUnlocked);
            }
        }

        /// <summary>
        /// 刷新小料资源图鉴列表。
        /// </summary>
        private void RefreshToppingList()
        {
            if (!EnsureIngredientItems(_toppingItems, _toppingEntries.Count, _toppingContentRoot, "小料"))
            {
                return;
            }

            for (int i = 0; i < _toppingItems.Count; i++)
            {
                ToppingSO topping = i < _toppingEntries.Count ? _toppingEntries[i] : null;
                bool isUnlocked = IsToppingUnlocked(topping);
                Sprite icon = GetToppingIcon(topping);
                _toppingItems[i].Configure(
                    topping != null ? topping.toppingName : string.Empty,
                    icon,
                    isUnlocked);
            }
        }

        /// <summary>
        /// 确保资源条目实例数量与配置列表一致。
        /// </summary>
        private bool EnsureIngredientItems(
            List<GalleryIngredientItemUI> items,
            int entryCount,
            Transform contentRoot,
            string itemTypeName)
        {
            if (contentRoot == null)
            {
                Debug.LogWarning($"[Gallery] 资源图鉴{itemTypeName}ContentRoot未绑定，无法刷新资源条目");
                return false;
            }

            while (items.Count < entryCount)
            {
                GalleryIngredientItemUI item = CreateIngredientItem(contentRoot);
                items.Add(item);
            }

            for (int i = 0; i < items.Count; i++)
            {
                items[i].gameObject.SetActive(i < entryCount);
            }

            return true;
        }

        /// <summary>
        /// 创建资源条目实例，优先使用预制体，未配置时创建基础运行时条目。
        /// </summary>
        private GalleryIngredientItemUI CreateIngredientItem(Transform parent)
        {
            if (_ingredientItemPrefab != null)
            {
                return Instantiate(_ingredientItemPrefab, parent);
            }

            GameObject itemObject = new GameObject(
                "资源图鉴条目",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(GalleryIngredientItemUI));
            itemObject.transform.SetParent(parent, false);

            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(250f, 350f);

            Image background = itemObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.85f);

            Image iconImage = CreateRuntimeImage(
                "资源图标",
                itemObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(190f, 190f),
                new Vector2(0f, 44f));

            TextMeshProUGUI nameText = CreateRuntimeText(
                "资源名",
                itemObject.transform,
                "???",
                28f,
                TextAlignmentOptions.Center);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(210f, 70f);
            nameRect.anchoredPosition = new Vector2(0f, -120f);

            GalleryIngredientItemUI item = itemObject.GetComponent<GalleryIngredientItemUI>();
            item.UseRuntimeReferences(nameText, iconImage);
            return item;
        }

        /// <summary>
        /// 查询辅助液是否已解锁。
        /// </summary>
        private bool IsLiquidUnlocked(LiquidSO liquid)
        {
            return IngredientUnlockManager.Instance != null
                   && IngredientUnlockManager.Instance.IsLiquidUnlocked(liquid);
        }

        /// <summary>
        /// 查询小料是否已解锁。
        /// </summary>
        private bool IsToppingUnlocked(ToppingSO topping)
        {
            return IngredientUnlockManager.Instance != null
                   && IngredientUnlockManager.Instance.IsToppingUnlocked(topping);
        }

        /// <summary>
        /// 获取小料图鉴显示图标，优先使用解锁提示专用图标。
        /// </summary>
        private Sprite GetToppingIcon(ToppingSO topping)
        {
            if (topping == null)
            {
                return null;
            }

            return topping.unlockIcon != null ? topping.unlockIcon : topping.icon;
        }

        /// <summary>
        /// 按辅助液ID和名称排序，保证自动加载列表顺序稳定。
        /// </summary>
        private int CompareLiquids(LiquidSO left, LiquidSO right)
        {
            string leftKey = left == null ? string.Empty : $"{left.liquidId}_{left.liquidName}";
            string rightKey = right == null ? string.Empty : $"{right.liquidId}_{right.liquidName}";
            return string.CompareOrdinal(leftKey, rightKey);
        }

        /// <summary>
        /// 按小料ID和名称排序，保证自动加载列表顺序稳定。
        /// </summary>
        private int CompareToppings(ToppingSO left, ToppingSO right)
        {
            string leftKey = left == null ? string.Empty : $"{left.toppingId}_{left.toppingName}";
            string rightKey = right == null ? string.Empty : $"{right.toppingId}_{right.toppingName}";
            return string.CompareOrdinal(leftKey, rightKey);
        }

        /// <summary>
        /// 刷新结局图鉴列表；未收集结局不生成条目。
        /// </summary>
        private void RefreshEndingList()
        {
            EnsureEndingConfigLoaded();
            RebuildUnlockedEndingEntries();

            if (!EnsureEndingItems())
            {
                return;
            }

            for (int i = 0; i < _endingItems.Count; i++)
            {
                if (i >= _unlockedEndingEntries.Count)
                {
                    _endingItems[i].gameObject.SetActive(false);
                    continue;
                }

                GameEnding ending = _unlockedEndingEntries[i];
                EndingConfig endingConfig = _endingConfigSO != null ? _endingConfigSO.GetEndingConfig(ending) : null;
                bool canViewDetail = _allowEndingReplay && ResolveEndingReplayPanel() != null;
                _endingItems[i].gameObject.SetActive(true);
                _endingItems[i].Configure(ending, endingConfig, canViewDetail, ShowEndingReplay);
            }
        }

        /// <summary>
        /// 在未手动配置结局配置时，从 Resources 自动加载第一个结局配置。
        /// </summary>
        private void EnsureEndingConfigLoaded()
        {
            if (_endingConfigSO != null)
            {
                return;
            }

            EndingConfigSO[] endingConfigs = Resources.LoadAll<EndingConfigSO>(EndingResourcePath);
            if (endingConfigs != null && endingConfigs.Length > 0)
            {
                _endingConfigSO = endingConfigs[0];
            }
        }

        /// <summary>
        /// 重建已收集结局列表，保持固定结局顺序。
        /// </summary>
        private void RebuildUnlockedEndingEntries()
        {
            _unlockedEndingEntries.Clear();
            AddEndingIfUnlocked(GameEnding.Lost);
            AddEndingIfUnlocked(GameEnding.Return);
            AddEndingIfUnlocked(GameEnding.Good);
        }

        /// <summary>
        /// 如果指定结局已收集，则加入本次显示列表。
        /// </summary>
        private void AddEndingIfUnlocked(GameEnding ending)
        {
            if (IsEndingUnlocked(ending))
            {
                _unlockedEndingEntries.Add(ending);
            }
        }

        /// <summary>
        /// 查询结局是否已收集；主菜单场景没有 GalleryManager 时回退到 PlayerPrefs。
        /// </summary>
        private bool IsEndingUnlocked(GameEnding ending)
        {
            if (GalleryManager.Instance != null)
            {
                return GalleryManager.Instance.IsEndingUnlocked(ending);
            }

            int generation = PlayerPrefs.GetInt(GalleryGenerationKey, 0);
            return PlayerPrefs.GetInt($"{EndingPrefKeyPrefix}{generation}_{ending}", 0) == 1;
        }

        /// <summary>
        /// 确保结局条目实例数量与已收集结局数量一致。
        /// </summary>
        private bool EnsureEndingItems()
        {
            if (_endingContentRoot == null)
            {
                Debug.LogWarning("[Gallery] 结局图鉴ContentRoot未绑定，无法刷新结局条目");
                return false;
            }

            while (_endingItems.Count < _unlockedEndingEntries.Count)
            {
                GalleryEndingItemUI item = CreateEndingItem(_endingContentRoot);
                _endingItems.Add(item);
            }

            for (int i = 0; i < _endingItems.Count; i++)
            {
                _endingItems[i].gameObject.SetActive(i < _unlockedEndingEntries.Count);
            }

            return true;
        }

        /// <summary>
        /// 创建结局条目实例，优先使用预制体，未配置时创建基础运行时条目。
        /// </summary>
        private GalleryEndingItemUI CreateEndingItem(Transform parent)
        {
            if (_endingItemPrefab != null)
            {
                return Instantiate(_endingItemPrefab, parent);
            }

            GameObject itemObject = new GameObject(
                "结局图鉴条目",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(GalleryEndingItemUI));
            itemObject.transform.SetParent(parent, false);

            RectTransform itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(300f, 380f);

            Image background = itemObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.85f);

            Image endingIcon = CreateRuntimeImage(
                "结局图标",
                itemObject.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(220f, 170f),
                new Vector2(0f, 62f));

            TextMeshProUGUI nameText = CreateRuntimeText(
                "结局名",
                itemObject.transform,
                string.Empty,
                28f,
                TextAlignmentOptions.Center);
            RectTransform nameRect = nameText.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(240f, 64f);
            nameRect.anchoredPosition = new Vector2(0f, -72f);

            Button detailButton = CreateRuntimeButton(
                "详细查看按钮",
                itemObject.transform,
                "详细查看",
                new Vector2(150f, 48f),
                new Vector2(0f, -150f));

            GalleryEndingItemUI item = itemObject.GetComponent<GalleryEndingItemUI>();
            item.UseRuntimeReferences(endingIcon, nameText, detailButton);
            return item;
        }

        /// <summary>
        /// 从局外图鉴重播指定结局；局内图鉴不允许重播。
        /// </summary>
        private void ShowEndingReplay(GameEnding ending)
        {
            EndingPanelUI endingReplayPanel = ResolveEndingReplayPanel();
            if (!_allowEndingReplay || endingReplayPanel == null)
            {
                return;
            }

            endingReplayPanel.ReplayEndingFromGallery(ending);
        }

        /// <summary>
        /// 获取用于图鉴重播的结局面板；未手动绑定时运行时查找当前可用实例。
        /// </summary>
        private EndingPanelUI ResolveEndingReplayPanel()
        {
            if (_endingReplayPanel != null)
            {
                return _endingReplayPanel;
            }

            _endingReplayPanel = FindObjectOfType<EndingPanelUI>(true);
            if (_endingReplayPanel == null && _allowEndingReplay)
            {
                Debug.LogWarning("[Gallery] 未找到 EndingPanelUI，结局图鉴详细查看将不可用");
            }

            return _endingReplayPanel;
        }

        /// <summary>
        /// 设置当前显示的页签面板。
        /// </summary>
        private void SetActiveTab(CanvasGroup activePanel)
        {
            SetCanvasGroupVisible(_characterPanel, activePanel == _characterPanel);
            SetCanvasGroupVisible(_collectiblePanel, activePanel == _collectiblePanel);
            SetCanvasGroupVisible(_endingPanel, activePanel == _endingPanel);
            SetCanvasGroupVisible(_ingredientPanel, activePanel == _ingredientPanel);
            _characterDetailPanel?.Hide();
            _characterPortraitPanel?.Hide();
            _collectibleDetailPanel?.Hide();
        }

        /// <summary>
        /// 打开角色详情面板。
        /// </summary>
        private void ShowCharacterDetail(CustomerSO customer, bool isPerfected)
        {
            _characterDetailPanel?.Show(customer, isPerfected, ShowCharacterPortraitDetail);
            _characterPortraitPanel?.Hide();
        }

        /// <summary>
        /// 打开角色四立绘详情面板。
        /// </summary>
        private void ShowCharacterPortraitDetail(CustomerSO customer, bool isPerfected)
        {
            _characterPortraitPanel?.Show(customer, isPerfected);
        }

        /// <summary>
        /// 创建运行时图片控件。
        /// </summary>
        private Image CreateRuntimeImage(
            string objectName,
            Transform parent,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;

            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            return image;
        }

        /// <summary>
        /// 创建运行时文本控件。
        /// </summary>
        private TextMeshProUGUI CreateRuntimeText(
            string objectName,
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 18f;
            textComponent.fontSizeMax = fontSize;
            textComponent.color = Color.black;
            textComponent.alignment = alignment;
            textComponent.enableWordWrapping = true;
            return textComponent;
        }

        /// <summary>
        /// 创建运行时按钮控件。
        /// </summary>
        private Button CreateRuntimeButton(
            string objectName,
            Transform parent,
            string label,
            Vector2 size,
            Vector2 position)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = size;
            buttonRect.anchoredPosition = position;

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.86f, 0.86f, 0.8f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI labelText = CreateRuntimeText(
                "按钮文本",
                buttonObject.transform,
                label,
                22f,
                TextAlignmentOptions.Center);
            RectTransform labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>
        /// 刷新图鉴入口红点。
        /// </summary>
        private void RefreshRedDot()
        {
            SetObjectActive(_entryRedDot, GalleryManager.Instance != null && GalleryManager.Instance.HasAnyUnread());
        }

        /// <summary>
        /// 设置图鉴面板显隐。
        /// </summary>
        private void SetPanelVisible(bool visible)
        {
            SetCanvasGroupVisible(_canvasGroup, visible);
        }

        /// <summary>
        /// 通过 CanvasGroup 设置面板显隐与交互。
        /// </summary>
        private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// 设置对象显隐。
        /// </summary>
        private void SetObjectActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
