using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.UI;

namespace InnsmouthCafe.Customer
{
    /// <summary>
    /// 新顾客系统的小票控制器，复用旧小票的显示组件但不依赖旧订单管理器。
    /// </summary>
    public class NewOrderTicketController : MonoBehaviour
    {
        [Header("小票列表")]
        [Tooltip("小票缩略项父对象")]
        [SerializeField] private Transform _ticketListRoot;

        [Tooltip("小票缩略项预制体")]
        [SerializeField] private OrderTicketPreviewItemUI _ticketPreviewPrefab;

        [Tooltip("小票详情面板")]
        [SerializeField] private OrderTicketDetailPanelUI _detailPanel;

        [Header("显示行为")]
        [Tooltip("接受订单后是否自动展开第一张小票详情")]
        [SerializeField] private bool _autoShowFirstDetail = true;

        [Header("快捷键")]
        [Tooltip("按住超过此时间视为长按")]
        [SerializeField] private float _longPressThreshold = 0.25f;

        private readonly List<OrderTicketPreviewItemUI> _previewItems =
            new List<OrderTicketPreviewItemUI>();
        private CustomerOrderSessionData _currentSession;
        private float _ticketKeyDownTime;
        private bool _ticketLongPressHandled;

        /// <summary>
        /// 当前详情面板是否处于展开或动画状态。
        /// </summary>
        public bool IsDetailVisibleOrAnimating => _detailPanel != null
            && _detailPanel.IsDetailVisibleOrAnimating;

        private void OnEnable()
        {
            if (NewCustomerManager.Instance == null)
            {
                return;
            }

            NewCustomerManager.Instance.OnOrderSessionAccepted += HandleOrderSessionAccepted;
            NewCustomerManager.Instance.OnOrderSessionChanged += HandleOrderSessionChanged;
            NewCustomerManager.Instance.OnCustomerLeft += HandleCustomerLeft;
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                _ticketKeyDownTime = Time.unscaledTime;
                _ticketLongPressHandled = false;
            }

            if (Input.GetKey(KeyCode.Q)
                && !_ticketLongPressHandled
                && Time.unscaledTime - _ticketKeyDownTime >= _longPressThreshold)
            {
                _ticketLongPressHandled = true;
                ShowSelectedDetail();
            }

            if (Input.GetKeyUp(KeyCode.Q))
            {
                if (_ticketLongPressHandled)
                {
                    CollapseDetail();
                }
                else
                {
                    ToggleSelectedDetail();
                }
            }
        }

        private void OnDisable()
        {
            if (NewCustomerManager.Instance == null)
            {
                return;
            }

            NewCustomerManager.Instance.OnOrderSessionAccepted -= HandleOrderSessionAccepted;
            NewCustomerManager.Instance.OnOrderSessionChanged -= HandleOrderSessionChanged;
            NewCustomerManager.Instance.OnCustomerLeft -= HandleCustomerLeft;
        }

        private void OnDestroy()
        {
            ClearPreviewItems();
        }

        /// <summary>
        /// 接受订单后显示新订单会话的小票。
        /// </summary>
        /// <param name="session">需要显示的订单会话。</param>
        private void HandleOrderSessionAccepted(CustomerOrderSessionData session)
        {
            RefreshSession(session);
        }

        /// <summary>
        /// 小票状态变化后刷新完成标记和可选状态。
        /// </summary>
        /// <param name="session">发生变化的订单会话。</param>
        private void HandleOrderSessionChanged(CustomerOrderSessionData session)
        {
            if (_currentSession != session)
            {
                return;
            }

            RefreshPreviewStates();
        }

        /// <summary>
        /// 顾客离场后清空当前小票。
        /// </summary>
        /// <param name="customer">离场的顾客。</param>
        private void HandleCustomerLeft(CustomerInstance customer)
        {
            if (_currentSession?.primaryCustomer == customer?.customerSO)
            {
                ResetTicket();
            }
        }

