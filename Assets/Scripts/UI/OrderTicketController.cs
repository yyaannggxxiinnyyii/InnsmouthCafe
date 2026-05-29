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

            // 初始隐藏并重置小票
            ResetTicket();

            // 订阅订单生成事件
            _orderManager.OnOrderGenerated += OnOrderGenerated;
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

            // 先把小票恢复到基准状态，再重新显示，避免继承上一单的收起状态
            if (_orderTicketUI != null)
            {
                _orderTicketUI.ResetTicketState();
            }

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

            TutorialEventBus.Publish(TutorialEvents.CustomerTicketShown);

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
        /// 隐藏并重置小票
        /// </summary>
        public void HideTicket()
        {
            ResetTicket();
            Debug.Log("[OrderTicketController] 隐藏订单小票");
        }

        /// <summary>
        /// 重置小票为初始状态
        /// </summary>
        public void ResetTicket()
        {
            if (_orderTicketUI != null)
            {
                _orderTicketUI.ResetTicketState();
            }

            if (_ticketPanel != null)
            {
                _ticketPanel.SetActive(false);
            }
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
