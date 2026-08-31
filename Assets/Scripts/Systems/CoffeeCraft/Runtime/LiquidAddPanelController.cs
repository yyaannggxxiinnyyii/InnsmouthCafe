using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 控制世界空间加辅助液区域，负责传入当前液体和工作杯并管理面板显示状态。
    /// </summary>
    public class LiquidAddPanelController : MonoBehaviour
    {
        [Header("空间面板")]
        [SerializeField] private GameObject _panelContentRoot;
        [SerializeField] private LiquidPouringPrototypeController _pouringController;
        [SerializeField] private CoffeeCupLiquidVolume _liquidVolume;
        [SerializeField] private LPParticleGroup _particleGroup;

        private LiquidSO _currentLiquid;
        private WorldCoffeeWorkCup _currentCup;
        private bool _isOpen;
        private float _sessionStartVolumeMilliliters;

        private void Awake()
        {
            if (_liquidVolume == null)
            {
                _liquidVolume = GetComponentInChildren<CoffeeCupLiquidVolume>(true);
            }

            if (_liquidVolume != null && _pouringController != null)
            {
                _liquidVolume.SetParticleSystem(_pouringController.LiquidParticleSystem);
            }

            // 只关闭面板显示内容，物理系统和容量组件位于外层并保持激活。
            _isOpen = false;
            SetPanelContentActive(false);
        }

        /// <summary>
        /// 当前是否已经打开加液面板。
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>
        /// 当前面板正在添加的辅助液。
        /// </summary>
        public LiquidSO CurrentLiquid => _currentLiquid;

        /// <summary>
        /// 当前空间面板中检测到的液体容量。
        /// </summary>
        public float CurrentVolumeMilliliters => _liquidVolume != null
            ? Mathf.Max(0f, _liquidVolume.CurrentMilliliters)
            : 0f;

        /// <summary>
        /// 当前面板是否已经绑定杯内液体计量组件。
        /// </summary>
        public bool HasLiquidVolume => _liquidVolume != null;

        /// <summary>
        /// 清理本次加液使用的物理粒子并关闭面板。
        /// </summary>
        public void ResetForDiscard()
        {
            _liquidVolume?.ClearParticles();
            _pouringController?.ResetForDiscard();
            _currentCup?.DeactivateSelectedCupProfile();
            Close();
        }

        /// <summary>
        /// 打开面板并绑定本次添加的辅助液和工作杯。
        /// </summary>
        public bool Open(LiquidSO liquid, WorldCoffeeWorkCup workCup)
        {
            if (liquid == null || workCup == null || _panelContentRoot == null
                || NewCoffeeCraftManager.Instance == null
                || !NewCoffeeCraftManager.Instance.CanAddLiquid() || IsOpen)
            {
                return false;
            }

            _currentLiquid = liquid;
            _currentCup = workCup;
            CupContainerData selectedCup = NewCoffeeCraftManager.Instance.SelectedCup;
            if (!_currentCup.ActivateCupProfile(selectedCup))
            {
                _currentLiquid = null;
                _currentCup = null;
                return false;
            }
            if (_particleGroup != null)
            {
                _particleGroup._Color = liquid.displayColor;
            }

            _isOpen = true;
            SetPanelContentActive(true);
            _pouringController?.SetLiquidAddActive(true);
            _liquidVolume?.RefreshMeasurement();
            _sessionStartVolumeMilliliters = CurrentVolumeMilliliters;

            Debug.Log($"[LiquidAddPanel] 打开加液面板：{liquid.liquidName}");
            return true;
        }

        /// <summary>
        /// 关闭当前加液面板并清除本次面板绑定。
        /// </summary>
        public void Close()
        {
            _isOpen = false;
            SetPanelContentActive(false);
            _pouringController?.SetLiquidAddActive(false);
            _currentCup?.SetCupProfileVisualActive(false);
            _sessionStartVolumeMilliliters = 0f;
            _currentLiquid = null;
            _currentCup = null;
        }

        /// <summary>
        /// 提交当前物理杯内检测到的辅助液容量并关闭面板。
        /// </summary>
        public bool CommitAndClose()
        {
            if (!IsOpen || _currentLiquid == null || _currentCup == null || _liquidVolume == null)
            {
                return false;
            }

            _liquidVolume.RefreshMeasurement();
            float amountMl = Mathf.Max(0f, CurrentVolumeMilliliters - _sessionStartVolumeMilliliters);
            if (amountMl <= 0.01f)
            {
                Close();
                return true;
            }

            bool committed = NewCoffeeCraftManager.Instance != null
                && NewCoffeeCraftManager.Instance.TryCommitLiquid(_currentLiquid, amountMl);
            if (committed)
            {
                Close();
            }

            return committed;
        }

        /// <summary>
        /// 处理加液面板中完成按钮的世界射线点击。
        /// </summary>
        /// <param name="targetCamera">用于检测鼠标点击的摄像机。</param>
        /// <param name="interactionMask">完成按钮所在的交互层。</param>
        public void HandleWorldInput(Camera targetCamera, LayerMask interactionMask)
        {
            if (!_isOpen || targetCamera == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            Ray screenRay = targetCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits3D = Physics.RaycastAll(screenRay, Mathf.Infinity, interactionMask);
            System.Array.Sort(hits3D, (first, second) => first.distance.CompareTo(second.distance));
            foreach (RaycastHit hit in hits3D)
            {
                WorldLiquidPanelFinishInteractable finish =
                    hit.collider.GetComponentInParent<WorldLiquidPanelFinishInteractable>();
                if (finish != null)
                {
                    finish.Finish();
                    return;
                }
            }

            RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(screenRay, Mathf.Infinity, interactionMask);
            System.Array.Sort(hits2D, (first, second) => first.distance.CompareTo(second.distance));
            foreach (RaycastHit2D hit2D in hits2D)
            {
                WorldLiquidPanelFinishInteractable finish2D =
                    hit2D.collider.GetComponentInParent<WorldLiquidPanelFinishInteractable>();
                if (finish2D != null)
                {
                    finish2D.Finish();
                    return;
                }
            }
        }

        /// <summary>
        /// 切换面板视觉内容的激活状态，不影响外层 LiquidFun 物理系统。
        /// </summary>
        private void SetPanelContentActive(bool isActive)
        {
            if (_panelContentRoot != null && _panelContentRoot.activeSelf != isActive)
            {
                _panelContentRoot.SetActive(isActive);
            }
        }
    }
}