        /// <summary>
        /// 刷新当前订单会话的小票列表。
        /// </summary>
        /// <param name="session">需要显示的订单会话。</param>
        private void RefreshSession(CustomerOrderSessionData session)
        {
            _currentSession = session;
            ClearPreviewItems();
            _detailPanel?.ResetDetail();

            if (session == null || session.orderSlots == null || _ticketListRoot == null
                || _ticketPreviewPrefab == null)
            {
                return;
            }

            for (int i = 0; i < session.orderSlots.Count; i++)
            {
                OrderTicketPreviewItemUI item = Instantiate(_ticketPreviewPrefab, _ticketListRoot);
                CustomerOrderSlotData slot = session.orderSlots[i];
                item.Bind(slot, i == session.selectedSlotIndex);
                item.OnSelected += HandlePreviewSelected;
                item.OnDetailRequested += HandlePreviewDetailRequested;
                _previewItems.Add(item);
            }

            RefreshPreviewStates();

            if (_autoShowFirstDetail && session.SelectedSlot != null)
            {
                _detailPanel?.ShowSlot(session.SelectedSlot, true);
            }
        }

        /// <summary>
        /// 选择指定小票作为下一杯咖啡的提交目标。
        /// </summary>
        /// <param name="item">被点击的小票项。</param>
        private void HandlePreviewSelected(OrderTicketPreviewItemUI item)
        {
            CustomerOrderSlotData slot = item?.BoundSlot;
            if (slot == null || !slot.IsWaitingForSubmission
                || NewCustomerManager.Instance == null)
            {
                return;
            }

            if (NewCustomerManager.Instance.TrySelectOrderSlot(slot.slotId))
            {
                _currentSession.TrySelectSlot(slot.slotId);
                RefreshPreviewSelection();
                _detailPanel?.ShowSlot(slot, false);
            }
        }

        /// <summary>
        /// 打开当前选中小票的详情。
        /// </summary>
        /// <param name="item">被点击的小票项。</param>
        private void HandlePreviewDetailRequested(OrderTicketPreviewItemUI item)
        {
            if (item?.BoundSlot == null || !item.BoundSlot.IsWaitingForSubmission)
            {
                return;
            }

            _detailPanel?.ShowSlot(item.BoundSlot, false);
        }

        /// <summary>
        /// 刷新小票的选中状态。
        /// </summary>
        private void RefreshPreviewSelection()
        {
            for (int i = 0; i < _previewItems.Count; i++)
            {
                _previewItems[i]?.SetSelected(i == _currentSession?.selectedSlotIndex);
            }
        }

        /// <summary>
        /// 刷新小票完成标记和可点击状态。
        /// </summary>
        private void RefreshPreviewStates()
        {
            RefreshPreviewSelection();
            foreach (OrderTicketPreviewItemUI item in _previewItems)
            {
                item?.RefreshState();
            }
        }

        /// <summary>
        /// 清空当前小票并隐藏详情。
        /// </summary>
        public void ResetTicket()
        {
            _currentSession = null;
            ClearPreviewItems();
            _detailPanel?.ResetDetail();
        }

        /// <summary>
        /// 切换当前选中订单的详情显示状态。
        /// </summary>
        public void ToggleSelectedDetail()
        {
            if (IsDetailVisibleOrAnimating)
            {
                _detailPanel?.Collapse();
                return;
            }

            ShowSelectedDetail();
        }

        /// <summary>
        /// 显示当前选中订单的详情。
        /// </summary>
        public void ShowSelectedDetail()
        {
            if (_currentSession?.SelectedSlot == null)
            {
                return;
            }

            _detailPanel?.gameObject.SetActive(true);
            _detailPanel?.ShowSlot(_currentSession.SelectedSlot, false);
        }

        /// <summary>
        /// 收起当前订单详情。
        /// </summary>
        public void CollapseDetail()
        {
            _detailPanel?.Collapse();
        }

        /// <summary>
        /// 销毁当前运行时生成的小票项。
        /// </summary>
        private void ClearPreviewItems()
        {
            foreach (OrderTicketPreviewItemUI item in _previewItems)
            {
                if (item == null)
                {
                    continue;
                }

                item.OnSelected -= HandlePreviewSelected;
                item.OnDetailRequested -= HandlePreviewDetailRequested;
                Destroy(item.gameObject);
            }

            _previewItems.Clear();
        }
    }
}
