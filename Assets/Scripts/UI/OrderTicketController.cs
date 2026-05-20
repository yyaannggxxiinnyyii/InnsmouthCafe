using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票控制器
    /// 监听OrderManager事件，自动刷新小票显示
    /// </summary>
    public class OrderTicketController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] [Tooltip("OrderManager引用")]
        private OrderManager _orderManager;

        [SerializeField] [Tooltip("订单小票UI")]
        private OrderTicketUI _orderTicketUI;

        [Header("动画（可选）")]
        [SerializeField] [Tooltip("是否启用显示动画")]
        private bool _enableAnimation = false;

        [SerializeField] [Tooltip("小票面板（用于动画）")]
        private GameObject _ticketPanel;

        private void Start()
        {
            // 自动查找OrderManager
            if (_orderManager == null)
            {
                _orderManager = FindObjectOfType<OrderManager>();
                if (_orderManager == null)
                {
                    Debug.LogError("[OrderTicketController] 场景中没有找到OrderManager");
                    return;
                }
            }

            // 订阅订单生成事件
            _orderManager.OnOrderGenerated += OnOrderGenerated;

            // 初始隐藏小票
            if (_ticketPanel != null)
            {
                _ticketPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_orderManager != null)
            {
                _orderManager.OnOrderGenerated -= OnOrderGenerated;
            }
        }

        /// <summary>
        /// 订单生成事件回调
        /// </summary>
        private void OnOrderGenerated(OrderSO order)
        {
            Debug.Log($"[OrderTicketController] 收到订单生成事件: {order.orderName}");

            // 显示小票面板
            if (_ticketPanel != null)
            {
                _ticketPanel.SetActive(true);
            }

            // 刷新小票内容
            if (_orderTicketUI != null)
            {
                _orderTicketUI.RefreshTicket(order);
            }

            // 播放动画（可选）
            if (_enableAnimation)
            {
                PlayShowAnimation();
            }
        }

        /// <summary>
        /// 播放显示动画（可选实现）
        /// </summary>
        private void PlayShowAnimation()
        {
            // TODO: 实现小票显示动画
            // 例如：从上方滑入、淡入等
            Debug.Log("[OrderTicketController] 播放小票显示动画");
        }

        /// <summary>
        /// 隐藏小票
        /// </summary>
        public void HideTicket()
        {
            if (_ticketPanel != null)
            {
                _ticketPanel.SetActive(false);
            }

            if (_orderTicketUI != null)
            {
                _orderTicketUI.ClearTicket();
            }

            Debug.Log("[OrderTicketController] 隐藏订单小票");
        }

        /// <summary>
        /// 手动刷新小票（用于测试）
        /// </summary>
        public void ManualRefresh(OrderSO order)
        {
            if (_ticketPanel != null)
            {
                _ticketPanel.SetActive(true);
            }

            if (_orderTicketUI != null)
            {
                _orderTicketUI.RefreshTicket(order);
            }
        }
    }
}
