using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 辅助液和小料UI交互组件
    /// 负责处理辅助液倒入和小料拖拽放置的交互
    /// </summary>
    public class LiquidToppingUI : MonoBehaviour
    {
        [Serializable]
        private class LiquidButtonBinding
        {
            [Tooltip("辅助液配置")]
            public LiquidSO liquid;

            [Tooltip("对应按钮")]
            public Button button;
        }

        [Serializable]
        private class ToppingButtonBinding
        {
            [Tooltip("小料配置")]
            public ToppingSO topping;

            [Tooltip("对应按钮")]
            public Button button;
        }

        [Header("辅助液配置")]
        [SerializeField] [Tooltip("辅助液与按钮的对应配置")]
        private List<LiquidButtonBinding> _liquidButtons = new List<LiquidButtonBinding>();

        [Header("小料配置")]
        [SerializeField] [Tooltip("小料与按钮的对应配置")]
        private List<ToppingButtonBinding> _toppingButtons = new List<ToppingButtonBinding>();

        [Header("操作按钮")]
        [SerializeField] [Tooltip("提交按钮")]
        private Button _submitButton;

        [Header("小料拖拽")]
        [SerializeField] [Tooltip("杯子动画管理器（提供放置区域和坐标转换）")]
        private CupAnimationManager _cupAnimationManager;

        [SerializeField] [Tooltip("小料拖拽幽灵预制体（含 Image 组件）")]
        private GameObject _toppingDragGhostPrefab;

        [SerializeField] [Tooltip("拖拽层（根 Canvas 下的全屏层，幽灵在此层显示）")]
        private RectTransform _dragLayer;

        [SerializeField] [Tooltip("UI 相机（ScreenSpaceCamera 时填写，Overlay 留空）")]
        private Camera _uiCamera;

        [Header("提示")]
        [SerializeField] [Tooltip("悬停提示显示延迟（秒）")]
        private float _tooltipDelay = 0.5f;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        private CoffeeCraftManager _manager;

        // 拖拽状态
        private ToppingSO _draggingTopping;
        private GameObject _dragGhostInstance;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            BindLiquidButtons();
            BindToppingButtons();

            if (_submitButton != null)
            {
                _submitButton.onClick.AddListener(OnSubmitButtonClick);
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
                _manager.OnModuleStateChanged += OnModuleStateChanged;
                _manager.OnCraftReset += OnCraftReset;
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
                _manager.OnModuleStateChanged -= OnModuleStateChanged;
                _manager.OnCraftReset -= OnCraftReset;
            }

            CancelDrag();
        }

        private void Update()
        {
            if (_draggingTopping == null) return;

            // 幽灵跟随鼠标
            if (_dragGhostInstance != null && _dragLayer != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragLayer,
                    Input.mousePosition,
                    _uiCamera,
                    out Vector2 localPoint
                );
                _dragGhostInstance.GetComponent<RectTransform>().anchoredPosition = localPoint;
            }

            // 右键取消
            if (Input.GetMouseButtonDown(1))
            {
                CancelDrag();
                return;
            }

            // 松开左键：尝试放置
            if (Input.GetMouseButtonUp(0))
            {
                TryPlaceTopping();
            }
        }

        private void OnCraftReset()
        {
            CancelDrag();
        }

        /// <summary>
        /// 绑定辅助液按钮、图标与事件
        /// </summary>
        private void BindLiquidButtons()
        {
            if (_liquidButtons == null)
            {
                return;
            }

            foreach (var binding in _liquidButtons)
            {
                if (binding == null || binding.button == null)
                {
                    continue;
                }

                binding.button.onClick.RemoveAllListeners();

                EventTrigger trigger = binding.button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = binding.button.gameObject.AddComponent<EventTrigger>();
                }

                trigger.triggers.Clear();

                EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerDown
                };
                LiquidSO liquid = binding.liquid;
                pointerDownEntry.callback.AddListener((data) => { OnLiquidButtonDown(liquid); });
                trigger.triggers.Add(pointerDownEntry);

                EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerUp
                };
                pointerUpEntry.callback.AddListener((data) => { OnLiquidButtonUp(); });
                trigger.triggers.Add(pointerUpEntry);

                AttachTooltip(binding.button, binding.liquid);
            }
        }

        /// <summary>
        /// 绑定小料按钮：PointerDown 开始拖拽
        /// </summary>
        private void BindToppingButtons()
        {
            if (_toppingButtons == null) return;

            foreach (var binding in _toppingButtons)
            {
                if (binding == null || binding.button == null) continue;

                binding.button.onClick.RemoveAllListeners();

                EventTrigger trigger = binding.button.gameObject.GetComponent<EventTrigger>()
                    ?? binding.button.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                ToppingSO topping = binding.topping;
                var pointerDownEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pointerDownEntry.callback.AddListener(eventData =>
                {
                    var ptr = (PointerEventData)eventData;
                    if (ptr.button == PointerEventData.InputButton.Left)
                        OnToppingButtonPointerDown(topping);
                });
                trigger.triggers.Add(pointerDownEntry);

                AttachTooltip(binding.button, binding.topping);
            }
        }

        private void AttachTooltip(Button button, ScriptableObject tooltipSource)
        {
            if (button == null || tooltipSource == null) return;

            var trigger = button.GetComponent<HoverTooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<HoverTooltipTrigger>();
            }

            trigger.Configure(tooltipSource, _tooltipDelay);
        }

        /// <summary>
        /// 辅助液按钮按下
        /// </summary>
        private void OnLiquidButtonDown(LiquidSO liquid)
        {
            if (_manager == null)
            {
                return;
            }

            _manager.StartPourLiquid(liquid);

            if (_showDebugLog)
            {
                Debug.Log($"[LiquidToppingUI] 开始倒入：{(liquid != null ? liquid.liquidName : "null")}");
            }
        }

        /// <summary>
        /// 辅助液按钮松开
        /// </summary>
        private void OnLiquidButtonUp()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.StopPourLiquid();

            if (_showDebugLog)
            {
                Debug.Log("[LiquidToppingUI] 停止倒入");
            }
        }

        /// <summary>
        /// 小料按钮按下：创建拖拽幽灵
        /// </summary>
        private void OnToppingButtonPointerDown(ToppingSO topping)
        {
            if (_manager == null || topping == null) return;

            var coffeeData = _manager.CurrentCoffeeData;
            bool hasExtracted = coffeeData.coffeeSegments.Count > 0;
            if (!hasExtracted || coffeeData.toppings.Count >= 6) return;

            if (_toppingDragGhostPrefab == null || _dragLayer == null) return;

            // 取消上一次未完成的拖拽
            CancelDrag();

            _draggingTopping = topping;

            _dragGhostInstance = Instantiate(_toppingDragGhostPrefab, _dragLayer);
            var rt = _dragGhostInstance.GetComponent<RectTransform>();
            if (rt == null) rt = _dragGhostInstance.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = topping.displaySize;

            var img = _dragGhostInstance.GetComponent<Image>();
            if (img == null) img = _dragGhostInstance.AddComponent<Image>();
            img.sprite         = topping.icon;
            img.preserveAspect = true;
            img.raycastTarget  = false; // 幽灵不阻挡射线

            if (_showDebugLog)
                Debug.Log($"[LiquidToppingUI] 开始拖拽小料：{topping.toppingName}");
        }

        /// <summary>
        /// 尝试在杯口区域放置小料
        /// </summary>
        private void TryPlaceTopping()
        {
            if (_draggingTopping == null) return;

            bool placed = false;

            if (_cupAnimationManager != null
                && _cupAnimationManager.ToppingDropZone != null
                && _cupAnimationManager.WorkCupRect != null)
            {
                bool inZone = RectTransformUtility.RectangleContainsScreenPoint(
                    _cupAnimationManager.ToppingDropZone,
                    Input.mousePosition,
                    _uiCamera
                );

                if (inZone)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _cupAnimationManager.ToppingContainer,
                        Input.mousePosition,
                        _uiCamera,
                        out Vector2 localPos
                    );

                    _manager?.AddTopping(_draggingTopping, localPos);
                    placed = true;

                    if (_showDebugLog)
                        Debug.Log($"[LiquidToppingUI] 放置小料：{_draggingTopping.toppingName} @ {localPos}");
                }
            }

            if (!placed && _showDebugLog)
                Debug.Log("[LiquidToppingUI] 放置区域外，取消放置");

            CancelDrag();
        }

        /// <summary>
        /// 取消拖拽，销毁幽灵
        /// </summary>
        private void CancelDrag()
        {
            if (_dragGhostInstance != null)
            {
                Destroy(_dragGhostInstance);
                _dragGhostInstance = null;
            }
            _draggingTopping = null;
        }

        /// <summary>
        /// 提交按钮点击
        /// </summary>
        private void OnSubmitButtonClick()
        {
            // 通过GameFlowManager提交，由流程管理器驱动后续评分和反馈
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnPlayerSubmitCoffee();
            }
            else
            {
                // 无流程管理器时直接提交（兼容独立测试）
                if (_manager != null)
                {
                    _manager.SubmitCoffee();
                }
            }

            if (_showDebugLog)
            {
                Debug.Log("[LiquidToppingUI] 提交咖啡");
            }
        }

        /// <summary>
        /// 咖啡数据变化回调
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData coffeeData)
        {
            RefreshUI();
        }

        /// <summary>
        /// 模块状态变化回调
        /// </summary>
        private void OnModuleStateChanged(CraftModuleState state)
        {
            RefreshUI();
        }

        /// <summary>
        /// 刷新UI显示
        /// </summary>
        private void RefreshUI()
        {
            if (_manager == null)
            {
                return;
            }

            var coffeeData = _manager.CurrentCoffeeData;
            var moduleState = _manager.ModuleState;

            // 只有萃取后才能倒辅助液和加小料
            bool hasExtracted = coffeeData.coffeeSegments.Count > 0;

            // 更新辅助液按钮状态
            if (_liquidButtons != null)
            {
                foreach (var binding in _liquidButtons)
                {
                    if (binding != null)
                    {
                        SetButtonInteractable(binding.button, hasExtracted);
                    }
                }
            }

            // 更新小料按钮状态
            bool canAddTopping = hasExtracted && coffeeData.toppings.Count < 6;
            if (_toppingButtons != null)
            {
                foreach (var binding in _toppingButtons)
                {
                    if (binding != null)
                    {
                        SetButtonInteractable(binding.button, canAddTopping);
                    }
                }
            }

            // 更新提交按钮状态
            if (_submitButton != null)
            {
                _submitButton.interactable = _manager.CanSubmit();
            }
        }

        /// <summary>
        /// 设置按钮可交互状态
        /// </summary>
        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
