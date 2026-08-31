using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 经营场景中的辅助液牌子，支持拖拽到工作杯附近打开空间加液面板。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldLiquidCardInteractable : MonoBehaviour
    {
        [Header("辅助液配置")]
        [SerializeField] private LiquidSO _liquid;

        [Header("拖拽配置")]
        [SerializeField] private float _activationRadius = 1.2f;
        [SerializeField] private LiquidAddPanelController _panelController;

        private Camera _camera;
        private Plane _dragPlane;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _dragging;

        /// <summary>
        /// 当前牌子绑定的辅助液配置。
        /// </summary>
        public LiquidSO Liquid => _liquid;

        /// <summary>
        /// 开始拖拽辅助液牌子。
        /// </summary>
        public bool BeginDrag(Camera targetCamera)
        {
            if (_liquid == null || _panelController == null || targetCamera == null)
            {
                return false;
            }

            if (NewCoffeeCraftManager.Instance == null
                || !NewCoffeeCraftManager.Instance.CanAddLiquid()
                || _panelController.IsOpen)
            {
                return false;
            }

            _camera = targetCamera;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            _dragPlane = new Plane(-_camera.transform.forward, transform.position);
            _dragging = true;
            return true;
        }

        /// <summary>
        /// 按鼠标位置更新辅助液牌子。
        /// </summary>
        public void Drag()
        {
            if (!_dragging || _camera == null)
            {
                return;
            }

            Ray pointerRay = _camera.ScreenPointToRay(Input.mousePosition);
            if (_dragPlane.Raycast(pointerRay, out float distance))
            {
                transform.position = pointerRay.GetPoint(distance);
            }
        }

        /// <summary>
        /// 结束拖拽，命中工作杯时打开空间加液面板，否则返回原位。
        /// </summary>
        public void EndDrag()
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            WorldCoffeeWorkCup targetCup = FindTargetCup();
            if (targetCup != null && _panelController.Open(_liquid, targetCup))
            {
                transform.SetPositionAndRotation(_startPosition, _startRotation);
                return;
            }

            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }

        /// <summary>
        /// 查找距离牌子最近且在激活范围内的工作杯。
        /// </summary>
        private WorldCoffeeWorkCup FindTargetCup()
        {
            WorldCoffeeWorkCup nearestCup = null;
            float nearestDistance = float.MaxValue;
            foreach (WorldCoffeeWorkCup cup in FindObjectsOfType<WorldCoffeeWorkCup>())
            {
                if (cup == null || !cup.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, cup.transform.position);
                if (distance <= Mathf.Max(0.01f, _activationRadius) && distance < nearestDistance)
                {
                    nearestCup = cup;
                    nearestDistance = distance;
                }
            }

            return nearestCup;
        }
    }
}
