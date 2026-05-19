using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 豆子和研磨UI交互组件
    /// 整合豆种选择、取豆、研磨、萃取等功能
    /// </summary>
    public class BeanGrindUI : MonoBehaviour
    {
        [Header("豆种配置")]
        [SerializeField] [Tooltip("普通豆配置")]
        private BeanSO _normalBeanConfig;

        [SerializeField] [Tooltip("阿拉比卡豆配置")]
        private BeanSO _arabicaBeanConfig;

        [SerializeField] [Tooltip("罗布斯塔豆配置")]
        private BeanSO _robustaBeanConfig;

        [Header("豆种选择")]
        [SerializeField] [Tooltip("普通豆按钮")]
        private Button _normalBeanButton;

        [SerializeField] [Tooltip("阿拉比卡豆按钮")]
        private Button _arabicaBeanButton;

        [SerializeField] [Tooltip("罗布斯塔豆按钮")]
        private Button _robustaBeanButton;

        [Header("操作按钮")]
        [SerializeField] [Tooltip("萃取按钮")]
        private Button _extractButton;

        [Header("状态显示")]
        [SerializeField] [Tooltip("当前状态文本")]
        private TextMeshProUGUI _statusText;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        private CoffeeCraftManager _manager;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 绑定豆种按钮
            if (_normalBeanButton != null)
            {
                _normalBeanButton.onClick.AddListener(() => OnBeanButtonClick(_normalBeanConfig));
            }

            if (_arabicaBeanButton != null)
            {
                _arabicaBeanButton.onClick.AddListener(() => OnBeanButtonClick(_arabicaBeanConfig));
            }

            if (_robustaBeanButton != null)
            {
                _robustaBeanButton.onClick.AddListener(() => OnBeanButtonClick(_robustaBeanConfig));
            }

            // 绑定操作按钮
            if (_extractButton != null)
            {
                _extractButton.onClick.AddListener(OnExtractButtonClick);
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged += OnBatchDataChanged;
                _manager.OnModuleStateChanged += OnModuleStateChanged;
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged -= OnBatchDataChanged;
                _manager.OnModuleStateChanged -= OnModuleStateChanged;
            }
        }

        /// <summary>
        /// 批次数据变化回调
        /// </summary>
        private void OnBatchDataChanged(CurrentBeanBatchData batch)
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
        /// 豆种按钮点击
        /// </summary>
        private void OnBeanButtonClick(BeanSO bean)
        {
            if (_manager == null)
            {
                return;
            }

            _manager.AddBean(bean);

            if (_showDebugLog)
            {
                Debug.Log($"[BeanGrindUI] 取豆：{(bean != null ? bean.beanName : "null")}");
            }
        }

        /// <summary>
        /// 萃取按钮点击
        /// </summary>
        private void OnExtractButtonClick()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.StartExtraction();

            if (_showDebugLog)
            {
                Debug.Log("[BeanGrindUI] 开始萃取");
            }
        }

        /// <summary>
        /// 倒掉豆子按钮点击
        /// </summary>
        private void OnClearBeansButtonClick()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.ClearCurrentBeans();

            if (_showDebugLog)
            {
                Debug.Log("[BeanGrindUI] 倒掉豆子");
            }
        }

        /// <summary>
        /// 倒掉咖啡粉按钮点击
        /// </summary>
        private void OnClearPowderButtonClick()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.ClearCurrentPowder();

            if (_showDebugLog)
            {
                Debug.Log("[BeanGrindUI] 倒掉咖啡粉");
            }
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

            var batch = _manager.CurrentBatch;
            var moduleState = _manager.ModuleState;

            // 更新豆种按钮状态
            bool canAddBean = !batch.grindType.HasValue; // 未研磨才能取豆
            bool beanLimitReached = batch.beanGram >= 20f;

            if (_normalBeanButton != null)
            {
                _normalBeanButton.interactable = canAddBean && !beanLimitReached;
            }

            if (_arabicaBeanButton != null)
            {
                _arabicaBeanButton.interactable = canAddBean && !beanLimitReached;
            }

            if (_robustaBeanButton != null)
            {
                _robustaBeanButton.interactable = canAddBean && !beanLimitReached;
            }

            // 更新萃取按钮状态
            if (_extractButton != null)
            {
                bool canExtract = batch.CanExtract() &&
                                  _manager.CurrentCoffeeData.selectedCup != null &&
                                  !_manager.IsExtracting;
                _extractButton.interactable = canExtract;
            }

            // 更新状态文本
            UpdateStatusText();
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText()
        {
            if (_statusText == null || _manager == null)
            {
                return;
            }

            var batch = _manager.CurrentBatch;
            var moduleState = _manager.ModuleState;

            string statusStr = "";

            // 显示当前批次信息
            if (batch.beanGram > 0f)
            {
                string beanName = GetBeanName(batch.bean);
                statusStr += $"豆子：{beanName} {batch.beanGram}g";

                if (batch.grindType.HasValue)
                {
                    string grindName = GetGrindTypeName(batch.grindType.Value);
                    statusStr += $"\n研磨：{grindName}";
                }
            }
            else
            {
                statusStr = "请选择豆种并取豆";
            }

            // 显示萃取状态
            if (_manager.IsExtracting)
            {
                float progress = _manager.ExtractionProgress * 100f;
                statusStr += $"\n萃取中... {progress:F0}%";
            }

            _statusText.text = statusStr;
        }

        /// <summary>
        /// 获取豆种名称
        /// </summary>
        private string GetBeanName(BeanSO bean)
        {
            if (bean == null)
            {
                return "无";
            }

            return bean.beanName;
        }

        /// <summary>
        /// 获取研磨度名称
        /// </summary>
        private string GetGrindTypeName(GrindType grindType)
        {
            switch (grindType)
            {
                case GrindType.Coarse:
                    return "粗磨";
                case GrindType.Fine:
                    return "细磨";
                case GrindType.ExtraFine:
                    return "精磨";
                default:
                    return "未知";
            }
        }
    }
}
