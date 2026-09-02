using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景出餐交互对象，负责确认工作杯位置并提交当前咖啡。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WorldCoffeeServeInteractable : MonoBehaviour
    {
        [Header("工作杯引用")]
        [Tooltip("用于判断工作杯是否位于出餐锚点的场景控制器")]
        [SerializeField] private CoffeeWorkCupController _workCupController;

        [Header("按钮显示")]
        [Tooltip("出餐按钮的贴图；为空时使用当前对象的 SpriteRenderer")]
        [SerializeField] private SpriteRenderer _buttonRenderer;

        [Tooltip("出餐按钮的碰撞体；为空时使用当前对象的 Collider")]
        [SerializeField] private Collider _buttonCollider;

        private void Awake()
        {
            if (_buttonRenderer == null)
            {
                _buttonRenderer = GetComponent<SpriteRenderer>();
            }

            if (_buttonCollider == null)
            {
                _buttonCollider = GetComponent<Collider>();
            }
        }

        private void OnEnable()
        {
            if (_workCupController != null)
            {
                _workCupController.OnWorkCupAnchorChanged += RefreshVisibility;
            }

            if (NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCoffeeDataChanged += HandleCoffeeDataChanged;
                NewCoffeeCraftManager.Instance.OnCraftReset += RefreshVisibility;
            }

            RefreshVisibility();
        }

        private void OnDisable()
        {
            if (_workCupController != null)
            {
                _workCupController.OnWorkCupAnchorChanged -= RefreshVisibility;
            }

            if (NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCoffeeDataChanged -= HandleCoffeeDataChanged;
                NewCoffeeCraftManager.Instance.OnCraftReset -= RefreshVisibility;
            }
        }

        /// <summary>
        /// 尝试从出餐点提交当前咖啡。
        /// </summary>
        public void ServeCoffee()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return;
            }

            if (_workCupController == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 出餐对象未配置工作杯控制器", this);
                return;
            }

            if (!_workCupController.IsWorkCupAtServe())
            {
                Debug.LogWarning("[NewCoffeeCraft] 工作杯未放在出餐锚点", this);
                return;
            }

            NewCoffeeCraftManager.Instance.TrySubmitCoffee();
        }

        /// <summary>
        /// 根据工作杯位置和当前成品状态刷新出餐按钮的显示与点击状态。
        /// </summary>
        private void RefreshVisibility()
        {
            bool isVisible = _workCupController != null
                && _workCupController.IsWorkCupAtServe()
                && NewCoffeeCraftManager.Instance != null
                && NewCoffeeCraftManager.Instance.CanSubmitCoffee();

            if (_buttonRenderer != null)
            {
                _buttonRenderer.enabled = isVisible;
            }

            if (_buttonCollider != null)
            {
                _buttonCollider.enabled = isVisible;
            }
        }

        /// <summary>
        /// 咖啡数据变化后刷新出餐按钮状态。
        /// </summary>
        private void HandleCoffeeDataChanged(CoffeeData coffeeData)
        {
            RefreshVisibility();
        }
    }
}
