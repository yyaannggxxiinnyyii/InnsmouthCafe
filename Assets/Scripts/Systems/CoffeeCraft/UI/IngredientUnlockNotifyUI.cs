using System;
using System.Collections.Generic;
using DG.Tweening;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 材料解锁提示弹窗，支持一次展示多个材料、横向滚动和释放后自动居中。
    /// </summary>
    public class IngredientUnlockNotifyUI : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("面板 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        [Tooltip("标题文本")]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        [Tooltip("数量提示文本")]
        private TextMeshProUGUI _countText;

        [SerializeField]
        [Tooltip("横向滚动组件")]
        private ScrollRect _scrollRect;

        [SerializeField]
        [Tooltip("材料条目父节点")]
        private RectTransform _contentRoot;

        [SerializeField]
        [Tooltip("材料条目预制体")]
        private IngredientUnlockNotifyItemUI _itemPrefab;

        [SerializeField]
        [Tooltip("确认按钮")]
        private Button _confirmButton;

        [Header("文案")]
        [SerializeField]
        [Tooltip("弹窗标题")]
        private string _title = "解锁新材料";

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("淡入时长")]
        private float _fadeInDuration = 0.3f;

        [SerializeField]
        [Tooltip("淡出时长")]
        private float _fadeOutDuration = 0.25f;

        [Header("滚动复位")]
        [SerializeField]
        [Tooltip("未拖拽时 Content 的 X 坐标恢复到 0 的时长")]
        private float _resetPositionDuration = 0.35f;

        private Action _onDismissCallback;
        private bool _isDragging;
        private bool _isShowing;
        private bool _isResettingPosition;
        private float _resetStartPositionX;
        private float _resetElapsedTime;
        private float _previousTimeScale = 1f;
        private EventTrigger _scrollEventTrigger;

        private void Awake()
        {
            _confirmButton?.onClick.AddListener(OnConfirmClicked);
            BindScrollDragEvents();
            SetGroupState(false);
        }

        private void Update()
        {
            if (!_isShowing || _isDragging || !_isResettingPosition)
            {
                return;
            }

            ResetContentPositionToOrigin();
        }

        /// <summary>
        /// 显示材料解锁提示。
        /// </summary>
        public void Show(IReadOnlyList<LiquidSO> liquids, IReadOnlyList<ToppingSO> toppings, Action onDismiss)
        {
            int liquidCount = liquids?.Count ?? 0;
            int toppingCount = toppings?.Count ?? 0;
            if (liquidCount + toppingCount <= 0)
            {
                onDismiss?.Invoke();
                return;
            }

            if (!HasRequiredReferences())
            {
                Debug.LogWarning("[IngredientUnlockNotify] 材料解锁提示UI引用未配置完整，已跳过弹窗");
                onDismiss?.Invoke();
                return;
            }

            _onDismissCallback = onDismiss;
            BuildItems(liquids, toppings);
            RefreshText(liquidCount, toppingCount);

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            _isShowing = true;
            _isDragging = false;
            _isResettingPosition = false;
            FadeIn(() =>
            {
                Canvas.ForceUpdateCanvases();
                SetContentPositionX(0f);
            });
        }

        /// <summary>
        /// 记录玩家开始拖拽滚动区域。
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _isResettingPosition = false;
        }

        /// <summary>
        /// 记录玩家结束拖拽滚动区域。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            StartResetContentPosition();
        }

        /// <summary>
        /// 检查弹窗运行所需的关键 UI 引用是否已配置完整。
        /// </summary>
        private bool HasRequiredReferences()
        {
            return _canvasGroup != null
                && _scrollRect != null
                && _contentRoot != null
                && _itemPrefab != null
                && _confirmButton != null;
        }

        /// <summary>
        /// 将拖拽事件绑定到 ScrollRect 所在对象，避免脚本挂在外层面板时收不到拖拽状态。
        /// </summary>
        private void BindScrollDragEvents()
        {
            if (_scrollRect == null)
            {
                return;
            }

            _scrollEventTrigger = _scrollRect.GetComponent<EventTrigger>();
            if (_scrollEventTrigger == null)
            {
                _scrollEventTrigger = _scrollRect.gameObject.AddComponent<EventTrigger>();
            }

            AddEventTriggerEntry(EventTriggerType.BeginDrag, _ => OnBeginDrag(null));
            AddEventTriggerEntry(EventTriggerType.EndDrag, _ => OnEndDrag(null));
        }

        /// <summary>
        /// 向滚动区域添加事件回调。
        /// </summary>
        private void AddEventTriggerEntry(EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            if (_scrollEventTrigger == null)
            {
                return;
            }

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType
            };
            entry.callback.AddListener(callback);
            _scrollEventTrigger.triggers.Add(entry);
        }

        /// <summary>
        /// 构建材料解锁条目。
        /// </summary>
        private void BuildItems(IReadOnlyList<LiquidSO> liquids, IReadOnlyList<ToppingSO> toppings)
        {
            ClearItems();

            if (liquids != null)
            {
                foreach (LiquidSO liquid in liquids)
                {
                    if (liquid != null)
                    {
                        CreateItem(liquid.icon, liquid.liquidName, "辅助液");
                    }
                }
            }

            if (toppings != null)
            {
                foreach (ToppingSO topping in toppings)
                {
                    if (topping != null)
                    {
                        Sprite unlockIcon = topping.unlockIcon != null ? topping.unlockIcon : topping.icon;
                        CreateItem(unlockIcon, topping.toppingName, "小料");
                    }
                }
            }
        }

        /// <summary>
        /// 创建单个材料条目。
        /// </summary>
        private void CreateItem(Sprite icon, string itemName, string itemType)
        {
            if (_itemPrefab == null || _contentRoot == null)
            {
                Debug.LogError("[IngredientUnlockNotify] 材料条目预制体或父节点未设置");
                return;
            }

            IngredientUnlockNotifyItemUI item = Instantiate(_itemPrefab, _contentRoot);
            item.Configure(icon, itemName, itemType);
        }

        /// <summary>
        /// 刷新标题和数量文本。
        /// </summary>
        private void RefreshText(int liquidCount, int toppingCount)
        {
            if (_titleText != null)
            {
                _titleText.text = _title;
            }

            if (_countText != null)
            {
                _countText.text = $"辅助液 {liquidCount} 个  小料 {toppingCount} 个";
            }
        }

        /// <summary>
        /// 清空材料条目。
        /// </summary>
        private void ClearItems()
        {
            if (_contentRoot == null)
            {
                return;
            }

            for (int i = _contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentRoot.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 开始将 Content 的 X 坐标恢复到初始位置。
        /// </summary>
        private void StartResetContentPosition()
        {
            if (_contentRoot == null)
            {
                return;
            }

            if (_scrollRect != null)
            {
                _scrollRect.velocity = Vector2.zero;
            }

            _resetStartPositionX = _contentRoot.anchoredPosition.x;
            _resetElapsedTime = 0f;
            _isResettingPosition = !Mathf.Approximately(_resetStartPositionX, 0f);
        }

        /// <summary>
        /// 按配置时长把 Content 的 X 坐标恢复到 0。
        /// </summary>
        private void ResetContentPositionToOrigin()
        {
            if (_contentRoot == null)
            {
                return;
            }

            if (_resetPositionDuration <= 0f)
            {
                SetContentPositionX(0f);
                _isResettingPosition = false;
                return;
            }

            _resetElapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_resetElapsedTime / _resetPositionDuration);
            float positionX = Mathf.Lerp(_resetStartPositionX, 0f, progress);
            SetContentPositionX(positionX);

            if (progress >= 1f)
            {
                _isResettingPosition = false;
            }
        }

        /// <summary>
        /// 直接设置 Content 的 X 坐标，并保留 Y 坐标不变。
        /// </summary>
        private void SetContentPositionX(float positionX)
        {
            if (_contentRoot == null)
            {
                return;
            }

            Vector2 anchoredPosition = _contentRoot.anchoredPosition;
            anchoredPosition.x = positionX;
            _contentRoot.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 响应确认按钮点击，关闭弹窗并恢复暂停前的时间缩放。
        /// </summary>
        private void OnConfirmClicked()
        {
            FadeOut(() =>
            {
                Time.timeScale = _previousTimeScale;
                _isShowing = false;
                ClearItems();

                _onDismissCallback?.Invoke();
                _onDismissCallback = null;
            });
        }

        /// <summary>
        /// 播放弹窗淡入动画，动画完成后允许玩家交互。
        /// </summary>
        private void FadeIn(Action onComplete)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, _fadeInDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _canvasGroup.interactable = true;
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 播放弹窗淡出动画，动画完成后阻止射线命中。
        /// </summary>
        private void FadeOut(Action onComplete)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _canvasGroup.DOKill();
            _canvasGroup.interactable = false;
            _canvasGroup.DOFade(0f, _fadeOutDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 设置弹窗 CanvasGroup 的初始可见和交互状态。
        /// </summary>
        private void SetGroupState(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
