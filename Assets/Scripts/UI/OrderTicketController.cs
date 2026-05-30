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
        [SerializeField] [Tooltip("OrderManager引用（为空时自动查找）")]
        private OrderManager _orderManager;

        [SerializeField] [Tooltip("订单小票UI")]
        private OrderTicketUI _orderTicketUI;

        private void Start()
        {
            if (_orderManager == null)
            {
                _orderManager = FindObjectOfType<OrderManager>();
                if (_orderManager == null)
                {
                    Debug.LogError("[OrderTicketController] 场景中没有找到OrderManager");
                    return;
                }
            }

            ResetTicket();

            _orderManager.OnOrderGenerated += OnOrderGenerated;
        }

        private void OnDestroy()
        {
            if (_orderManager != null)
            {
                _orderManager.OnOrderGenerated -= OnOrderGenerated;
            }
        }

        private void OnOrderGenerated(OrderSO order)
        {
            if (_orderTicketUI == null) return;

            _orderTicketUI.ResetTicketState();
            _orderTicketUI.gameObject.SetActive(true);  // 先激活，再刷新
            _orderTicketUI.RefreshTicket(order);

            TutorialEventBus.Publish(TutorialEvents.CustomerTicketShown);
        }

        /// <summary>
        /// 隐藏并重置小票
        /// </summary>
        public void ResetTicket()
        {
            if (_orderTicketUI == null) return;

            _orderTicketUI.ResetTicketState();
            _orderTicketUI.gameObject.SetActive(false);
        }
    }
}
