using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 杯子动画管理器
    /// 管理独立的"工作杯子"实例（和场景容器同级），处理跨场景穿越移动
    /// 同时管理杯子上的小料图标显示（自由放置，跟随杯子移动）
    /// </summary>
    public class CupAnimationManager : MonoBehaviour
    {
        [Header("工作杯子实例")]
        [SerializeField] [Tooltip("工作杯子的Image组件（和场景容器同级）")]
        private Image _workCupImage;

        [SerializeField] [Tooltip("工作杯子的RectTransform（阴影节点，杯子是其子节点）")]
        private RectTransform _workCupRect;

        [SerializeField] [Tooltip("阴影节点上的CanvasGroup，未选中杯子时隐藏")]
        private CanvasGroup _shadowGroup;

        [Header("杯子选择器引用")]
        [SerializeField] [Tooltip("杯型选择UI组件")]
        private CupSelectorUI _cupSelector;

        [Header("场景工作位置锚点")]
        [SerializeField] [Tooltip("CraftBase场景中杯子的工作位置")]
        private RectTransform _craftBaseAnchor;

        [SerializeField] [Tooltip("CraftMix场景中杯子的工作位置")]
        private RectTransform _craftMixAnchor;

        [SerializeField] [Tooltip("Bar场景中杯子的工作位置（可选，为空则隐藏）")]
        private RectTransform _barAnchor;

        [Header("边界过渡锚点")]
        [SerializeField] [Tooltip("左边界锚点（屏幕左侧外）")]
        private RectTransform _leftBoundaryAnchor;

        [SerializeField] [Tooltip("右边界锚点（屏幕右侧外）")]
        private RectTransform _rightBoundaryAnchor;

        [Header("垃圾桶")]
        [SerializeField] [Tooltip("垃圾桶锚点位置")]
        private RectTransform _trashBinAnchor;

        [SerializeField] [Tooltip("丢弃动画时长")]
        private float _trashDuration = 0.4f;

        [SerializeField] [Tooltip("抛物线弧度高度（相对于起点和终点中点的偏移）")]
        private float _trashArcHeight = 150f;

        [Header("动画配置")]
        [SerializeField] [Tooltip("杯子从按钮飞到工作位置的时长")]
        private float _pickUpDuration = 0.35f;

        [SerializeField] [Tooltip("杯子飞回按钮位置的时长")]
        private float _putBackDuration = 0.25f;

        [SerializeField] [Tooltip("杯子飞出到边界的时长")]
        private float _exitDuration = 0.2f;

        [SerializeField] [Tooltip("杯子从边界飞入的时长")]
        private float _enterDuration = 0.25f;

        [Header("小料显示")]
        [SerializeField] [Tooltip("小料图标预制体（含 Image 组件）")]
        private GameObject _toppingIconPrefab;

        [SerializeField] [Tooltip("小料容器（_workCupRect 的子节点，位于背景层之上、前景层之下）")]
        private RectTransform _toppingContainer;

        [SerializeField] [Tooltip("杯子前景 Image（盖在小料之上，有溶液时显示）")]
        private Image _cupForegroundImage;

        [SerializeField] [Tooltip("杯口放置区域（_workCupRect 的子节点，定义可放置范围）")]
        private RectTransform _toppingDropZone;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        private bool _hasCup = false;
        private bool _isAnimating = false;
        private int _lastSelectedIndex = -1;

        // 已放置的小料图标对象列表
        private List<GameObject> _placedToppingObjects = new List<GameObject>();

        private void Start()
        {
            // 初始隐藏工作杯子和前景
            SetCupVisible(false);
            if (_cupForegroundImage != null)
                _cupForegroundImage.gameObject.SetActive(false);

            // 监听杯子选择事件
            if (_cupSelector != null)
            {
                _cupSelector.OnCupSelected += OnCupSelected;
            }

            // 监听场景切换开始事件（带方向）
            if (ViewSwitchManager.Instance != null)
            {
                ViewSwitchManager.Instance.OnViewSwitchStarted += OnViewSwitchStarted;
                ViewSwitchManager.Instance.OnViewSwitched += OnViewSwitched;
            }

            // 监听制作重置事件（咖啡提交后，隐藏工作杯子并重置状态）
            if (CoffeeCraftManager.Instance != null)
            {
                CoffeeCraftManager.Instance.OnCraftReset += ResetWorkCup;
                CoffeeCraftManager.Instance.OnCoffeeDataChanged += OnCoffeeDataChanged;
            }
        }

        private void OnDestroy()
        {
            if (_cupSelector != null)
            {
                _cupSelector.OnCupSelected -= OnCupSelected;
            }

            if (ViewSwitchManager.Instance != null)
            {
                ViewSwitchManager.Instance.OnViewSwitchStarted -= OnViewSwitchStarted;
                ViewSwitchManager.Instance.OnViewSwitched -= OnViewSwitched;
            }

            if (CoffeeCraftManager.Instance != null)
            {
                CoffeeCraftManager.Instance.OnCraftReset -= ResetWorkCup;
                CoffeeCraftManager.Instance.OnCoffeeDataChanged -= OnCoffeeDataChanged;
            }

            _workCupRect?.DOKill();
        }

        /// <summary>
        /// 杯子选中回调
        /// </summary>
        /// <param name="cup">选中的杯型SO</param>
        /// <param name="newIndex">新选中的索引</param>
        /// <param name="oldIndex">旧选中的索引（-1表示第一次选择）</param>
        private void OnCupSelected(CupContainerSO cup, int newIndex, int oldIndex)
        {
            if (cup == null || _isAnimating) return;

            if (oldIndex < 0)
            {
                // 第一次选择：从按钮位置取出杯子
                AnimateFirstPick(cup, newIndex);
            }
            else
            {
                // 切换杯子：放回旧位置，从新位置取出
                AnimateCupSwap(cup, newIndex, oldIndex);
            }

            _lastSelectedIndex = newIndex;
        }

        /// <summary>
        /// 第一次选择杯子动画：定位到按钮 → 显示 → 飞到工作位置
        /// </summary>
        private void AnimateFirstPick(CupContainerSO cup, int index)
        {
            Vector2 buttonPos = GetButtonLocalPosition(index);
            RectTransform targetAnchor = GetAnchorForCurrentView();

            if (targetAnchor == null) return;

            _isAnimating = true;
            _workCupRect.DOKill();

            // 定位到按钮位置
            _workCupRect.anchoredPosition = buttonPos;

            // 设置贴图并显示（空杯状态）
            if (_workCupImage != null)
                _workCupImage.sprite = cup.GetSpriteForFillRatio(0f);
            SetCupVisible(true);

            // 飞到工作位置
            _workCupRect.DOAnchorPos(targetAnchor.anchoredPosition, _pickUpDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    _hasCup = true;

                    if (_showDebugLog)
                    {
                        Debug.Log($"[CupAnimation] 取出杯子: {cup.cupName}");
                    }
                });
        }

        /// <summary>
        /// 切换杯子动画：飞回旧按钮 → 隐藏 → 闪现到新按钮 → 显示新贴图 → 飞到工作位置
        /// </summary>
        private void AnimateCupSwap(CupContainerSO newCup, int newIndex, int oldIndex)
        {
            Vector2 oldButtonPos = GetButtonLocalPosition(oldIndex);
            Vector2 newButtonPos = GetButtonLocalPosition(newIndex);
            RectTransform targetAnchor = GetAnchorForCurrentView();

            if (targetAnchor == null) return;

            _isAnimating = true;
            _workCupRect.DOKill();

            // 阶段1：飞回旧杯子按钮位置
            _workCupRect.DOAnchorPos(oldButtonPos, _putBackDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // 阶段2：隐藏，闪现到新按钮位置
                    SetCupVisible(false);
                    _workCupRect.anchoredPosition = newButtonPos;

                    // 设置新贴图并显示（空杯状态）
                    if (_workCupImage != null)
                        _workCupImage.sprite = newCup.GetSpriteForFillRatio(0f);
                    SetCupVisible(true);

                    // 阶段4：飞到工作位置
                    _workCupRect.DOAnchorPos(targetAnchor.anchoredPosition, _pickUpDuration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            _isAnimating = false;

                            if (_showDebugLog)
                            {
                                Debug.Log($"[CupAnimation] 切换杯子: {newCup.cupName}");
                            }
                        });
                });
        }

        /// <summary>
        /// 获取按钮在工作杯子父级坐标系中的位置
        /// </summary>
        private Vector2 GetButtonLocalPosition(int index)
        {
            if (_cupSelector == null || index < 0 || index >= _cupSelector.CupBindings.Count)
            {
                return Vector2.zero;
            }

            var binding = _cupSelector.CupBindings[index];
            if (binding?.button == null) return Vector2.zero;

            RectTransform buttonRect = binding.button.GetComponent<RectTransform>();
            if (buttonRect == null) return Vector2.zero;

            return GetLocalPositionOf(buttonRect);
        }

        /// <summary>
        /// 将任意RectTransform的世界坐标转换为工作杯子父级坐标系中的本地坐标
        /// </summary>
        private Vector2 GetLocalPositionOf(RectTransform target)
        {
            RectTransform parentRect = _workCupRect.parent as RectTransform;
            if (parentRect == null || target == null) return Vector2.zero;

            Vector3 worldPos = target.position;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null,
                out localPos
            );

            return localPos;
        }

        /// <summary>
        /// 场景切换开始回调（执行穿越动画）
        /// </summary>
        private void OnViewSwitchStarted(GameViewType fromView, GameViewType toView, bool isNext)
        {
            if (!_hasCup || _isAnimating) return;

            RectTransform targetAnchor = GetAnchorForView(toView);

            // 目标场景没有杯子位置，直接隐藏
            if (targetAnchor == null)
            {
                SetCupVisible(false);
                return;
            }

            // 来源场景没有杯子位置，直接定位显示
            RectTransform fromAnchor = GetAnchorForView(fromView);
            if (fromAnchor == null)
            {
                _workCupRect.anchoredPosition = targetAnchor.anchoredPosition;
                SetCupVisible(true);
                return;
            }

            // 执行穿越动画
            AnimateCrossingTransition(targetAnchor, isNext);
        }

        /// <summary>
        /// 场景切换完成回调（用于非动画的直接切换）
        /// </summary>
        private void OnViewSwitched(GameViewType viewType)
        {
            if (_isAnimating) return;
            if (!_hasCup) return;

            RectTransform targetAnchor = GetAnchorForView(viewType);
            if (targetAnchor == null)
            {
                SetCupVisible(false);
            }
            else
            {
                _workCupRect.anchoredPosition = targetAnchor.anchoredPosition;
                SetCupVisible(true);
            }
        }

        /// <summary>
        /// 穿越动画：飞出边界 → 闪现到另一侧 → 飞入目标位置
        /// </summary>
        private void AnimateCrossingTransition(RectTransform targetAnchor, bool isNext)
        {
            if (_workCupRect == null || _leftBoundaryAnchor == null || _rightBoundaryAnchor == null)
            {
                _workCupRect.anchoredPosition = targetAnchor.anchoredPosition;
                return;
            }

            _isAnimating = true;
            _workCupRect.DOKill();

            // 向下一个场景：杯子向右飞出 → 从左侧飞入
            // 向上一个场景：杯子向左飞出 → 从右侧飞入
            RectTransform exitAnchor = isNext ? _rightBoundaryAnchor : _leftBoundaryAnchor;
            RectTransform enterAnchor = isNext ? _leftBoundaryAnchor : _rightBoundaryAnchor;

            // 阶段1：飞出到边界
            _workCupRect.DOAnchorPos(exitAnchor.anchoredPosition, _exitDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // 阶段2：闪现到另一侧边界
                    _workCupRect.anchoredPosition = enterAnchor.anchoredPosition;

                    // 阶段3：从边界飞入目标位置
                    _workCupRect.DOAnchorPos(targetAnchor.anchoredPosition, _enterDuration)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            _isAnimating = false;

                            if (_showDebugLog)
                            {
                                Debug.Log($"[CupAnimation] 穿越动画完成");
                            }
                        });
                });
        }

        /// <summary>
        /// 设置工作杯子可见性
        /// </summary>
        private void SetCupVisible(bool visible)
        {
            if (_workCupImage != null)
            {
                _workCupImage.enabled = visible;
            }

            if (_shadowGroup != null)
            {
                _shadowGroup.alpha = visible ? 1f : 0f;
            }

            // 杯子隐藏时同步隐藏前景
            if (!visible && _cupForegroundImage != null)
            {
                _cupForegroundImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 获取当前场景对应的锚点
        /// </summary>
        private RectTransform GetAnchorForCurrentView()
        {
            if (ViewSwitchManager.Instance == null) return _craftBaseAnchor;
            return GetAnchorForView(ViewSwitchManager.Instance.CurrentViewType);
        }

        /// <summary>
        /// 获取指定场景对应的锚点
        /// </summary>
        private RectTransform GetAnchorForView(GameViewType viewType)
        {
            switch (viewType)
            {
                case GameViewType.CraftBase:
                    return _craftBaseAnchor;
                case GameViewType.CraftMix:
                    return _craftMixAnchor;
                case GameViewType.Bar:
                    return _barAnchor;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 杯子丢进垃圾桶动画（带弧度抛物线 + 缩小）
        /// 动画结束后自动重置杯子状态
        /// </summary>
        public void AnimateTrashCup()
        {
            if (!_hasCup || _isAnimating) return;
            if (_workCupRect == null || _trashBinAnchor == null) return;

            _isAnimating = true;
            _workCupRect.DOKill();

            Vector2 startPos = _workCupRect.anchoredPosition;
            Vector2 endPos = _trashBinAnchor.anchoredPosition;

            // 计算中间控制点（在起点和终点中间偏上方）
            Vector2 midPoint = (startPos + endPos) * 0.5f;
            midPoint.y += _trashArcHeight;

            // 用DOVirtual手动沿二次贝塞尔曲线插值anchoredPosition
            // DOPath操作的是transform.position（世界坐标），不适合UI的anchoredPosition
            Sequence seq = DOTween.Sequence();

            seq.Append(DOVirtual.Float(0f, 1f, _trashDuration, t =>
            {
                // 二次贝塞尔: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
                float oneMinusT = 1f - t;
                Vector2 pos = oneMinusT * oneMinusT * startPos
                            + 2f * oneMinusT * t * midPoint
                            + t * t * endPos;
                _workCupRect.anchoredPosition = pos;
            }).SetEase(Ease.InQuad));

            seq.Join(_workCupRect.DOScale(Vector3.zero, _trashDuration)
                .SetEase(Ease.InQuad));

            seq.Join(_workCupRect.DORotate(new Vector3(0f, 0f, -45f), _trashDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InQuad));

            seq.OnComplete(() =>
            {
                _isAnimating = false;
                _hasCup = false;
                _lastSelectedIndex = -1;
                SetCupVisible(false);

                // 恢复缩放和旋转
                _workCupRect.localScale = Vector3.one;
                _workCupRect.localEulerAngles = Vector3.zero;

                // 通知选择器重置
                if (_cupSelector != null)
                {
                    _cupSelector.ResetSelection();
                }

                if (_showDebugLog)
                {
                    Debug.Log("[CupAnimation] 杯子丢进垃圾桶");
                }
            });
        }

        // ── 小料显示 ──────────────────────────────────────────

        /// <summary>
        /// 杯口放置区域（供 LiquidToppingUI 拖拽检测使用）
        /// </summary>
        public RectTransform ToppingDropZone => _toppingDropZone;

        /// <summary>
        /// 工作杯子 RectTransform（供 LiquidToppingUI 坐标转换使用）
        /// </summary>
        public RectTransform WorkCupRect => _workCupRect;

        /// <summary>
        /// 小料容器 RectTransform（坐标转换应以此为基准，与小料实例化父节点一致）
        /// </summary>
        public RectTransform ToppingContainer => _toppingContainer != null ? _toppingContainer : _workCupRect;

        /// <summary>
        /// 咖啡数据变化时同步刷新小料图标和杯子贴图
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData data)
        {
            RefreshToppings(data);
            RefreshCupSprite(data);
        }

        /// <summary>
        /// 根据当前填充量更新杯子贴图
        /// </summary>
        private void RefreshCupSprite(CoffeeData data)
        {
            if (_workCupImage == null || data == null || data.selectedCup == null) return;

            float capacity = data.selectedCup.capacity;
            if (capacity <= 0f) return;

            float totalVolume = 0f;
            foreach (var seg in data.coffeeSegments)
                totalVolume += seg.extractedVolume;
            foreach (var seg in data.liquidSegments)
                totalVolume += seg.amountMl;

            float ratio = Mathf.Clamp01(totalVolume / capacity);

            var cup = data.selectedCup;
            _workCupImage.sprite = cup.GetSpriteForFillRatio(ratio);

            // 前景：有溶液时显示，并同步贴图
            if (_cupForegroundImage != null)
            {
                bool hasliquid = totalVolume > 0f;
                _cupForegroundImage.gameObject.SetActive(hasliquid);
                if (hasliquid && cup.foregroundSprite != null)
                    _cupForegroundImage.sprite = cup.foregroundSprite;
            }
        }

        /// <summary>
        /// 刷新杯子上的小料图标：销毁旧图标，按数据重建
        /// </summary>
        private void RefreshToppings(CoffeeData data)
        {
            // 销毁所有已放置图标
            foreach (var obj in _placedToppingObjects)
                if (obj != null) Destroy(obj);
            _placedToppingObjects.Clear();

            if (data == null || _toppingIconPrefab == null || _workCupRect == null) return;

            // 小料父节点：优先用 _toppingContainer，没配置则直接挂在 _workCupRect 下
            Transform toppingParent = _toppingContainer != null ? _toppingContainer : _workCupRect;

            for (int i = 0; i < data.toppings.Count; i++)
            {
                var toppingData = data.toppings[i];
                if (toppingData.topping == null) continue;

                var go = Instantiate(_toppingIconPrefab, toppingParent);
                var rt = go.GetComponent<RectTransform>();
                if (rt == null) rt = go.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = toppingData.localPosition;
                rt.sizeDelta        = toppingData.topping.displaySize;

                var img = go.GetComponent<Image>();
                if (img == null) img = go.AddComponent<Image>();
                img.sprite          = toppingData.topping.icon;
                img.preserveAspect  = true;
                img.raycastTarget   = true;

                // 右键点击移除该小料
                int capturedIndex = i;
                var et = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener(eventData =>
                {
                    var ptr = (PointerEventData)eventData;
                    if (ptr.button == PointerEventData.InputButton.Right)
                        CoffeeCraftManager.Instance?.RemoveToppingAt(capturedIndex);
                });
                et.triggers.Add(entry);

                _placedToppingObjects.Add(go);
            }
        }

        /// <summary>
        /// 清空所有小料图标
        /// </summary>
        private void ClearToppings()
        {
            foreach (var obj in _placedToppingObjects)
                if (obj != null) Destroy(obj);
            _placedToppingObjects.Clear();
        }

        // ── 重置 ──────────────────────────────────────────────

        /// <summary>
        /// 重置工作杯子（新一轮制作时调用）
        /// </summary>
        public void ResetWorkCup()
        {
            _hasCup = false;
            _isAnimating = false;
            _lastSelectedIndex = -1;
            SetCupVisible(false);
            ClearToppings();

            if (_workCupRect != null)
            {
                _workCupRect.DOKill();
                _workCupRect.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 是否已有工作杯子
        /// </summary>
        public bool HasWorkCup => _hasCup;
    }
}
