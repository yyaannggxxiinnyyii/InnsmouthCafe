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
    /// 负责处理辅助液倒入和小料添加/移除的交互
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

        [Header("提示")]
        [SerializeField] [Tooltip("悬停提示显示延迟（秒）")]
        private float _tooltipDelay = 0.5f;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        private CoffeeCraftManager _manager;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            BindLiquidButtons();
            BindToppingButtons();

            // 设置提交按钮
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
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
                _manager.OnModuleStateChanged -= OnModuleStateChanged;
            }
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

                if (binding.button.image != null && binding.liquid != null)
                {
                    Sprite buttonSprite = binding.liquid.containerIcon != null ? binding.liquid.containerIcon : binding.liquid.icon;
                    if (buttonSprite != null)
                    {
                        binding.button.image.sprite = buttonSprite;
                    }
                }

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
        /// 绑定小料按钮、图标与事件
        /// </summary>
        private void BindToppingButtons()
        {
            if (_toppingButtons == null)
            {
                return;
            }

            foreach (var binding in _toppingButtons)
            {
                if (binding == null || binding.button == null)
                {
                    continue;
                }

                binding.button.onClick.RemoveAllListeners();

                if (binding.button.image != null && binding.topping != null)
                {
                    Sprite buttonSprite = binding.topping.icon;
                    if (buttonSprite != null)
                    {
                        binding.button.image.sprite = buttonSprite;
                    }
                }

                EventTrigger trigger = binding.button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = binding.button.gameObject.AddComponent<EventTrigger>();
                }

                trigger.triggers.Clear();

                EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };
                ToppingSO topping = binding.topping;
                pointerClickEntry.callback.AddListener((data) =>
                {
                    PointerEventData pointerData = (PointerEventData)data;
                    if (pointerData.button == PointerEventData.InputButton.Left)
                    {
                        OnToppingButtonLeftClick(topping);
                    }
                    else if (pointerData.button == PointerEventData.InputButton.Right)
                    {
                        OnToppingButtonRightClick(topping);
                    }
                });
                trigger.triggers.Add(pointerClickEntry);

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
        /// 小料按钮左键点击（添加）
        /// </summary>
        private void OnToppingButtonLeftClick(ToppingSO topping)
        {
            if (_manager == null)
            {
                return;
            }

            _manager.AddTopping(topping);

            if (_showDebugLog)
            {
                Debug.Log($"[LiquidToppingUI] 添加小料：{(topping != null ? topping.toppingName : "null")}");
            }
        }

        /// <summary>
        /// 小料按钮右键点击（移除）
        /// </summary>
        private void OnToppingButtonRightClick(ToppingSO topping)
        {
            if (_manager == null)
            {
                return;
            }

            _manager.RemoveLastToppingOfType(topping);

            if (_showDebugLog)
            {
                Debug.Log($"[LiquidToppingUI] 移除小料：{(topping != null ? topping.toppingName : "null")}");
            }
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
            bool canAddTopping = hasExtracted && coffeeData.toppings.Count < 20;
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
