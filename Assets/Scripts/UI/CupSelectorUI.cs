using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 杯型选择UI组件
    /// 只负责按钮交互和选中状态，不处理杯子移动动画
    /// </summary>
    public class CupSelectorUI : MonoBehaviour
    {
        [Serializable]
        public class CupButtonBinding
        {
            [Tooltip("杯型SO配置")]
            public CupContainerSO cup;

            [Tooltip("对应按钮")]
            public Button button;
        }

        [Header("杯型配置")]
        [SerializeField] [Tooltip("杯型与按钮的对应配置")]
        private List<CupButtonBinding> _cupBindings = new List<CupButtonBinding>();

        [Header("提示")]
        [SerializeField] [Tooltip("悬停提示显示延迟（秒）")]
        private float _tooltipDelay = 0.5f;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        /// <summary>
        /// 杯子选中事件（参数：选中的CupContainerSO, 新索引, 旧索引）
        /// 旧索引为-1表示第一次选择
        /// </summary>
        public event Action<CupContainerSO, int, int> OnCupSelected;

        private CoffeeCraftManager _manager;
        private int _selectedIndex = -1;

        /// <summary>
        /// 获取杯型绑定列表（供CupAnimationManager读取）
        /// </summary>
        public List<CupButtonBinding> CupBindings => _cupBindings;

        /// <summary>
        /// 当前选中索引
        /// </summary>
        public int SelectedIndex => _selectedIndex;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            for (int i = 0; i < _cupBindings.Count; i++)
            {
                var binding = _cupBindings[i];
                if (binding == null || binding.button == null) continue;

                AttachTooltip(binding.button, binding.cup);

                int index = i;
                binding.button.onClick.AddListener(() => OnCupButtonClick(index));
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
                _manager.OnCraftReset += ResetSelection;
            }

            RefreshButtonStates();
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
                _manager.OnCraftReset -= ResetSelection;
            }
        }

        /// <summary>
        /// 咖啡数据变化回调
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData coffeeData)
        {
            RefreshButtonStates();
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
        /// 杯子按钮点击
        /// </summary>
        private void OnCupButtonClick(int index)
        {
            if (index < 0 || index >= _cupBindings.Count) return;
            if (_manager == null || !_manager.CanSelectCup()) return;

            var binding = _cupBindings[index];
            if (binding.cup == null) return;

            // 点击已选中的杯子，忽略
            if (index == _selectedIndex) return;

            int oldIndex = _selectedIndex;
            _selectedIndex = index;

            // 通知管理器选择杯子
            _manager.SelectCup(binding.cup.ToData());

            // 触发选中事件（CupAnimationManager监听）
            OnCupSelected?.Invoke(binding.cup, index, oldIndex);

            // 更新按钮视觉
            RefreshButtonStates();

            if (_showDebugLog)
            {
                Debug.Log($"[CupSelector] 选择杯型: {binding.cup.cupName} ({binding.cup.capacity}ml)");
            }
        }

        /// <summary>
        /// 刷新按钮可交互状态和选中高亮
        /// </summary>
        private void RefreshButtonStates()
        {
            if (_manager == null) return;

            bool canSelect = _manager.CanSelectCup();

            for (int i = 0; i < _cupBindings.Count; i++)
            {
                var binding = _cupBindings[i];
                if (binding?.button == null) continue;

                binding.button.interactable = canSelect;
            }
        }

        /// <summary>
        /// 重置选择状态（新一轮制作时调用）
        /// </summary>
        public void ResetSelection()
        {
            _selectedIndex = -1;
            RefreshButtonStates();
        }
    }
}
