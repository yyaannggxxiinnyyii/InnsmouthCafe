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
        [Header("辅助液配置")]
        [SerializeField] [Tooltip("热水配置")]
        private LiquidSO _hotWaterConfig;

        [SerializeField] [Tooltip("牛奶配置")]
        private LiquidSO _milkConfig;

        [SerializeField] [Tooltip("奶泡配置")]
        private LiquidSO _foamConfig;

        [SerializeField] [Tooltip("冰水配置")]
        private LiquidSO _iceWaterConfig;

        [Header("小料配置")]
        [SerializeField] [Tooltip("焦糖碎配置")]
        private ToppingSO _caramelCrispConfig;

        [SerializeField] [Tooltip("巧克力粉配置")]
        private ToppingSO _chocolatePowderConfig;

        [SerializeField] [Tooltip("海星糖配置")]
        private ToppingSO _starfishCandyConfig;

        [SerializeField] [Tooltip("眼球爆珠配置")]
        private ToppingSO _eyeballPoppingBobaConfig;

        [SerializeField] [Tooltip("月尘粉配置")]
        private ToppingSO _moonDustConfig;

        [SerializeField] [Tooltip("黑盐配置")]
        private ToppingSO _blackSaltConfig;

        [Header("辅助液按钮")]
        [SerializeField] [Tooltip("热水按钮")]
        private Button _hotWaterButton;

        [SerializeField] [Tooltip("牛奶按钮")]
        private Button _milkButton;

        [SerializeField] [Tooltip("奶泡按钮")]
        private Button _foamButton;

        [SerializeField] [Tooltip("冰水按钮")]
        private Button _iceWaterButton;

        [Header("小料按钮")]
        [SerializeField] [Tooltip("焦糖碎按钮")]
        private Button _caramelCrispButton;

        [SerializeField] [Tooltip("巧克力粉按钮")]
        private Button _chocolatePowderButton;

        [SerializeField] [Tooltip("海星糖按钮")]
        private Button _starfishCandyButton;

        [SerializeField] [Tooltip("眼球爆珠按钮")]
        private Button _eyeballPoppingBobaButton;

        [SerializeField] [Tooltip("月尘粉按钮")]
        private Button _moonDustButton;

        [SerializeField] [Tooltip("黑盐按钮")]
        private Button _blackSaltButton;

        [Header("操作按钮")]
        [SerializeField] [Tooltip("提交按钮")]
        private Button _submitButton;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        private CoffeeCraftManager _manager;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 设置辅助液按钮的按住/松开事件
            SetupLiquidButton(_hotWaterButton, _hotWaterConfig);
            SetupLiquidButton(_milkButton, _milkConfig);
            SetupLiquidButton(_foamButton, _foamConfig);
            SetupLiquidButton(_iceWaterButton, _iceWaterConfig);

            // 设置小料按钮的左键/右键点击事件
            SetupToppingButton(_caramelCrispButton, _caramelCrispConfig);
            SetupToppingButton(_chocolatePowderButton, _chocolatePowderConfig);
            SetupToppingButton(_starfishCandyButton, _starfishCandyConfig);
            SetupToppingButton(_eyeballPoppingBobaButton, _eyeballPoppingBobaConfig);
            SetupToppingButton(_moonDustButton, _moonDustConfig);
            SetupToppingButton(_blackSaltButton, _blackSaltConfig);

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
        /// 设置辅助液按钮的按住/松开事件
        /// </summary>
        private void SetupLiquidButton(Button button, LiquidSO liquid)
        {
            if (button == null)
            {
                return;
            }

            // 获取或添加EventTrigger组件
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            // 添加PointerDown事件（按住）
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            pointerDownEntry.callback.AddListener((data) => { OnLiquidButtonDown(liquid); });
            trigger.triggers.Add(pointerDownEntry);

            // 添加PointerUp事件（松开）
            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            pointerUpEntry.callback.AddListener((data) => { OnLiquidButtonUp(); });
            trigger.triggers.Add(pointerUpEntry);
        }

        /// <summary>
        /// 设置小料按钮的左键/右键点击事件
        /// </summary>
        private void SetupToppingButton(Button button, ToppingSO topping)
        {
            if (button == null)
            {
                return;
            }

            // 获取或添加EventTrigger组件
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            // 添加PointerClick事件（点击）
            EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
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
            if (_manager == null)
            {
                return;
            }

            _manager.SubmitCoffee();

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
            SetButtonInteractable(_hotWaterButton, hasExtracted);
            SetButtonInteractable(_milkButton, hasExtracted);
            SetButtonInteractable(_foamButton, hasExtracted);
            SetButtonInteractable(_iceWaterButton, hasExtracted);

            // 更新小料按钮状态
            bool canAddTopping = hasExtracted && coffeeData.toppings.Count < 20;
            SetButtonInteractable(_caramelCrispButton, canAddTopping);
            SetButtonInteractable(_chocolatePowderButton, canAddTopping);
            SetButtonInteractable(_starfishCandyButton, canAddTopping);
            SetButtonInteractable(_eyeballPoppingBobaButton, canAddTopping);
            SetButtonInteractable(_moonDustButton, canAddTopping);
            SetButtonInteractable(_blackSaltButton, canAddTopping);

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
