using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 管理场景工作杯的取用、回收及托盘初始放置。
    /// </summary>
    public class CoffeeWorkCupController : MonoBehaviour
    {
        [Header("工作杯配置")]
        [SerializeField] private WorldCoffeeWorkCup _workCup;
        [SerializeField] private CoffeeCupAnchor _trayCupAnchor;
        [SerializeField] private CoffeeCupAnchor _extractorCupAnchor;

        private GameObject _sourceCup;

        private void Awake()
        {
            if (_workCup != null)
            {
                _workCup.gameObject.SetActive(false);
                _workCup.SetInteractionLocked(false);
            }
        }

        private void OnEnable()
        {
            if (NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCraftReset += ReturnCup;
                NewCoffeeCraftManager.Instance.OnCoffeeDataChanged += HandleCoffeeDataChanged;
                NewCoffeeCraftManager.Instance.OnExtractionStateChanged += HandleExtractionStateChanged;
            }
        }

        private void OnDisable()
        {
            if (NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCraftReset -= ReturnCup;
                NewCoffeeCraftManager.Instance.OnCoffeeDataChanged -= HandleCoffeeDataChanged;
                NewCoffeeCraftManager.Instance.OnExtractionStateChanged -= HandleExtractionStateChanged;
            }
        }

        /// <summary>
        /// 将选中的杯架杯子替换为托盘中的实体工作杯。
        /// </summary>
        public void TakeCup(CupContainerSO cup, GameObject sourceCup)
        {
            if (_workCup == null || _trayCupAnchor == null || cup == null)
            {
                Debug.LogWarning("[CoffeeWorkCup] 未配置工作杯、托盘锚点或杯子数据", this);
                return;
            }

            ReturnCup();
            _sourceCup = sourceCup;
            if (_sourceCup != null)
            {
                _sourceCup.SetActive(false);
            }

            _workCup.SetCup(cup.ToData(), _trayCupAnchor);
        }

        /// <summary>
        /// 隐藏工作杯并恢复被取用的杯架杯子。
        /// </summary>
        public void ReturnCup()
        {
            if (_workCup != null)
            {
                _workCup.SetInteractionLocked(false);
                _workCup.gameObject.SetActive(false);
            }

            if (_sourceCup != null)
            {
                _sourceCup.SetActive(true);
                _sourceCup = null;
            }
        }

        /// <summary>
        /// 判断当前工作杯是否吸附在指定制作工位。
        /// </summary>
        public bool IsWorkCupAtAnchor(CoffeeCupAnchor anchor)
        {
            return _workCup != null
                && _workCup.gameObject.activeInHierarchy
                && _workCup.IsAtAnchor(anchor);
        }

        /// <summary>
        /// 判断当前工作杯是否已放入萃取机工位。
        /// </summary>
        public bool IsWorkCupAtExtractor()
        {
            return IsWorkCupAtAnchor(_extractorCupAnchor);
        }

        /// <summary>
        /// 根据制作数据更新工作杯的咖啡液填充贴图。
        /// </summary>
        private void HandleCoffeeDataChanged(CoffeeData coffeeData)
        {
            if (_workCup == null || coffeeData?.selectedCup == null)
            {
                return;
            }

            float capacity = coffeeData.selectedCup.capacity;
            float fillRatio = capacity > 0f ? coffeeData.currentTotalVolume / capacity : 0f;
            _workCup.SetFillRatio(fillRatio);
        }

        /// <summary>
        /// 萃取进行中锁定工作杯，结束后恢复拖拽。
        /// </summary>
        private void HandleExtractionStateChanged(bool isExtracting)
        {
            _workCup?.SetInteractionLocked(isExtracting);
        }
    }
}
