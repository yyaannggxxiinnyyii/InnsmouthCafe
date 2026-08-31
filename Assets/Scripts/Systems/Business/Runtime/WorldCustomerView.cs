using TMPro;
using UnityEngine;
using InnsmouthCafe.Customer;
using InnsmouthCafe.Data;
using RuntimeCustomerState = InnsmouthCafe.Customer.CustomerState;

namespace InnsmouthCafe.Business
{
    /// <summary>
    /// 单个顾客场景对象，负责同步立绘和基础名称显示。
    /// </summary>
    public class WorldCustomerView : MonoBehaviour
    {
        [Header("显示组件")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private TMP_Text _orderText;
        [SerializeField] private GameObject _dialogueBubble;
        [SerializeField] private GameObject _orderBubble;
        [SerializeField] private GameObject _orderActions;
        [SerializeField] private WorldCustomerOrderInteractable _acceptInteractable;
        [SerializeField] private WorldCustomerOrderInteractable _rejectInteractable;

        private CustomerInstance _customer;

        /// <summary>
        /// 当前绑定的顾客数据。
        /// </summary>
        public CustomerInstance Customer => _customer;

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_nameText == null)
            {
                _nameText = GetComponentInChildren<TMP_Text>();
            }
        }

        /// <summary>
        /// 绑定顾客数据并刷新场景显示。
        /// </summary>
        public void Bind(CustomerInstance customer)
        {
            _customer = customer;
            RefreshVisual(customer != null ? customer.currentState : RuntimeCustomerState.None);
            RefreshInteraction();
        }

        /// <summary>
        /// 根据顾客状态切换对应立绘。
        /// </summary>
        public void RefreshVisual(RuntimeCustomerState state)
        {
            if (_customer?.customerSO == null)
            {
                return;
            }

            CustomerSO config = _customer.customerSO;
            Sprite sprite = state switch
            {
                RuntimeCustomerState.Angry => config.angrySprite,
                RuntimeCustomerState.Happy => config.happySprite,
                _ => config.normalSprite
            };

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite != null ? sprite : config.normalSprite;
            }

            if (_nameText != null)
            {
                _nameText.text = config.customerName;
            }

            RefreshInteraction();
        }

        /// <summary>
        /// 根据顾客状态显示对白、订单和操作对象。
        /// </summary>
        public void RefreshInteraction()
        {
            if (_customer?.customerSO == null)
            {
                return;
            }

            bool proposed = _customer.currentState == RuntimeCustomerState.OrderProposed;
            bool greeting = _customer.currentState == RuntimeCustomerState.Queuing
                || _customer.currentState == RuntimeCustomerState.Thinking;

            // 订单提出后仍保留气泡，单气泡配置时直接复用对白气泡。
            if (_dialogueBubble != null) _dialogueBubble.SetActive(greeting || proposed);
            if (_orderBubble != null && _orderBubble != _dialogueBubble) _orderBubble.SetActive(proposed);
            if (_orderActions != null) _orderActions.SetActive(proposed);

            TMP_Text sharedText = _orderText != null ? _orderText : _dialogueText;
            if (proposed && sharedText != null)
            {
                sharedText.text = _customer.proposedOrder != null
                    ? _customer.proposedOrder.orderName
                    : string.Empty;
            }
            else if (_dialogueText != null && _customer.customerSO.enterDialogueTexts != null
                     && _customer.customerSO.enterDialogueTexts.Count > 0)
            {
                _dialogueText.text = _customer.customerSO.enterDialogueTexts[0];
            }

            if (_orderText != null && _orderText != sharedText)
            {
                _orderText.text = _customer.proposedOrder != null
                    ? _customer.proposedOrder.orderName
                    : string.Empty;
            }

            _acceptInteractable?.Bind(_customer);
            _rejectInteractable?.Bind(_customer);
        }
    }

}
