using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景交互射线检测器。
    /// 使用摄像机射线检测 3D Collider，并转发给场景交互对象。
    /// </summary>
    public class WorldInteractionRaycaster : MonoBehaviour
    {
        [Header("射线配置")]
        [Tooltip("用于发射射线的摄像机，留空时使用主摄像机")]
        [SerializeField] private Camera _targetCamera;

        [Tooltip("可交互对象所在层")]
        [SerializeField] private LayerMask _interactionMask = Physics.DefaultRaycastLayers;

        [Header("豆勺光标")]
        [Tooltip("用于显示豆桶悬停和手持豆子的鼠标豆勺")]
        [SerializeField] private BeanScoopCursorPresenter _beanScoopCursor;

        private WorldCoffeeWorkCup _draggingCup;
        private WorldBeanBarrelInteractable _hoveredBeanBarrel;
        [SerializeField] private LiquidAddPanelController _liquidAddPanel;

        private void Awake()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_liquidAddPanel == null)
            {
                _liquidAddPanel = FindObjectOfType<LiquidAddPanelController>();
            }
        }

        private void Update()
        {
            if (_liquidAddPanel != null && _liquidAddPanel.IsOpen)
            {
                SetHoveredBeanBarrel(null);
                _liquidAddPanel.HandleWorldInput(_targetCamera, _interactionMask);
                return;
            }

            if (_draggingCup != null)
            {
                SetHoveredBeanBarrel(null);
                if (Input.GetMouseButton(0))
                {
                    _draggingCup.Drag();
                }

                if (Input.GetMouseButtonUp(0))
                {
                    _draggingCup.EndDrag();
                    _draggingCup = null;
                }

                return;
            }

            RefreshBeanBarrelHover();

            if (!Input.GetMouseButtonDown(0) || _targetCamera == null)
            {
                return;
            }

            Ray screenRay = _targetCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(screenRay, out RaycastHit hit, Mathf.Infinity, _interactionMask))
            {
                return;
            }

            WorldLiquidCardInteractable liquidCard = hit.collider.GetComponentInParent<WorldLiquidCardInteractable>();
            if (liquidCard != null)
            {
                liquidCard.TryOpenPanel();
                return;
            }

            WorldCoffeeWorkCup workCup = hit.collider.GetComponentInParent<WorldCoffeeWorkCup>();
            if (workCup != null)
            {
                if (workCup.BeginDrag())
                {
                    _draggingCup = workCup;
                }

                return;
            }

            WorldToppingInteractable topping = hit.collider.GetComponentInParent<WorldToppingInteractable>();
            if (topping != null)
            {
                topping.AddTopping();
                return;
            }

            WorldCupInteractable cup = hit.collider.GetComponentInParent<WorldCupInteractable>();
            if (cup != null)
            {
                cup.Select();
                return;
            }

            WorldBeanBarrelInteractable beanBarrel = hit.collider.GetComponentInParent<WorldBeanBarrelInteractable>();
            if (beanBarrel != null)
            {
                if (beanBarrel.TakeBeans())
                {
                    _beanScoopCursor?.PlayScoopAnimation();
                }

                return;
            }

            WorldCoffeeGrinderBeanInputInteractable grinderBeanInput =
                hit.collider.GetComponentInParent<WorldCoffeeGrinderBeanInputInteractable>();
            if (grinderBeanInput != null)
            {
                if (grinderBeanInput.LoadBeans())
                {
                    _beanScoopCursor?.PlayEmptyScoopAfterLoadAnimation();
                }

                return;
            }

            WorldCoffeeGrinderHandleInteractable grinderHandle =
                hit.collider.GetComponentInParent<WorldCoffeeGrinderHandleInteractable>();
            if (grinderHandle != null)
            {
                grinderHandle.GrindBeans();
                return;
            }

            WorldCoffeeExtractorInteractable extractor =
                hit.collider.GetComponentInParent<WorldCoffeeExtractorInteractable>();
            if (extractor != null)
            {
                extractor.StartExtraction();
                return;
            }

            WorldCoffeeServeInteractable serve =
                hit.collider.GetComponentInParent<WorldCoffeeServeInteractable>();
            if (serve != null)
            {
                serve.ServeCoffee();
                return;
            }

            WorldCoffeeTrashBinInteractable trashBin =
                hit.collider.GetComponentInParent<WorldCoffeeTrashBinInteractable>();
            if (trashBin != null)
            {
                trashBin.DiscardHeldBeans();
                return;
            }

            InnsmouthCafe.Business.WorldCustomerOrderInteractable orderAction =
                hit.collider.GetComponentInParent<InnsmouthCafe.Business.WorldCustomerOrderInteractable>();
            orderAction?.Interact();
        }

        private void OnDisable()
        {
            SetHoveredBeanBarrel(null);
        }

        /// <summary>
        /// 检测鼠标是否悬停在可取豆的场景豆桶上。
        /// </summary>
        private void RefreshBeanBarrelHover()
        {
            if (_targetCamera == null)
            {
                SetHoveredBeanBarrel(null);
                return;
            }

            Ray screenRay = _targetCamera.ScreenPointToRay(Input.mousePosition);
            WorldBeanBarrelInteractable beanBarrel = null;
            if (Physics.Raycast(screenRay, out RaycastHit hit, Mathf.Infinity, _interactionMask))
            {
                beanBarrel = hit.collider.GetComponentInParent<WorldBeanBarrelInteractable>();
            }

            SetHoveredBeanBarrel(beanBarrel);
        }

        /// <summary>
        /// 更新当前悬停豆桶，并转发给豆勺显示组件。
        /// </summary>
        private void SetHoveredBeanBarrel(WorldBeanBarrelInteractable beanBarrel)
        {
            if (_hoveredBeanBarrel == beanBarrel)
            {
                return;
            }

            _hoveredBeanBarrel = beanBarrel;
            if (_beanScoopCursor != null)
            {
                _beanScoopCursor.SetHoveredBeanBarrel(_hoveredBeanBarrel);
            }
        }
    }
}
