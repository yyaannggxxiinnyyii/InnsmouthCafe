using UnityEngine;
using InnsmouthCafe.Data;
using System.Collections.Generic;

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
        [SerializeField] private Collider _toppingExitCollider;

        [Tooltip("场景中的实体工作杯，用于在萃取阶段获取当前杯型侧剖面")]
        [SerializeField] private WorldCoffeeWorkCup _workCup;

        [Header("咖啡粒子")]
        [Tooltip("将已萃取咖啡转换为粒子时使用的颜色")]
        [SerializeField] private Color _coffeeParticleColor = new Color(0.18f, 0.07f, 0.02f, 1f);

        private LiquidSO _currentLiquid;
        private WorldCoffeeWorkCup _currentCup;
        private WorldLiquidCardInteractable _hiddenLiquidObject;
        private bool _isOpen;
        private bool _isExtractingCoffee;
        private Transform _coffeeParticleOutlet;
        private int _generatedCoffeeParticleCount;
        private float _sessionStartVolumeMilliliters;
        private readonly Dictionary<int, ToppingSO> _toppingsByUserData = new Dictionary<int, ToppingSO>();
        private readonly Dictionary<LPParticle, ToppingSO> _toppingParticles = new Dictionary<LPParticle, ToppingSO>();
        private readonly HashSet<LPParticle> _committedToppingParticles = new HashSet<LPParticle>();

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

            if (_workCup == null)
            {
                _workCup = FindObjectOfType<WorldCoffeeWorkCup>();
            }

            // 只关闭面板显示内容，物理系统和容量组件位于外层并保持激活。
            _isOpen = false;
            SetPanelContentActive(false);
            _toppingExitCollider?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCraftReset += HandleCraftReset;
                NewCoffeeCraftManager.Instance.OnExtractionStateChanged += HandleExtractionStateChanged;
                NewCoffeeCraftManager.Instance.OnExtractionProgressChanged += HandleExtractionProgressChanged;
            }
        }

        private void OnDisable()
        {
            if (NewCoffeeCraftManager.Instance != null)
            {
                NewCoffeeCraftManager.Instance.OnCraftReset -= HandleCraftReset;
                NewCoffeeCraftManager.Instance.OnExtractionStateChanged -= HandleExtractionStateChanged;
                NewCoffeeCraftManager.Instance.OnExtractionProgressChanged -= HandleExtractionProgressChanged;
            }
        }

        /// <summary>
        /// 当前是否已经打开加液面板。
        /// </summary>
        public bool IsOpen => _isOpen;

        /// <summary>在已打开的侧剖面中切换当前辅助液，并提交上一种辅助液。</summary>
        public bool SwitchLiquid(LiquidSO liquid, WorldLiquidCardInteractable sourceObject)
        {
            if (!_isOpen || liquid == null || _currentCup == null)
            {
                return false;
            }

            CommitCurrentLiquidOnly();
            _currentLiquid = liquid;
            _hiddenLiquidObject = sourceObject;
            ApplyLiquidColor(liquid);
            _pouringController?.SetLiquidIcon(liquid.icon);
            _pouringController?.SetLiquidAddActive(true);
            _pouringController?.SetParticleRendererActive(true);
            if (_toppingExitCollider != null)
            {
                _toppingExitCollider.gameObject.SetActive(true);
            }
            sourceObject?.gameObject.SetActive(false);
            return true;
        }

        /// <summary>进入小料拖拽模式并显示杯子侧剖面。</summary>
        public bool BeginToppingMode(WorldToppingInteractable source)
        {
            if (source == null || NewCoffeeCraftManager.Instance == null
                || !NewCoffeeCraftManager.Instance.CanAddLiquid())
            {
                return false;
            }

            if (!_isOpen)
            {
                _currentCup = _workCup;
                if (_currentCup == null || ! _currentCup.ActivateCupProfile(NewCoffeeCraftManager.Instance.SelectedCup))
                {
                    return false;
                }
                _isOpen = true;
                SetPanelContentActive(true);
                _pouringController?.SetLiquidAddActive(false);
            }

            _pouringController?.SetParticleRendererActive(true);
            _toppingsByUserData[source.UserData] = source.Topping;
            return true;
        }

        /// <summary>在指定位置生成一个白色小料粒子并写入类型编号。</summary>
        public void SpawnToppingParticle(ToppingSO topping, Vector2 position,
            LPParticleMaterial particleMaterial, LPParticleGroupMaterial groupMaterial, int userData)
        {
            if (!_isOpen || topping == null || _pouringController == null
                || _pouringController.LiquidParticleSystem == null)
            {
                return;
            }

            LPParticleSystem system = _pouringController.LiquidParticleSystem;
            int particleFlags = particleMaterial != null ? particleMaterial.GetInt() : 0;
            LPAPIParticles.CreateParticleInSystem(
                system.GetPtr(), particleFlags, position.x, position.y, 0f, -0.5f,
                255, 255, 255, 255, 99999f);

            // 原生创建接口不接收 UserData，在创建后定位出口附近的新粒子并补写类型编号。
            system.UpdateData();
            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;
            if (system.Particles != null)
            {
                for (int index = 0; index < system.Particles.Count; index++)
                {
                    LPParticle particle = system.Particles[index];
                    float distance = Vector2.Distance(particle.Position, position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestIndex = index;
                    }
                }
            }

            if (nearestIndex >= 0)
            {
                int[] selectedIndices = { 1, nearestIndex };
                LPAPIParticles.SetSelectedParticleUserData(
                    system.GetPtr(), selectedIndices, userData);
                system.UpdateData();

                if (nearestIndex < system.Particles.Count)
                {
                    _toppingParticles[system.Particles[nearestIndex]] = topping;
                }
            }

            _toppingsByUserData[userData] = topping;
            Debug.Log($"[LiquidAddPanel] 生成小料粒子：{topping.toppingName}，位置={position}，UserData={userData}", this);
        }

        /// <summary>在当前杯型配置的萃取出口点生成一个小料粒子。</summary>
        public void SpawnToppingParticleAtOutlet(ToppingSO topping,
            LPParticleMaterial particleMaterial, LPParticleGroupMaterial groupMaterial, int userData)
        {
            if (_currentCup == null || NewCoffeeCraftManager.Instance == null)
            {
                return;
            }

            Transform outlet = _currentCup.GetCupExtractionOutlet(NewCoffeeCraftManager.Instance.SelectedCup);
            if (outlet == null)
            {
                Debug.LogWarning("[LiquidAddPanel] 当前杯型未配置萃取出口点，无法生成小料粒子。", this);
                return;
            }

            SpawnToppingParticle(topping, outlet.position, particleMaterial, groupMaterial, userData);
        }

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
            _generatedCoffeeParticleCount = 0;
            _coffeeParticleOutlet = null;
            _committedToppingParticles.Clear();
            _toppingParticles.Clear();
            Close();
        }

        /// <summary>
        /// 打开面板并绑定本次添加的辅助液和工作杯。
        /// </summary>
        public bool Open(LiquidSO liquid, WorldCoffeeWorkCup workCup, WorldLiquidCardInteractable sourceObject = null)
        {
            if (liquid == null || workCup == null || _panelContentRoot == null
                || NewCoffeeCraftManager.Instance == null
                || !NewCoffeeCraftManager.Instance.CanAddLiquid() || IsOpen)
            {
                return false;
            }

            _currentLiquid = liquid;
            _currentCup = workCup;
            _hiddenLiquidObject = sourceObject;
            CupContainerData selectedCup = NewCoffeeCraftManager.Instance.SelectedCup;
            if (!_currentCup.ActivateCupProfile(selectedCup))
            {
                _currentLiquid = null;
                _currentCup = null;
                _hiddenLiquidObject = null;
                return false;
            }

            ApplyLiquidColor(liquid);

            _pouringController?.SetLiquidIcon(liquid.icon);

            _isOpen = true;
            SetPanelContentActive(true);
            _toppingExitCollider?.gameObject.SetActive(true);
            _pouringController?.SetLiquidAddActive(true);
            _pouringController?.SetParticleRendererActive(true);
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
            _pouringController?.StopPouringForPanel();
            _pouringController?.SetBottleActive(false);
            _pouringController?.SetParticleRendererActive(false);
            _pouringController?.SetLiquidIcon(null);
            _currentCup?.SetCupProfileVisualActive(false);
            if (_hiddenLiquidObject != null)
            {
                _hiddenLiquidObject.gameObject.SetActive(true);
            }
            _sessionStartVolumeMilliliters = 0f;
            _currentLiquid = null;
            _currentCup = null;
            _hiddenLiquidObject = null;
            _toppingExitCollider?.gameObject.SetActive(false);
        }

        /// <summary>提交当前辅助液但保持侧剖面打开。</summary>
        private void CommitCurrentLiquidOnly()
        {
            if (_currentLiquid == null || _currentCup == null || _liquidVolume == null)
            {
                return;
            }

            _liquidVolume.RefreshMeasurement();
            float amountMl = Mathf.Max(0f, CurrentVolumeMilliliters - _sessionStartVolumeMilliliters);
            if (amountMl > 0.01f)
            {
                NewCoffeeCraftManager.Instance?.TryCommitLiquid(_currentLiquid, amountMl);
            }

            _hiddenLiquidObject?.gameObject.SetActive(true);
            _currentLiquid = null;
            _hiddenLiquidObject = null;
            _sessionStartVolumeMilliliters = CurrentVolumeMilliliters;
            _pouringController?.SetLiquidAddActive(false);
            _pouringController?.SetLiquidIcon(null);
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

            string exitColliderName = _toppingExitCollider != null ? _toppingExitCollider.name : "null";
            Debug.Log($"[LiquidAddPanel] 侧剖面点击：退出碰撞体={exitColliderName}", this);

            Ray screenRay = targetCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits3D = Physics.RaycastAll(screenRay, Mathf.Infinity, interactionMask);
            System.Array.Sort(hits3D, (first, second) => first.distance.CompareTo(second.distance));

            // 退出区域可能与杯型或其他场景碰撞体重叠，优先处理它，避免被其他命中拦截。
            foreach (RaycastHit hit in hits3D)
            {
                if (IsExitCollider(hit.collider))
                {
                    Debug.Log("[LiquidAddPanel] 命中侧剖面退出碰撞体，关闭面板。", this);
                    CommitToppingsAndClose();
                    return;
                }
            }

            foreach (RaycastHit hit in hits3D)
            {
                WorldToppingInteractable topping = hit.collider.GetComponentInParent<WorldToppingInteractable>();
                if (topping != null)
                {
                    topping.AddTopping();
                    return;
                }
                WorldLiquidCardInteractable liquidCard = hit.collider.GetComponentInParent<WorldLiquidCardInteractable>();
                if (liquidCard != null)
                {
                    liquidCard.TrySelectLiquid();
                    return;
                }
                LiquidPouringPrototypeController bottle =
                    hit.collider.GetComponentInParent<LiquidPouringPrototypeController>();
                if (bottle != null)
                {
                    FinishBottleOnly();
                    return;
                }
            }

            RaycastHit[] fallbackHits = Physics.RaycastAll(screenRay, Mathf.Infinity, ~0);
            foreach (RaycastHit fallbackHit in fallbackHits)
            {
                if (IsExitCollider(fallbackHit.collider))
                {
                    CommitToppingsAndClose();
                    return;
                }
            }

            RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(screenRay, Mathf.Infinity, interactionMask);
            System.Array.Sort(hits2D, (first, second) => first.distance.CompareTo(second.distance));
            foreach (RaycastHit2D hit2D in hits2D)
            {
                if (_pouringController != null && hit2D.collider == _pouringController.BottleInputCollider)
                {
                    FinishBottleOnly();
                    return;
                }
            }
        }

        /// <summary>提交当前辅助液并隐藏倾倒瓶，但保持侧剖面打开。</summary>
        private void FinishBottleOnly()
        {
            CommitCurrentLiquidOnly();
        }

        /// <summary>记录杯内仍存在的小料粒子并关闭侧剖面。</summary>
        private void CommitToppingsAndClose()
        {
            Debug.Log("[LiquidAddPanel] 开始扫描杯内小料粒子。", this);
            CommitCurrentLiquidOnly();
            if (_liquidParticleSystemForTopping() != null && _liquidVolume != null
                && NewCoffeeCraftManager.Instance != null)
            {
                _liquidParticleSystemForTopping().UpdateData();
                int insideCount = 0;
                foreach (LPParticle particle in _liquidParticleSystemForTopping().Particles)
                {
                    if (!_liquidVolume.IsInsideCup(particle.Position))
                    {
                        continue;
                    }

                    insideCount++;

                    // UserData 为 0 的粒子属于咖啡液或辅助液，不参与小料识别。
                    if (particle.UserData == 0)
                    {
                        continue;
                    }

                    ToppingSO topping = null;
                    if (!_toppingParticles.TryGetValue(particle, out topping))
                    {
                        _toppingsByUserData.TryGetValue(particle.UserData, out topping);
                    }

                    if (topping == null)
                    {
                        Debug.LogWarning($"[LiquidAddPanel] 杯内粒子未找到对应小料：UserData={particle.UserData}，位置={particle.Position}", this);
                        continue;
                    }

                    if (_committedToppingParticles.Add(particle))
                    {
                        bool committed = NewCoffeeCraftManager.Instance.TryCommitTopping(topping);
                        Debug.Log($"[LiquidAddPanel] 提交小料：{topping.toppingName}，结果={committed}", this);
                    }
                }
                Debug.Log($"[LiquidAddPanel] 杯内粒子扫描完成：总数={_liquidParticleSystemForTopping().Particles.Count}，杯内数={insideCount}", this);
            }
            else
            {
                Debug.LogWarning("[LiquidAddPanel] 无法扫描小料：粒子系统或杯内区域为空。", this);
            }
            Close();
        }

        private LPParticleSystem _liquidParticleSystemForTopping()
        {
            return _pouringController != null ? _pouringController.LiquidParticleSystem : null;
        }

        private bool IsExitCollider(Collider collider)
        {
            if (_toppingExitCollider == null || collider == null)
            {
                return false;
            }

            return collider == _toppingExitCollider
                || collider.transform == _toppingExitCollider.transform
                || collider.transform.IsChildOf(_toppingExitCollider.transform);
        }

        private void ApplyLiquidColor(LiquidSO liquid)
        {
            if (liquid == null)
            {
                return;
            }

            if (_particleGroup != null)
            {
                _particleGroup._Color = liquid.displayColor;
            }

            _pouringController?.SetLiquidColor(liquid.displayColor);
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

        /// <summary>萃取开始时初始化固定杯型侧剖面，但隐藏所有加液视觉。</summary>
        private void HandleExtractionStateChanged(bool isExtracting)
        {
            _isExtractingCoffee = isExtracting;
            if (!isExtracting)
            {
                _coffeeParticleOutlet = null;
                _workCup?.SetCupProfileVisualActive(false);
                return;
            }

            NewCoffeeCraftManager manager = NewCoffeeCraftManager.Instance;
            if (manager?.SelectedCup == null || _workCup == null
                || !_workCup.ActivateCupProfile(manager.SelectedCup))
            {
                _isExtractingCoffee = false;
                return;
            }

            _coffeeParticleOutlet = _workCup.GetCupExtractionOutlet(manager.SelectedCup);
            _generatedCoffeeParticleCount = 0;
            _workCup.SetCupProfileVisualActive(false);
            _pouringController?.SetLiquidAddActive(false);
            SetPanelContentActive(false);
        }

        /// <summary>根据萃取进度逐粒补齐咖啡粒子，生成过程保持视觉隐藏。</summary>
        private void HandleExtractionProgressChanged(float currentVolume, float targetVolume)
        {
            if (!_isExtractingCoffee || _coffeeParticleOutlet == null || _liquidVolume == null)
            {
                return;
            }

            int targetParticleCount = Mathf.FloorToInt(
                currentVolume / Mathf.Max(0.0001f, _liquidVolume.SingleParticleCapacityMilliliters));
            int particleCount = targetParticleCount - _generatedCoffeeParticleCount;
            if (particleCount <= 0)
            {
                return;
            }

            int particleFlags = _particleGroup != null && _particleGroup.ParticlesMaterial != null
                ? _particleGroup.ParticlesMaterial.GetInt()
                : 0;
            int created = _liquidVolume.CreateCoffeeParticles(
                particleCount,
                _coffeeParticleOutlet,
                _coffeeParticleColor,
                particleFlags);
            _generatedCoffeeParticleCount += created;
        }

        /// <summary>制作流程重置时允许下一杯咖啡重新生成咖啡粒子。</summary>
        private void HandleCraftReset()
        {
            _isExtractingCoffee = false;
            _generatedCoffeeParticleCount = 0;
            _coffeeParticleOutlet = null;
            _committedToppingParticles.Clear();
            _toppingParticles.Clear();
        }
    }
}
