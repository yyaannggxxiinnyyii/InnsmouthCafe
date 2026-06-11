using System;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票控制器，统一管理订单缩略列表、当前选中小票和唯一详情面板。
    /// </summary>
    public class OrderTicketController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField]
        [Tooltip("OrderManager引用（为空时自动查找）")]
        private OrderManager _orderManager;

        [SerializeField]
        [Tooltip("小票缩略项父对象")]
        private Transform _ticketListRoot;

        [SerializeField]
        [Tooltip("小票缩略项预制体")]
        private OrderTicketPreviewItemUI _ticketPreviewPrefab;

        [SerializeField]
        [Tooltip("唯一详细小票面板")]
        private OrderTicketDetailPanelUI _detailPanel;

        [Header("行为")]
        [SerializeField]
        [Tooltip("生成小票时是否自动展开当前选中小票详情")]
        private bool _autoShowSelectedDetail = true;

        /// <summary>
        /// 当前选中的订单槽变化事件。
        /// </summary>
        public event Action<CustomerOrderSlotData> OnSlotSelected;

        /// <summary>
        /// 订单槽详情展开事件。
        /// </summary>
        public event Action<CustomerOrderSlotData> OnSlotDetailShown;

        /// <summary>
        /// 当前顾客组订单会话。
        /// </summary>
        private CustomerOrderSessionData _currentSession;

        /// <summary>
        /// 当前生成的小票缩略项。
        /// </summary>
        private readonly List<OrderTicketPreviewItemUI> _previewItems = new List<OrderTicketPreviewItemUI>();

        /// <summary>
        /// 当前选中的订单槽。
        /// </summary>
        public CustomerOrderSlotData SelectedSlot => _currentSession?.SelectedSlot;

        private void Start()
        {
            if (_orderManager == null)
            {
                _orderManager = FindObjectOfType<OrderManager>();
                if (_orderManager == null)
                {
                    Debug.LogError("[OrderTicket] 场景中没有找到 OrderManager");
                    return;
                }
            }

            ResetTicket();
            _orderManager.OnOrderGenerated += OnOrderGenerated;

            if (_detailPanel != null)
            {
                _detailPanel.OnDetailShown += OnDetailShown;
                _detailPanel.OnDetailHidden += OnDetailHidden;
            }
        }

        private void OnDestroy()
        {
            if (_orderManager != null)
            {
                _orderManager.OnOrderGenerated -= OnOrderGenerated;
            }

            if (_detailPanel != null)
            {
                _detailPanel.OnDetailShown -= OnDetailShown;
                _detailPanel.OnDetailHidden -= OnDetailHidden;
            }

            ClearPreviewItems();
        }

        /// <summary>
        /// 兼容旧订单生成事件：把单个订单包装为单槽会话并刷新小票列表。
        /// </summary>
        /// <param name="order">生成的订单。</param>
        private void OnOrderGenerated(OrderSO order)
        {
            if (order == null)
            {
                ResetTicket();
                return;
            }

            CustomerSO customer = _orderManager != null ? _orderManager.CurrentCustomer : null;
            CustomerOrderSessionData session = new CustomerOrderSessionData();
            session.Initialize("single_order_session", customer);

            string displayName = customer != null && !string.IsNullOrEmpty(customer.customerName)
                ? customer.customerName
                : "订单";

            CustomerOrderSlotData slot = new CustomerOrderSlotData();
            slot.Initialize("single_order_slot", displayName, order, customer?.ordererAvatarSprite);
            session.AddSlot(slot);
            RefreshSession(session);
        }

        /// <summary>
        /// 使用顾客组订单会话刷新小票列表。
        /// </summary>
        /// <param name="session">需要显示的顾客组订单会话。</param>
        /// <param name="allowAutoShowSelectedDetail">是否允许按配置自动展开当前选中小票详情。</param>
        public void RefreshSession(CustomerOrderSessionData session, bool allowAutoShowSelectedDetail = true)
        {
            _currentSession = session;

            if (session == null || session.orderSlots == null || session.orderSlots.Count == 0)
            {
                ResetTicket();
                return;
            }

            if (!ValidateRequiredReferences())
            {
                return;
            }

            ClearPreviewItems();
            EnsureSelectedSlot(session);
            RebuildPreviewItems(session);
            RefreshPreviewSelection();
            OnSlotSelected?.Invoke(session.SelectedSlot);

            if (allowAutoShowSelectedDetail && _autoShowSelectedDetail && session.SelectedSlot != null)
            {
                ShowSlotDetail(session.SelectedSlot, true);
            }
        }

        /// <summary>
        /// 选择指定索引的小票订单槽。
        /// </summary>
        /// <param name="slotIndex">订单槽索引。</param>
        /// <returns>选择成功返回 true。</returns>
        public bool SelectSlot(int slotIndex)
        {
            if (_currentSession == null || !_currentSession.TrySelectSlot(slotIndex))
            {
                return false;
            }

            RefreshPreviewSelection();
            OnSlotSelected?.Invoke(_currentSession.SelectedSlot);
            return true;
        }

        /// <summary>
        /// 选择指定ID的小票订单槽。
        /// </summary>
        /// <param name="slotId">订单槽ID。</param>
        /// <returns>选择成功返回 true。</returns>
        public bool SelectSlot(string slotId)
        {
            if (_currentSession == null || !_currentSession.TrySelectSlot(slotId))
            {
                return false;
            }

            RefreshPreviewSelection();
            OnSlotSelected?.Invoke(_currentSession.SelectedSlot);
            return true;
        }

        /// <summary>
        /// 展开当前选中小票的详情。
        /// </summary>
        public void ShowSelectedDetail()
        {
            if (_currentSession?.SelectedSlot == null)
            {
                return;
            }

            ShowSlotDetail(_currentSession.SelectedSlot, false);
        }

        /// <summary>
        /// 收起当前小票详情。
        /// </summary>
        public void CollapseDetail()
        {
            _detailPanel?.Collapse();
        }

        /// <summary>
        /// 隐藏并重置所有小票。
        /// </summary>
        public void ResetTicket()
        {
            _currentSession = null;
            ClearPreviewItems();
            _detailPanel?.ResetDetail();
        }

        /// <summary>
        /// 校验小票列表和详情面板引用。
        /// </summary>
        /// <returns>引用有效返回 true。</returns>
        private bool ValidateRequiredReferences()
        {
            bool valid = true;

            if (_ticketListRoot == null)
            {
                Debug.LogError("[OrderTicket] TicketListRoot 未配置，无法显示订单小票列表");
                valid = false;
            }

            if (_ticketPreviewPrefab == null)
            {
                Debug.LogError("[OrderTicket] TicketPreviewPrefab 未配置，无法显示订单小票列表");
                valid = false;
            }

            if (_detailPanel == null)
            {
                Debug.LogError("[OrderTicket] DetailPanel 未配置，无法显示订单详情");
                valid = false;
            }

            return valid;
        }

        /// <summary>
        /// 确保订单会话存在可选中的订单槽。
        /// </summary>
        /// <param name="session">需要检查的订单会话。</param>
        private void EnsureSelectedSlot(CustomerOrderSessionData session)
        {
            if (session.TrySelectSlot(session.selectedSlotIndex))
            {
                return;
            }

            if (session.SelectFirstWaitingSlot())
            {
                return;
            }

            if (session.SlotCount > 0)
            {
                session.TrySelectSlot(0);
            }
        }

        /// <summary>
        /// 重建小票缩略项列表。
        /// </summary>
        /// <param name="session">需要显示的订单会话。</param>
        private void RebuildPreviewItems(CustomerOrderSessionData session)
        {
            for (int i = 0; i < session.orderSlots.Count; i++)
            {
                CustomerOrderSlotData slot = session.orderSlots[i];
                OrderTicketPreviewItemUI item = Instantiate(_ticketPreviewPrefab, _ticketListRoot);
                item.gameObject.SetActive(true);
                item.Bind(slot, i == session.selectedSlotIndex);
                item.OnSelected += OnPreviewSelected;
                item.OnDetailRequested += OnPreviewDetailRequested;
                _previewItems.Add(item);
            }
        }

        /// <summary>
        /// 清理当前小票缩略项。
        /// </summary>
        private void ClearPreviewItems()
        {
            for (int i = _previewItems.Count - 1; i >= 0; i--)
            {
                OrderTicketPreviewItemUI item = _previewItems[i];
                if (item == null)
                {
                    continue;
                }

                item.OnSelected -= OnPreviewSelected;
                item.OnDetailRequested -= OnPreviewDetailRequested;
                Destroy(item.gameObject);
            }

            _previewItems.Clear();
        }

        /// <summary>
        /// 刷新缩略项选中高亮。
        /// </summary>
        private void RefreshPreviewSelection()
        {
            for (int i = 0; i < _previewItems.Count; i++)
            {
                _previewItems[i]?.SetSelected(i == _currentSession?.selectedSlotIndex);
                _previewItems[i]?.RefreshState();
            }
        }

        /// <summary>
        /// 处理缩略项选择请求。
        /// </summary>
        /// <param name="item">被点击的小票缩略项。</param>
        private void OnPreviewSelected(OrderTicketPreviewItemUI item)
        {
            if (item?.BoundSlot == null)
            {
                return;
            }

            SelectSlot(item.BoundSlot.slotId);
        }

        /// <summary>
        /// 处理缩略项详情请求。
        /// </summary>
        /// <param name="item">请求打开详情的小票缩略项。</param>
        private void OnPreviewDetailRequested(OrderTicketPreviewItemUI item)
        {
            if (item?.BoundSlot == null)
            {
                return;
            }

            SelectSlot(item.BoundSlot.slotId);
            ShowSlotDetail(item.BoundSlot, false);
        }

        /// <summary>
        /// 打开指定订单槽的详情面板。
        /// </summary>
        /// <param name="slot">需要展示的订单槽。</param>
        /// <param name="autoCollapse">是否自动收起。</param>
        private void ShowSlotDetail(CustomerOrderSlotData slot, bool autoCollapse)
        {
            if (_detailPanel == null || slot == null)
            {
                return;
            }

            _detailPanel.gameObject.SetActive(true);
            _detailPanel.ShowSlot(slot, autoCollapse);
            OnSlotDetailShown?.Invoke(slot);
        }

        /// <summary>
        /// 处理详情面板展开完成。
        /// </summary>
        private void OnDetailShown()
        {
            TutorialEventBus.Publish(TutorialEvents.CustomerTicketDetailShown);
        }

        /// <summary>
        /// 处理详情面板收起完成。
        /// </summary>
        private void OnDetailHidden()
        {
            TutorialEventBus.Publish(TutorialEvents.CustomerTicketShown);
        }
    }
}
