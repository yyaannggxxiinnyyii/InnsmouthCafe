using UnityEngine;
using InnsmouthCafe.Customer;

namespace InnsmouthCafe.Business
{
    /// <summary>
    /// 顾客订单接受或拒绝的场景交互对象。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCustomerOrderInteractable : MonoBehaviour
    {
        public enum ActionType
        {
            Accept,
            Reject
        }

        [SerializeField] private ActionType _action;
        private CustomerInstance _customer;

        /// <summary>
        /// 绑定当前顾客。
        /// </summary>
        public void Bind(CustomerInstance customer)
        {
            _customer = customer;
        }

        /// <summary>
        /// 响应玩家点击并处理订单。
        /// </summary>
        public void Interact()
        {
            NewCustomerManager manager = NewCustomerManager.Instance;
            if (manager == null || manager.CurrentServingCustomer != _customer)
            {
                return;
            }

            if (_action == ActionType.Accept)
            {
                manager.AcceptOrder();
            }
            else
            {
                manager.RejectOrder();
            }
        }
    }
}
