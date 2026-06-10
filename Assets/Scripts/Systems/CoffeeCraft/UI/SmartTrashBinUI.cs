using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 智能垃圾桶UI组件
    /// 根据当前制作阶段自动判断倒掉什么
    /// </summary>
    public class SmartTrashBinUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("垃圾桶按钮")]
        private Button _trashButton;

        [Header("杯子动画")]
        [SerializeField] [Tooltip("杯子动画管理器（倒掉整杯时触发丢弃动画）")]
        private CupAnimationManager _cupAnimationManager;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        private CoffeeCraftManager _manager;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 绑定按钮事件
            if (_trashButton != null)
            {
                _trashButton.onClick.AddListener(OnTrashButtonClick);
            }

            // 绑定 Tooltip
            var tooltip = GetComponent<HoverTooltipTrigger>();
            if (tooltip == null)
                tooltip = gameObject.AddComponent<HoverTooltipTrigger>();
            tooltip.ConfigureText("垃圾桶", "用来丢弃失败品");
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged += OnBatchDataChanged;
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
            }

            RefreshButtonState();
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged -= OnBatchDataChanged;
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
            }
        }

        /// <summary>
        /// 批次数据变化回调
        /// </summary>
        private void OnBatchDataChanged(CurrentBeanBatchData batch)
        {
            RefreshButtonState();
        }

        /// <summary>
        /// 咖啡数据变化回调
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData coffeeData)
        {
            RefreshButtonState();
        }

        /// <summary>
        /// 垃圾桶按钮点击
        /// </summary>
        private void OnTrashButtonClick()
        {
            if (_manager == null)
            {
                return;
            }

            var batch = _manager.CurrentBatch;
            var coffeeData = _manager.CurrentCoffeeData;

            // 判断当前阶段并执行对应的倒掉操作
            if (coffeeData.coffeeSegments.Count > 0)
            {
                // 已萃取阶段：倒掉整杯咖啡 + 杯子丢进垃圾桶动画
                //if (_showDebugLog)
                //{
                //    ActionLogBus.Log("倒掉整杯咖啡");
                //}

                // 触发杯子丢弃动画
                if (_cupAnimationManager != null)
                {
                    _cupAnimationManager.AnimateTrashCup();
                }

                _manager.ClearWholeCoffee();
            }
            else if (batch.grindType.HasValue)
            {
                // 已研磨未萃取阶段：倒掉咖啡粉（不重置杯子）
                //if (_showDebugLog)
                //{
                //    ActionLogBus.Log("倒掉咖啡粉");
                //}
                _manager.ClearCurrentPowder();
            }
            else if (batch.beanGram > 0f)
            {
                // 取豆未研磨阶段：倒掉豆子（不重置杯子）
                //if (_showDebugLog)
                //{
                //    ActionLogBus.Log("倒掉豆子");
                //}
                _manager.ClearCurrentBeans();
            }
            else
            {
                // 没有任何东西可以倒掉
                if (_showDebugLog)
                {
                    ActionLogBus.LogWarning("没有任何东西可以倒掉");
                }
            }
        }

        /// <summary>
        /// 刷新按钮状态
        /// </summary>
        private void RefreshButtonState()
        {
            if (_trashButton == null || _manager == null)
            {
                return;
            }

            var batch = _manager.CurrentBatch;
            var coffeeData = _manager.CurrentCoffeeData;

            // 只要有豆子、咖啡粉或已萃取的咖啡，就可以点击垃圾桶
            //bool hasAnythingToTrash = batch.beanGram > 0f ||
            //                          batch.grindType.HasValue ||
            //                          coffeeData.coffeeSegments.Count > 0;

            //_trashButton.interactable = hasAnythingToTrash;
        }
    }
}
