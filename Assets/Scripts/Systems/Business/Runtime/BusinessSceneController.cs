using UnityEngine;
using TMPro;
using InnsmouthCafe.Customer;
using InnsmouthCafe.Data;
using InnsmouthCafe.GameFlow;
using InnsmouthCafe.CoffeeCraft;
using FlowState = InnsmouthCafe.GameFlow.GameFlowState;

namespace InnsmouthCafe.Business
{
    /// <summary>
    /// 店铺营业阶段控制器，负责连接营业计时、顾客队列和营业场景玩法。
    /// </summary>
    public class BusinessSceneController : MonoBehaviour
    {
        [Header("营业界面")]
        [SerializeField] private GameObject _businessStatusPanel;
        [SerializeField] private TMP_Text _businessTimerText;
        [SerializeField] private TMP_Text _customerCountText;
        [SerializeField] private TMP_Text _orderCountText;

        private bool _businessStarted;
        private BusinessPhaseState _currentState = BusinessPhaseState.Inactive;

        /// <summary>
        /// 当前营业环节状态。
        /// </summary>
        public BusinessPhaseState CurrentState => _currentState;

        private void OnEnable()
        {
            if (NewCustomerManager.Instance != null)
            {
                NewCustomerManager.Instance.OnOrderAccepted += HandleOrderAccepted;
                NewCustomerManager.Instance.OnOrderSelected += HandleOrderSelected;
            }
            if (NewGameFlowManager.Instance != null)
            {
                NewGameFlowManager.Instance.OnStateChanged += HandleStateChanged;
            }

            if (BusinessTimerManager.Instance != null)
            {
                BusinessTimerManager.Instance.OnTimeUp += HandleBusinessTimeUp;
            }

            if (NewGameFlowManager.Instance != null
                && NewGameFlowManager.Instance.CurrentState == FlowState.Business)
            {
                StartBusiness();
            }
            else if (NewGameFlowManager.Instance != null
                     && NewGameFlowManager.Instance.CurrentState == FlowState.BusinessPreparation)
            {
                _currentState = BusinessPhaseState.Preparing;
            }

            RefreshBusinessPanel();
        }

        private void Update()
        {
            RefreshBusinessPanel();
        }

        private void OnDisable()
        {
            if (NewCustomerManager.Instance != null)
            {
                NewCustomerManager.Instance.OnOrderAccepted -= HandleOrderAccepted;
                NewCustomerManager.Instance.OnOrderSelected -= HandleOrderSelected;
            }
            if (NewGameFlowManager.Instance != null)
            {
                NewGameFlowManager.Instance.OnStateChanged -= HandleStateChanged;
            }

            if (BusinessTimerManager.Instance != null)
            {
                BusinessTimerManager.Instance.OnTimeUp -= HandleBusinessTimeUp;
            }
        }

        /// <summary>
        /// 将顾客接受的订单传递给咖啡制作流程。
        /// </summary>
        private void HandleOrderAccepted(OrderSO order)
        {
            if (_currentState != BusinessPhaseState.Running)
            {
                Debug.LogWarning("[BusinessScene] 当前不在营业运行阶段，忽略制作订单请求", this);
                return;
            }

            NewCoffeeCraftManager.Instance?.BeginOrder(order);
        }

        /// <summary>
        /// 将玩家切换的小票订单传递给咖啡制作流程。
        /// </summary>
        private void HandleOrderSelected(OrderSO order)
        {
            if (_currentState != BusinessPhaseState.Running || order == null)
            {
                return;
            }

            NewCoffeeCraftManager.Instance?.BeginOrder(order);
        }

        /// <summary>
        /// 监听全局流程进入营业状态。
        /// </summary>
        private void HandleStateChanged(FlowState state)
        {
            if (state == FlowState.Business)
            {
                StartBusiness();
            }
            else if (state == FlowState.Settlement)
            {
                _currentState = BusinessPhaseState.Completed;
                _businessStarted = false;
            }
        }

        /// <summary>
        /// 标记营业环节进入准备阶段。
        /// </summary>
        public void MarkPreparing()
        {
            _currentState = BusinessPhaseState.Preparing;
        }

        /// <summary>
        /// 启动本场营业所需的计时器和顾客队列。
        /// </summary>
        private void StartBusiness()
        {
            if (_businessStarted)
            {
                return;
            }

            if (BusinessTimerManager.Instance == null || NewCustomerManager.Instance == null)
            {
                Debug.LogWarning("[BusinessScene] 营业依赖尚未初始化，无法启动营业", this);
                return;
            }

            _businessStarted = true;
            _currentState = BusinessPhaseState.Running;

            NewCoffeeCraftManager.Instance?.ResetForBusiness();

            NewCustomerManager.Instance.GenerateTodayQueue();

            BusinessTimerManager.Instance.StartBusinessTimer();
            NewCustomerManager.Instance.StartBusinessPhase();
            Debug.Log("[BusinessScene] 已启动营业阶段", this);
            RefreshBusinessPanel();
        }

        /// <summary>
        /// 营业时间结束后停止生成新顾客，等待场上顾客处理完成。
        /// </summary>
        private void HandleBusinessTimeUp()
        {
            if (!_businessStarted)
            {
                return;
            }

            NewCustomerManager.Instance?.EndBusinessPhase();
            _currentState = BusinessPhaseState.WaitingForCustomers;
            Debug.Log("[BusinessScene] 营业时间结束，停止生成新顾客", this);
            RefreshBusinessPanel();
        }

        /// <summary>
        /// 根据全局和营业状态刷新营业面板及实时数据。
        /// </summary>
        private void RefreshBusinessPanel()
        {
            FlowState flowState = NewGameFlowManager.Instance != null
                ? NewGameFlowManager.Instance.CurrentState
                : FlowState.None;

            if (_businessStatusPanel != null)
            {
                _businessStatusPanel.SetActive(flowState == FlowState.Business);
            }

            if (_businessTimerText != null && BusinessTimerManager.Instance != null)
            {
                float remaining = BusinessTimerManager.Instance.RemainingBusinessTime;
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                _businessTimerText.text = $"营业时间：{minutes:00}:{seconds:00}";
            }

            if (_customerCountText != null && NewCustomerManager.Instance != null)
            {
                _customerCountText.text = $"在场顾客：{NewCustomerManager.Instance.ActiveCustomers.Count}";
            }

            if (_orderCountText != null && NewCustomerManager.Instance != null)
            {
                _orderCountText.text = $"完成订单：{NewCustomerManager.Instance.OrdersCompletedTonight}";
            }
        }
    }

    /// <summary>
    /// 营业环节内部状态。
    /// </summary>
    public enum BusinessPhaseState
    {
        Inactive,
        Preparing,
        Running,
        WaitingForCustomers,
        Completed
    }
}
