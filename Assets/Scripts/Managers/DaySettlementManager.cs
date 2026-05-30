using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 日结算管理器
    /// 负责一天结束后的数据统计、结算界面展示和天数管理
    /// 结算界面采用卷帘门从上方下滑效果
    /// </summary>
    public class DaySettlementManager : Singleton<DaySettlementManager>
    {
        [Header("结算面板")]
        [SerializeField]
        [Tooltip("结算面板RectTransform（用于下滑动画）")]
        private RectTransform _settlementPanel;

        [SerializeField]
        [Tooltip("结算面板CanvasGroup")]
        private CanvasGroup _settlementCanvasGroup;

        [Header("结算内容UI")]
        [SerializeField]
        [Tooltip("标题文本（第X天结算）")]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        [Tooltip("当前理智值文本")]
        private TextMeshProUGUI _sanityValueText;

        [SerializeField]
        [Tooltip("当日理智值变化文本")]
        private TextMeshProUGUI _sanityChangeText;

        [SerializeField]
        [Tooltip("理智值变化明细列表容器")]
        private Transform _sanityChangeListContainer;

        [SerializeField]
        [Tooltip("理智值变化明细条目预制体")]
        private GameObject _sanityChangeEntryPrefab;

        [SerializeField]
        [Tooltip("接待客人数文本")]
        private TextMeshProUGUI _customerCountText;

        [SerializeField]
        [Tooltip("好评订单数文本")]
        private TextMeshProUGUI _satisfiedCountText;

        [SerializeField]
        [Tooltip("一般订单数文本")]
        private TextMeshProUGUI _neutralCountText;

        [SerializeField]
        [Tooltip("差评订单数文本")]
        private TextMeshProUGUI _dissatisfiedCountText;

        [Header("按钮")]
        [SerializeField]
        [Tooltip("继续按钮")]
        private Button _continueButton;

        [SerializeField]
        [Tooltip("继续按钮文本")]
        private TextMeshProUGUI _continueButtonText;

        [Header("页签")]
        [SerializeField]
        [Tooltip("页签按钮列表（7个，按天数顺序）")]
        private List<Button> _tabButtons = new List<Button>();

        [SerializeField]
        [Tooltip("页签选中颜色")]
        private Color _tabActiveColor = new Color(1f, 1f, 1f, 1f);

        [SerializeField]
        [Tooltip("页签未选中颜色")]
        private Color _tabInactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Header("过渡效果")]
        [SerializeField]
        [Tooltip("黑幕遮罩CanvasGroup（用于天亮过渡效果）")]
        private CanvasGroup _transitionOverlay;

        [Header("动画配置")]
        [SerializeField]
        [Tooltip("卷帘门下滑动画时长（秒）")]
        [Range(0.3f, 6f)]
        private float _slideDownDuration = 0.8f;

        [SerializeField]
        [Tooltip("卷帘门上滑动画时长（秒）")]
        [Range(0.3f, 4f)]
        private float _slideUpDuration = 0.6f;

        [Header("游戏配置")]
        [SerializeField]
        [Tooltip("总天数")]
        private int _totalDays = 7;

        [Header("调试")]
        [SerializeField]
        [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        /// <summary>
        /// 历史结算数据（最多7天）
        /// </summary>
        private List<DaySettlementData> _settlementHistory = new List<DaySettlementData>();

        /// <summary>
        /// 当前正在展示的结算数据
        /// </summary>
        private DaySettlementData _currentSettlement;

        /// <summary>
        /// 当前正在查看的页签索引
        /// </summary>
        private int _currentViewingTab = -1;

        /// <summary>
        /// 继续按钮回调
        /// </summary>
        private Action _onContinueCallback;

        /// <summary>
        /// 面板隐藏时的Y坐标（屏幕上方外）
        /// </summary>
        private float _panelHiddenPosY;

        /// <summary>
        /// 面板显示时的Y坐标（覆盖屏幕）
        /// </summary>
        private float _panelVisiblePosY;

        /// <summary>
        /// 是否正在显示结算界面
        /// </summary>
        private bool _isShowing = false;

        /// <summary>
        /// 历史结算数据（只读）
        /// </summary>
        public IReadOnlyList<DaySettlementData> SettlementHistory => _settlementHistory.AsReadOnly();

        /// <summary>
        /// 总天数
        /// </summary>
        public int TotalDays => _totalDays;

        /// <summary>
        /// 结算开始事件
        /// </summary>
        public event Action<DaySettlementData> OnSettlementStart;

        /// <summary>
        /// 结算完成事件（点击继续按钮后）
        /// </summary>
        public event Action OnSettlementComplete;

        /// <summary>
        /// 进入下一天事件
        /// </summary>
        public event Action<int> OnNextDayStart;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                return;
            }

            InitializePanel();
        }

        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            // 绑定继续按钮事件
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueButtonClicked);
            }

            // 绑定页签按钮事件
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                int tabIndex = i;
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].onClick.AddListener(() => OnTabClicked(tabIndex));
                }
            }

            // 初始隐藏过渡遮罩
            if (_transitionOverlay != null)
            {
                _transitionOverlay.alpha = 0f;
                _transitionOverlay.blocksRaycasts = false;
            }

            if (_showDebugLog)
            {
                Debug.Log("[Settlement] 日结算管理器初始化完成");
            }
        }

        /// <summary>
        /// 初始化面板位置
        /// </summary>
        private void InitializePanel()
        {
            if (_settlementPanel == null)
            {
                return;
            }

            // 面板手动摆放在屏幕正上方(0, 1080)，无需动态计算
            _panelVisiblePosY = 0f;
            _panelHiddenPosY = 1080f;

            if (_settlementCanvasGroup != null)
            {
                _settlementCanvasGroup.alpha = 1f;
                _settlementCanvasGroup.interactable = false;
                _settlementCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 显示结算界面
        /// </summary>
        /// <param name="data">当天结算数据</param>
        /// <param name="onContinue">点击继续按钮后的回调</param>
        public void ShowSettlement(DaySettlementData data, Action onContinue = null)
        {
            if (_isShowing)
            {
                Debug.LogWarning("[Settlement] 结算界面已在显示中");
                return;
            }

            if (data == null)
            {
                Debug.LogError("[Settlement] 结算数据为空");
                return;
            }

            _currentSettlement = data;
            _onContinueCallback = onContinue;

            // 保存到历史记录
            _settlementHistory.Add(data);

            // 触发结算开始事件
            OnSettlementStart?.Invoke(data);

            // 刷新UI内容
            RefreshUI(data);

            // 更新页签
            UpdateTabButtons();

            // 选中当前天数的页签
            _currentViewingTab = data.dayNumber - 1;
            HighlightTab(_currentViewingTab);

            // 更新继续按钮文本
            if (_continueButtonText != null)
            {
                _continueButtonText.text = data.dayNumber >= _totalDays ? "结算" : "继续";
            }

            // 等一帧让Canvas布局重建完成，再启动动画
            StartCoroutine(DelayedSlideDown(() =>
            {
                _isShowing = true;

                if (_settlementCanvasGroup != null)
                {
                    _settlementCanvasGroup.interactable = true;
                    _settlementCanvasGroup.blocksRaycasts = true;
                }

                if (_showDebugLog)
                {
                    Debug.Log($"[Settlement] 结算界面已显示: {data}");
                }
            }));
        }

        /// <summary>
        /// 延迟一帧后执行下滑动画
        /// 避免Canvas布局重建导致面板位置跳变
        /// </summary>
        private IEnumerator DelayedSlideDown(Action onComplete)
        {
            yield return null;

            // 确保面板在正确的起始位置
            _settlementPanel.anchoredPosition = new Vector2(
                _settlementPanel.anchoredPosition.x,
                _panelHiddenPosY
            );

            SlideDown(onComplete);
        }

        /// <summary>
        /// 刷新结算UI内容
        /// </summary>
        private void RefreshUI(DaySettlementData data)
        {
            if (data == null) return;

            // 标题
            if (_titleText != null)
            {
                _titleText.text = $"第 {data.dayNumber} 天结算";
            }

            // 理智值
            if (_sanityValueText != null)
            {
                _sanityValueText.text = $"{data.sanityEnd:F1}";
            }

            // 理智值变化
            if (_sanityChangeText != null)
            {
                string sign = data.sanityChange >= 0 ? "+" : "";
                _sanityChangeText.text = $"{sign}{data.sanityChange:F1}";
            }

            // 顾客统计
            if (_customerCountText != null)
            {
                _customerCountText.text = $"{data.customersServed}";
            }

            if (_satisfiedCountText != null)
            {
                _satisfiedCountText.text = $"{data.satisfiedCount}";
            }

            if (_neutralCountText != null)
            {
                _neutralCountText.text = $"{data.neutralCount}";
            }

            if (_dissatisfiedCountText != null)
            {
                _dissatisfiedCountText.text = $"{data.dissatisfiedCount}";
            }

            // 理智值变化明细列表
            PopulateSanityChangeList(data.sanityChangeEntries);
        }

        /// <summary>
        /// 填充理智值变化明细列表
        /// </summary>
        private void PopulateSanityChangeList(List<SanityChangeEntry> entries)
        {
            ClearSanityChangeList();

            if (_sanityChangeListContainer == null || _sanityChangeEntryPrefab == null)
            {
                return;
            }

            if (entries == null || entries.Count == 0)
            {
                return;
            }

            foreach (var entry in entries)
            {
                GameObject entryObj = Instantiate(_sanityChangeEntryPrefab, _sanityChangeListContainer);
                TextMeshProUGUI entryText = entryObj.GetComponentInChildren<TextMeshProUGUI>();
                if (entryText != null)
                {
                    string sign = entry.delta >= 0 ? "+" : "";
                    entryText.text = $"{entry.reason}  {sign}{entry.delta:F1}";
                }
            }
        }

        /// <summary>
        /// 清空理智值变化明细列表
        /// </summary>
        private void ClearSanityChangeList()
        {
            if (_sanityChangeListContainer == null) return;

            for (int i = _sanityChangeListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_sanityChangeListContainer.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 更新页签按钮状态
        /// 只显示已到达天数的页签
        /// </summary>
        private void UpdateTabButtons()
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null) continue;

                // 只显示有历史数据的页签
                bool hasData = i < _settlementHistory.Count;
                _tabButtons[i].gameObject.SetActive(hasData);

                if (hasData)
                {
                    _tabButtons[i].interactable = true;
                }
            }
        }

        /// <summary>
        /// 高亮选中的页签
        /// </summary>
        private void HighlightTab(int activeIndex)
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (_tabButtons[i] == null) continue;

                Image tabImage = _tabButtons[i].GetComponent<Image>();
                if (tabImage != null)
                {
                    tabImage.color = (i == activeIndex) ? _tabActiveColor : _tabInactiveColor;
                }
            }
        }

        /// <summary>
        /// 页签点击事件处理
        /// </summary>
        private void OnTabClicked(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= _settlementHistory.Count)
            {
                return;
            }

            if (tabIndex == _currentViewingTab)
            {
                return;
            }

            _currentViewingTab = tabIndex;
            HighlightTab(tabIndex);

            // 刷新UI为对应天数的数据
            DaySettlementData data = _settlementHistory[tabIndex];
            RefreshUI(data);

            if (_showDebugLog)
            {
                Debug.Log($"[Settlement] 切换到第 {data.dayNumber} 天页签");
            }
        }

        /// <summary>
        /// 继续按钮点击事件处理
        /// </summary>
        private void OnContinueButtonClicked()
        {
            if (!_isShowing) return;

            _isShowing = false;

            if (_settlementCanvasGroup != null)
            {
                _settlementCanvasGroup.interactable = false;
                _settlementCanvasGroup.blocksRaycasts = false;
            }

            // 触发结算完成事件
            OnSettlementComplete?.Invoke();

            if (_showDebugLog)
            {
                Debug.Log("[Settlement] 点击继续按钮");
            }

            int nextDay = _currentSettlement != null ? _currentSettlement.dayNumber + 1 : 1;
            bool isLastDay = _currentSettlement != null && _currentSettlement.dayNumber >= _totalDays;

            if (isLastDay)
            {
                // 最后一天：不做卷帘门动画，直接触发结局流程
                // EndingPanelUI 的黑幕会覆盖结算面板
                OnNextDayStart?.Invoke(nextDay);
                _onContinueCallback?.Invoke();
                _onContinueCallback = null;
            }
            else
            {
                // 非最后一天：卷帘门上滑 + 黑幕渐退
                SlideUp(() =>
                {
                    OnNextDayStart?.Invoke(nextDay);
                    _onContinueCallback?.Invoke();
                    _onContinueCallback = null;
                });
            }
        }

        /// <summary>
        /// 卷帘门下滑动画（从屏幕上方滑入覆盖屏幕）
        /// 同时黑幕渐入
        /// </summary>
        private void SlideDown(Action onComplete = null)
        {
            if (_settlementPanel == null)
            {
                onComplete?.Invoke();
                return;
            }

            // 清除残留动画，防止冲突
            _settlementPanel.DOKill();

            // 黑幕同步渐入
            if (_transitionOverlay != null)
            {
                _transitionOverlay.DOKill();
                Image overlayImage = _transitionOverlay.GetComponent<Image>();
                if (overlayImage != null)
                {
                    overlayImage.color = Color.black;
                }
                _transitionOverlay.alpha = 0f;
                _transitionOverlay.blocksRaycasts = true;
                _transitionOverlay.DOFade(1f, _slideDownDuration).SetEase(Ease.Linear);
            }

            _settlementPanel.DOAnchorPosY(_panelVisiblePosY, _slideDownDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();

                    if (_showDebugLog)
                    {
                        Debug.Log("[Settlement] 卷帘门下滑完成");
                    }
                });
        }

        /// <summary>
        /// 卷帘门上滑动画（滑出屏幕上方）
        /// 同时黑幕渐出消失
        /// </summary>
        private void SlideUp(Action onComplete = null)
        {
            if (_settlementPanel == null)
            {
                onComplete?.Invoke();
                return;
            }

            // 黑幕同步渐出
            if (_transitionOverlay != null)
            {
                _transitionOverlay.DOFade(0f, _slideUpDuration).SetEase(Ease.InQuad);
            }

            _settlementPanel.DOAnchorPosY(_panelHiddenPosY, _slideUpDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    if (_transitionOverlay != null)
                    {
                        _transitionOverlay.blocksRaycasts = false;
                    }

                    onComplete?.Invoke();

                    if (_showDebugLog)
                    {
                        Debug.Log("[Settlement] 卷帘门上滑完成");
                    }
                });
        }

        /// <summary>
        /// 根据天数获取历史结算数据
        /// </summary>
        /// <param name="dayNumber">天数（从1开始）</param>
        /// <returns>对应天数的结算数据，不存在则返回null</returns>
        public DaySettlementData GetSettlementByDay(int dayNumber)
        {
            return _settlementHistory.Find(d => d.dayNumber == dayNumber);
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory()
        {
            _settlementHistory.Clear();
            _currentViewingTab = -1;

            if (_showDebugLog)
            {
                Debug.Log("[Settlement] 历史记录已清空");
            }
        }

        /// <summary>
        /// 是否正在显示结算界面
        /// </summary>
        public bool IsShowing => _isShowing;

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器调试：模拟显示结算界面
        /// </summary>
        [ContextMenu("测试：显示结算界面")]
        private void DebugShowSettlement()
        {
            var testData = new DaySettlementData
            {
                dayNumber = _settlementHistory.Count + 1,
                sanityStart = 85f,
                sanityEnd = 72.5f,
                sanityChange = -12.5f,
                customersServed = 5,
                satisfiedCount = 2,
                neutralCount = 2,
                dissatisfiedCount = 1,
                sanityChangeEntries = new List<SanityChangeEntry>
                {
                    new SanityChangeEntry("顾客评价：满意", 5f),
                    new SanityChangeEntry("顾客评价：满意", 5f),
                    new SanityChangeEntry("顾客评价：不满意", -5f),
                    new SanityChangeEntry("浪费咖啡豆", -0.2f),
                    new SanityChangeEntry("倒掉咖啡粉", -0.3f),
                    new SanityChangeEntry("咖啡溢出", -0.2f),
                    new SanityChangeEntry("顾客愤怒", -1.8f),
                    new SanityChangeEntry("倒掉整杯咖啡", -0.4f),
                    new SanityChangeEntry("每日固定消耗", -14.6f)
                }
            };

            ShowSettlement(testData, () =>
            {
                Debug.Log("[Settlement] 测试结算完成");
            });
        }

        /// <summary>
        /// 编辑器调试：清空历史记录
        /// </summary>
        [ContextMenu("测试：清空历史记录")]
        private void DebugClearHistory()
        {
            ClearHistory();
        }

        /// <summary>
        /// 编辑器调试：重置面板位置
        /// </summary>
        [ContextMenu("测试：重置面板位置")]
        private void DebugResetPanel()
        {
            InitializePanel();
            _isShowing = false;
            Debug.Log("[Settlement] 面板位置已重置");
        }
#endif
    }
}