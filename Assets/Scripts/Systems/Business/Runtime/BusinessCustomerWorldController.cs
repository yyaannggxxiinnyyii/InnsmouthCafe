using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Customer;
using RuntimeCustomerState = InnsmouthCafe.Customer.CustomerState;

namespace InnsmouthCafe.Business
{
    /// <summary>
    /// 经营场景顾客表现控制器，负责将顾客数据映射为场景中的顾客对象。
    /// </summary>
    public class BusinessCustomerWorldController : MonoBehaviour
    {
        [Header("顾客对象")]
        [Tooltip("顾客场景对象预制体，需包含 WorldCustomerView")]
        [SerializeField] private WorldCustomerView _customerPrefab;

        [Header("顾客点位")]
        [Tooltip("顾客依次进入的座位点")]
        [SerializeField] private List<Transform> _seatPoints = new List<Transform>();

        private readonly Dictionary<CustomerInstance, WorldCustomerView> _views =
            new Dictionary<CustomerInstance, WorldCustomerView>();

        private void OnEnable()
        {
            if (NewCustomerManager.Instance == null)
            {
                return;
            }

            NewCustomerManager.Instance.OnCustomerSpawned += HandleCustomerSpawned;
            NewCustomerManager.Instance.OnCustomerStateChanged += HandleCustomerStateChanged;
            NewCustomerManager.Instance.OnCustomerLeft += HandleCustomerLeft;
        }

        private void OnDisable()
        {
            if (NewCustomerManager.Instance == null)
            {
                return;
            }

            NewCustomerManager.Instance.OnCustomerSpawned -= HandleCustomerSpawned;
            NewCustomerManager.Instance.OnCustomerStateChanged -= HandleCustomerStateChanged;
            NewCustomerManager.Instance.OnCustomerLeft -= HandleCustomerLeft;
        }

        /// <summary>
        /// 顾客数据生成后创建对应的场景对象。
        /// </summary>
        private void HandleCustomerSpawned(CustomerInstance customer)
        {
            if (customer == null || _customerPrefab == null)
            {
                Debug.LogWarning("[BusinessCustomerWorld] 未配置顾客预制体，无法显示顾客", this);
                return;
            }

            int seatIndex = _views.Count;
            Transform seat = seatIndex < _seatPoints.Count ? _seatPoints[seatIndex] : null;
            WorldCustomerView view = Instantiate(
                _customerPrefab,
                seat != null ? seat : transform);

            if (seat != null)
            {
                view.transform.localPosition = Vector3.zero;
                view.transform.localRotation = Quaternion.identity;
            }

            view.Bind(customer);
            _views[customer] = view;
        }

        /// <summary>
        /// 顾客状态变化时刷新场景立绘。
        /// </summary>
        private void HandleCustomerStateChanged(CustomerInstance customer, RuntimeCustomerState state)
        {
            if (_views.TryGetValue(customer, out WorldCustomerView view) && view != null)
            {
                view.RefreshVisual(state);
            }
        }

        /// <summary>
        /// 顾客离场时销毁对应场景对象。
        /// </summary>
        private void HandleCustomerLeft(CustomerInstance customer)
        {
            if (!_views.TryGetValue(customer, out WorldCustomerView view))
            {
                return;
            }

            _views.Remove(customer);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }
    }
}
